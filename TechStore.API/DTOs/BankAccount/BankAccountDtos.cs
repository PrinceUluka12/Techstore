namespace TechStore.API.DTOs.BankAccount;

// What customers see at checkout and on an unpaid order
public record PublicBankAccountDto(string BankName, string AccountName, string AccountNumber);

public record BankAccountDto(int Id, string BankName, string AccountName, string AccountNumber, bool IsActive, DateTime UpdatedAt);

public record SaveBankAccountRequest(string BankName, string AccountName, string AccountNumber, bool IsActive = true);

public record BankAccountLogDto(
    int Id,
    int BankAccountId,
    string Action,
    string Details,
    string ChangedByName,
    string ChangedByEmail,
    DateTime ChangedAt
);
