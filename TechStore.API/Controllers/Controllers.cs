using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechStore.API.DTOs.Auth;
using TechStore.API.DTOs.Cart;
using TechStore.API.DTOs.Coupon;
using TechStore.API.DTOs.Inventory;
using TechStore.API.DTOs.Order;
using TechStore.API.DTOs.Product;
using TechStore.API.Services.Interfaces;

namespace TechStore.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;
    public AuthController(IAuthService auth) => _auth = auth;

    /// <summary>Register a new customer account</summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req)
    {
        try
        {
            var result = await _auth.RegisterAsync(req);
            return Ok(result);
        }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    /// <summary>Login and receive JWT tokens</summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        try
        {
            var result = await _auth.LoginAsync(req);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex) { return Unauthorized(new { message = ex.Message }); }
    }

    /// <summary>Exchange a refresh token for new tokens</summary>
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest req)
    {
        try
        {
            var result = await _auth.RefreshTokenAsync(req.RefreshToken);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex) { return Unauthorized(new { message = ex.Message }); }
    }

    /// <summary>Logout and revoke refresh token</summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest? req = null)
    {
        await _auth.LogoutAsync(GetUserId(), req?.RefreshToken);
        return NoContent();
    }

    /// <summary>Logout from all devices</summary>
    [HttpPost("logout-all")]
    [Authorize]
    public async Task<IActionResult> LogoutAll()
    {
        await _auth.LogoutEverywhereAsync(GetUserId());
        return NoContent();
    }

    /// <summary>Get current user profile</summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetProfile()
    {
        var userId = GetUserId();
        var profile = await _auth.GetProfileAsync(userId);
        return profile == null ? NotFound() : Ok(profile);
    }

    /// <summary>Update current user profile</summary>
    [HttpPut("me")]
    [Authorize]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest req)
    {
        var userId = GetUserId();
        var result = await _auth.UpdateProfileAsync(userId, req);
        return Ok(result);
    }

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

    /// <summary>Create a new product (Admin only)</summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateProductRequest req)
    {
        var product = await _products.CreateAsync(req);
        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
    }

    /// <summary>Update a product (Admin only)</summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateProductRequest req)
    {
        var product = await _products.UpdateAsync(id, req);
        return product == null ? NotFound() : Ok(product);
    }

    /// <summary>Delete (deactivate) a product (Admin only)</summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
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

    /// <summary>Get all orders (Admin only)</summary>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null) =>
        Ok(await _orders.GetAllOrdersAsync(page, pageSize, status));

    /// <summary>Update order status (Admin only)</summary>
    [HttpPut("{id}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateOrderStatusRequest req)
    {
        var order = await _orders.UpdateStatusAsync(id, req);
        return order == null ? NotFound() : Ok(order);
    }

    private int GetUserId() =>
        int.Parse(User.FindFirstValue("userId") ?? User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
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
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly IAdminService _admin;
    public AdminController(IAdminService admin) => _admin = admin;

    /// <summary>Get dashboard stats and KPIs</summary>
    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard() =>
        Ok(await _admin.GetDashboardStatsAsync());

    /// <summary>Get sales report for a date range</summary>
    [HttpGet("reports/sales")]
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
    public async Task<IActionResult> Users([FromQuery] int page = 1, [FromQuery] int pageSize = 20) =>
        Ok(await _admin.GetUsersAsync(page, pageSize));

    /// <summary>Toggle user active/inactive status</summary>
    [HttpPut("users/{userId}/toggle")]
    public async Task<IActionResult> ToggleUser(int userId)
    {
        var result = await _admin.ToggleUserStatusAsync(userId);
        return result ? Ok(new { message = "User status updated." }) : NotFound();
    }
}

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class CouponsController : ControllerBase
{
    private readonly ICouponService _coupons;
    public CouponsController(ICouponService coupons) => _coupons = coupons;

    /// <summary>Get all coupons (Admin)</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] bool? active = null) =>
        Ok(await _coupons.GetAllAsync(page, pageSize, active));

    /// <summary>Get coupon by id (Admin)</summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var c = await _coupons.GetByIdAsync(id);
        return c == null ? NotFound() : Ok(c);
    }

    /// <summary>Get public coupon info by code (safe for unauthenticated users)</summary>
    [HttpGet("code/{code}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetByCode(string code)
    {
        var c = await _coupons.GetPublicByCodeAsync(code);
        return c == null ? NotFound() : Ok(c);
    }

    /// <summary>Create a new coupon (Admin)</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCouponRequest req)
    {
        try
        {
            var created = await _coupons.CreateAsync(req);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    /// <summary>Update a coupon (Admin)</summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCouponRequest req)
    {
        try
        {
            var updated = await _coupons.UpdateAsync(id, req);
            return updated == null ? NotFound() : Ok(updated);
        }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    /// <summary>Delete a coupon (Admin)</summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var ok = await _coupons.DeleteAsync(id);
        return ok ? NoContent() : NotFound();
    }
}
