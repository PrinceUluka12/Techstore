using Microsoft.EntityFrameworkCore;
using TechStore.API.Data;
using TechStore.API.DTOs.Transfer;
using TechStore.API.Models;
using TechStore.API.Repositories.Interfaces;
using TechStore.API.Services.Interfaces;

namespace TechStore.API.Services;

// Moving stock between shops. Sending takes the units off the sending shop at once;
// receiving adds them to the other shop; cancelling while in transit gives them back.
// Admins can work with any shop; everyone else sends from and receives at their home shop.
public class TransferService : ITransferService
{
    private readonly AppDbContext _db;
    private readonly IInventoryRepository _inventory;
    private readonly ILogger<TransferService> _logger;

    public TransferService(AppDbContext db, IInventoryRepository inventory, ILogger<TransferService> logger)
    {
        _db = db;
        _inventory = inventory;
        _logger = logger;
    }

    public async Task<IEnumerable<TransferDto>> ListAsync(int userId, bool isAdmin, int? storeId, TransferStatus? status)
    {
        var q = _db.Transfers().AsNoTracking();
        if (!isAdmin)
        {
            var home = await HomeStoreAsync(userId);
            if (storeId.HasValue && storeId != home)
                throw new PosForbiddenException("You can only see your home shop's transfers.");
            q = q.Where(t => t.FromStoreId == home || t.ToStoreId == home);
        }
        if (storeId.HasValue) q = q.Where(t => t.FromStoreId == storeId || t.ToStoreId == storeId);
        if (status.HasValue)  q = q.Where(t => t.Status == status);

        var transfers = await q.OrderByDescending(t => t.CreatedAt).Take(200).ToListAsync();
        return transfers.Select(MapToDto);
    }

    public async Task<TransferDto?> GetAsync(int userId, bool isAdmin, int id)
    {
        var t = await _db.Transfers().AsNoTracking().FirstOrDefaultAsync(t => t.Id == id);
        if (t == null) return null;
        if (!isAdmin)
        {
            var home = await HomeStoreAsync(userId);
            if (t.FromStoreId != home && t.ToStoreId != home)
                throw new PosForbiddenException("This transfer doesn't involve your shop.");
        }
        return MapToDto(t);
    }

    public async Task<TransferDto> CreateAsync(int userId, bool isAdmin, CreateTransferRequest req)
    {
        var home = await HomeStoreAsync(userId);
        var fromId = req.FromStoreId ?? home
            ?? throw new InvalidOperationException("You don't have a home shop yet. An admin can set one on the Customers page.");
        if (!isAdmin && fromId != home)
            throw new PosForbiddenException("You can only send stock from your home shop.");
        if (fromId == req.ToStoreId)
            throw new InvalidOperationException("Choose a different shop to send to.");

        var stores = await _db.Stores.AsNoTracking()
            .Where(s => s.Id == fromId || s.Id == req.ToStoreId).ToDictionaryAsync(s => s.Id);
        var from = stores.GetValueOrDefault(fromId) ?? throw new KeyNotFoundException("Sending shop not found.");
        var to   = stores.GetValueOrDefault(req.ToStoreId) ?? throw new KeyNotFoundException("Receiving shop not found.");
        if (!from.IsActive || !to.IsActive)
            throw new InvalidOperationException("Both shops must be active.");

        // The same product twice becomes one line
        var lines = req.Items.GroupBy(i => i.ProductId)
            .Select(g => (ProductId: g.Key, Quantity: g.Sum(i => i.Quantity))).ToList();
        var ids = lines.Select(l => l.ProductId).ToList();
        var products = await _db.Products.AsNoTracking().Where(p => ids.Contains(p.Id)).ToDictionaryAsync(p => p.Id);
        var missing = ids.FirstOrDefault(id => !products.ContainsKey(id));
        if (missing != 0) throw new KeyNotFoundException($"Product {missing} not found.");

        var now = DateTime.UtcNow;
        var transfer = new StockTransfer
        {
            TransferNumber  = $"TRF-{from.Code}-{now:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..5].ToUpper()}",
            FromStoreId     = from.Id,
            ToStoreId       = to.Id,
            Notes           = string.IsNullOrWhiteSpace(req.Notes) ? null : req.Notes.Trim(),
            CreatedByUserId = userId,
            CreatedAt       = now,
            Items = lines.Select(l => new StockTransferItem
            {
                ProductId   = l.ProductId,
                ProductName = products[l.ProductId].Name,
                ProductSKU  = products[l.ProductId].SKU,
                Quantity    = l.Quantity,
            }).ToList(),
        };

        // Same rule as a sale: only free units leave, never ones reserved for online orders
        await InTransactionAsync(async () =>
        {
            _db.StockTransfers.Add(transfer);
            await _db.SaveChangesAsync();
            foreach (var l in lines)
            {
                if (!await _inventory.TrySellAsync(from.Id, l.ProductId, l.Quantity))
                {
                    var inv = await _inventory.GetAsync(from.Id, l.ProductId);
                    throw new InvalidOperationException(
                        $"Only {Math.Max(0, inv?.QuantityAvailable ?? 0)} '{products[l.ProductId].Name}' available at {from.Name}.");
                }
            }
        });

        _logger.LogInformation("Transfer {Number}: {From} → {To} by user {UserId}", transfer.TransferNumber, from.Code, to.Code, userId);
        return (await GetAsync(userId, true, transfer.Id))!;
    }

    public Task<TransferDto> ReceiveAsync(int userId, bool isAdmin, int id) =>
        CompleteAsync(userId, isAdmin, id, TransferStatus.Received);

    public Task<TransferDto> CancelAsync(int userId, bool isAdmin, int id) =>
        CompleteAsync(userId, isAdmin, id, TransferStatus.Cancelled);

    // Receiving puts the units in the receiving shop; cancelling returns them to the sender.
    // The status change is a conditional UPDATE, so a transfer can't be received twice.
    private async Task<TransferDto> CompleteAsync(int userId, bool isAdmin, int id, TransferStatus outcome)
    {
        var t = await _db.Transfers().AsNoTracking().FirstOrDefaultAsync(t => t.Id == id)
            ?? throw new KeyNotFoundException("Transfer not found.");

        if (!isAdmin)
        {
            var home = await HomeStoreAsync(userId);
            if (outcome == TransferStatus.Received && home != t.ToStoreId)
                throw new PosForbiddenException("Only the receiving shop can confirm this transfer.");
            if (outcome == TransferStatus.Cancelled && home != t.FromStoreId)
                throw new PosForbiddenException("Only the sending shop can cancel this transfer.");
        }

        await InTransactionAsync(async () =>
        {
            var now = DateTime.UtcNow;
            var changed = await _db.StockTransfers
                .Where(x => x.Id == id && x.Status == TransferStatus.InTransit)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.Status, outcome)
                    .SetProperty(x => x.CompletedByUserId, userId)
                    .SetProperty(x => x.CompletedAt, now));
            if (changed == 0)
                throw new InvalidOperationException($"This transfer is already {t.Status.ToString().ToLower()}.");

            foreach (var item in t.Items)
            {
                if (outcome == TransferStatus.Received)
                    await _inventory.RestockAsync(t.ToStoreId, item.ProductId, item.Quantity);
                else
                    await _inventory.TryAdjustOnHandAsync(t.FromStoreId, item.ProductId, item.Quantity);
            }
        });

        _logger.LogInformation("Transfer {Number} {Outcome} by user {UserId}", t.TransferNumber, outcome, userId);
        return (await GetAsync(userId, true, id))!;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private Task<int?> HomeStoreAsync(int userId) =>
        _db.Users.Where(u => u.Id == userId).Select(u => u.StoreId).FirstOrDefaultAsync();

    // The retrying execution strategy requires a manual transaction to run inside it
    private Task InTransactionAsync(Func<Task> work) =>
        _db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync();
            await work();
            await tx.CommitAsync();
        });

    private static TransferDto MapToDto(StockTransfer t) => new(
        t.Id, t.TransferNumber, t.Status,
        new TransferShopDto(t.FromStore.Id, t.FromStore.Name, t.FromStore.Code),
        new TransferShopDto(t.ToStore.Id, t.ToStore.Name, t.ToStore.Code),
        t.Notes,
        $"{t.CreatedBy.FirstName} {t.CreatedBy.LastName}".Trim(), t.CreatedAt,
        t.CompletedBy != null ? $"{t.CompletedBy.FirstName} {t.CompletedBy.LastName}".Trim() : null, t.CompletedAt,
        t.Items.Select(i => new TransferItemDto(i.ProductId, i.ProductName, i.ProductSKU, i.Quantity)).ToList(),
        t.Items.Sum(i => i.Quantity));
}

internal static class TransferQueries
{
    public static IQueryable<StockTransfer> Transfers(this AppDbContext db) =>
        db.StockTransfers
            .Include(t => t.FromStore).Include(t => t.ToStore)
            .Include(t => t.CreatedBy).Include(t => t.CompletedBy)
            .Include(t => t.Items)
            .AsSplitQuery();
}
