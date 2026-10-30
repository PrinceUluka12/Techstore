namespace TechStore.API.DTOs.Store;

public record StoreDto(
    int Id,
    string Name,
    string Code,
    string? Address,
    string? City,
    string? Phone,
    bool IsActive,
    bool SendsOnlineOrders,
    DateTime CreatedAt
);

public record CreateStoreRequest(
    string Name,
    string Code,
    string? Address = null,
    string? City = null,
    string? Phone = null,
    bool SendsOnlineOrders = false
);

public record UpdateStoreRequest(
    string? Name = null,
    string? Code = null,
    string? Address = null,
    string? City = null,
    string? Phone = null,
    bool? IsActive = null,
    bool? SendsOnlineOrders = null
);
