using TechStore.API.DTOs.Store;
using TechStore.API.Models;
using TechStore.API.Repositories.Interfaces;
using TechStore.API.Services.Interfaces;

namespace TechStore.API.Services;

public class StoreService : IStoreService
{
    private readonly IStoreRepository _stores;
    private readonly IInventoryRepository _inventory;

    public StoreService(IStoreRepository stores, IInventoryRepository inventory)
    {
        _stores = stores;
        _inventory = inventory;
    }

    public async Task<IEnumerable<StoreDto>> GetAllAsync() =>
        (await _stores.GetAllAsync()).Select(MapToDto);

    public async Task<StoreDto> CreateAsync(CreateStoreRequest req)
    {
        var code = req.Code.Trim().ToUpperInvariant();
        if (await _stores.CodeExistsAsync(code))
            throw new InvalidOperationException($"A shop with code '{code}' already exists.");

        var online = await _stores.GetOnlineAsync();
        var store = await _stores.CreateAsync(new Store
        {
            Name    = req.Name.Trim(),
            Code    = code,
            Address = req.Address?.Trim(),
            City    = req.City?.Trim(),
            Phone   = req.Phone?.Trim(),
        });

        // Every product gets a stock row here, starting at 0 with the main shop's thresholds
        await _inventory.CreateRowsForStoreAsync(store.Id, online.Id);

        if (req.SendsOnlineOrders) await MakeOnlineShopAsync(store);
        return MapToDto(store);
    }

    public async Task<StoreDto> UpdateAsync(int id, UpdateStoreRequest req)
    {
        var store = await _stores.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("Shop not found.");

        if (req.Code != null)
        {
            var code = req.Code.Trim().ToUpperInvariant();
            if (await _stores.CodeExistsAsync(code, id))
                throw new InvalidOperationException($"A shop with code '{code}' already exists.");
            store.Code = code;
        }
        if (req.Name    != null) store.Name    = req.Name.Trim();
        if (req.Address != null) store.Address = req.Address.Trim();
        if (req.City    != null) store.City    = req.City.Trim();
        if (req.Phone   != null) store.Phone   = req.Phone.Trim();

        // The website must always have a shop to sell from
        if (req.SendsOnlineOrders == false && store.SendsOnlineOrders)
            throw new InvalidOperationException("Choose another shop to send online orders first.");
        if (req.IsActive == false && (store.SendsOnlineOrders || req.SendsOnlineOrders == true))
            throw new InvalidOperationException("The shop that sends online orders can't be deactivated.");
        if (req.IsActive != null) store.IsActive = req.IsActive.Value;

        store.UpdatedAt = DateTime.UtcNow;
        await _stores.SaveAsync();

        if (req.SendsOnlineOrders == true && !store.SendsOnlineOrders) await MakeOnlineShopAsync(store);
        return MapToDto(store);
    }

    // Exactly one shop sends online orders. Orders placed before the switch keep their
    // StoreId, so cancelling or delivering them still updates the shop that reserved them.
    private async Task MakeOnlineShopAsync(Store store)
    {
        if (!store.IsActive)
            throw new InvalidOperationException("Activate the shop before it can send online orders.");
        foreach (var s in await _stores.GetAllAsync())
            s.SendsOnlineOrders = s.Id == store.Id;
        await _stores.SaveAsync();
    }

    private static StoreDto MapToDto(Store s) => new(
        s.Id, s.Name, s.Code, s.Address, s.City, s.Phone, s.IsActive, s.SendsOnlineOrders, s.CreatedAt);
}
