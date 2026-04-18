using TechStore.API.DTOs.Inventory;
using TechStore.API.Repositories.Interfaces;
using TechStore.API.Services.Interfaces;

namespace TechStore.API.Services;

public class InventoryService : IInventoryService
{
    private readonly IInventoryRepository _inventory;
    private readonly IProductRepository _products;

    public InventoryService(IInventoryRepository inventory, IProductRepository products)
    {
        _inventory = inventory;
        _products = products;
    }

    public async Task<InventoryDto?> GetByProductIdAsync(int productId)
    {
        var inv = await _inventory.GetByProductIdAsync(productId);
        return inv == null ? null : MapToDto(inv);
    }

    public async Task<IEnumerable<InventoryDto>> GetLowStockAsync()
    {
        var items = await _inventory.GetLowStockAsync();
        return items.Select(MapToDto);
    }

    public async Task<InventoryDto> AdjustStockAsync(int productId, AdjustStockRequest req)
    {
        var inv = await _inventory.GetByProductIdAsync(productId)
            ?? throw new KeyNotFoundException("Inventory record not found.");

        inv.QuantityOnHand += req.Quantity; // negative = removal

        if (inv.QuantityOnHand < 0)
            throw new InvalidOperationException("Adjustment would result in negative stock.");

        await _inventory.UpdateAsync(inv);
        return MapToDto(inv);
    }

    public async Task<InventoryDto> RestockAsync(int productId, RestockRequest req)
    {
        var inv = await _inventory.GetByProductIdAsync(productId)
            ?? throw new KeyNotFoundException("Inventory record not found.");

        inv.QuantityOnHand += req.Quantity;
        inv.LastRestockedAt = DateTime.UtcNow;

        await _inventory.UpdateAsync(inv);
        return MapToDto(inv);
    }

    public async Task<InventoryDto> UpdateThresholdAsync(int productId, UpdateThresholdRequest req)
    {
        var inv = await _inventory.GetByProductIdAsync(productId)
            ?? throw new KeyNotFoundException("Inventory record not found.");

        inv.LowStockThreshold = req.LowStockThreshold;
        await _inventory.UpdateAsync(inv);
        return MapToDto(inv);
    }

    private static InventoryDto MapToDto(TechStore.API.Models.Inventory i) => new(
        i.Id, i.ProductId,
        i.Product?.Name ?? "",
        i.Product?.SKU ?? "",
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
