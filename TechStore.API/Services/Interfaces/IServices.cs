using TechStore.API.DTOs.Admin;
using TechStore.API.DTOs.Auth;
using TechStore.API.DTOs.Cart;
using TechStore.API.DTOs.Inventory;
using TechStore.API.DTOs.Order;
using TechStore.API.DTOs.Pos;
using TechStore.API.DTOs.Product;
using TechStore.API.DTOs.Role;
using TechStore.API.DTOs.Store;
using TechStore.API.DTOs.Transfer;
using TechStore.API.Models;
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
    Task AssignStoreAsync(int targetUserId, int? storeId);
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
    Task<PagedResult<OrderSummaryDto>> GetAllOrdersAsync(int page, int pageSize, string? status,
        SalesChannel? channel = null, int? storeId = null, string? search = null);
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
    // storeId null = the shop that sends online orders
    Task<IEnumerable<InventoryDto>> GetByStoreAsync(int? storeId, bool lowStockOnly);
    Task<InventoryDto?> GetAsync(int? storeId, int productId);
    Task<IEnumerable<InventoryDto>> GetLowStockAsync(int? storeId); // null = all shops
    Task<InventoryDto> AdjustStockAsync(int? storeId, int productId, AdjustStockRequest request);
    Task<InventoryDto> RestockAsync(int? storeId, int productId, RestockRequest request);
    Task<InventoryDto> UpdateThresholdAsync(int? storeId, int productId, UpdateThresholdRequest request);
}

public interface IPosService
{
    // isAdmin: admins can work at any shop, everyone else only at their home shop
    Task<IEnumerable<PosProductDto>> SearchProductsAsync(int userId, bool isAdmin, int? storeId, string? query);
    Task<IEnumerable<PosApproverDto>> GetApproversAsync();
    Task<PosSaleDto> CreateSaleAsync(int cashierId, bool isAdmin, PosSaleRequest request);
    Task<PosSaleDto?> GetSaleAsync(int userId, bool isAdmin, int id);
    Task SetApprovalPinAsync(int userId, string currentPassword, string pin);
    Task<CashUpDto> GetCashUpAsync(int userId, bool isAdmin, int? storeId, DateOnly date);
}

public interface ITransferService
{
    // isAdmin: admins can work with any shop, everyone else only their home shop
    Task<IEnumerable<TransferDto>> ListAsync(int userId, bool isAdmin, int? storeId, TransferStatus? status);
    Task<TransferDto?> GetAsync(int userId, bool isAdmin, int id);
    Task<TransferDto> CreateAsync(int userId, bool isAdmin, CreateTransferRequest request);
    Task<TransferDto> ReceiveAsync(int userId, bool isAdmin, int id);
    Task<TransferDto> CancelAsync(int userId, bool isAdmin, int id);
}

public interface IStoreService
{
    Task<IEnumerable<StoreDto>> GetAllAsync();
    Task<StoreDto> CreateAsync(CreateStoreRequest request);
    Task<StoreDto> UpdateAsync(int id, UpdateStoreRequest request);
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