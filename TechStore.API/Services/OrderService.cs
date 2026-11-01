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
    private readonly IStoreRepository _stores;
    private readonly AppDbContext _db;
    private readonly IHttpContextAccessor _http;
    private readonly IEmailService _email;
    private readonly int _payWithinHours; // unpaid bank-transfer orders are cancelled after this

    public OrderService(
        IOrderRepository orders,
        ICartRepository carts,
        IInventoryRepository inventory,
        IUserRepository users,
        ICouponRepository coupons,
        IStoreRepository stores,
        AppDbContext db,
        IHttpContextAccessor http,
        IEmailService email,
        IConfiguration config)
    {
        _orders    = orders;
        _carts     = carts;
        _inventory = inventory;
        _users     = users;
        _coupons   = coupons;
        _stores    = stores;
        _db        = db;
        _http      = http;
        _email     = email;
        _payWithinHours = config.GetValue("Orders:PayWithinHours", 24);
    }

    public async Task<OrderDto?> GetByIdAsync(int id)
    {
        var order = await _orders.GetByIdAsync(id);
        return order == null ? null : MapToDto(order);
    }

    public async Task<PagedResult<OrderSummaryDto>> GetMyOrdersAsync(int userId, int page, int pageSize)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 50);
        var (orders, total) = await _orders.GetByUserIdAsync(userId, page, pageSize);
        return new PagedResult<OrderSummaryDto>(orders.Select(MapToSummaryDto), total, page, pageSize);
    }

    public async Task<PagedResult<OrderSummaryDto>> GetAllOrdersAsync(
        int page, int pageSize, string? status, SalesChannel? channel = null, int? storeId = null, string? search = null)
    {
        var (items, total) = await _orders.GetAllAsync(page, pageSize, status, channel, storeId, search);
        return new PagedResult<OrderSummaryDto>(items.Select(MapToSummaryDto), total, page, pageSize);
    }

    public async Task<OrderDto> CreateFromCartAsync(int userId, CreateOrderRequest req)
    {
        var user = await _users.GetByIdAsync(userId)
            ?? throw new KeyNotFoundException("User not found.");

        var cart = await _carts.GetByUserIdAsync(userId);
        if (cart == null || !cart.Items.Any())
            throw new InvalidOperationException("Cart is empty.");

        // Bank transfer is how online orders are paid, so there has to be an account to pay into
        var byTransfer = req.PaymentMethod is null or "BankTransfer";
        if (byTransfer && !await _db.BankAccounts.AnyAsync(b => b.IsActive))
            throw new InvalidOperationException(
                "Online orders are paused: there's no bank account to pay into yet. Please call or visit one of our shops.");

        // Deliveries go out from the shop that sends online orders; pickups are held at the
        // shop the customer chose, from that shop's stock
        var pickup = req.Fulfilment == FulfilmentMethod.Pickup;
        Store store;
        if (pickup)
        {
            var chosen = req.PickupStoreId is int sid ? await _stores.GetByIdAsync(sid) : null;
            store = chosen is { IsActive: true } ? chosen
                : throw new InvalidOperationException("Choose one of our shops to collect your order from.");
        }
        else store = await _stores.GetOnlineAsync();

        var productIds = cart.Items.Select(i => i.ProductId).ToList();
        var available = await _db.Inventories
            .Where(i => i.StoreId == store.Id && productIds.Contains(i.ProductId))
            .ToDictionaryAsync(i => i.ProductId, i => i.QuantityOnHand - i.QuantityReserved);

        foreach (var item in cart.Items)
        {
            // It may have been deactivated after it went into the cart
            if (item.Product is not { IsActive: true })
                throw new InvalidOperationException(
                    $"'{item.Product?.Name}' is no longer available. Remove it from your cart to continue.");

            var left = available.GetValueOrDefault(item.ProductId);
            if (left < item.Quantity)
                throw new InvalidOperationException(pickup
                    ? $"{store.Name} only has {Math.Max(left, 0)} of '{item.Product?.Name}'. Choose another shop or delivery."
                    : $"Insufficient stock for '{item.Product?.Name}'. Available: {Math.Max(left, 0)}");
        }

        // Delivery is priced per state; states without a fee only get pickup
        decimal shipping = 0;
        if (!pickup)
        {
            var rate = await _db.DeliveryRates.FirstOrDefaultAsync(r => r.State == req.ShippingProvince)
                ?? throw new InvalidOperationException(
                    $"We don't deliver to {req.ShippingProvince} yet. Choose pickup from one of our shops, or call us.");
            shipping = rate.Fee;
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

        // A coupon that's stopped working since the customer applied it is an error, not silently dropped
        decimal discount = 0;
        Coupon? appliedCoupon = null;
        if (!string.IsNullOrWhiteSpace(req.CouponCode))
        {
            appliedCoupon = await _coupons.GetByCodeAsync(req.CouponCode.Trim());
            var uses = appliedCoupon == null ? 0 : await _coupons.CountUsesByCustomerAsync(appliedCoupon.Code, userId);
            var problem = CouponRules.Problem(appliedCoupon, subTotal, uses, DateTime.UtcNow);
            if (problem != null)
                throw new InvalidOperationException($"{problem} Remove the coupon to continue.");
            discount = CouponRules.Discount(appliedCoupon!, subTotal);
        }

        // No VAT is charged. Tax stays on orders because older ones were charged 7.5%.
        var tax   = 0m;
        var total = subTotal - discount + shipping;
        var now   = DateTime.UtcNow;

        var order = new Order
        {
            OrderNumber        = GenerateOrderNumber(),
            UserId             = userId,
            StoreId            = store.Id,
            Fulfilment         = req.Fulfilment,
            Status             = OrderStatus.Pending,
            PaymentStatus      = PaymentStatus.Pending,
            PaymentMethod      = req.PaymentMethod,
            TransactionId      = string.IsNullOrWhiteSpace(req.TransactionId) ? null : req.TransactionId.Trim(),
            PaymentDueAt       = byTransfer ? now.AddHours(_payWithinHours) : null,
            SubTotal           = subTotal,
            DiscountAmount     = discount,
            CouponCode         = appliedCoupon?.Code,
            Tax                = tax,
            ShippingCost       = shipping,
            Total              = total,
            ShippingFirstName  = req.ShippingFirstName,
            ShippingLastName   = req.ShippingLastName,
            ShippingAddress    = pickup ? "" : req.ShippingAddress ?? "",
            ShippingCity       = pickup ? "" : req.ShippingCity ?? "",
            ShippingProvince   = pickup ? "" : req.ShippingProvince ?? "",
            ShippingPostalCode = pickup ? "" : req.ShippingPostalCode ?? "",
            ShippingCountry    = string.IsNullOrWhiteSpace(req.ShippingCountry) ? "Nigeria" : req.ShippingCountry,
            ShippingPhone      = req.ShippingPhone,
            Notes              = req.Notes,
            CreatedAt          = now,
            Items              = orderItems
        };

        // The order, stock reservation, emptied cart and coupon use save together or not at
        // all. Each reservation and the coupon use is a conditional UPDATE, so two checkouts
        // can't take the same last unit or last use. The retrying execution strategy requires
        // the transaction to run inside it.
        var strategy = _db.Database.CreateExecutionStrategy();
        var created = await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync();

            var saved = await _orders.CreateAsync(order);

            foreach (var item in cart.Items)
                if (!await _inventory.TryReserveAsync(store.Id, item.ProductId, item.Quantity))
                    throw new InvalidOperationException(
                        $"'{item.Product?.Name}' has just sold out. Please update your cart and try again.");

            if (appliedCoupon != null && !await _coupons.TryUseAsync(appliedCoupon.Id))
                throw new InvalidOperationException(
                    $"Coupon {appliedCoupon.Code} has just run out. Remove it to continue.");

            await WriteLogAsync(saved.Id, null, OrderStatus.Pending, OrderStatus.Pending,
                pickup ? $"Order placed by customer, to collect from {store.Name}" : "Order placed by customer",
                isSystem: true);

            await _carts.ClearAsync(userId);

            await tx.CommitAsync();
            return saved;
        });

        var result = MapToDto(await _orders.GetByIdAsync(created.Id) ?? created);
        _ = _email.SendOrderConfirmationAsync(user.Email, user.FirstName, result.OrderNumber, result.Total,
            order.PaymentDueAt, pickup ? store.Name : null);
        return result;
    }

    // Shops the customer can collect from, and whether each has everything in their cart
    public async Task<IEnumerable<PickupShopDto>> GetPickupShopsAsync(int userId)
    {
        var cart = await _carts.GetByUserIdAsync(userId);
        var wanted = (cart?.Items ?? [])
            .GroupBy(i => i.ProductId)
            .ToDictionary(g => g.Key, g => g.Sum(i => i.Quantity));
        var ids = wanted.Keys.ToList();

        var stock = await _db.Inventories
            .Where(i => ids.Contains(i.ProductId))
            .Select(i => new { i.StoreId, i.ProductId, Available = i.QuantityOnHand - i.QuantityReserved })
            .ToListAsync();

        return (await _stores.GetAllAsync())
            .Where(s => s.IsActive)
            .OrderBy(s => s.Name)
            .Select(s => new PickupShopDto(s.Id, s.Name, s.Address, s.City, s.Phone,
                wanted.Count > 0 && wanted.All(w =>
                    stock.Any(r => r.StoreId == s.Id && r.ProductId == w.Key && r.Available >= w.Value))));
    }

    public async Task<OrderDto?> UpdateStatusAsync(int id, UpdateOrderStatusRequest req)
    {
        var order = await _orders.GetByIdAsync(id);
        if (order == null) return null;

        // Don't log a no-op
        if (order.Status == req.Status) return MapToDto(order);

        if (order.Channel != SalesChannel.Online)
            throw new InvalidOperationException("In-store sales are final and can't be changed here.");

        if (!NextStatuses(order).Contains(req.Status))
            throw new InvalidOperationException(order.Status == OrderStatus.Pending && req.Status == OrderStatus.Confirmed
                ? "Use \"Mark as paid\" once the payment has arrived."
                : $"An order that's {Describe(order.Status)} can't be moved to {Describe(req.Status)}.");

        await MoveAsync(order, req.Status, GetCurrentUserId(), req.Notes);

        EmailStatus(order);
        return MapToDto(order);
    }

    public async Task<OrderDto?> MarkPaidAsync(int id, string? reference)
    {
        var order = await _orders.GetByIdAsync(id);
        if (order == null) return null;

        if (!CanMarkPaid(order))
            throw new InvalidOperationException(order.PaymentStatus == PaymentStatus.Paid
                ? "This order is already marked as paid."
                : $"An order that's {Describe(order.Status)} can't be marked as paid.");

        reference = string.IsNullOrWhiteSpace(reference) ? null : reference.Trim();
        var note = reference == null ? "Payment received" : $"Payment received (ref: {reference})";
        void RecordPayment(Order o)
        {
            o.PaymentStatus   = PaymentStatus.Paid;
            o.PaidAt          = DateTime.UtcNow;
            o.TransactionId ??= reference;
        }

        if (order.Status == OrderStatus.Pending)
        {
            await MoveAsync(order, OrderStatus.Confirmed, GetCurrentUserId(), note, RecordPayment);
            EmailStatus(order);
        }
        else
        {
            // An older order that staff moved on before payment was recorded
            RecordPayment(order);
            order.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            await WriteLogAsync(order.Id, GetCurrentUserId(), order.Status, order.Status, note);
        }

        return MapToDto(order);
    }

    // Run by UnpaidOrderService. Halfway to the deadline the customer gets a reminder; at the
    // deadline an order that's still unpaid is cancelled, so its phones go back on sale.
    // Returns how many orders were cancelled.
    public async Task<int> ProcessUnpaidOrdersAsync()
    {
        var now = DateTime.UtcNow;
        var remindFrom = now.AddHours(_payWithinHours / 2.0);

        var toRemind = await _db.Orders.Include(o => o.User)
            .Where(o => o.Status == OrderStatus.Pending && o.PaymentStatus == PaymentStatus.Pending
                && o.PaymentDueAt != null && o.PaymentDueAt > now && o.PaymentDueAt <= remindFrom
                && o.PaymentReminderSentAt == null)
            .ToListAsync();
        foreach (var o in toRemind)
        {
            // Claimed with a conditional update, so a second copy of the app can't send it too
            var claimed = await _db.Orders
                .Where(x => x.Id == o.Id && x.PaymentReminderSentAt == null)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.PaymentReminderSentAt, now)) == 1;
            if (claimed && o.User != null)
                await _email.SendPaymentReminderAsync(o.User.Email, o.User.FirstName, o.OrderNumber, o.Total, o.PaymentDueAt!.Value);
        }

        var dueIds = await _db.Orders
            .Where(o => o.Status == OrderStatus.Pending && o.PaymentStatus == PaymentStatus.Pending
                && o.PaymentDueAt != null && o.PaymentDueAt <= now)
            .Select(o => o.Id)
            .ToListAsync();

        var cancelled = 0;
        foreach (var id in dueIds)
        {
            var order = await _orders.GetByIdAsync(id);
            if (order is not { Status: OrderStatus.Pending, PaymentStatus: PaymentStatus.Pending }) continue;
            try
            {
                await MoveAsync(order, OrderStatus.Cancelled, null, $"Not paid within {_payWithinHours} hours");
                cancelled++;
                EmailStatus(order);
            }
            catch (InvalidOperationException) { } // paid or changed by staff in the meantime
        }
        return cancelled;
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

    // ── Order flow ────────────────────────────────────────────────────────────

    // Where staff can move an online order next. Pending → Confirmed only happens through
    // MarkPaidAsync, and nothing moves once delivered or cancelled (there are no returns).
    private static List<OrderStatus> NextStatuses(Order o)
    {
        if (o.Channel != SalesChannel.Online) return [];
        var handOver = o.Fulfilment == FulfilmentMethod.Pickup ? OrderStatus.ReadyForPickup : OrderStatus.Shipped;
        return o.Status switch
        {
            OrderStatus.Pending    => [OrderStatus.Cancelled],
            OrderStatus.Confirmed  => [OrderStatus.Processing, handOver, OrderStatus.Cancelled],
            OrderStatus.Processing => [handOver, OrderStatus.Cancelled],
            OrderStatus.Shipped or OrderStatus.ReadyForPickup => [OrderStatus.Delivered, OrderStatus.Cancelled],
            _ => [],
        };
    }

    private static bool CanMarkPaid(Order o) =>
        o.Channel == SalesChannel.Online && o.PaymentStatus == PaymentStatus.Pending
        && o.Status is not (OrderStatus.Delivered or OrderStatus.Cancelled or OrderStatus.Refunded);

    // Moves the order on, together with its stock and coupon. The status only changes if it's
    // still what we read, so two clicks (or a click and the unpaid-order sweep) can't both
    // release the same stock. `also` makes extra changes in the same save.
    private async Task MoveAsync(Order order, OrderStatus to, int? actorId, string? note, Action<Order>? also = null)
    {
        var from = order.Status;
        var storeId = order.StoreId ?? (await _stores.GetOnlineAsync()).Id;

        var strategy = _db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync();

            var moved = await _db.Orders
                .Where(o => o.Id == order.Id && o.Status == from)
                .ExecuteUpdateAsync(s => s.SetProperty(o => o.Status, to)) == 1;
            if (!moved)
                throw new InvalidOperationException("This order has just been changed by someone else. Refresh it and try again.");

            var now = DateTime.UtcNow;
            order.Status    = to;
            order.UpdatedAt = now;
            if (to == OrderStatus.Shipped) order.ShippedAt ??= now;
            if (to == OrderStatus.Delivered)
            {
                order.DeliveredAt ??= now;
                // Older orders weren't marked paid until delivery
                if (order.PaymentStatus != PaymentStatus.Paid)
                {
                    order.PaymentStatus = PaymentStatus.Paid;
                    order.PaidAt ??= now;
                }
            }
            // Money that came in has to go back; an unpaid order just stays unpaid
            if (to == OrderStatus.Cancelled && order.PaymentStatus == PaymentStatus.Paid)
                order.PaymentStatus = PaymentStatus.Refunded;
            also?.Invoke(order);

            // Stock stays reserved from checkout until the phones are handed over or the order is cancelled
            if (to is OrderStatus.Delivered or OrderStatus.Cancelled)
                foreach (var item in order.Items)
                {
                    if (to == OrderStatus.Delivered)
                        await _inventory.FulfilAsync(storeId, item.ProductId, item.Quantity);
                    else
                        await _inventory.ReleaseAsync(storeId, item.ProductId, item.Quantity);
                }
            if (to == OrderStatus.Cancelled && order.CouponCode != null)
                await _coupons.ReleaseUseAsync(order.CouponCode);

            await _db.SaveChangesAsync();
            await WriteLogAsync(order.Id, actorId, from, to, note, isSystem: actorId == null);
            await tx.CommitAsync();
        });
    }

    // Tells the customer what's happened, in words rather than status names
    private void EmailStatus(Order order)
    {
        if (order.User?.Email == null) return;
        var shop = order.Store?.Name ?? "our shop";
        var (title, detail) = order.Status switch
        {
            OrderStatus.Confirmed      => ("Payment received", "Thank you, we've received your payment and we're getting your order ready."),
            OrderStatus.Processing     => ("Being prepared", "We're getting your order ready."),
            OrderStatus.Shipped        => ("On its way", "Your order has left our shop and is on its way to you."),
            OrderStatus.ReadyForPickup => ("Ready for pickup", $"Your order is ready to collect from {shop}{(order.Store?.Address is { } a ? $", {a}" : "")}. Bring your order number."),
            OrderStatus.Delivered      => order.Fulfilment == FulfilmentMethod.Pickup
                                            ? ("Collected", "Thanks for collecting your order. Enjoy!")
                                            : ("Delivered", "Your order has been delivered. Enjoy!"),
            OrderStatus.Cancelled      => ("Cancelled", order.PaymentStatus == PaymentStatus.Refunded
                                            ? "Your order has been cancelled. As you'd already paid, we'll be in touch about your refund."
                                            : order.PaymentDueAt <= DateTime.UtcNow
                                                ? $"We didn't receive payment within {_payWithinHours} hours, so your order has been cancelled. You're welcome to order again."
                                                : "Your order has been cancelled."),
            _                          => (order.Status.ToString(), null),
        };
        _ = _email.SendOrderStatusUpdateAsync(order.User.Email, order.User.FirstName ?? "", order.OrderNumber, title, detail);
    }

    // For messages: "An order that's waiting for payment can't be moved to shipped."
    private static string Describe(OrderStatus s) => s switch
    {
        OrderStatus.Pending        => "waiting for payment",
        OrderStatus.Confirmed      => "paid",
        OrderStatus.Processing     => "being prepared",
        OrderStatus.Shipped        => "shipped",
        OrderStatus.ReadyForPickup => "ready for pickup",
        OrderStatus.Delivered      => "delivered",
        OrderStatus.Cancelled      => "cancelled",
        OrderStatus.Refunded       => "refunded",
        _                          => s.ToString(),
    };

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

    // Walk-in sales may have no account, only the name the cashier typed (or nothing)
    private static string CustomerNameOf(Order o) =>
        o.User != null ? $"{o.User.FirstName} {o.User.LastName}".Trim()
        : !string.IsNullOrWhiteSpace(o.CustomerName) ? o.CustomerName
        : "Walk-in customer";

    private static OrderDto MapToDto(Order o)
    {
        var pickup = o.Channel == SalesChannel.Online && o.Fulfilment == FulfilmentMethod.Pickup;
        return new(
            o.Id, o.OrderNumber, o.UserId,
            CustomerNameOf(o),
            o.User?.Email ?? o.CustomerEmail ?? "",
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
                i.UnitPrice, i.Quantity, i.LineTotal)).ToList(),
            o.Channel, o.Store?.Name,
            o.Fulfilment,
            o.ShippingPhone,
            pickup ? string.Join(", ", new[] { o.Store?.Address, o.Store?.City }.Where(x => !string.IsNullOrWhiteSpace(x))) : null,
            pickup ? o.Store?.Phone : null,
            o.Status == OrderStatus.Pending && o.PaymentStatus == PaymentStatus.Pending ? o.PaymentDueAt : null,
            o.PaidAt,
            NextStatuses(o),
            CanMarkPaid(o)
        );
    }

    private static OrderSummaryDto MapToSummaryDto(Order o) => new(
        o.Id, o.OrderNumber,
        CustomerNameOf(o),
        o.Status, o.PaymentStatus, o.Total,
        o.Items.Sum(i => i.Quantity), o.CreatedAt,
        o.Channel, o.Store?.Name,
        o.Fulfilment
    );
}
