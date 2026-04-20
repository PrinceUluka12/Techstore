using Microsoft.EntityFrameworkCore;
using TechStore.API.Data;
using TechStore.API.DTOs.Product;
using TechStore.API.Models;
using TechStore.API.Repositories.Interfaces;

namespace TechStore.API.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _db;
    public UserRepository(AppDbContext db) => _db = db;

    public Task<User?> GetByIdAsync(int id) =>
        _db.Users.FirstOrDefaultAsync(u => u.Id == id);

    public Task<User?> GetByEmailAsync(string email) =>
        _db.Users.FirstOrDefaultAsync(u => u.Email == email.ToLower());

    public async Task<IEnumerable<User>> GetAllAsync(int page, int pageSize) =>
        await _db.Users.OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

    public async Task<User> CreateAsync(User user)
    {
        user.Email = user.Email.ToLower();
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return user;
    }

    public async Task<User> UpdateAsync(User user)
    {
        user.UpdatedAt = DateTime.UtcNow;
        _db.Users.Update(user);
        await _db.SaveChangesAsync();
        return user;
    }

    public Task<bool> EmailExistsAsync(string email) =>
        _db.Users.AnyAsync(u => u.Email == email.ToLower());

    public Task<int> CountAsync() => _db.Users.CountAsync();

    public Task<int> CountNewThisMonthAsync()
    {
        var start = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        return _db.Users.CountAsync(u => u.CreatedAt >= start);
    }
}

public class ProductRepository : IProductRepository
{
    private readonly AppDbContext _db;
    public ProductRepository(AppDbContext db) => _db = db;

    public Task<Product?> GetByIdAsync(int id) =>
        _db.Products.Include(p => p.Category).Include(p => p.Inventory)
            .FirstOrDefaultAsync(p => p.Id == id);

    public Task<Product?> GetBySkuAsync(string sku) =>
        _db.Products.Include(p => p.Category).Include(p => p.Inventory)
            .FirstOrDefaultAsync(p => p.SKU == sku);

    public async Task<(IEnumerable<Product> Items, int Total)> SearchAsync(ProductSearchParams s)
    {
        var q = _db.Products.Include(p => p.Category).Include(p => p.Inventory).AsQueryable();

        if (!string.IsNullOrWhiteSpace(s.Query))
            q = q.Where(p => p.Name.Contains(s.Query) || (p.Description != null && p.Description.Contains(s.Query))
                || (p.Brand != null && p.Brand.Contains(s.Query)) || p.SKU.Contains(s.Query));

        if (s.CategoryId.HasValue) q = q.Where(p => p.CategoryId == s.CategoryId);
        if (s.MinPrice.HasValue) q = q.Where(p => p.Price >= s.MinPrice);
        if (s.MaxPrice.HasValue) q = q.Where(p => p.Price <= s.MaxPrice);
        if (s.Brand != null) q = q.Where(p => p.Brand == s.Brand);
        if (s.InStock == true) q = q.Where(p => p.Inventory != null && p.Inventory.QuantityOnHand - p.Inventory.QuantityReserved > 0);

        q = s.SortBy.ToLower() switch
        {
            "price" => s.SortDir == "desc" ? q.OrderByDescending(p => p.Price) : q.OrderBy(p => p.Price),
            "rating" => q.OrderByDescending(p => p.Rating),
            "newest" => q.OrderByDescending(p => p.CreatedAt),
            _ => s.SortDir == "desc" ? q.OrderByDescending(p => p.Name) : q.OrderBy(p => p.Name)
        };

        var total = await q.CountAsync();
        var items = await q.Skip((s.Page - 1) * s.PageSize).Take(s.PageSize).ToListAsync();
        return (items, total);
    }

    public async Task<IEnumerable<Product>> GetFeaturedAsync(int count) =>
        await _db.Products.Include(p => p.Category).Include(p => p.Inventory)
            .Where(p => p.IsFeatured && p.IsActive).Take(count).ToListAsync();

    public async Task<IEnumerable<Product>> GetByCategoryAsync(int categoryId, int page, int pageSize) =>
        await _db.Products.Include(p => p.Category).Include(p => p.Inventory)
            .Where(p => p.CategoryId == categoryId && p.IsActive)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

    public async Task<Product> CreateAsync(Product product)
    {
        _db.Products.Add(product);
        await _db.SaveChangesAsync();
        return product;
    }

    public async Task<Product> UpdateAsync(Product product)
    {
        product.UpdatedAt = DateTime.UtcNow;
        _db.Products.Update(product);
        await _db.SaveChangesAsync();
        return product;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var p = await _db.Products.FindAsync(id);
        if (p == null) return false;
        p.IsActive = false;
        await _db.SaveChangesAsync();
        return true;
    }

    public Task<int> CountAsync() => _db.Products.CountAsync(p => p.IsActive);

    public async Task<IEnumerable<Category>> GetCategoriesAsync() =>
        await _db.Categories.Where(c => c.IsActive).ToListAsync();

    public Task<Category?> GetCategoryByIdAsync(int id) =>
        _db.Categories.FirstOrDefaultAsync(c => c.Id == id);
}

public class OrderRepository : IOrderRepository
{
    private readonly AppDbContext _db;
    public OrderRepository(AppDbContext db) => _db = db;

    public Task<Order?> GetByIdAsync(int id) =>
        _db.Orders.Include(o => o.User).Include(o => o.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.Id == id);

    public Task<Order?> GetByOrderNumberAsync(string num) =>
        _db.Orders.Include(o => o.User).Include(o => o.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.OrderNumber == num);

    public async Task<IEnumerable<Order>> GetByUserIdAsync(int userId, int page, int pageSize) =>
        await _db.Orders.Include(o => o.Items)
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

    public async Task<(IEnumerable<Order> Items, int Total)> GetAllAsync(int page, int pageSize, string? status)
    {
        var q = _db.Orders.Include(o => o.User).Include(o => o.Items).AsQueryable();
        if (status != null && Enum.TryParse<OrderStatus>(status, true, out var s))
            q = q.Where(o => o.Status == s);
        q = q.OrderByDescending(o => o.CreatedAt);
        var total = await q.CountAsync();
        var items = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return (items, total);
    }

    public async Task<Order> CreateAsync(Order order)
    {
        _db.Orders.Add(order);
        await _db.SaveChangesAsync();
        return order;
    }

    public async Task<Order> UpdateAsync(Order order)
    {
        order.UpdatedAt = DateTime.UtcNow;
        _db.Orders.Update(order);
        await _db.SaveChangesAsync();
        return order;
    }

    public Task<int> CountAsync() => _db.Orders.CountAsync();
    public Task<int> CountTodayAsync() =>
        _db.Orders.CountAsync(o => o.CreatedAt.Date == DateTime.UtcNow.Date);

    public async Task<decimal> GetTotalRevenueAsync() =>
        await _db.Orders.Where(o => o.PaymentStatus == PaymentStatus.Paid).SumAsync(o => o.Total);

    public async Task<decimal> GetMonthRevenueAsync()
    {
        var start = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        return await _db.Orders.Where(o => o.PaymentStatus == PaymentStatus.Paid && o.CreatedAt >= start).SumAsync(o => o.Total);
    }
}

public class CartRepository : ICartRepository
{
    private readonly AppDbContext _db;
    public CartRepository(AppDbContext db) => _db = db;

    public Task<Cart?> GetByUserIdAsync(int userId) =>
        _db.Carts.Include(c => c.Items).ThenInclude(i => i.Product).ThenInclude(p => p.Inventory)
            .FirstOrDefaultAsync(c => c.UserId == userId);

    public async Task<Cart> GetOrCreateAsync(int userId)
    {
        var cart = await GetByUserIdAsync(userId);
        if (cart != null) return cart;
        cart = new Cart { UserId = userId };
        _db.Carts.Add(cart);
        await _db.SaveChangesAsync();
        return cart;
    }

    public async Task<Cart> UpdateAsync(Cart cart)
    {
        cart.UpdatedAt = DateTime.UtcNow;
        _db.Carts.Update(cart);
        await _db.SaveChangesAsync();
        return await GetByUserIdAsync(cart.UserId) ?? cart;
    }

    public async Task ClearAsync(int userId)
    {
        var cart = await GetByUserIdAsync(userId);
        if (cart == null) return;
        _db.CartItems.RemoveRange(cart.Items);
        await _db.SaveChangesAsync();
    }
}

public class InventoryRepository : IInventoryRepository
{
    private readonly AppDbContext _db;
    public InventoryRepository(AppDbContext db) => _db = db;

    public Task<Inventory?> GetByProductIdAsync(int productId) =>
        _db.Inventories.Include(i => i.Product).FirstOrDefaultAsync(i => i.ProductId == productId);

    public async Task<IEnumerable<Inventory>> GetLowStockAsync() =>
        await _db.Inventories.Include(i => i.Product)
            .Where(i => (i.QuantityOnHand - i.QuantityReserved) <= i.LowStockThreshold)
            .ToListAsync();

    public async Task<Inventory> CreateAsync(Inventory inventory)
    {
        _db.Inventories.Add(inventory);
        await _db.SaveChangesAsync();
        return inventory;
    }

    public async Task<Inventory> UpdateAsync(Inventory inventory)
    {
        inventory.UpdatedAt = DateTime.UtcNow;
        _db.Inventories.Update(inventory);
        await _db.SaveChangesAsync();
        return inventory;
    }

    public Task<int> CountLowStockAsync() =>
        _db.Inventories.CountAsync(i => (i.QuantityOnHand - i.QuantityReserved) <= i.LowStockThreshold);
}

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly AppDbContext _db;
    public RefreshTokenRepository(AppDbContext db) => _db = db;

    public Task<RefreshToken?> GetByTokenAsync(string token) =>
        _db.RefreshTokens.Include(rt => rt.User).FirstOrDefaultAsync(rt => rt.Token == token);

    public async Task<RefreshToken> CreateAsync(RefreshToken token)
    {
        _db.RefreshTokens.Add(token);
        await _db.SaveChangesAsync();
        return token;
    }

    public async Task RevokeAsync(RefreshToken token, string? replacedByToken = null)
    {
        token.RevokedAt = DateTime.UtcNow;
        token.ReplacedByToken = replacedByToken;
        _db.RefreshTokens.Update(token);
        await _db.SaveChangesAsync();
    }

    public async Task RevokeAllUserTokensAsync(int userId)
    {
        var tokens = await _db.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.RevokedAt == null)
            .ToListAsync();

        foreach (var token in tokens)
            token.RevokedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
    }

    public async Task RemoveExpiredTokensAsync(int retentionDays = 14)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);

        var tokensToRemove = await _db.RefreshTokens
            .Where(rt => 
                // Expired tokens that were never revoked
                (rt.ExpiresAt < DateTime.UtcNow && rt.RevokedAt == null) ||
                // Revoked tokens older than retention window
                (rt.RevokedAt != null && rt.RevokedAt < cutoffDate))
            .ToListAsync();

        _db.RefreshTokens.RemoveRange(tokensToRemove);
        await _db.SaveChangesAsync();
    }
}

public class ReviewRepository : IReviewRepository
{
    private readonly AppDbContext _db;
    public ReviewRepository(AppDbContext db) => _db = db;

    public Task<Review?> GetByIdAsync(int id) =>
        _db.Reviews.Include(r => r.Product).Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == id);

    public Task<Review?> GetByUserAndProductAsync(int userId, int productId) =>
        _db.Reviews.FirstOrDefaultAsync(r => r.UserId == userId && r.ProductId == productId);

    public async Task<(IEnumerable<Review> Items, int Total)> GetByProductIdAsync(int productId, int page, int pageSize, bool? isApproved = null)
    {
        var q = _db.Reviews.Include(r => r.User)
            .Where(r => r.ProductId == productId);

        if (isApproved.HasValue)
            q = q.Where(r => r.IsApproved == isApproved.Value);

        q = q.OrderByDescending(r => r.CreatedAt);
        var total = await q.CountAsync();
        var items = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return (items, total);
    }

    public async Task<(IEnumerable<Review> Items, int Total)> GetAllAsync(int page, int pageSize, bool? isApproved = null)
    {
        var q = _db.Reviews.Include(r => r.User).Include(r => r.Product).AsQueryable();

        if (isApproved.HasValue)
            q = q.Where(r => r.IsApproved == isApproved.Value);

        q = q.OrderByDescending(r => r.CreatedAt);
        var total = await q.CountAsync();
        var items = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return (items, total);
    }

    public async Task<Review> CreateAsync(Review review)
    {
        _db.Reviews.Add(review);
        await _db.SaveChangesAsync();
        return review;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var review = await _db.Reviews.FindAsync(id);
        if (review == null) return false;
        _db.Reviews.Remove(review);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> SetApprovalAsync(int id, bool isApproved)
    {
        var review = await _db.Reviews.FindAsync(id);
        if (review == null) return false;
        review.IsApproved = isApproved;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<(double AverageRating, int Count)> GetApprovedProductStatsAsync(int productId)
    {
        var stats = await _db.Reviews
            .Where(r => r.ProductId == productId && r.IsApproved)
            .GroupBy(r => r.ProductId)
            .Select(g => new { Avg = g.Average(r => r.Rating), Count = g.Count() })
            .FirstOrDefaultAsync();

        return stats == null ? (0.0, 0) : (stats.Avg, stats.Count);
    }

    public async Task<Dictionary<int, int>> GetRatingDistributionAsync(int productId)
    {
        return await _db.Reviews
            .Where(r => r.ProductId == productId && r.IsApproved)
            .GroupBy(r => r.Rating)
            .Select(g => new { Rating = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.Rating, g => g.Count);
    }

    public async Task<bool> HasUserPurchasedProductAsync(int userId, int productId)
    {
        return await _db.OrderItems
            .Include(oi => oi.Order)
            .AnyAsync(oi => oi.ProductId == productId && oi.Order.UserId == userId && oi.Order.Status == OrderStatus.Delivered);
    }
}
