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

        var available = product.Inventory?.QuantityAvailable ?? 0;
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
            var available = item.Product?.Inventory?.QuantityAvailable ?? int.MaxValue;
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
            i.Product?.Inventory?.QuantityAvailable
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
