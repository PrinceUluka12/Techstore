using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechStore.API.Services.Interfaces;

namespace TechStore.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WishlistController : ControllerBase
{
    private readonly IWishlistService _wishlist;
    public WishlistController(IWishlistService wishlist) => _wishlist = wishlist;

    /// <summary>Get current user's wishlist</summary>
    [HttpGet]
    public async Task<IActionResult> Get() =>
        Ok(await _wishlist.GetAsync(GetUserId()));

    /// <summary>Add product to wishlist</summary>
    [HttpPost("{productId}")]
    public async Task<IActionResult> Add(int productId)
    {
        try { return Ok(await _wishlist.AddAsync(GetUserId(), productId)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    /// <summary>Remove product from wishlist</summary>
    [HttpDelete("{productId}")]
    public async Task<IActionResult> Remove(int productId)
    {
        var removed = await _wishlist.RemoveAsync(GetUserId(), productId);
        return removed ? NoContent() : NotFound();
    }

    /// <summary>Check if product is in wishlist</summary>
    [HttpGet("{productId}/check")]
    public async Task<IActionResult> Check(int productId) =>
        Ok(new { inWishlist = await _wishlist.IsInWishlistAsync(GetUserId(), productId) });

    private int GetUserId() =>
        int.Parse(User.FindFirstValue("userId") ?? User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
