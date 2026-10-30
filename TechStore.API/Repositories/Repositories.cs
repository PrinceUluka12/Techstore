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

    public Task<User?> GetByResetTokenAsync(string token) =>
        _db.Users.FirstOrDefaultAsync(u => u.PasswordResetToken == token);

    public Task<int> CountNewThisMonthAsync()
    {
        var start = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        return _db.Users.CountAsync(u => u.CreatedAt >= start);
    }
}

public class WishlistRepository : IWishlistRepository
{
    private readonly AppDbContext _db;
    public WishlistRepository(AppDbContext db) => _db = db;

    public async Task<IEnumerable<Wishlist>> GetByUserIdAsync(int userId) =>
        await _db.Wishlists
            .Include(w => w.Product)
            .Where(w => w.UserId == userId)
            .OrderByDescending(w => w.AddedAt)
            .ToListAsync();

    public Task<Wishlist?> GetAsync(int userId, int productId) =>
        _db.Wishlists.FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId);

    public async Task<Wishlist> AddAsync(Wishlist wishlist)
    {
        _db.Wishlists.Add(wishlist);
        await _db.SaveChangesAsync();
        return wishlist;
    }

    public async Task<bool> RemoveAsync(int userId, int productId)
    {
        var item = await GetAsync(userId, productId);
        if (item == null) return false;
        _db.Wishlists.Remove(item);
        await _db.SaveChangesAsync();
        return true;
    }

    public Task<bool> ExistsAsync(int userId, int productId) =>
        _db.Wishlists.AnyAsync(w => w.UserId == userId && w.ProductId == productId);
}

public class ProductRepository : IProductRepository
{
    private readonly AppDbContext _db;
    public ProductRepository(AppDbContext db) => _db = db;

    public Task<Product?> GetByIdAsync(int id) =>
        _db.Products.Include(p => p.Category).IncludeOnlineStock()
            .FirstOrDefaultAsync(p => p.Id == id);

    public Task<Product?> GetBySkuAsync(string sku) =>
        _db.Products.Include(p => p.Category).IncludeOnlineStock()
            .FirstOrDefaultAsync(p => p.SKU == sku);

    public async Task<(IEnumerable<Product> Items, int Total)> SearchAsync(ProductSearchParams s)
    {
        var q = _db.Products.Include(p => p.Category).IncludeOnlineStock().AsQueryable();

        if (!string.IsNullOrWhiteSpace(s.Query))
            q = q.Where(p => p.Name.Contains(s.Query) || (p.Description != null && p.Description.Contains(s.Query))
                || (p.Brand != null && p.Brand.Contains(s.Query)) || p.SKU.Contains(s.Query));

        if (s.CategoryId.HasValue) q = q.Where(p => p.CategoryId == s.CategoryId);
        if (s.MinPrice.HasValue) q = q.Where(p => p.Price >= s.MinPrice);
        if (s.MaxPrice.HasValue) q = q.Where(p => p.Price <= s.MaxPrice);
        if (s.Brand != null) q = q.Where(p => p.Brand == s.Brand);
        if (s.InStock == true) q = q.Where(p => p.Inventories.Any(i => i.Store.SendsOnlineOrders && i.QuantityOnHand - i.QuantityReserved > 0));

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
        await _db.Products.Include(p => p.Category).IncludeOnlineStock()
            .Where(p => p.IsFeatured && p.IsActive).Take(count).ToListAsync();

    public async Task<IEnumerable<Product>> GetByCategoryAsync(int categoryId, int page, int pageSize) =>
        await _db.Products.Include(p => p.Category).IncludeOnlineStock()
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
        // Update() on a tracked product also marks its loaded stock rows as modified and
        // writes their stale counts back; a tracked product only needs SaveChanges
        if (_db.Entry(product).State == EntityState.Detached) _db.Products.Update(product);
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
        _db.Orders.Include(o => o.User).Include(o => o.Store).Include(o => o.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.Id == id);

    public Task<Order?> GetByOrderNumberAsync(string num) =>
        _db.Orders.Include(o => o.User).Include(o => o.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.OrderNumber == num);

    public async Task<IEnumerable<Order>> GetByUserIdAsync(int userId, int page, int pageSize) =>
        await _db.Orders.Include(o => o.Items)
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

    public async Task<(IEnumerable<Order> Items, int Total)> GetAllAsync(
        int page, int pageSize, string? status, SalesChannel? channel = null, int? storeId = null, string? search = null)
    {
        var q = _db.Orders.Include(o => o.User).Include(o => o.Store).Include(o => o.Items).AsQueryable();
        if (status != null && Enum.TryParse<OrderStatus>(status, true, out var s))
            q = q.Where(o => o.Status == s);
        if (channel.HasValue) q = q.Where(o => o.Channel == channel);
        if (storeId.HasValue) q = q.Where(o => o.StoreId == storeId);

        // Order number, customer (account or walk-in) or an IMEI/serial sold on the order
        var term = search?.Trim();
        if (!string.IsNullOrEmpty(term))
        {
            // Phones are saved without spaces; match on the number after its 0 or +234 so
            // "0803 …" and "+234 803 …" find each other
            var digits = new string(term.Where(char.IsAsciiDigit).ToArray());
            if (digits.StartsWith("234") && digits.Length > 10) digits = digits[3..];
            digits = digits.TrimStart('0');
            var phone = digits.Length >= 4 ? digits : null;
            q = q.Where(o => o.OrderNumber.Contains(term)
                || (o.CustomerName != null && o.CustomerName.Contains(term))
                || (phone != null && o.CustomerPhone != null && o.CustomerPhone.Contains(phone))
                || (o.CustomerEmail != null && o.CustomerEmail.Contains(term))
                || (o.User != null && (o.User.Email.Contains(term) || (o.User.FirstName + " " + o.User.LastName).Contains(term)))
                || o.Items.Any(i => i.Serials.Any(sn => sn.SerialNumber.Contains(term))));
        }
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
        // Same as ProductRepository.UpdateAsync: Update() would also rewrite the loaded
        // customer, shop and products with the values read earlier
        if (_db.Entry(order).State == EntityState.Detached) _db.Orders.Update(order);
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
        _db.Carts.Include(c => c.Items).ThenInclude(i => i.Product)
            .ThenInclude(p => p.Inventories.Where(inv => inv.Store.SendsOnlineOrders)).ThenInclude(inv => inv.Store)
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
        // Same as ProductRepository.UpdateAsync: Update() would rewrite the loaded stock rows
        if (_db.Entry(cart).State == EntityState.Detached) _db.Carts.Update(cart);
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

    // Reads are untracked: stock changes go through ExecuteUpdate, which a tracked copy wouldn't see
    private IQueryable<Inventory> Rows() =>
        _db.Inventories.AsNoTracking().Include(i => i.Product).Include(i => i.Store);

    private IQueryable<Inventory> Row(int storeId, int productId) =>
        _db.Inventories.Where(i => i.StoreId == storeId && i.ProductId == productId);

    public Task<Inventory?> GetAsync(int storeId, int productId) =>
        Rows().FirstOrDefaultAsync(i => i.StoreId == storeId && i.ProductId == productId);

    public async Task<IEnumerable<Inventory>> GetByStoreAsync(int storeId, bool lowStockOnly = false) =>
        await Rows()
            .Where(i => i.StoreId == storeId)
            .Where(i => !lowStockOnly || (i.QuantityOnHand - i.QuantityReserved) <= i.LowStockThreshold)
            .OrderBy(i => i.Product.Name)
            .ToListAsync();

    public async Task<IEnumerable<Inventory>> GetLowStockAsync() =>
        await Rows()
            .Where(i => i.Store.IsActive && (i.QuantityOnHand - i.QuantityReserved) <= i.LowStockThreshold)
            .OrderBy(i => i.Store.Name).ThenBy(i => i.Product.Name)
            .ToListAsync();

    public Task<int> CountLowStockAsync() =>
        _db.Inventories.CountAsync(i => i.Store.IsActive && (i.QuantityOnHand - i.QuantityReserved) <= i.LowStockThreshold);

    public async Task CreateManyAsync(IEnumerable<Inventory> rows)
    {
        _db.Inventories.AddRange(rows);
        await _db.SaveChangesAsync();
    }

    public async Task CreateRowsForStoreAsync(int storeId, int copyThresholdsFromStoreId)
    {
        var thresholds = await _db.Inventories.AsNoTracking()
            .Where(i => i.StoreId == copyThresholdsFromStoreId)
            .ToDictionaryAsync(i => i.ProductId, i => i.LowStockThreshold);
        var productIds = await _db.Products.Select(p => p.Id).ToListAsync();

        await CreateManyAsync(productIds.Select(id => new Inventory
        {
            StoreId = storeId,
            ProductId = id,
            LowStockThreshold = thresholds.GetValueOrDefault(id, 10),
        }));
    }

    public async Task<bool> TryReserveAsync(int storeId, int productId, int quantity) =>
        await Row(storeId, productId)
            .Where(i => i.QuantityOnHand - i.QuantityReserved >= quantity)
            .ExecuteUpdateAsync(s => s
                .SetProperty(i => i.QuantityReserved, i => i.QuantityReserved + quantity)
                .SetProperty(i => i.UpdatedAt, DateTime.UtcNow)) == 1;

    // In-store sale: the units leave the shop now, but never ones reserved for online orders
    public async Task<bool> TrySellAsync(int storeId, int productId, int quantity) =>
        await Row(storeId, productId)
            .Where(i => i.QuantityOnHand - i.QuantityReserved >= quantity)
            .ExecuteUpdateAsync(s => s
                .SetProperty(i => i.QuantityOnHand, i => i.QuantityOnHand - quantity)
                .SetProperty(i => i.UpdatedAt, DateTime.UtcNow)) == 1;

    public Task ReleaseAsync(int storeId, int productId, int quantity) =>
        Row(storeId, productId).ExecuteUpdateAsync(s => s
            .SetProperty(i => i.QuantityReserved, i => i.QuantityReserved > quantity ? i.QuantityReserved - quantity : 0)
            .SetProperty(i => i.UpdatedAt, DateTime.UtcNow));

    public Task FulfilAsync(int storeId, int productId, int quantity) =>
        Row(storeId, productId).ExecuteUpdateAsync(s => s
            .SetProperty(i => i.QuantityOnHand, i => i.QuantityOnHand > quantity ? i.QuantityOnHand - quantity : 0)
            .SetProperty(i => i.QuantityReserved, i => i.QuantityReserved > quantity ? i.QuantityReserved - quantity : 0)
            .SetProperty(i => i.UpdatedAt, DateTime.UtcNow));

    public async Task<bool> TryAdjustOnHandAsync(int storeId, int productId, int delta) =>
        await Row(storeId, productId)
            .Where(i => i.QuantityOnHand + delta >= 0)
            .ExecuteUpdateAsync(s => s
                .SetProperty(i => i.QuantityOnHand, i => i.QuantityOnHand + delta)
                .SetProperty(i => i.UpdatedAt, DateTime.UtcNow)) == 1;

    public async Task<bool> RestockAsync(int storeId, int productId, int quantity) =>
        await Row(storeId, productId).ExecuteUpdateAsync(s => s
            .SetProperty(i => i.QuantityOnHand, i => i.QuantityOnHand + quantity)
            .SetProperty(i => i.LastRestockedAt, DateTime.UtcNow)
            .SetProperty(i => i.UpdatedAt, DateTime.UtcNow)) == 1;

    public async Task<bool> SetThresholdAsync(int storeId, int productId, int threshold) =>
        await Row(storeId, productId).ExecuteUpdateAsync(s => s
            .SetProperty(i => i.LowStockThreshold, threshold)
            .SetProperty(i => i.UpdatedAt, DateTime.UtcNow)) == 1;
}

public class StoreRepository : IStoreRepository
{
    private readonly AppDbContext _db;
    public StoreRepository(AppDbContext db) => _db = db;

    public Task<List<Store>> GetAllAsync() =>
        _db.Stores.OrderByDescending(s => s.SendsOnlineOrders).ThenBy(s => s.Name).ToListAsync();

    public Task<Store?> GetByIdAsync(int id) => _db.Stores.FirstOrDefaultAsync(s => s.Id == id);

    public async Task<Store> GetOnlineAsync() =>
        await _db.Stores.FirstOrDefaultAsync(s => s.SendsOnlineOrders)
        ?? throw new InvalidOperationException("No shop is set to send online orders.");

    public Task<bool> CodeExistsAsync(string code, int? excludeId = null) =>
        _db.Stores.AnyAsync(s => s.Code == code && s.Id != excludeId);

    public async Task<Store> CreateAsync(Store store)
    {
        _db.Stores.Add(store);
        await _db.SaveChangesAsync();
        return store;
    }

    public Task SaveAsync() => _db.SaveChangesAsync();
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


public class RoleRepository(AppDbContext db) : IRoleRepository
{
    public Task<IEnumerable<AppRole>> GetAllAsync() =>
        Task.FromResult<IEnumerable<AppRole>>(db.AppRoles.OrderBy(r => r.Name).ToList());

    public Task<AppRole?> GetByIdAsync(int id) =>
        db.AppRoles.FirstOrDefaultAsync(r => r.Id == id);

    public Task<AppRole?> GetByNameAsync(string name) =>
        db.AppRoles.FirstOrDefaultAsync(r => r.Name == name);

    public async Task<AppRole> CreateAsync(AppRole role)
    {
        db.AppRoles.Add(role);
        await db.SaveChangesAsync();
        return role;
    }

    public async Task<AppRole> UpdateAsync(AppRole role)
    {
        db.AppRoles.Update(role);
        await db.SaveChangesAsync();
        return role;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var role = await db.AppRoles.FindAsync(id);
        if (role == null || role.IsSystem) return false;
        db.AppRoles.Remove(role);
        await db.SaveChangesAsync();
        return true;
    }

    public Task<bool> ExistsAsync(string name) =>
        db.AppRoles.AnyAsync(r => r.Name == name);
}
