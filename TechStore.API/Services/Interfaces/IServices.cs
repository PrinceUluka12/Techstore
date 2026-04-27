using TechStore.API.DTOs.Admin;
using TechStore.API.DTOs.Auth;
using TechStore.API.DTOs.Cart;
using TechStore.API.DTOs.Inventory;
using TechStore.API.DTOs.Order;
using TechStore.API.DTOs.Product;
using TechStore.API.DTOs.Role;
using TechStore.API.DTOs.Wishlist;
using TechStore.API.Helpers;

namespace TechStore.API.Services.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<AuthResponse> RefreshTokenAsync(string token);
    Task LogoutAsync(int userId, string? refreshToken = null);
    Task LogoutEverywhereAsync(int userId);
    Task ChangePasswordAsync(int userId, string currentPassword, string newPassword);
    Task PromoteToAdminAsync(int targetUserId);
    Task<UserProfileDto?> GetProfileAsync(int userId);
    Task<UserProfileDto> UpdateProfileAsync(int userId, UpdateProfileRequest request);
    Task ForgotPasswordAsync(string email);
    Task ResetPasswordAsync(string token, string newPassword);
    Task<UserProfileDto> CreateStaffAccountAsync(CreateStaffRequest request);
    Task AssignRoleAsync(int targetUserId, string role);
}

public interface IEmailService
{
    Task SendWelcomeAsync(string toEmail, string firstName);
    Task SendOrderConfirmationAsync(string toEmail, string firstName, string orderNumber, decimal total);
    Task SendOrderStatusUpdateAsync(string toEmail, string firstName, string orderNumber, string newStatus);
    Task SendPasswordResetAsync(string toEmail, string firstName, string resetLink);
}

public interface IWishlistService
{
    Task<IEnumerable<WishlistItemDto>> GetAsync(int userId);
    Task<WishlistItemDto> AddAsync(int userId, int productId);
    Task<bool> RemoveAsync(int userId, int productId);
    Task<bool> IsInWishlistAsync(int userId, int productId);
}

public record UpdateProfileRequest(string? FirstName, string? LastName, string? Phone, string? Address);

public interface IProductService
{
    Task<ProductDto?> GetByIdAsync(int id);
    Task<PagedResult<ProductListDto>> SearchAsync(ProductSearchParams p);
    Task<IEnumerable<ProductListDto>> GetFeaturedAsync();
    Task<IEnumerable<ProductListDto>> GetByCategoryAsync(int categoryId, int page, int pageSize);
    Task<ProductDto> CreateAsync(CreateProductRequest request);
    Task<ProductDto?> UpdateAsync(int id, UpdateProductRequest request);
    Task<bool> DeleteAsync(int id);
    Task<IEnumerable<CategoryDto>> GetCategoriesAsync();
}

public interface IOrderService
{
    Task<OrderDto?> GetByIdAsync(int id);
    Task<IEnumerable<OrderSummaryDto>> GetMyOrdersAsync(int userId, int page, int pageSize);
    Task<PagedResult<OrderSummaryDto>> GetAllOrdersAsync(int page, int pageSize, string? status);
    Task<OrderDto> CreateFromCartAsync(int userId, CreateOrderRequest request);
    Task<OrderDto?> UpdateStatusAsync(int id, UpdateOrderStatusRequest request);
    Task<IEnumerable<OrderStatusLogDto>> GetStatusHistoryAsync(int orderId);
}

public interface ICartService
{
    Task<CartDto> GetCartAsync(int userId);
    Task<CartDto> AddItemAsync(int userId, AddToCartRequest request);
    Task<CartDto> UpdateItemAsync(int userId, int cartItemId, UpdateCartItemRequest request);
    Task<CartDto> RemoveItemAsync(int userId, int cartItemId);
    Task ClearCartAsync(int userId);
}

public interface IInventoryService
{
    Task<InventoryDto?> GetByProductIdAsync(int productId);
    Task<IEnumerable<InventoryDto>> GetLowStockAsync();
    Task<InventoryDto> AdjustStockAsync(int productId, AdjustStockRequest request);
    Task<InventoryDto> RestockAsync(int productId, RestockRequest request);
    Task<InventoryDto> UpdateThresholdAsync(int productId, UpdateThresholdRequest request);
}

public interface IAdminService
{
    Task<DashboardStatsDto> GetDashboardStatsAsync();
    Task<SalesReportDto> GetSalesReportAsync(DateTime from, DateTime to);
    Task<PagedResult<UserAdminDto>> GetUsersAsync(int page, int pageSize);
    Task<bool> ToggleUserStatusAsync(int userId);
}

public interface IRoleService
{
    Task<IEnumerable<RoleDto>> GetAllAsync();
    Task<RoleDto?> GetByIdAsync(int id);
    Task<RoleDto> CreateAsync(CreateRoleRequest request);
    Task<RoleDto> UpdateAsync(int id, UpdateRoleRequest request);
    Task DeleteAsync(int id);
    Task<IEnumerable<PermissionInfoDto>> GetAvailablePermissionsAsync();
    Task<IEnumerable<string>> GetPermissionsForRoleAsync(string roleName);
}

public record PagedResult<T>(IEnumerable<T> Items, int Total, int Page, int PageSize)
{
    public int TotalPages => (int)Math.Ceiling((double)Total / PageSize);
    public bool HasNext => Page < TotalPages;
    public bool HasPrev => Page > 1;
}