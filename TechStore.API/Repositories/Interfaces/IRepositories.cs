using TechStore.API.Models;
using TechStore.API.DTOs.Product;

namespace TechStore.API.Repositories.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(int id);
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByResetTokenAsync(string token);
    Task<IEnumerable<User>> GetAllAsync(int page = 1, int pageSize = 20);
    Task<User> CreateAsync(User user);
    Task<User> UpdateAsync(User user);
    Task<bool> EmailExistsAsync(string email);
    Task<int> CountAsync();
    Task<int> CountNewThisMonthAsync();
}

public interface IRoleRepository
{
    Task<IEnumerable<AppRole>> GetAllAsync();
    Task<AppRole?> GetByIdAsync(int id);
    Task<AppRole?> GetByNameAsync(string name);
    Task<AppRole> CreateAsync(AppRole role);
    Task<AppRole> UpdateAsync(AppRole role);
    Task<bool> DeleteAsync(int id);
    Task<bool> ExistsAsync(string name);
}

public interface IWishlistRepository
{
    Task<IEnumerable<Wishlist>> GetByUserIdAsync(int userId);
    Task<Wishlist?> GetAsync(int userId, int productId);
    Task<Wishlist> AddAsync(Wishlist wishlist);
    Task<bool> RemoveAsync(int userId, int productId);
    Task<bool> ExistsAsync(int userId, int productId);
}

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(int id);
    Task<List<Product>> GetByIdsAsync(IEnumerable<int> ids);
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
    Task<Order?> GetByGuestTokenAsync(string token);
    Task<(IEnumerable<Order> Items, int Total)> GetByUserIdAsync(int userId, int page = 1, int pageSize = 10);
    Task<(IEnumerable<Order> Items, int Total)> GetAllAsync(int page = 1, int pageSize = 20, string? status = null,
        SalesChannel? channel = null, int? storeId = null, string? search = null);
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
    Task<Inventory?> GetAsync(int storeId, int productId);
    Task<IEnumerable<Inventory>> GetByStoreAsync(int storeId, bool lowStockOnly = false);
    Task<IEnumerable<Inventory>> GetLowStockAsync(); // across all active shops
    Task<int> CountLowStockAsync();
    Task CreateManyAsync(IEnumerable<Inventory> rows);
    Task CreateRowsForStoreAsync(int storeId, int copyThresholdsFromStoreId);

    // Each of these is a single UPDATE, so sales and restocks happening at the
    // same moment can't overwrite each other's numbers
    Task<bool> TryReserveAsync(int storeId, int productId, int quantity);
    Task<bool> TrySellAsync(int storeId, int productId, int quantity); // in-store sale
    Task ReleaseAsync(int storeId, int productId, int quantity);
    Task FulfilAsync(int storeId, int productId, int quantity); // reserved units leave the shop
    Task<bool> TryAdjustOnHandAsync(int storeId, int productId, int delta);
    Task<bool> RestockAsync(int storeId, int productId, int quantity);
    Task<bool> SetThresholdAsync(int storeId, int productId, int threshold);
}

public interface IStoreRepository
{
    Task<List<Store>> GetAllAsync();
    Task<Store?> GetByIdAsync(int id);
    Task<Store> GetOnlineAsync(); // the shop that sends online orders; there is always one
    Task<bool> CodeExistsAsync(string code, int? excludeId = null);
    Task<Store> CreateAsync(Store store);
    Task SaveAsync();
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
    Task<bool> TryUseAsync(int couponId);
    Task ReleaseUseAsync(string code);
    Task<int> CountUsesByCustomerAsync(string code, int userId);
    Task<int> CountGuestUsesAsync(string code, string email);
    Task<int> CountAsync();
}

public interface IReviewRepository
{
    Task<Review?> GetByIdAsync(int id);
    Task<Review?> GetByUserAndProductAsync(int userId, int productId);
    Task<(IEnumerable<Review> Items, int Total)> GetByProductIdAsync(int productId, int page, int pageSize, bool? isApproved = null);
    Task<(IEnumerable<Review> Items, int Total)> GetAllAsync(int page, int pageSize, bool? isApproved = null);
    Task<Review> CreateAsync(Review review);
    Task<bool> DeleteAsync(int id);
    Task<bool> SetApprovalAsync(int id, bool isApproved);
    Task<(double AverageRating, int Count)> GetApprovedProductStatsAsync(int productId);
    Task<Dictionary<int, int>> GetRatingDistributionAsync(int productId);
    Task<bool> HasUserPurchasedProductAsync(int userId, int productId);
}
