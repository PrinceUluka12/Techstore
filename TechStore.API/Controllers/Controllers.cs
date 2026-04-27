using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TechStore.API.DTOs.Auth;
using TechStore.API.DTOs.Cart;
using TechStore.API.DTOs.Inventory;
using TechStore.API.DTOs.Order;
using TechStore.API.DTOs.Product;
using TechStore.API.DTOs.Role;
using TechStore.API.Helpers;
using TechStore.API.Services.Interfaces;

namespace TechStore.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private const string RefreshTokenCookie = "refreshToken";
    private readonly IAuthService _auth;
    private readonly IConfiguration _config;

    public AuthController(IAuthService auth, IConfiguration config)
    {
        _auth   = auth;
        _config = config;
    }

    /// <summary>Register a new customer account</summary>
    [HttpPost("register")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req)
    {
        var result = await _auth.RegisterAsync(req);
        SetRefreshTokenCookie(result.RefreshToken);
        return Ok(ToClientResponse(result));
    }

    /// <summary>Login and receive access token; refresh token set as HttpOnly cookie</summary>
    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        var result = await _auth.LoginAsync(req);
        SetRefreshTokenCookie(result.RefreshToken);
        return Ok(ToClientResponse(result));
    }

    /// <summary>Issue a new access token using the HttpOnly refresh token cookie</summary>
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh()
    {
        var token = Request.Cookies[RefreshTokenCookie];
        if (string.IsNullOrEmpty(token))
            return Unauthorized(new { message = "No refresh token." });

        var result = await _auth.RefreshTokenAsync(token);
        SetRefreshTokenCookie(result.RefreshToken);
        return Ok(ToClientResponse(result));
    }

    /// <summary>Logout — revokes the current refresh token and clears the cookie</summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var token = Request.Cookies[RefreshTokenCookie];
        await _auth.LogoutAsync(GetUserId(), token);
        ClearRefreshTokenCookie();
        return NoContent();
    }

    /// <summary>Logout from all devices — revokes all refresh tokens for this user</summary>
    [HttpPost("logout-everywhere")]
    [Authorize]
    public async Task<IActionResult> LogoutEverywhere()
    {
        await _auth.LogoutEverywhereAsync(GetUserId());
        ClearRefreshTokenCookie();
        return NoContent();
    }

    /// <summary>Change password — invalidates all other sessions</summary>
    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest req)
    {
        await _auth.ChangePasswordAsync(GetUserId(), req.CurrentPassword, req.NewPassword);
        ClearRefreshTokenCookie();
        return NoContent();
    }

    /// <summary>Get current user profile</summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetProfile()
    {
        var profile = await _auth.GetProfileAsync(GetUserId());
        return profile == null ? NotFound() : Ok(profile);
    }

    /// <summary>Update current user profile</summary>
    [HttpPut("me")]
    [Authorize]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest req)
    {
        var result = await _auth.UpdateProfileAsync(GetUserId(), req);
        return Ok(result);
    }

    /// <summary>Send password reset email</summary>
    [HttpPost("forgot-password")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest req)
    {
        await _auth.ForgotPasswordAsync(req.Email);
        return Ok(new { message = "If an account with that email exists, a reset link has been sent." });
    }

    /// <summary>Reset password using token from email</summary>
    [HttpPost("reset-password")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest req)
    {
        try
        {
            await _auth.ResetPasswordAsync(req.Token, req.NewPassword);
            return Ok(new { message = "Password reset successfully. Please log in." });
        }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    // ── Cookie helpers ────────────────────────────────────────────────────────

    private void SetRefreshTokenCookie(string token)
    {
        var expiryDays = double.Parse(_config["Jwt:RefreshTokenExpiryDays"] ?? "7");
        Response.Cookies.Append(RefreshTokenCookie, token, new CookieOptions
        {
            HttpOnly = true,
            Secure   = true,
            SameSite = SameSiteMode.None,
            Expires  = DateTime.UtcNow.AddDays(expiryDays),
            Path     = "/api/auth"
        });
    }

    private void ClearRefreshTokenCookie() =>
        Response.Cookies.Delete(RefreshTokenCookie, new CookieOptions
        {
            HttpOnly = true,
            Secure   = true,
            SameSite = SameSiteMode.None,
            Path     = "/api/auth"
        });

    private static AuthClientResponse ToClientResponse(AuthResponse r) =>
        new(r.UserId, r.Email, r.FirstName, r.LastName, r.Role, r.AccessToken, r.ExpiresAt, r.Permissions ?? []);

    private int GetUserId() =>
        int.Parse(User.FindFirstValue("userId") ?? User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _products;
    public ProductsController(IProductService products) => _products = products;

    /// <summary>Search and filter products</summary>
    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] ProductSearchParams p) =>
        Ok(await _products.SearchAsync(p));

    /// <summary>Get featured products</summary>
    [HttpGet("featured")]
    public async Task<IActionResult> Featured() =>
        Ok(await _products.GetFeaturedAsync());

    /// <summary>Get all categories</summary>
    [HttpGet("categories")]
    public async Task<IActionResult> Categories() =>
        Ok(await _products.GetCategoriesAsync());

    /// <summary>Get products by category</summary>
    [HttpGet("category/{categoryId}")]
    public async Task<IActionResult> ByCategory(int categoryId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20) =>
        Ok(await _products.GetByCategoryAsync(categoryId, page, pageSize));

    /// <summary>Get product by ID</summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var product = await _products.GetByIdAsync(id);
        return product == null ? NotFound() : Ok(product);
    }

    /// <summary>Create a new product</summary>
    [HttpPost]
    [RequirePermission(Perms.ProductsManage)]
    public async Task<IActionResult> Create([FromBody] CreateProductRequest req)
    {
        var product = await _products.CreateAsync(req);
        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
    }

    /// <summary>Update a product</summary>
    [HttpPut("{id}")]
    [RequirePermission(Perms.ProductsManage)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateProductRequest req)
    {
        var product = await _products.UpdateAsync(id, req);
        return product == null ? NotFound() : Ok(product);
    }

    /// <summary>Delete (deactivate) a product</summary>
    [HttpDelete("{id}")]
    [RequirePermission(Perms.ProductsManage)]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _products.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }
}

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CartController : ControllerBase
{
    private readonly ICartService _cart;
    public CartController(ICartService cart) => _cart = cart;

    /// <summary>Get current user's cart</summary>
    [HttpGet]
    public async Task<IActionResult> GetCart() =>
        Ok(await _cart.GetCartAsync(GetUserId()));

    /// <summary>Add item to cart</summary>
    [HttpPost("items")]
    public async Task<IActionResult> AddItem([FromBody] AddToCartRequest req)
    {
        try { return Ok(await _cart.AddItemAsync(GetUserId(), req)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    /// <summary>Update cart item quantity</summary>
    [HttpPut("items/{itemId}")]
    public async Task<IActionResult> UpdateItem(int itemId, [FromBody] UpdateCartItemRequest req)
    {
        try { return Ok(await _cart.UpdateItemAsync(GetUserId(), itemId, req)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    /// <summary>Remove item from cart</summary>
    [HttpDelete("items/{itemId}")]
    public async Task<IActionResult> RemoveItem(int itemId)
    {
        try { return Ok(await _cart.RemoveItemAsync(GetUserId(), itemId)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    /// <summary>Clear entire cart</summary>
    [HttpDelete]
    public async Task<IActionResult> ClearCart()
    {
        await _cart.ClearCartAsync(GetUserId());
        return NoContent();
    }

    private int GetUserId() =>
        int.Parse(User.FindFirstValue("userId") ?? User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orders;
    public OrdersController(IOrderService orders) => _orders = orders;

    /// <summary>Get logged-in user's order history</summary>
    [HttpGet("my")]
    public async Task<IActionResult> MyOrders([FromQuery] int page = 1, [FromQuery] int pageSize = 10) =>
        Ok(await _orders.GetMyOrdersAsync(GetUserId(), page, pageSize));

    /// <summary>Get a specific order (own orders or Admin)</summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var order = await _orders.GetByIdAsync(id);
        if (order == null) return NotFound();

        // Customers can only see their own orders
        if (!User.IsInRole("Admin") && order.UserId != GetUserId())
            return Forbid();

        return Ok(order);
    }

    /// <summary>Place order from cart</summary>
    [HttpPost("checkout")]
    public async Task<IActionResult> Checkout([FromBody] CreateOrderRequest req)
    {
        try
        {
            var order = await _orders.CreateFromCartAsync(GetUserId(), req);
            return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    /// <summary>Get all orders</summary>
    [HttpGet]
    [RequirePermission(Perms.OrdersView)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null) =>
        Ok(await _orders.GetAllOrdersAsync(page, pageSize, status));

    /// <summary>Update order status</summary>
    [HttpPut("{id}/status")]
    [RequirePermission(Perms.OrdersManage)]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateOrderStatusRequest req)
    {
        var order = await _orders.UpdateStatusAsync(id, req);
        return order == null ? NotFound() : Ok(order);
    }

    /// <summary>Get full status change history for an order (owner or staff)</summary>
    [HttpGet("{id}/history")]
    public async Task<IActionResult> GetHistory(int id)
    {
        var order = await _orders.GetByIdAsync(id);
        if (order == null) return NotFound();
        var perms = User.FindFirst("permissions")?.Value ?? "";
        var isStaff = User.IsInRole("Admin") || perms.Contains(Perms.OrdersView) || perms.Contains(Perms.OrdersManage);
        if (!isStaff && order.UserId != GetUserId())
            return Forbid();
        return Ok(await _orders.GetStatusHistoryAsync(id));
    }

    private int GetUserId() =>
        int.Parse(User.FindFirstValue("userId") ?? User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}

[ApiController]
[Route("api/[controller]")]
[RequirePermission(Perms.InventoryManage)]
public class InventoryController : ControllerBase
{
    private readonly IInventoryService _inventory;
    public InventoryController(IInventoryService inventory) => _inventory = inventory;

    /// <summary>Get inventory for a product</summary>
    [HttpGet("product/{productId}")]
    public async Task<IActionResult> GetByProduct(int productId)
    {
        var inv = await _inventory.GetByProductIdAsync(productId);
        return inv == null ? NotFound() : Ok(inv);
    }

    /// <summary>Get all low-stock items</summary>
    [HttpGet("low-stock")]
    public async Task<IActionResult> LowStock() =>
        Ok(await _inventory.GetLowStockAsync());

    /// <summary>Adjust stock level (positive or negative)</summary>
    [HttpPost("product/{productId}/adjust")]
    public async Task<IActionResult> Adjust(int productId, [FromBody] AdjustStockRequest req)
    {
        try { return Ok(await _inventory.AdjustStockAsync(productId, req)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    /// <summary>Restock a product</summary>
    [HttpPost("product/{productId}/restock")]
    public async Task<IActionResult> Restock(int productId, [FromBody] RestockRequest req)
    {
        try { return Ok(await _inventory.RestockAsync(productId, req)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    /// <summary>Update low-stock threshold</summary>
    [HttpPut("product/{productId}/threshold")]
    public async Task<IActionResult> UpdateThreshold(int productId, [FromBody] UpdateThresholdRequest req)
    {
        try { return Ok(await _inventory.UpdateThresholdAsync(productId, req)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }
}

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AdminController : ControllerBase
{
    private readonly IAdminService _admin;
    public AdminController(IAdminService admin) => _admin = admin;

    /// <summary>Get dashboard stats and KPIs</summary>
    [HttpGet("dashboard")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Dashboard() =>
        Ok(await _admin.GetDashboardStatsAsync());

    /// <summary>Get sales report for a date range</summary>
    [HttpGet("reports/sales")]
    [RequirePermission(Perms.ReportsView)]
    public async Task<IActionResult> SalesReport(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        var start = from ?? DateTime.UtcNow.AddDays(-30);
        var end = to ?? DateTime.UtcNow;
        return Ok(await _admin.GetSalesReportAsync(start, end));
    }

    /// <summary>Get paginated user list with stats</summary>
    [HttpGet("users")]
    [RequirePermission(Perms.UsersView)]
    public async Task<IActionResult> Users([FromQuery] int page = 1, [FromQuery] int pageSize = 20) =>
        Ok(await _admin.GetUsersAsync(page, pageSize));

    /// <summary>Toggle user active/inactive status</summary>
    [HttpPut("users/{userId}/toggle")]
    [RequirePermission(Perms.UsersManage)]
    public async Task<IActionResult> ToggleUser(int userId)
    {
        var result = await _admin.ToggleUserStatusAsync(userId);
        return result ? Ok(new { message = "User status updated." }) : NotFound();
    }

    /// <summary>Create a back office (staff/admin) account directly</summary>
    [HttpPost("users/staff")]
    [RequirePermission(Perms.UsersManage)]
    public async Task<IActionResult> CreateStaff([FromBody] CreateStaffRequest req, [FromServices] IAuthService auth)
    {
        try { return Ok(await auth.CreateStaffAccountAsync(req)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    /// <summary>Assign a role to a user</summary>
    [HttpPut("users/{userId}/role")]
    [RequirePermission(Perms.UsersManage)]
    public async Task<IActionResult> AssignRole(int userId, [FromBody] AssignRoleRequest req, [FromServices] IAuthService auth)
    {
        try
        {
            await auth.AssignRoleAsync(userId, req.Role);
            return Ok(new { message = $"Role updated to {req.Role}." });
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }
}

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RolesController : ControllerBase
{
    private readonly IRoleService _roles;
    public RolesController(IRoleService roles) => _roles = roles;

    /// <summary>List all roles</summary>
    [HttpGet]
    [RequirePermission(Perms.RolesManage)]
    public async Task<IActionResult> GetAll() =>
        Ok(await _roles.GetAllAsync());

    /// <summary>List all available permission keys and labels</summary>
    [HttpGet("permissions")]
    [RequirePermission(Perms.RolesManage)]
    public async Task<IActionResult> GetPermissions() =>
        Ok(await _roles.GetAvailablePermissionsAsync());

    /// <summary>Get a single role by ID</summary>
    [HttpGet("{id}")]
    [RequirePermission(Perms.RolesManage)]
    public async Task<IActionResult> GetById(int id)
    {
        var role = await _roles.GetByIdAsync(id);
        return role == null ? NotFound() : Ok(role);
    }

    /// <summary>Create a custom role</summary>
    [HttpPost]
    [RequirePermission(Perms.RolesManage)]
    public async Task<IActionResult> Create([FromBody] CreateRoleRequest req)
    {
        try { return Ok(await _roles.CreateAsync(req)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    /// <summary>Update a role's description and permissions</summary>
    [HttpPut("{id}")]
    [RequirePermission(Perms.RolesManage)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateRoleRequest req)
    {
        try { return Ok(await _roles.UpdateAsync(id, req)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    /// <summary>Delete a custom role</summary>
    [HttpDelete("{id}")]
    [RequirePermission(Perms.RolesManage)]
    public async Task<IActionResult> Delete(int id)
    {
        try { await _roles.DeleteAsync(id); return NoContent(); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }
}