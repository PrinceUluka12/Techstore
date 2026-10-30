using TechStore.API.Models;

namespace TechStore.API.DTOs.Pos;

// ── Till requests ─────────────────────────────────────────────────────────────

public record PosSaleRequest(
    List<PosSaleItemRequest> Items,
    List<PosPaymentRequest> Payments,
    int? StoreId = null,                 // defaults to the cashier's home shop
    decimal DiscountAmount = 0,
    string? DiscountReason = null,       // required with a discount or price change
    PosApprovalRequest? Approval = null, // required with a discount or price change
    string? CustomerName = null,
    string? CustomerPhone = null,
    string? CustomerEmail = null,
    string? Notes = null
);

public record PosSaleItemRequest(
    int ProductId,
    int Quantity,
    decimal? UnitPrice = null,           // a different price needs manager approval
    List<string>? SerialNumbers = null   // one IMEI per unit for IMEI-tracked categories
);

// For cash, Amount is what the customer handed over; change is worked out from it
public record PosPaymentRequest(string Method, decimal Amount, string? Reference = null);

public record PosApprovalRequest(int ApproverUserId, string Pin);

public record SetApprovalPinRequest(string CurrentPassword, string Pin);

// ── Till responses ────────────────────────────────────────────────────────────

public record PosProductDto(
    int Id,
    string Name,
    string SKU,
    string? Brand,
    decimal Price,
    string? ImageUrl,
    string CategoryName,
    bool RequiresImei,
    int QuantityAvailable // at the selected shop
);

public record PosApproverDto(int Id, string Name, string Role, bool HasPin);

// Everything a receipt needs
public record PosSaleDto(
    int Id,
    string OrderNumber,
    DateTime CreatedAt,
    OrderStatus Status,
    PosShopDto Shop,
    string CashierName,
    string? CustomerName,
    string? CustomerPhone,
    string? CustomerEmail,
    List<PosSaleItemDto> Items,
    decimal SubTotal,
    decimal DiscountAmount,
    string? DiscountReason,
    string? DiscountApprovedBy,
    decimal Tax,
    decimal Total,
    List<PosPaymentDto> Payments,
    decimal ChangeGiven,
    string? Notes
);

public record PosShopDto(int Id, string Name, string Code, string? Address, string? City, string? Phone);

public record PosSaleItemDto(
    int ProductId,
    string ProductName,
    string? ProductSKU,
    int Quantity,
    decimal UnitPrice,
    decimal? OriginalUnitPrice,
    decimal LineTotal,
    List<string> SerialNumbers
);

public record PosPaymentDto(string Method, decimal Amount, decimal? Tendered, string? Reference, string? ReceivedBy);

// ── End-of-day cash-up ────────────────────────────────────────────────────────

public record CashUpDto(
    DateOnly Date,                 // a day in Nigeria time (WAT, UTC+1)
    PosShopDto Shop,
    int SalesCount,
    decimal Total,
    decimal DiscountTotal,
    decimal CashTendered,          // handed over by customers
    decimal ChangeGiven,
    List<CashUpMethodDto> ByMethod, // Cash is what stayed in the till (tendered − change)
    List<CashUpCashierDto> ByCashier,
    List<CashUpSaleDto> Sales,
    int CancelledCount             // cancelled sales are left out of the totals
);

public record CashUpMethodDto(string Method, decimal Amount, int Count);

public record CashUpCashierDto(int? CashierId, string CashierName, int SalesCount, decimal Total, List<CashUpMethodDto> ByMethod);

public record CashUpSaleDto(int Id, string OrderNumber, DateTime CreatedAt, string CashierName, decimal Total, string PaymentMethod, OrderStatus Status);
