namespace TechStore.API.DTOs.Inventory;

public record InventoryDto(
    int Id,
    int ProductId,
    string ProductName,
    string ProductSKU,
    int QuantityOnHand,
    int QuantityReserved,
    int QuantityAvailable,
    int LowStockThreshold,
    bool IsLowStock,
    string? WarehouseLocation,
    DateTime LastRestockedAt,
    DateTime UpdatedAt
);

public record AdjustStockRequest(int Quantity, string Reason, string? Notes = null);
public record RestockRequest(int Quantity, string? Supplier = null, string? Notes = null);
public record UpdateThresholdRequest(int LowStockThreshold);
