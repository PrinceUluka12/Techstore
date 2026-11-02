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
    private readonly IApprovalService _approvals;

    public PosController(IPosService pos, IApprovalService approvals)
    {
        _pos = pos;
        _approvals = approvals;
    }

    /// <summary>Search products with their stock at the shop (exact SKU matches first, for scanners)</summary>
    [HttpGet("products")]
    [RequirePermission(Perms.PosSell)]
    public Task<IActionResult> Products([FromQuery] int? storeId, [FromQuery] string? q) =>
        Run(async () => Ok(await _pos.SearchProductsAsync(UserId, IsAdmin, storeId, q)));

    /// <summary>Who can approve discounts and price changes at the shop: its managers and Admins</summary>
    [HttpGet("approvers")]
    [RequirePermission(Perms.PosSell)]
    public Task<IActionResult> Approvers([FromQuery] int? storeId) =>
        Run(async () => Ok(await _pos.GetApproversAsync(UserId, IsAdmin, storeId)));

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

    /// <summary>End-of-day cash-up for a shop; date is a day in Nigeria time (default today).
    /// Cashiers see their own sales; the whole shop needs See Whole-Shop Cash-up.</summary>
    [HttpGet("cash-up")]
    [RequirePermission(Perms.PosSell)]
    public Task<IActionResult> CashUp([FromQuery] int? storeId, [FromQuery] DateOnly? date) =>
        Run(async () => Ok(await _pos.GetCashUpAsync(UserId, IsAdmin, User.HasPermission(Perms.PosCashUpShop),
            storeId, date ?? PosService.TodayInNigeria())));

    /// <summary>Set your own approval PIN (staff who can approve discounts)</summary>
    [HttpPut("approval-pin")]
    public Task<IActionResult> SetApprovalPin([FromBody] SetApprovalPinRequest req) =>
        Run(async () =>
        {
            await _pos.SetApprovalPinAsync(UserId, req.CurrentPassword, req.Pin);
            return Ok(new { message = "Approval PIN saved." });
        });

    // ── Asking a manager from the till; they answer on their own phone or computer ──

    /// <summary>Ask the shop's managers to approve this cart's discount or price changes. They get
    /// a live alert; the answer comes back live too, or from GET approval-requests/{id}.</summary>
    [HttpPost("approval-requests")]
    [RequirePermission(Perms.PosSell)]
    public Task<IActionResult> AskApproval([FromBody] AskApprovalRequest req) =>
        Run(async () => Ok(await _approvals.AskAsync(UserId, IsAdmin, req)));

    /// <summary>Requests for your home shop (Admins: every shop), waiting ones or the latest 50</summary>
    [HttpGet("approval-requests")]
    [RequirePermission(Perms.PosApproveDiscounts)]
    public async Task<IActionResult> ApprovalRequests([FromQuery] bool waiting = false) =>
        Ok(await _approvals.ListAsync(UserId, IsAdmin, waiting));

    /// <summary>One request: for the cashier who asked, and the managers who can answer it</summary>
    [HttpGet("approval-requests/{id}")]
    public async Task<IActionResult> ApprovalRequest(int id)
    {
        var request = await _approvals.GetAsync(UserId, IsAdmin, User.HasPermission(Perms.PosApproveDiscounts), id);
        return request == null ? NotFound() : Ok(request);
    }

    /// <summary>Approve with your own approval PIN</summary>
    [HttpPost("approval-requests/{id}/approve")]
    [RequirePermission(Perms.PosApproveDiscounts)]
    public async Task<IActionResult> Approve(int id, [FromBody] ApproveRequest req) =>
        Ok(await _approvals.ApproveAsync(UserId, id, req.Pin));

    [HttpPost("approval-requests/{id}/decline")]
    [RequirePermission(Perms.PosApproveDiscounts)]
    public async Task<IActionResult> Decline(int id, [FromBody] DeclineRequest req) =>
        Ok(await _approvals.DeclineAsync(UserId, id, req.Note));

    /// <summary>The cashier withdraws their request (an approved one too, if it isn't used)</summary>
    [HttpPost("approval-requests/{id}/cancel")]
    [RequirePermission(Perms.PosSell)]
    public async Task<IActionResult> CancelApproval(int id) =>
        Ok(await _approvals.CancelAsync(UserId, id));

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
