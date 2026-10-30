using Microsoft.EntityFrameworkCore;
using TechStore.API.Data;
using TechStore.API.DTOs.Pos;
using TechStore.API.Helpers;
using TechStore.API.Models;
using TechStore.API.Repositories.Interfaces;
using TechStore.API.Services.Interfaces;

namespace TechStore.API.Services;

// A cashier working outside their home shop; PosController turns it into a 403
public class PosForbiddenException(string message) : Exception(message);

// In-store (walk-in) sales. A sale is saved as an order that is already paid and
// delivered, with its payments, the IMEIs of the units sold, and the cashier.
public class PosService : IPosService
{
    private const decimal VatRate = 0.075m; // same as online checkout (OrderService)
    private const int MaxPinAttempts = 5;
    private static readonly TimeSpan PinLockout = TimeSpan.FromMinutes(15);

    private readonly AppDbContext _db;
    private readonly IInventoryRepository _inventory;
    private readonly IRoleService _roles;
    private readonly ILogger<PosService> _logger;

    public PosService(AppDbContext db, IInventoryRepository inventory, IRoleService roles, ILogger<PosService> logger)
    {
        _db = db;
        _inventory = inventory;
        _roles = roles;
        _logger = logger;
    }

    public async Task<IEnumerable<PosProductDto>> SearchProductsAsync(int userId, bool isAdmin, int? storeId, string? query)
    {
        var store = await ResolveStoreAsync(userId, isAdmin, storeId);
        var term = query?.Trim() ?? "";

        var q = _db.Inventories.AsNoTracking().Where(i => i.StoreId == store.Id && i.Product.IsActive);
        if (term != "")
            q = q.Where(i => i.Product.SKU == term || i.Product.Name.Contains(term) || i.Product.SKU.Contains(term)
                || (i.Product.Brand != null && i.Product.Brand.Contains(term)));

        return await q
            .OrderByDescending(i => i.Product.SKU == term) // a scanned SKU comes first
            .ThenBy(i => i.Product.Name)
            .Take(30)
            .Select(i => new PosProductDto(
                i.ProductId, i.Product.Name, i.Product.SKU, i.Product.Brand, i.Product.Price, i.Product.ImageUrl,
                i.Product.Category.Name, i.Product.Category.RequiresImei,
                i.QuantityOnHand - i.QuantityReserved))
            .ToListAsync();
    }

    public async Task<IEnumerable<PosApproverDto>> GetApproversAsync()
    {
        var staff = await _db.Users.AsNoTracking()
            .Where(u => u.IsActive && u.Role != "Customer")
            .OrderBy(u => u.FirstName).ThenBy(u => u.LastName)
            .ToListAsync();

        var approvers = new List<PosApproverDto>();
        foreach (var u in staff)
            if (await CanApproveAsync(u))
                approvers.Add(new PosApproverDto(u.Id, FullName(u), u.Role, u.ApprovalPinHash != null));
        return approvers;
    }

    public async Task SetApprovalPinAsync(int userId, string currentPassword, string pin)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId)
            ?? throw new KeyNotFoundException("User not found.");
        if (!await CanApproveAsync(user))
            throw new PosForbiddenException("Only staff who can approve discounts need an approval PIN.");
        if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
            throw new InvalidOperationException("Current password is incorrect.");

        user.ApprovalPinHash   = BCrypt.Net.BCrypt.HashPassword(pin, workFactor: 12);
        user.FailedPinAttempts = 0;
        user.PinLockoutUntil   = null;
        user.UpdatedAt         = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        _logger.LogInformation("Approval PIN set for user {UserId}", userId);
    }

    public async Task<PosSaleDto> CreateSaleAsync(int cashierId, bool isAdmin, PosSaleRequest req)
    {
        var cashier = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == cashierId)
            ?? throw new KeyNotFoundException("User not found.");
        var store = await ResolveStoreAsync(cashierId, isAdmin, req.StoreId);

        // ── Items, prices and IMEIs ──
        var ids = req.Items.Select(i => i.ProductId).Distinct().ToList();
        var products = await _db.Products.AsNoTracking().Include(p => p.Category)
            .Where(p => ids.Contains(p.Id)).ToDictionaryAsync(p => p.Id);

        var items = new List<OrderItem>();
        var serials = new List<string>();
        var priceChanged = false;

        foreach (var line in req.Items)
        {
            if (!products.TryGetValue(line.ProductId, out var product) || !product.IsActive)
                throw new InvalidOperationException($"Product {line.ProductId} is not available.");

            var lineSerials = (line.SerialNumbers ?? []).Select(s => s.Trim()).Where(s => s != "").ToList();
            if (product.Category.RequiresImei)
            {
                if (lineSerials.Count != line.Quantity)
                    throw new InvalidOperationException(
                        $"Enter one IMEI for each '{product.Name}' sold ({line.Quantity}).");
                var bad = lineSerials.FirstOrDefault(s => !IsValidImei(s));
                if (bad != null)
                    throw new InvalidOperationException($"'{bad}' is not a valid IMEI (15 digits).");
            }
            else if (lineSerials.Count != 0 && lineSerials.Count != line.Quantity)
                throw new InvalidOperationException(
                    $"Enter a serial number for each '{product.Name}' sold, or none.");
            serials.AddRange(lineSerials);

            var unitPrice = line.UnitPrice.HasValue ? Math.Round(line.UnitPrice.Value, 2) : product.Price;
            var changed = unitPrice != product.Price;
            priceChanged |= changed;

            items.Add(new OrderItem
            {
                ProductId         = product.Id,
                ProductName       = product.Name,
                ProductSKU        = product.SKU,
                UnitPrice         = unitPrice,
                OriginalUnitPrice = changed ? product.Price : null,
                Quantity          = line.Quantity,
                LineTotal         = unitPrice * line.Quantity,
                Serials           = lineSerials.Select(s => new OrderItemSerial { SerialNumber = s }).ToList(),
            });
        }

        var repeated = serials.GroupBy(s => s).FirstOrDefault(g => g.Count() > 1)?.Key;
        if (repeated != null)
            throw new InvalidOperationException($"IMEI/serial '{repeated}' is entered more than once.");
        if (serials.Count > 0)
        {
            // A cancelled or refunded sale frees its IMEIs; returns come in a later phase
            var alreadySold = await _db.OrderItemSerials.AsNoTracking()
                .Where(s => serials.Contains(s.SerialNumber)
                    && s.OrderItem.Order.Status != OrderStatus.Cancelled
                    && s.OrderItem.Order.Status != OrderStatus.Refunded)
                .Select(s => s.SerialNumber)
                .FirstOrDefaultAsync();
            if (alreadySold != null)
                throw new InvalidOperationException($"IMEI/serial '{alreadySold}' has already been sold.");
        }

        // ── Totals, VAT as at online checkout ──
        var subTotal = items.Sum(i => i.LineTotal);
        var discount = Math.Round(req.DiscountAmount, 2);
        if (discount > subTotal)
            throw new InvalidOperationException("The discount can't be more than the subtotal.");
        var tax   = Math.Round((subTotal - discount) * VatRate, 2);
        var total = subTotal - discount + tax;

        // ── Discounts and price changes need a manager's PIN ──
        User? approver = null;
        if (discount > 0 || priceChanged)
        {
            if (string.IsNullOrWhiteSpace(req.DiscountReason))
                throw new InvalidOperationException("Give a reason for the discount or price change.");
            if (req.Approval == null)
                throw new InvalidOperationException("A discount or price change needs a manager's approval.");
            approver = await VerifyApprovalAsync(req.Approval);
        }

        var payments = BuildPayments(req.Payments, total, cashierId);
        var now = DateTime.UtcNow;

        var order = new Order
        {
            OrderNumber      = $"{store.Code}-{now:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}",
            Channel          = SalesChannel.InStore,
            StoreId          = store.Id,
            SoldByUserId     = cashierId,
            CustomerName     = Clean(req.CustomerName),
            CustomerPhone    = Clean(req.CustomerPhone)?.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", ""),
            CustomerEmail    = Clean(req.CustomerEmail),
            Status           = OrderStatus.Delivered, // handed over at the counter
            PaymentStatus    = PaymentStatus.Paid,
            PaymentMethod    = payments.Select(p => p.Method).Distinct().Count() == 1 ? payments[0].Method : "Split",
            SubTotal         = subTotal,
            DiscountAmount   = discount,
            DiscountReason   = approver != null ? req.DiscountReason!.Trim() : null,
            DiscountApprovedByUserId = approver?.Id,
            Tax              = tax,
            ShippingCost     = 0,
            Total            = total,
            Notes            = Clean(req.Notes),
            DeliveredAt      = now,
            Items            = items,
            Payments         = payments,
        };

        // The sale and the stock change save together or not at all. TrySellAsync is a
        // conditional UPDATE, so two tills can't sell the same last unit, and units reserved
        // for online orders stay reserved.
        var strategy = _db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync();
            _db.Orders.Add(order);
            await _db.SaveChangesAsync();

            foreach (var g in items.GroupBy(i => i.ProductId))
            {
                if (!await _inventory.TrySellAsync(store.Id, g.Key, g.Sum(i => i.Quantity)))
                {
                    var inv = await _inventory.GetAsync(store.Id, g.Key);
                    throw new InvalidOperationException(
                        $"Only {Math.Max(0, inv?.QuantityAvailable ?? 0)} '{g.First().ProductName}' left in {store.Name}.");
                }
            }

            _db.OrderStatusLogs.Add(new OrderStatusLog
            {
                OrderId         = order.Id,
                ChangedByUserId = cashierId,
                ChangedByName   = FullName(cashier),
                ChangedByEmail  = cashier.Email,
                FromStatus      = OrderStatus.Pending,
                ToStatus        = OrderStatus.Delivered,
                Note            = $"Sold in store at {store.Name}",
                ChangedAt       = now,
            });
            await _db.SaveChangesAsync();
            await tx.CommitAsync();
        });

        _logger.LogInformation("In-store sale {OrderNumber} at {Store} by user {UserId}: {Total}",
            order.OrderNumber, store.Code, cashierId, total);
        return (await GetSaleAsync(cashierId, isAdmin, order.Id))!;
    }

    public async Task<PosSaleDto?> GetSaleAsync(int userId, bool isAdmin, int id)
    {
        var o = await _db.Orders.AsNoTracking()
            .Include(o => o.Store).Include(o => o.SoldBy).Include(o => o.DiscountApprovedBy)
            .Include(o => o.Items).ThenInclude(i => i.Serials)
            .Include(o => o.Payments).ThenInclude(p => p.ReceivedBy)
            .AsSplitQuery()
            .FirstOrDefaultAsync(o => o.Id == id && o.Channel == SalesChannel.InStore);
        if (o == null) return null;

        if (!isAdmin)
        {
            var homeStoreId = await _db.Users.Where(u => u.Id == userId).Select(u => u.StoreId).FirstOrDefaultAsync();
            if (o.StoreId != homeStoreId) throw new PosForbiddenException("This sale belongs to another shop.");
        }

        var cash = o.Payments.Where(p => p.Method == PaymentMethods.Cash).ToList();
        return new PosSaleDto(
            o.Id, o.OrderNumber, o.CreatedAt, o.Status,
            new PosShopDto(o.Store!.Id, o.Store.Name, o.Store.Code, o.Store.Address, o.Store.City, o.Store.Phone),
            o.SoldBy != null ? FullName(o.SoldBy) : "",
            o.CustomerName, o.CustomerPhone, o.CustomerEmail,
            o.Items.Select(i => new PosSaleItemDto(
                i.ProductId, i.ProductName, i.ProductSKU, i.Quantity, i.UnitPrice, i.OriginalUnitPrice, i.LineTotal,
                i.Serials.Select(s => s.SerialNumber).ToList())).ToList(),
            o.SubTotal, o.DiscountAmount, o.DiscountReason,
            o.DiscountApprovedBy != null ? FullName(o.DiscountApprovedBy) : null,
            o.Tax, o.Total,
            o.Payments.OrderBy(p => p.Id).Select(p => new PosPaymentDto(
                p.Method, p.Amount, p.Tendered, p.Reference,
                p.ReceivedBy != null ? FullName(p.ReceivedBy) : null)).ToList(),
            cash.Sum(p => p.Tendered ?? p.Amount) - cash.Sum(p => p.Amount),
            o.Notes);
    }

    // End-of-day cash-up for one shop: what each cashier took, by payment method.
    // Without wholeShop (managers and admins have it) cashiers only see their own sales.
    // Cancelled or refunded sales are counted separately and left out of the totals.
    public async Task<CashUpDto> GetCashUpAsync(int userId, bool isAdmin, bool wholeShop, int? storeId, DateOnly date)
    {
        var store   = await ResolveStoreAsync(userId, isAdmin, storeId);
        var fromUtc = date.ToDateTime(TimeOnly.MinValue) - NigeriaOffset;
        var toUtc   = fromUtc.AddDays(1);

        var orders = await _db.Orders.AsNoTracking()
            .Include(o => o.SoldBy).Include(o => o.Payments)
            .Where(o => o.Channel == SalesChannel.InStore && o.StoreId == store.Id
                && o.CreatedAt >= fromUtc && o.CreatedAt < toUtc
                && (wholeShop || o.SoldByUserId == userId))
            .OrderBy(o => o.CreatedAt)
            .AsSplitQuery()
            .ToListAsync();

        var counted  = orders.Where(o => o.Status is not (OrderStatus.Cancelled or OrderStatus.Refunded)).ToList();
        var payments = counted.SelectMany(o => o.Payments).ToList();
        var cash     = payments.Where(p => p.Method == PaymentMethods.Cash).ToList();

        static List<CashUpMethodDto> ByMethod(IEnumerable<OrderPayment> parts) => PaymentMethods.InStore
            .Select(m => new CashUpMethodDto(m, parts.Where(p => p.Method == m).Sum(p => p.Amount), parts.Count(p => p.Method == m)))
            .Where(m => m.Count > 0)
            .ToList();

        var byCashier = counted
            .GroupBy(o => o.SoldByUserId)
            .Select(g => new CashUpCashierDto(
                g.Key,
                g.First().SoldBy != null ? FullName(g.First().SoldBy!) : "Unknown",
                g.Count(), g.Sum(o => o.Total), ByMethod(g.SelectMany(o => o.Payments))))
            .OrderByDescending(c => c.Total)
            .ToList();

        return new CashUpDto(
            date,
            new PosShopDto(store.Id, store.Name, store.Code, store.Address, store.City, store.Phone),
            counted.Count,
            counted.Sum(o => o.Total),
            counted.Sum(o => o.DiscountAmount),
            cash.Sum(p => p.Tendered ?? p.Amount),
            cash.Sum(p => (p.Tendered ?? p.Amount) - p.Amount),
            ByMethod(payments),
            byCashier,
            orders.Select(o => new CashUpSaleDto(
                o.Id, o.OrderNumber, o.CreatedAt, o.SoldBy != null ? FullName(o.SoldBy) : "",
                o.Total, o.PaymentMethod ?? "", o.Status)).ToList(),
            orders.Count - counted.Count,
            !wholeShop);
    }

    // Nigeria has no daylight saving, so its day always starts at 23:00 UTC the day before
    private static readonly TimeSpan NigeriaOffset = TimeSpan.FromHours(1);

    public static DateOnly TodayInNigeria() => DateOnly.FromDateTime(DateTime.UtcNow + NigeriaOffset);

    // ── Helpers ───────────────────────────────────────────────────────────────

    // 15 digits whose last digit is the Luhn check digit, like every real IMEI
    public static bool IsValidImei(string s)
    {
        if (s.Length != 15 || !s.All(char.IsAsciiDigit)) return false;
        var sum = 0;
        for (var i = 0; i < 15; i++)
        {
            var d = s[i] - '0';
            if (i % 2 == 1) { d *= 2; if (d > 9) d -= 9; }
            sum += d;
        }
        return sum % 10 == 0;
    }

    private async Task<Store> ResolveStoreAsync(int userId, bool isAdmin, int? storeId)
    {
        var homeStoreId = await _db.Users.Where(u => u.Id == userId).Select(u => u.StoreId).FirstOrDefaultAsync();
        var id = storeId ?? homeStoreId
            ?? throw new InvalidOperationException("You don't have a home shop yet. An admin can set one on the Staff page.");
        if (!isAdmin && id != homeStoreId)
            throw new PosForbiddenException("You can only sell from your home shop.");

        var store = await _db.Stores.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id)
            ?? throw new KeyNotFoundException("Shop not found.");
        if (!store.IsActive)
            throw new InvalidOperationException($"{store.Name} is not active.");
        return store;
    }

    private async Task<bool> CanApproveAsync(User u) =>
        u.Role == "Admin" || (await _roles.GetPermissionsForRoleAsync(u.Role)).Contains(Perms.PosApproveDiscounts);

    private async Task<User> VerifyApprovalAsync(PosApprovalRequest approval)
    {
        var approver = await _db.Users.FirstOrDefaultAsync(u => u.Id == approval.ApproverUserId && u.IsActive);
        if (approver == null || !await CanApproveAsync(approver))
            throw new InvalidOperationException("That person can't approve discounts.");
        if (approver.ApprovalPinHash == null)
            throw new InvalidOperationException($"{approver.FirstName} hasn't set an approval PIN yet.");
        if (approver.PinLockoutUntil > DateTime.UtcNow)
            throw new InvalidOperationException(
                $"Too many wrong PINs for {approver.FirstName}. Try again after {approver.PinLockoutUntil:HH:mm} UTC.");

        if (!BCrypt.Net.BCrypt.Verify(approval.Pin, approver.ApprovalPinHash))
        {
            approver.FailedPinAttempts++;
            var left = MaxPinAttempts - approver.FailedPinAttempts;
            if (left <= 0)
            {
                approver.FailedPinAttempts = 0;
                approver.PinLockoutUntil   = DateTime.UtcNow.Add(PinLockout);
            }
            await _db.SaveChangesAsync(); // the attempt counts even though the sale is refused
            _logger.LogWarning("Wrong approval PIN for user {UserId}", approver.Id);
            throw new InvalidOperationException(left > 0
                ? $"Wrong PIN. {left} attempt{(left == 1 ? "" : "s")} left."
                : $"Wrong PIN. {approver.FirstName}'s PIN is locked for {PinLockout.TotalMinutes:0} minutes.");
        }

        if (approver.FailedPinAttempts != 0)
        {
            approver.FailedPinAttempts = 0;
            await _db.SaveChangesAsync();
        }
        return approver;
    }

    // Cash can be overpaid and the difference is given back as change; other methods must
    // not go over what's due. Cash parts keep Tendered (what was handed over) and their
    // Amount becomes what was kept.
    private static List<OrderPayment> BuildPayments(List<PosPaymentRequest> parts, decimal total, int cashierId)
    {
        var payments = parts.Select(p =>
        {
            if (!PaymentMethods.InStore.Contains(p.Method))
                throw new InvalidOperationException($"Unknown payment method '{p.Method}'.");
            if (PaymentMethods.NeedReference.Contains(p.Method) && string.IsNullOrWhiteSpace(p.Reference))
                throw new InvalidOperationException($"Enter the {MethodLabel(p.Method)} reference.");
            var amount = Math.Round(p.Amount, 2);
            return new OrderPayment
            {
                Method           = p.Method,
                Amount           = amount,
                Tendered         = p.Method == PaymentMethods.Cash ? amount : null,
                Reference        = Clean(p.Reference),
                ReceivedByUserId = cashierId,
            };
        }).ToList();

        var paid = payments.Sum(p => p.Amount);
        if (paid < total)
            throw new InvalidOperationException($"₦{total - paid:N2} still to pay.");

        var change = paid - total;
        var cashParts = payments.Where(p => p.Method == PaymentMethods.Cash).ToList();
        if (change > cashParts.Sum(p => p.Amount))
            throw new InvalidOperationException(
                "Card, transfer and other payments can't be more than what's due. Only cash can be overpaid.");

        foreach (var p in Enumerable.Reverse(cashParts))
        {
            var giveBack = Math.Min(change, p.Amount);
            p.Amount -= giveBack;
            change   -= giveBack;
            if (change == 0) break;
        }
        return payments;
    }

    private static string MethodLabel(string method) => method switch
    {
        PaymentMethods.Card         => "card terminal",
        PaymentMethods.BankTransfer => "bank transfer",
        _                           => method,
    };

    private static string FullName(User u) => $"{u.FirstName} {u.LastName}".Trim();

    private static string? Clean(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
