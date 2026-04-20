using TechStore.API.DTOs.Order;
using TechStore.API.Models;
using TechStore.API.Repositories.Interfaces;
using TechStore.API.Services.Interfaces;

namespace TechStore.API.Services;

public class OrderService : IOrderService
{
    private readonly IOrderRepository _orders;
    private readonly ICartRepository _carts;
    private readonly IInventoryRepository _inventory;
    private readonly IUserRepository _users;
    private readonly ICouponService _couponService;

    public OrderService(
        IOrderRepository orders,
        ICartRepository carts,
        IInventoryRepository inventory,
        IUserRepository users,
        ICouponService couponService)
    {
        _orders = orders;
        _carts = carts;
        _inventory = inventory;
        _users = users;
        _couponService = couponService;
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

        // Validate stock for all items
        foreach (var item in cart.Items)
        {
            var inv = await _inventory.GetByProductIdAsync(item.ProductId);
            if (inv == null || inv.QuantityAvailable < item.Quantity)
                throw new InvalidOperationException(
                    $"Insufficient stock for '{item.Product?.Name}'. Available: {inv?.QuantityAvailable ?? 0}");
        }

        // Build order
        var orderItems = cart.Items.Select(i => new OrderItem
        {
            ProductId = i.ProductId,
            ProductName = i.Product?.Name ?? "",
            ProductSKU = i.Product?.SKU,
            UnitPrice = i.Product?.Price ?? 0,
            Quantity = i.Quantity,
            LineTotal = (i.Product?.Price ?? 0) * i.Quantity
        }).ToList();

        var subTotal = orderItems.Sum(i => i.LineTotal);

        decimal discountAmount = 0;
        string? appliedCouponCode = null;
        int? appliedCouponId = null;
        if (!string.IsNullOrWhiteSpace(req.CouponCode))
        {
            var (isValid, error, discount, couponEntity) = await _couponService.ValidateAndCalculateDiscountAsync(req.CouponCode, subTotal);
            if (!isValid)
                throw new InvalidOperationException(error ?? "Invalid coupon.");
            discountAmount = discount;
            appliedCouponCode = couponEntity?.Code;
            appliedCouponId = couponEntity?.Id;
        }

        var tax = Math.Round(subTotal * 0.13m, 2); // Ontario HST
        var shipping = subTotal >= 100 ? 0 : 9.99m;  // Free shipping over $100
        var total = subTotal + tax + shipping - discountAmount;

        var order = new Order
        {
            OrderNumber = GenerateOrderNumber(),
            UserId = userId,
            Status = OrderStatus.Pending,
            PaymentStatus = PaymentStatus.Pending,
            PaymentMethod = req.PaymentMethod,
            TransactionId = req.TransactionId,
            SubTotal = subTotal,
            Tax = tax,
            ShippingCost = shipping,
            DiscountAmount = discountAmount,
            CouponCode = appliedCouponCode,
            Total = total,
            ShippingFirstName = req.ShippingFirstName,
            ShippingLastName = req.ShippingLastName,
            ShippingAddress = req.ShippingAddress,
            ShippingCity = req.ShippingCity,
            ShippingProvince = req.ShippingProvince,
            ShippingPostalCode = req.ShippingPostalCode,
            ShippingCountry = req.ShippingCountry,
            ShippingPhone = req.ShippingPhone,
            Notes = req.Notes,
            Items = orderItems
        };

        var created = await _orders.CreateAsync(order);

        if (appliedCouponId.HasValue)
        {
            await _couponService.IncrementUsageAsync(appliedCouponId.Value);
        }

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

        // Clear cart
        await _carts.ClearAsync(userId);

        return MapToDto(await _orders.GetByIdAsync(created.Id) ?? created);
    }

    public async Task<OrderDto?> UpdateStatusAsync(int id, UpdateOrderStatusRequest req)
    {
        var order = await _orders.GetByIdAsync(id);
        if (order == null) return null;

        var prevStatus = order.Status;
        order.Status = req.Status;

        if (req.Status == OrderStatus.Shipped && order.ShippedAt == null)
            order.ShippedAt = DateTime.UtcNow;

        if (req.Status == OrderStatus.Delivered && order.DeliveredAt == null)
        {
            order.DeliveredAt = DateTime.UtcNow;
            order.PaymentStatus = PaymentStatus.Paid;
        }

        if (req.Status == OrderStatus.Cancelled && prevStatus != OrderStatus.Cancelled)
        {
            // Release reserved inventory
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
        return MapToDto(order);
    }

    public Task<bool> HasUserPurchasedProductAsync(int userId, int productId) =>
        _orders.HasUserPurchasedProductAsync(userId, productId);

    private static string GenerateOrderNumber() =>
        $"TS-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpper()}";

    private static OrderDto MapToDto(Order o) => new(
        o.Id, o.OrderNumber, o.UserId,
        $"{o.User?.FirstName} {o.User?.LastName}".Trim(),
        o.User?.Email ?? "",
        o.Status, o.PaymentStatus, o.PaymentMethod,
        o.SubTotal, o.Tax, o.ShippingCost, o.DiscountAmount, o.Total,
        o.ShippingAddress, o.ShippingCity, o.ShippingProvince,
        o.ShippingPostalCode, o.ShippingCountry,
        o.CouponCode,
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
