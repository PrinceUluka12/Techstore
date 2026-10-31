namespace TechStore.API.Models;

// A bank account customers pay into for online orders. Admins manage these; every change
// is recorded in BankAccountLog.
public class BankAccount
{
    public int Id { get; set; }
    public string BankName { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty; // 10-digit NUBAN
    public bool IsActive { get; set; } = true;               // shown at checkout
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

// History of changes to the bank accounts: only ever added to, never edited or deleted
public class BankAccountLog
{
    public int Id { get; set; }
    public int BankAccountId { get; set; }              // no foreign key, so the history outlives a removed account
    public string Action { get; set; } = string.Empty;  // Added, Changed, Removed
    public string Details { get; set; } = string.Empty;
    public int? ChangedByUserId { get; set; }
    public string ChangedByName { get; set; } = string.Empty;  // as they were at the time
    public string ChangedByEmail { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
}
