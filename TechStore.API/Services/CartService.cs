using TechStore.API.DTOs.Cart;
using TechStore.API.Models;
using TechStore.API.Repositories.Interfaces;
using TechStore.API.Services.Interfaces;

namespace TechStore.API.Services;

public class CartService : ICartService
{
    private readonly ICartRepository _carts;
    private readonly IProductRepository _products;

    public CartService(ICartRepository carts, IProductRepository products)
    {
        _carts = carts;
        _products = products;
    }

    public async Task<CartDto> GetCartAsync(int userId)
    {
        var cart = await _carts.GetOrCreateAsync(userId);
        return MapToDto(cart);
    }

    public async Task<CartDto> AddItemAsync(int userId, AddToCartRequest req)
    {
        var product = await _products.GetByIdAsync(req.ProductId)
            ?? throw new KeyNotFoundException("Product not found.");

        if (!product.IsActive)
            throw new InvalidOperationException("Product is not available.");

        var available = product.OnlineInventory?.QuantityAvailable ?? 0;
        if (available < req.Quantity)
            throw new InvalidOperationException($"Only {available} units available.");

        var cart = await _carts.GetOrCreateAsync(userId);
        var existing = cart.Items.FirstOrDefault(i => i.ProductId == req.ProductId);

        if (existing != null)
        {
            var newQty = existing.Quantity + req.Quantity;
            if (newQty > available)
                throw new InvalidOperationException($"Only {available} units available.");
            existing.Quantity = newQty;
        }
        else
        {
            cart.Items.Add(new CartItem
            {
                CartId = cart.Id,
                ProductId = req.ProductId,
                Quantity = req.Quantity
            });
        }

        await _carts.UpdateAsync(cart);
        return MapToDto(await _carts.GetOrCreateAsync(userId));
    }

    public async Task<CartDto> UpdateItemAsync(int userId, int cartItemId, UpdateCartItemRequest req)
    {
        var cart = await _carts.GetOrCreateAsync(userId);
        var item = cart.Items.FirstOrDefault(i => i.Id == cartItemId)
            ?? throw new KeyNotFoundException("Cart item not found.");

        if (req.Quantity <= 0)
        {
            cart.Items.Remove(item);
        }
        else
        {
            var available = item.Product?.OnlineInventory?.QuantityAvailable ?? int.MaxValue;
            if (req.Quantity > available)
                throw new InvalidOperationException($"Only {available} units available.");
            item.Quantity = req.Quantity;
        }

        await _carts.UpdateAsync(cart);
        return MapToDto(await _carts.GetOrCreateAsync(userId));
    }

    public async Task<CartDto> RemoveItemAsync(int userId, int cartItemId)
    {
        var cart = await _carts.GetOrCreateAsync(userId);
        var item = cart.Items.FirstOrDefault(i => i.Id == cartItemId)
            ?? throw new KeyNotFoundException("Cart item not found.");

        cart.Items.Remove(item);
        await _carts.UpdateAsync(cart);
        return MapToDto(await _carts.GetOrCreateAsync(userId));
    }

    public Task ClearCartAsync(int userId) => _carts.ClearAsync(userId);

    // A signed-out shopper's cart, priced from the catalogue in the order it was filled.
    // Products that have gone or been switched off are left out; the browser drops them too.
    public async Task<CartDto> PreviewAsync(IEnumerable<CartLine> lines)
    {
        var wanted = lines.Where(l => l.Quantity > 0).GroupBy(l => l.ProductId)
            .ToDictionary(g => g.Key, g => g.Sum(l => l.Quantity));
        var products = (await _products.GetByIdsAsync(wanted.Keys))
            .Where(p => p.IsActive).ToDictionary(p => p.Id);
        var items = wanted.Where(w => products.ContainsKey(w.Key)).Select(w =>
        {
            var p = products[w.Key];
            return new CartItemDto(p.Id, p.Id, p.Name, p.ImageUrl, p.Price, w.Value, p.Price * w.Value,
                p.OnlineInventory?.QuantityAvailable);
        }).ToList();
        return new CartDto(0, 0, items, items.Sum(i => i.LineTotal), items.Sum(i => i.Quantity), DateTime.UtcNow);
    }

    private static CartDto MapToDto(Cart cart)
    {
        var items = cart.Items.Select(i => new CartItemDto(
            i.Id,
            i.ProductId,
            i.Product?.Name ?? "",
            i.Product?.ImageUrl,
            i.Product?.Price ?? 0,
            i.Quantity,
            (i.Product?.Price ?? 0) * i.Quantity,
            i.Product?.OnlineInventory?.QuantityAvailable
        )).ToList();

        return new CartDto(
            cart.Id,
            cart.UserId,
            items,
            items.Sum(i => i.LineTotal),
            items.Sum(i => i.Quantity),
            cart.UpdatedAt
        );
    }
}
