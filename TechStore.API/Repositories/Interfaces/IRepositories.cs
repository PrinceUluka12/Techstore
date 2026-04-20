using TechStore.API.Models;
using TechStore.API.DTOs.Product;

namespace TechStore.API.Repositories.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(int id);
    Task<User?> GetByEmailAsync(string email);
    Task<IEnumerable<User>> GetAllAsync(int page = 1, int pageSize = 20);
    Task<User> CreateAsync(User user);
    Task<User> UpdateAsync(User user);
    Task<bool> EmailExistsAsync(string email);
    Task<int> CountAsync();
    Task<int> CountNewThisMonthAsync();
}

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(int id);
    Task<Product?> GetBySkuAsync(string sku);
    Task<(IEnumerable<Product> Items, int Total)> SearchAsync(ProductSearchParams p);
    Task<IEnumerable<Product>> GetFeaturedAsync(int count = 8);
    Task<IEnumerable<Product>> GetByCategoryAsync(int categoryId, int page = 1, int pageSize = 20);
    Task<Product> CreateAsync(Product product);
    Task<Product> UpdateAsync(Product product);
    Task<bool> DeleteAsync(int id);
    Task<int> CountAsync();
    Task<IEnumerable<Category>> GetCategoriesAsync();
    Task<Category?> GetCategoryByIdAsync(int id);
}

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(int id);
    Task<Order?> GetByOrderNumberAsync(string orderNumber);
    Task<IEnumerable<Order>> GetByUserIdAsync(int userId, int page = 1, int pageSize = 10);
    Task<(IEnumerable<Order> Items, int Total)> GetAllAsync(int page = 1, int pageSize = 20, string? status = null);
    Task<Order> CreateAsync(Order order);
    Task<Order> UpdateAsync(Order order);
    Task<int> CountAsync();
    Task<int> CountTodayAsync();
    Task<decimal> GetTotalRevenueAsync();
    Task<decimal> GetMonthRevenueAsync();
}

public interface ICartRepository
{
    Task<Cart?> GetByUserIdAsync(int userId);
    Task<Cart> GetOrCreateAsync(int userId);
    Task<Cart> UpdateAsync(Cart cart);
    Task ClearAsync(int userId);
}

public interface IInventoryRepository
{
    Task<Inventory?> GetByProductIdAsync(int productId);
    Task<IEnumerable<Inventory>> GetLowStockAsync();
    Task<Inventory> CreateAsync(Inventory inventory);
    Task<Inventory> UpdateAsync(Inventory inventory);
    Task<int> CountLowStockAsync();
}

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByTokenAsync(string token);
    Task<RefreshToken> CreateAsync(RefreshToken token);
    Task RevokeAsync(RefreshToken token, string? replacedByToken = null);
    Task RevokeAllUserTokensAsync(int userId);
    Task RemoveExpiredTokensAsync(int retentionDays = 14);
}

public interface ICouponRepository
{
    Task<Coupon?> GetByIdAsync(int id);
    Task<Coupon?> GetByCodeAsync(string code);
    Task<(IEnumerable<Coupon> Items, int Total)> GetAllAsync(int page = 1, int pageSize = 20, bool? isActive = null);
    Task<Coupon> CreateAsync(Coupon coupon);
    Task<Coupon> UpdateAsync(Coupon coupon);
    Task<bool> DeleteAsync(int id);
    Task<bool> CodeExistsAsync(string code, int? excludeId = null);
    Task IncrementUsageAsync(int couponId);
    Task<int> CountAsync();
}
