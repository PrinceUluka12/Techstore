using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using TechStore.API.Data;
using TechStore.API.DTOs.Pos;
using TechStore.API.Helpers;
using TechStore.API.Hubs;
using TechStore.API.Models;
using TechStore.API.Services.Interfaces;

namespace TechStore.API.Services;

// A problem with the manager's approval itself, rather than with the sale: a wrong PIN, an
// approval that has run out or been used, or a cart that changed after it was approved. The API
// answers 400 with "code" set (ExceptionMiddleware), so the till can tell these from the sale's
// other problems without reading the message, which can contain product and shop names.
//   "approval"  the till needs a (new) approval
//   "answered"  a manager answering a request that's no longer waiting
public class ApprovalException(string message, string code = "approval") : InvalidOperationException(message)
{
    public string Code { get; } = code;
}

// Manager approval of discounts and price changes at the till, either way:
//   - at the till: the manager picks their name and types their PIN (VerifyPinAsync)
//   - from their own device: the till asks (AskAsync), the shop's managers get a live alert
//     (NotificationsHub), one approves with their PIN or declines, and the till completes the
//     sale with the approved request (CheckForSaleAsync, then MarkUsedAsync as it's saved)
// Either way the approver must be an Admin or a manager whose home shop is the till's.
public class ApprovalService : IApprovalService
{
    private const int MaxPinAttempts = 5;
    private static readonly TimeSpan PinLockout  = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan AnswerWithin = TimeSpan.FromMinutes(5);  // then the request expires
    private static readonly TimeSpan UseWithin    = TimeSpan.FromMinutes(10); // from approval to completing the sale

    private readonly AppDbContext _db;
    private readonly IRoleService _roles;
    private readonly IHubContext<NotificationsHub> _hub;
    private readonly StaffPresence _presence;
    private readonly ILogger<ApprovalService> _logger;

    public ApprovalService(AppDbContext db, IRoleService roles, IHubContext<NotificationsHub> hub,
        StaffPresence presence, ILogger<ApprovalService> logger)
    {
        _db = db;
        _roles = roles;
        _hub = hub;
        _presence = presence;
        _logger = logger;
    }

    public async Task<bool> CanApproveAsync(User u) =>
        u.Role == "Admin" || (await _roles.GetPermissionsForRoleAsync(u.Role)).Contains(Perms.PosApproveDiscounts);

    // The approver's PIN, with a lockout after too many wrong ones
    public async Task<User> VerifyPinAsync(int approverUserId, string pin, int storeId)
    {
        var approver = await _db.Users.FirstOrDefaultAsync(u => u.Id == approverUserId && u.IsActive);
        if (approver == null || !await CanApproveAsync(approver))
            throw new ApprovalException("That person can't approve discounts.");
        if (approver.Role != "Admin" && approver.StoreId != storeId)
            throw new ApprovalException($"{approver.FirstName} isn't a manager at this shop.");
        if (approver.ApprovalPinHash == null)
            throw new ApprovalException($"{approver.FirstName} hasn't set an approval PIN yet.");
        if (approver.PinLockoutUntil > DateTime.UtcNow)
            throw new ApprovalException(
                $"Too many wrong PINs for {approver.FirstName}. Try again after {approver.PinLockoutUntil:HH:mm} UTC.");

        if (!BCrypt.Net.BCrypt.Verify(pin, approver.ApprovalPinHash))
        {
            approver.FailedPinAttempts++;
            var left = MaxPinAttempts - approver.FailedPinAttempts;
            if (left <= 0)
            {
                approver.FailedPinAttempts = 0;
                approver.PinLockoutUntil   = DateTime.UtcNow.Add(PinLockout);
            }
            await _db.SaveChangesAsync(); // the attempt counts even though the approval is refused
            _logger.LogWarning("Wrong approval PIN for user {UserId}", approver.Id);
            throw new ApprovalException(left > 0
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

    // ── Asking from the till ──────────────────────────────────────────────────

    public async Task<ApprovalAskedDto> AskAsync(int userId, bool isAdmin, AskApprovalRequest req)
    {
        var store = await PosService.ResolveStoreAsync(_db, userId, isAdmin, req.StoreId);

        var ids = req.Items.Select(i => i.ProductId).Distinct().ToList();
        var products = await _db.Products.AsNoTracking().Where(p => ids.Contains(p.Id)).ToDictionaryAsync(p => p.Id);
        var items = req.Items.Select(line =>
        {
            if (!products.TryGetValue(line.ProductId, out var p) || !p.IsActive)
                throw new InvalidOperationException($"Product {line.ProductId} is not available.");
            return new ApprovalRequestItem
            {
                ProductId   = p.Id,
                ProductName = p.Name,
                ProductSKU  = p.SKU,
                Quantity    = line.Quantity,
                NormalPrice = p.Price,
                UnitPrice   = line.UnitPrice.HasValue ? Math.Round(line.UnitPrice.Value, 2) : p.Price,
            };
        }).ToList();

        var subTotal = items.Sum(i => i.UnitPrice * i.Quantity);
        var discount = Math.Round(req.DiscountAmount, 2);
        if (discount > subTotal)
            throw new InvalidOperationException("The discount can't be more than the subtotal.");
        if (discount == 0 && items.All(i => i.UnitPrice == i.NormalPrice))
            throw new InvalidOperationException("There's no discount or price change to approve.");

        // A cashier waits on one request at a time: asking again (after changing the sale, say)
        // replaces the last one, so managers aren't left answering a cart that has gone
        var now = DateTime.UtcNow;
        var replaced = await _db.ApprovalRequests
            .Where(r => r.RequestedByUserId == userId
                && ((r.Status == ApprovalStatus.Waiting && r.ExpiresAt > now) || r.Status == ApprovalStatus.Approved))
            .Select(r => r.Id).ToListAsync();
        if (replaced.Count > 0)
            await _db.ApprovalRequests
                .Where(r => replaced.Contains(r.Id) && (r.Status == ApprovalStatus.Waiting || r.Status == ApprovalStatus.Approved))
                .ExecuteUpdateAsync(s => s.SetProperty(r => r.Status, ApprovalStatus.Cancelled));

        var request = new ApprovalRequest
        {
            StoreId           = store.Id,
            RequestedByUserId = userId,
            CreatedAt         = now,
            ExpiresAt         = now.Add(AnswerWithin),
            SubTotal          = subTotal,
            DiscountAmount    = discount,
            Total             = subTotal - discount,
            Reason            = req.Reason!.Trim(),
            Items             = items,
        };
        _db.ApprovalRequests.Add(request);
        await _db.SaveChangesAsync();
        _logger.LogInformation("Approval request {Id} at {Store} by user {UserId}: {Discount} off, prices changed: {Changed}",
            request.Id, store.Code, userId, discount, items.Any(i => i.UnitPrice != i.NormalPrice));

        foreach (var id in replaced) await NotifyAsync(id);
        var dto = (await LoadAsync(request.Id))!;
        await SendAsync(NotificationsHub.ApprovalRequested, dto);
        return new ApprovalAskedDto(dto, _presence.ManagersOnline(store.Id, userId));
    }

    public async Task<ApprovalRequestDto> CancelAsync(int userId, int id)
    {
        var now = DateTime.UtcNow;
        var cancelled = await _db.ApprovalRequests
            .Where(r => r.Id == id && r.RequestedByUserId == userId
                && ((r.Status == ApprovalStatus.Waiting && r.ExpiresAt > now) || r.Status == ApprovalStatus.Approved))
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.Status, ApprovalStatus.Cancelled));
        var dto = await LoadAsync(id);
        if (dto == null || dto.RequestedById != userId) throw new KeyNotFoundException("Approval request not found.");
        if (cancelled > 0) await SendAsync(NotificationsHub.ApprovalUpdated, dto);
        return dto; // already answered, expired or used: as it is
    }

    // ── Answering, on the manager's own device ─────────────────────────────────

    // Waiting requests (oldest first) or the latest 50, for the manager's home shop; Admins see every shop's
    public async Task<IEnumerable<ApprovalRequestDto>> ListAsync(int userId, bool isAdmin, bool waitingOnly)
    {
        var homeStoreId = await _db.Users.Where(u => u.Id == userId).Select(u => u.StoreId).FirstOrDefaultAsync();
        var q = WithDetails();
        if (!isAdmin) q = q.Where(r => r.StoreId == homeStoreId);

        var now = DateTime.UtcNow;
        q = waitingOnly
            ? q.Where(r => r.Status == ApprovalStatus.Waiting && r.ExpiresAt > now).OrderBy(r => r.Id)
            : q.OrderByDescending(r => r.Id).Take(50);
        return (await q.ToListAsync()).Select(r => ToDto(r, now)).ToList();
    }

    // For the cashier who asked, and the managers who can answer it
    public async Task<ApprovalRequestDto?> GetAsync(int userId, bool isAdmin, bool canApprove, int id)
    {
        var dto = await LoadAsync(id);
        if (dto == null || dto.RequestedById == userId || isAdmin) return dto;
        var homeStoreId = await _db.Users.Where(u => u.Id == userId).Select(u => u.StoreId).FirstOrDefaultAsync();
        return canApprove && homeStoreId == dto.StoreId ? dto : null;
    }

    public async Task<ApprovalRequestDto> ApproveAsync(int userId, int id, string pin)
    {
        var request = await ForDecisionAsync(userId, id);
        await VerifyPinAsync(userId, pin, request.StoreId);
        return await DecideAsync(userId, id, ApprovalStatus.Approved, null);
    }

    public async Task<ApprovalRequestDto> DeclineAsync(int userId, int id, string? note)
    {
        await ForDecisionAsync(userId, id);
        return await DecideAsync(userId, id, ApprovalStatus.Declined, string.IsNullOrWhiteSpace(note) ? null : note.Trim());
    }

    // Who may answer: an Admin, or a manager of that shop, but not the cashier who asked
    private async Task<ApprovalRequest> ForDecisionAsync(int userId, int id)
    {
        var request = await _db.ApprovalRequests.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id)
            ?? throw new KeyNotFoundException("Approval request not found.");
        var manager = await _db.Users.AsNoTracking().FirstAsync(u => u.Id == userId);
        if (!manager.IsActive || !await CanApproveAsync(manager))
            throw new ForbiddenException("You can't approve discounts.");
        if (manager.Role != "Admin" && manager.StoreId != request.StoreId)
            throw new ForbiddenException("This request is for another shop.");
        if (request.RequestedByUserId == userId)
            throw new ForbiddenException("You can't answer your own request.");
        if (request.Status != ApprovalStatus.Waiting || request.ExpiresAt <= DateTime.UtcNow)
            throw new ApprovalException(await AnsweredAsync(id), "answered");
        return request;
    }

    // Only one answer counts: if two managers answer at once, the first to save wins
    private async Task<ApprovalRequestDto> DecideAsync(int userId, int id, ApprovalStatus status, string? note)
    {
        var now = DateTime.UtcNow;
        var won = await _db.ApprovalRequests
            .Where(r => r.Id == id && r.Status == ApprovalStatus.Waiting && r.ExpiresAt > now)
            .ExecuteUpdateAsync(s => s
                .SetProperty(r => r.Status, status)
                .SetProperty(r => r.DecidedByUserId, userId)
                .SetProperty(r => r.DecidedAt, now)
                .SetProperty(r => r.DecisionNote, note));
        if (won == 0) throw new ApprovalException(await AnsweredAsync(id), "answered");

        _logger.LogInformation("Approval request {Id} {Status} by user {UserId}", id, status, userId);
        var dto = (await LoadAsync(id))!;
        await SendAsync(NotificationsHub.ApprovalUpdated, dto);
        return dto;
    }

    private async Task<string> AnsweredAsync(int id)
    {
        var dto = (await LoadAsync(id))!;
        return dto.Status switch
        {
            ApprovalStatus.Approved or ApprovalStatus.Used => $"{dto.DecidedByName} has already approved this.",
            ApprovalStatus.Declined  => $"{dto.DecidedByName} has already declined this.",
            ApprovalStatus.Cancelled => "The cashier has cancelled this request.",
            _                        => "This request has expired.",
        };
    }

    // ── Completing the sale ───────────────────────────────────────────────────

    // The approved request a sale uses: the same cashier, shop, items, prices and discount
    public async Task<ApprovalRequest> CheckForSaleAsync(int requestId, int cashierId, int storeId,
        IEnumerable<OrderItem> items, decimal discount)
    {
        var request = await _db.ApprovalRequests.AsNoTracking().Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.Id == requestId && r.RequestedByUserId == cashierId)
            ?? throw new ApprovalException("That approval request wasn't found.");
        if (request.Status == ApprovalStatus.Used)
            throw new ApprovalException("That approval has already been used for a sale.");
        if (request.Status != ApprovalStatus.Approved)
            throw new ApprovalException("The discount or price change hasn't been approved.");
        if (request.DecidedAt!.Value.Add(UseWithin) <= DateTime.UtcNow)
            throw new ApprovalException("That approval has run out. Ask a manager again.");

        static string Cart(IEnumerable<(int ProductId, decimal UnitPrice, int Quantity)> lines) => string.Join(";", lines
            .GroupBy(l => (l.ProductId, l.UnitPrice))
            .Select(g => $"{g.Key.ProductId}@{g.Key.UnitPrice:0.00}x{g.Sum(l => l.Quantity)}")
            .Order(StringComparer.Ordinal));
        if (request.StoreId != storeId || request.DiscountAmount != discount
            || Cart(request.Items.Select(i => (i.ProductId, i.UnitPrice, i.Quantity))) != Cart(items.Select(i => (i.ProductId, i.UnitPrice, i.Quantity))))
            throw new ApprovalException("The sale has changed since the manager approved it. Ask again.");
        return request;
    }

    // Runs in the sale's transaction, so a sale that fails leaves the approval to try again,
    // and two sales can't both use it
    public async Task<bool> MarkUsedAsync(int requestId, int orderId) =>
        await _db.ApprovalRequests
            .Where(r => r.Id == requestId && r.Status == ApprovalStatus.Approved)
            .ExecuteUpdateAsync(s => s
                .SetProperty(r => r.Status, ApprovalStatus.Used)
                .SetProperty(r => r.OrderId, orderId)) == 1;

    // Tells the shop's managers, the Admins and the cashier the request's latest state
    public async Task NotifyAsync(int requestId)
    {
        var dto = await LoadAsync(requestId);
        if (dto != null) await SendAsync(NotificationsHub.ApprovalUpdated, dto);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    // An alert that doesn't go out mustn't undo what was saved: the till and the Approvals
    // page also check the API
    private async Task SendAsync(string message, ApprovalRequestDto dto)
    {
        try
        {
            await _hub.Clients.Groups(NotificationsHub.ApproversOf(dto.StoreId), NotificationsHub.Admins).SendAsync(message, dto);
            if (message == NotificationsHub.ApprovalUpdated)
                await _hub.Clients.User(dto.RequestedById.ToString()).SendAsync(message, dto);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Couldn't send the live update for approval request {Id}", dto.Id);
        }
    }

    private IQueryable<ApprovalRequest> WithDetails() => _db.ApprovalRequests.AsNoTracking()
        .Include(r => r.Store).Include(r => r.RequestedBy).Include(r => r.DecidedBy).Include(r => r.Order)
        .Include(r => r.Items)
        .AsSplitQuery();

    private async Task<ApprovalRequestDto?> LoadAsync(int id)
    {
        var request = await WithDetails().FirstOrDefaultAsync(r => r.Id == id);
        return request == null ? null : ToDto(request, DateTime.UtcNow);
    }

    // A waiting request past its time shows as expired (that isn't saved: nothing else changes)
    private static ApprovalRequestDto ToDto(ApprovalRequest r, DateTime now) => new(
        r.Id,
        r.Status == ApprovalStatus.Waiting && r.ExpiresAt <= now ? ApprovalStatus.Expired : r.Status,
        r.StoreId, r.Store.Name,
        r.RequestedByUserId, FullName(r.RequestedBy),
        r.CreatedAt, r.ExpiresAt,
        r.Items.OrderBy(i => i.Id).Select(i => new ApprovalItemDto(
            i.ProductId, i.ProductName, i.ProductSKU, i.Quantity, i.NormalPrice, i.UnitPrice)).ToList(),
        r.Items.Sum(i => i.NormalPrice * i.Quantity),
        r.SubTotal, r.DiscountAmount, r.Total, r.Reason,
        r.DecidedBy != null ? FullName(r.DecidedBy) : null, r.DecidedAt, r.DecisionNote,
        r.OrderId, r.Order?.OrderNumber);

    private static string FullName(User u) => $"{u.FirstName} {u.LastName}".Trim();
}
