using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using TechStore.API.Data;
using TechStore.API.DTOs.Order;
using TechStore.API.Models;
using TechStore.API.Repositories.Interfaces;
using TechStore.API.Services.Interfaces;
using TechStore.API.DTOs.Coupon;

namespace TechStore.API.Services;

public class OrderService : IOrderService
{
    private readonly IOrderRepository _orders;
    private readonly ICartRepository _carts;
    private readonly IInventoryRepository _inventory;
    private readonly IUserRepository _users;
    private readonly ICouponRepository _coupons;
    private readonly AppDbContext _db;
    private readonly IHttpContextAccessor _http;
    private readonly IEmailService _email;

    public OrderService(
        IOrderRepository orders,
        ICartRepository carts,
        IInventoryRepository inventory,
        IUserRepository users,
        ICouponRepository coupons,
        AppDbContext db,
        IHttpContextAccessor http,
        IEmailService email)
    {
        _orders    = orders;
        _carts     = carts;
        _inventory = inventory;
        _users     = users;
        _coupons   = coupons;
        _db        = db;
        _http      = http;
        _email     = email;
    }

    public async Task<OrderDto?> GetByIdAsync(int id)
    {
        var order = await _orders.GetByIdAsync(id);
        return order == null ? null : MapToDto(order);
    }

    public async Task<IEnumerable<OrderSummaryDto>> GetMyOrdersAsync(int userId, int page, int pageSize)
    {
        var orders = await _orders.GetByUserIdAsync(userId, page, pageSize);
        return orders.Select(MapToSummaryDto);
    }

    public async Task<PagedResult<OrderSummaryDto>> GetAllOrdersAsync(int page, int pageSize, string? status)
    {
        var (items, total) = await _orders.GetAllAsync(page, pageSize, status);
        return new PagedResult<OrderSummaryDto>(items.Select(MapToSummaryDto), total, page, pageSize);
    }

    public async Task<OrderDto> CreateFromCartAsync(int userId, CreateOrderRequest req)
    {
        var user = await _users.GetByIdAsync(userId)
            ?? throw new KeyNotFoundException("User not found.");

        var cart = await _carts.GetByUserIdAsync(userId);
        if (cart == null || !cart.Items.Any())
            throw new InvalidOperationException("Cart is empty.");

        foreach (var item in cart.Items)
        {
            var inv = await _inventory.GetByProductIdAsync(item.ProductId);
            if (inv == null || inv.QuantityAvailable < item.Quantity)
                throw new InvalidOperationException(
                    $"Insufficient stock for '{item.Product?.Name}'. Available: {inv?.QuantityAvailable ?? 0}");
        }

        var orderItems = cart.Items.Select(i => new OrderItem
        {
            ProductId   = i.ProductId,
            ProductName = i.Product?.Name ?? "",
            ProductSKU  = i.Product?.SKU,
            UnitPrice   = i.Product?.Price ?? 0,
            Quantity    = i.Quantity,
            LineTotal   = (i.Product?.Price ?? 0) * i.Quantity
        }).ToList();

        var subTotal = orderItems.Sum(i => i.LineTotal);

        // Validate and apply coupon if provided
        decimal discount = 0;
        Coupon? appliedCoupon = null;
        if (!string.IsNullOrWhiteSpace(req.CouponCode))
        {
            appliedCoupon = await _coupons.GetByCodeAsync(req.CouponCode.ToUpper());
            if (appliedCoupon != null && appliedCoupon.IsActive &&
                (appliedCoupon.ValidFrom == null || appliedCoupon.ValidFrom <= DateTime.UtcNow) &&
                (appliedCoupon.ValidTo == null || appliedCoupon.ValidTo >= DateTime.UtcNow) &&
                (appliedCoupon.UsageLimit == null || appliedCoupon.UsedCount < appliedCoupon.UsageLimit) &&
                (appliedCoupon.MinimumOrderAmount == null || subTotal >= appliedCoupon.MinimumOrderAmount))
            {
                discount = appliedCoupon.Type == DiscountType.Percentage
                    ? Math.Round(subTotal * appliedCoupon.Value / 100, 2)
                    : appliedCoupon.Value;

                if (appliedCoupon.MaximumDiscountAmount.HasValue)
                    discount = Math.Min(discount, appliedCoupon.MaximumDiscountAmount.Value);
            }
        }

        var tax      = Math.Round((subTotal - discount) * 0.075m, 2);
        var shipping = subTotal >= 100 ? 0 : 9.99m;
        var total    = subTotal - discount + tax + shipping;

        var order = new Order
        {
            OrderNumber        = GenerateOrderNumber(),
            UserId             = userId,
            Status             = OrderStatus.Pending,
            PaymentStatus      = PaymentStatus.Pending,
            PaymentMethod      = req.PaymentMethod,
            TransactionId      = req.TransactionId,
            SubTotal           = subTotal,
            DiscountAmount     = discount,
            CouponCode         = appliedCoupon?.Code,
            Tax                = tax,
            ShippingCost       = shipping,
            Total              = total,
            ShippingFirstName  = req.ShippingFirstName,
            ShippingLastName   = req.ShippingLastName,
            ShippingAddress    = req.ShippingAddress,
            ShippingCity       = req.ShippingCity,
            ShippingProvince   = req.ShippingProvince,
            ShippingPostalCode = req.ShippingPostalCode,
            ShippingCountry    = req.ShippingCountry,
            ShippingPhone      = req.ShippingPhone,
            Notes              = req.Notes,
            Items              = orderItems
        };

        var created = await _orders.CreateAsync(order);

        // Write initial status log — placed by customer
        await WriteLogAsync(created.Id, null, OrderStatus.Pending, OrderStatus.Pending,
            "Order placed by customer", isSystem: true);

        // Reserve inventory
        foreach (var item in cart.Items)
        {
            var inv = await _inventory.GetByProductIdAsync(item.ProductId);
            if (inv != null)
            {
                inv.QuantityReserved += item.Quantity;
                await _inventory.UpdateAsync(inv);
            }
        }

        await _carts.ClearAsync(userId);
        // Increment coupon usage
        if (appliedCoupon != null)
            await _coupons.IncrementUsageAsync(appliedCoupon.Id);

        var result = MapToDto(await _orders.GetByIdAsync(created.Id) ?? created);
        _ = _email.SendOrderConfirmationAsync(user.Email, user.FirstName, result.OrderNumber, result.Total);
        return result;
    }

    public async Task<OrderDto?> UpdateStatusAsync(int id, UpdateOrderStatusRequest req)
    {
        var order = await _orders.GetByIdAsync(id);
        if (order == null) return null;

        var prevStatus = order.Status;

        // Don't log a no-op
        if (prevStatus == req.Status) return MapToDto(order);

        order.Status = req.Status;

        if (req.Status == OrderStatus.Shipped && order.ShippedAt == null)
            order.ShippedAt = DateTime.UtcNow;

        if (req.Status == OrderStatus.Delivered && order.DeliveredAt == null)
        {
            order.DeliveredAt     = DateTime.UtcNow;
            order.PaymentStatus   = PaymentStatus.Paid;
        }

        if (req.Status == OrderStatus.Cancelled && prevStatus != OrderStatus.Cancelled)
        {
            foreach (var item in order.Items)
            {
                var inv = await _inventory.GetByProductIdAsync(item.ProductId);
                if (inv != null)
                {
                    inv.QuantityReserved = Math.Max(0, inv.QuantityReserved - item.Quantity);
                    await _inventory.UpdateAsync(inv);
                }
            }
            order.PaymentStatus = PaymentStatus.Refunded;
        }

        if (req.Notes != null) order.Notes = req.Notes;

        await _orders.UpdateAsync(order);

        // Write audit log entry
        await WriteLogAsync(id, GetCurrentUserId(), prevStatus, req.Status, req.Notes);

        var dto = MapToDto(order);
        if (order.User?.Email != null)
            _ = _email.SendOrderStatusUpdateAsync(order.User.Email, order.User.FirstName ?? "", dto.OrderNumber, req.Status.ToString());

        return dto;
    }

    public async Task<IEnumerable<OrderStatusLogDto>> GetStatusHistoryAsync(int orderId)
    {
        var logs = await _db.OrderStatusLogs
            .Where(l => l.OrderId == orderId)
            .OrderBy(l => l.ChangedAt)
            .ToListAsync();

        return logs.Select(l => new OrderStatusLogDto(
            l.Id,
            l.FromStatus.ToString(),
            l.ToStatus.ToString(),
            l.ChangedByName,
            l.ChangedByEmail,
            l.Note,
            l.ChangedAt
        ));
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private async Task WriteLogAsync(
        int orderId,
        int? adminUserId,
        OrderStatus from,
        OrderStatus to,
        string? note,
        bool isSystem = false)
    {
        string name  = "System";
        string email = "system";

        if (!isSystem && adminUserId.HasValue)
        {
            var admin = await _users.GetByIdAsync(adminUserId.Value);
            if (admin != null)
            {
                name  = $"{admin.FirstName} {admin.LastName}".Trim();
                email = admin.Email;
            }
        }

        _db.OrderStatusLogs.Add(new OrderStatusLog
        {
            OrderId         = orderId,
            ChangedByUserId = adminUserId ?? 0,
            ChangedByName   = name,
            ChangedByEmail  = email,
            FromStatus      = from,
            ToStatus        = to,
            Note            = note,
            ChangedAt       = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
    }

    private int? GetCurrentUserId()
    {
        var claim = _http.HttpContext?.User?.FindFirstValue("userId")
                 ?? _http.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        return claim != null && int.TryParse(claim, out var id) ? id : null;
    }

    private static string GenerateOrderNumber() =>
        $"TS-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpper()}";

    private static string? BuildPaymentMethodDetails(Order o)
    {
        if (o.PaymentMethod == "Card" && o.TransactionId != null)
            return $"Card ending {o.TransactionId}";
        if (o.PaymentMethod == "BankTransfer" && o.TransactionId != null)
            return $"Transfer ref: {o.TransactionId}";
        return null;
    }

    private static OrderDto MapToDto(Order o) => new(
        o.Id, o.OrderNumber, o.UserId,
        $"{o.User?.FirstName} {o.User?.LastName}".Trim(),
        o.User?.Email ?? "",
        o.Status, o.PaymentStatus, o.PaymentMethod,
        BuildPaymentMethodDetails(o),
        o.SubTotal, o.DiscountAmount, o.CouponCode,
        o.Tax, o.ShippingCost, o.Total,
        o.ShippingAddress, o.ShippingCity, o.ShippingProvince,
        o.ShippingPostalCode, o.ShippingCountry,
        o.Notes, o.ShippedAt, o.DeliveredAt, o.CreatedAt,
        o.Items.Select(i => new OrderItemDto(
            i.Id, i.ProductId, i.ProductName, i.ProductSKU,
            i.Product?.ImageUrl,
            i.UnitPrice, i.Quantity, i.LineTotal)).ToList()
    );

    private static OrderSummaryDto MapToSummaryDto(Order o) => new(
        o.Id, o.OrderNumber,
        $"{o.User?.FirstName} {o.User?.LastName}".Trim(),
        o.Status, o.PaymentStatus, o.Total,
        o.Items.Sum(i => i.Quantity), o.CreatedAt
    );
}