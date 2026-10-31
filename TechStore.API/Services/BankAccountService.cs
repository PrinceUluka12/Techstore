using Microsoft.EntityFrameworkCore;
using TechStore.API.Data;
using TechStore.API.DTOs.BankAccount;
using TechStore.API.Models;
using TechStore.API.Services.Interfaces;

namespace TechStore.API.Services;

// Bank accounts customers pay into for online orders. Each change is saved in the same
// transaction as its history entry, so the history can't miss one.
public class BankAccountService : IBankAccountService
{
    private readonly AppDbContext _db;

    public BankAccountService(AppDbContext db) => _db = db;

    public Task<List<PublicBankAccountDto>> GetActiveAsync() =>
        _db.BankAccounts.AsNoTracking()
            .Where(b => b.IsActive)
            .OrderBy(b => b.Id)
            .Select(b => new PublicBankAccountDto(b.BankName, b.AccountName, b.AccountNumber))
            .ToListAsync();

    public Task<List<BankAccountDto>> GetAllAsync() =>
        _db.BankAccounts.AsNoTracking()
            .OrderBy(b => b.Id)
            .Select(b => new BankAccountDto(b.Id, b.BankName, b.AccountName, b.AccountNumber, b.IsActive, b.UpdatedAt))
            .ToListAsync();

    public async Task<BankAccountDto> CreateAsync(int userId, SaveBankAccountRequest req)
    {
        var account = new BankAccount
        {
            BankName      = req.BankName.Trim(),
            AccountName   = req.AccountName.Trim(),
            AccountNumber = req.AccountNumber.Trim(),
            IsActive      = req.IsActive,
        };
        await EnsureNotListedAsync(account, exceptId: null);

        // The account needs its Id before the history entry can point at it
        await InTransactionAsync(async () =>
        {
            _db.BankAccounts.Add(account);
            await _db.SaveChangesAsync();
            _db.BankAccountLogs.Add(await LogAsync(userId, account.Id, "Added",
                $"Added {Describe(account)}{(account.IsActive ? "" : " (hidden from checkout)")}"));
            await _db.SaveChangesAsync();
        });
        return ToDto(account);
    }

    public async Task<BankAccountDto?> UpdateAsync(int userId, int id, SaveBankAccountRequest req)
    {
        var account = await _db.BankAccounts.FirstOrDefaultAsync(b => b.Id == id);
        if (account == null) return null;

        var bank = req.BankName.Trim();
        var name = req.AccountName.Trim();
        var number = req.AccountNumber.Trim();

        var changes = new List<string>();
        void Compare(string label, string before, string after)
        {
            if (before != after) changes.Add($"{label} {before} → {after}");
        }
        Compare("bank", account.BankName, bank);
        Compare("account name", account.AccountName, name);
        Compare("account number", account.AccountNumber, number);
        if (account.IsActive != req.IsActive)
            changes.Add(req.IsActive ? "now shown at checkout" : "hidden from checkout");
        if (changes.Count == 0) return ToDto(account);

        var before = Describe(account);
        account.BankName      = bank;
        account.AccountName   = name;
        account.AccountNumber = number;
        account.IsActive      = req.IsActive;
        account.UpdatedAt     = DateTime.UtcNow;
        await EnsureNotListedAsync(account, exceptId: account.Id);

        _db.BankAccountLogs.Add(await LogAsync(userId, account.Id, "Changed", $"{before}: {string.Join("; ", changes)}"));
        await _db.SaveChangesAsync();
        return ToDto(account);
    }

    public async Task<bool> DeleteAsync(int userId, int id)
    {
        var account = await _db.BankAccounts.FirstOrDefaultAsync(b => b.Id == id);
        if (account == null) return false;

        _db.BankAccounts.Remove(account);
        _db.BankAccountLogs.Add(await LogAsync(userId, account.Id, "Removed", $"Removed {Describe(account)}"));
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<PagedResult<BankAccountLogDto>> GetHistoryAsync(int page, int pageSize)
    {
        var q = _db.BankAccountLogs.AsNoTracking();
        var total = await q.CountAsync();
        var items = await q
            .OrderByDescending(l => l.ChangedAt).ThenByDescending(l => l.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(l => new BankAccountLogDto(l.Id, l.BankAccountId, l.Action, l.Details, l.ChangedByName, l.ChangedByEmail, l.ChangedAt))
            .ToListAsync();
        return new PagedResult<BankAccountLogDto>(items, total, page, pageSize);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task EnsureNotListedAsync(BankAccount account, int? exceptId)
    {
        var listed = await _db.BankAccounts.AnyAsync(b => b.Id != exceptId
            && b.AccountNumber == account.AccountNumber && b.BankName == account.BankName);
        if (listed)
            throw new InvalidOperationException($"{account.BankName} {account.AccountNumber} is already listed.");
    }

    // Who made the change is copied in, so the history still reads right if their name changes
    private async Task<BankAccountLog> LogAsync(int userId, int accountId, string action, string details)
    {
        var user = await _db.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new { u.FirstName, u.LastName, u.Email })
            .FirstOrDefaultAsync();
        return new BankAccountLog
        {
            BankAccountId   = accountId,
            Action          = action,
            Details         = details,
            ChangedByUserId = userId,
            ChangedByName   = user == null ? "Unknown" : $"{user.FirstName} {user.LastName}".Trim(),
            ChangedByEmail  = user?.Email ?? "",
        };
    }

    private static string Describe(BankAccount b) => $"{b.BankName} {b.AccountNumber} ({b.AccountName})";

    private static BankAccountDto ToDto(BankAccount b) =>
        new(b.Id, b.BankName, b.AccountName, b.AccountNumber, b.IsActive, b.UpdatedAt);

    private Task InTransactionAsync(Func<Task> work) =>
        _db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync();
            await work();
            await tx.CommitAsync();
        });
}
