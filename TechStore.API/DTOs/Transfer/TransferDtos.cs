using TechStore.API.Models;

namespace TechStore.API.DTOs.Transfer;

public record CreateTransferRequest(
    int ToStoreId,
    List<TransferItemRequest> Items,
    int? FromStoreId = null, // defaults to the sender's home shop
    string? Notes = null
);

public record TransferItemRequest(int ProductId, int Quantity);

public record TransferDto(
    int Id,
    string TransferNumber,
    TransferStatus Status,
    TransferShopDto FromStore,
    TransferShopDto ToStore,
    string? Notes,
    string CreatedBy,
    DateTime CreatedAt,
    string? CompletedBy,
    DateTime? CompletedAt,
    List<TransferItemDto> Items,
    int TotalUnits
);

public record TransferShopDto(int Id, string Name, string Code);

public record TransferItemDto(int ProductId, string ProductName, string? ProductSKU, int Quantity);
