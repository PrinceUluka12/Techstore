using TechStore.API.DTOs.Wishlist;
using TechStore.API.Models;
using TechStore.API.Repositories.Interfaces;
using TechStore.API.Services.Interfaces;

namespace TechStore.API.Services;

public class WishlistService : IWishlistService
{
    private readonly IWishlistRepository _wishlist;
    private readonly IProductRepository _products;

    public WishlistService(IWishlistRepository wishlist, IProductRepository products)
    {
        _wishlist = wishlist;
        _products = products;
    }

    public async Task<IEnumerable<WishlistItemDto>> GetAsync(int userId)
    {
        var items = await _wishlist.GetByUserIdAsync(userId);
        return items.Select(Map);
    }

    public async Task<WishlistItemDto> AddAsync(int userId, int productId)
    {
        var product = await _products.GetByIdAsync(productId)
            ?? throw new KeyNotFoundException("Product not found.");

        var existing = await _wishlist.GetAsync(userId, productId);
        if (existing != null) return Map(existing);

        var item = await _wishlist.AddAsync(new Wishlist
        {
            UserId    = userId,
            ProductId = productId,
            AddedAt   = DateTime.UtcNow,
        });

        item.Product = product;
        return Map(item);
    }

    public Task<bool> RemoveAsync(int userId, int productId) =>
        _wishlist.RemoveAsync(userId, productId);

    public Task<bool> IsInWishlistAsync(int userId, int productId) =>
        _wishlist.ExistsAsync(userId, productId);

    private static WishlistItemDto Map(Wishlist w) => new(
        w.Id, w.ProductId,
        w.Product?.Name ?? "",
        w.Product?.ImageUrl,
        w.Product?.Price ?? 0,
        w.AddedAt);
}
