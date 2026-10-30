using TechStore.API.DTOs.Inventory;
using TechStore.API.Repositories.Interfaces;
using TechStore.API.Services.Interfaces;

namespace TechStore.API.Services;

// Every method takes a shop; a null storeId means the shop that sends online orders,
// which is what the single-shop endpoints did before shops existed
public class InventoryService : IInventoryService
{
    private readonly IInventoryRepository _inventory;
    private readonly IStoreRepository _stores;

    public InventoryService(IInventoryRepository inventory, IStoreRepository stores)
    {
        _inventory = inventory;
        _stores = stores;
    }

    public async Task<IEnumerable<InventoryDto>> GetByStoreAsync(int? storeId, bool lowStockOnly)
    {
        var id = await ResolveStoreAsync(storeId);
        var items = await _inventory.GetByStoreAsync(id, lowStockOnly);
        return items.Select(MapToDto);
    }

    public async Task<InventoryDto?> GetAsync(int? storeId, int productId)
    {
        var inv = await _inventory.GetAsync(await ResolveStoreAsync(storeId), productId);
        return inv == null ? null : MapToDto(inv);
    }

    public async Task<IEnumerable<InventoryDto>> GetLowStockAsync(int? storeId)
    {
        var items = storeId.HasValue
            ? await _inventory.GetByStoreAsync(await ResolveStoreAsync(storeId), lowStockOnly: true)
            : await _inventory.GetLowStockAsync();
        return items.Select(MapToDto);
    }

    public async Task<InventoryDto> AdjustStockAsync(int? storeId, int productId, AdjustStockRequest req)
    {
        var id = await ResolveStoreAsync(storeId);
        await RequireRowAsync(id, productId);
        if (!await _inventory.TryAdjustOnHandAsync(id, productId, req.Quantity)) // negative = removal
            throw new InvalidOperationException("Adjustment would result in negative stock.");
        return await ReloadAsync(id, productId);
    }

    public async Task<InventoryDto> RestockAsync(int? storeId, int productId, RestockRequest req)
    {
        var id = await ResolveStoreAsync(storeId);
        if (!await _inventory.RestockAsync(id, productId, req.Quantity))
            throw new KeyNotFoundException("Inventory record not found.");
        return await ReloadAsync(id, productId);
    }

    public async Task<InventoryDto> UpdateThresholdAsync(int? storeId, int productId, UpdateThresholdRequest req)
    {
        var id = await ResolveStoreAsync(storeId);
        if (!await _inventory.SetThresholdAsync(id, productId, req.LowStockThreshold))
            throw new KeyNotFoundException("Inventory record not found.");
        return await ReloadAsync(id, productId);
    }

    private async Task<int> ResolveStoreAsync(int? storeId)
    {
        if (!storeId.HasValue) return (await _stores.GetOnlineAsync()).Id;
        var store = await _stores.GetByIdAsync(storeId.Value)
            ?? throw new KeyNotFoundException("Shop not found.");
        return store.Id;
    }

    private async Task RequireRowAsync(int storeId, int productId)
    {
        if (await _inventory.GetAsync(storeId, productId) == null)
            throw new KeyNotFoundException("Inventory record not found.");
    }

    private async Task<InventoryDto> ReloadAsync(int storeId, int productId) =>
        MapToDto(await _inventory.GetAsync(storeId, productId)
            ?? throw new KeyNotFoundException("Inventory record not found."));

    private static InventoryDto MapToDto(TechStore.API.Models.Inventory i) => new(
        i.Id, i.ProductId,
        i.Product?.Name ?? "",
        i.Product?.SKU ?? "",
        i.StoreId,
        i.Store?.Name ?? "",
        i.QuantityOnHand,
        i.QuantityReserved,
        i.QuantityAvailable,
        i.LowStockThreshold,
        i.QuantityAvailable <= i.LowStockThreshold,
        i.WarehouseLocation,
        i.LastRestockedAt,
        i.UpdatedAt
    );
}
