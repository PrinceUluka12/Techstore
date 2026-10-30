using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechStore.API.DTOs.Pos;
using TechStore.API.Helpers;
using TechStore.API.Services;
using TechStore.API.Services.Interfaces;

namespace TechStore.API.Controllers;

// The in-store till. Cashiers sell from their home shop; admins can pick any shop.
[ApiController]
[Route("api/pos")]
[Authorize]
public class PosController : ControllerBase
{
    private readonly IPosService _pos;
    public PosController(IPosService pos) => _pos = pos;

    /// <summary>Search products with their stock at the shop (exact SKU matches first, for scanners)</summary>
    [HttpGet("products")]
    [RequirePermission(Perms.PosSell)]
    public Task<IActionResult> Products([FromQuery] int? storeId, [FromQuery] string? q) =>
        Run(async () => Ok(await _pos.SearchProductsAsync(UserId, IsAdmin, storeId, q)));

    /// <summary>Staff who can approve discounts and price changes</summary>
    [HttpGet("approvers")]
    [RequirePermission(Perms.PosSell)]
    public async Task<IActionResult> Approvers() => Ok(await _pos.GetApproversAsync());

    /// <summary>Record a sale: takes the stock off the shop and returns the receipt</summary>
    [HttpPost("sales")]
    [RequirePermission(Perms.PosSell)]
    public Task<IActionResult> CreateSale([FromBody] PosSaleRequest req) =>
        Run(async () =>
        {
            var sale = await _pos.CreateSaleAsync(UserId, IsAdmin, req);
            return CreatedAtAction(nameof(GetSale), new { id = sale.Id }, sale);
        });

    /// <summary>A sale's receipt</summary>
    [HttpGet("sales/{id}")]
    [RequirePermission(Perms.PosSell)]
    public Task<IActionResult> GetSale(int id) =>
        Run(async () =>
        {
            var sale = await _pos.GetSaleAsync(UserId, IsAdmin, id);
            return sale == null ? NotFound() : Ok(sale);
        });

    /// <summary>Set your own approval PIN (staff who can approve discounts)</summary>
    [HttpPut("approval-pin")]
    public Task<IActionResult> SetApprovalPin([FromBody] SetApprovalPinRequest req) =>
        Run(async () =>
        {
            await _pos.SetApprovalPinAsync(UserId, req.CurrentPassword, req.Pin);
            return Ok(new { message = "Approval PIN saved." });
        });

    // Validation problems (400) and missing records (404) go through ExceptionMiddleware;
    // working at another shop is a 403 rather than the middleware's generic 500
    private async Task<IActionResult> Run(Func<Task<IActionResult>> action)
    {
        try { return await action(); }
        catch (PosForbiddenException ex) { return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message }); }
    }

    private int UserId =>
        int.Parse(User.FindFirstValue("userId") ?? User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private bool IsAdmin => User.IsInRole("Admin");
}
