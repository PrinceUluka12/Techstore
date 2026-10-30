using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechStore.API.DTOs.Transfer;
using TechStore.API.Helpers;
using TechStore.API.Models;
using TechStore.API.Services;
using TechStore.API.Services.Interfaces;

namespace TechStore.API.Controllers;

// Stock transfers between shops. Admins can use any shop; other staff send from and
// receive at their home shop.
[ApiController]
[Route("api/transfers")]
[Authorize]
[RequirePermission(Perms.TransfersManage)]
public class TransfersController : ControllerBase
{
    private readonly ITransferService _transfers;
    private readonly IPosService _pos;

    public TransfersController(ITransferService transfers, IPosService pos)
    {
        _transfers = transfers;
        _pos = pos;
    }

    /// <summary>Transfers to or from a shop, newest first (at most 200)</summary>
    [HttpGet]
    public Task<IActionResult> List([FromQuery] int? storeId, [FromQuery] TransferStatus? status) =>
        Run(async () => Ok(await _transfers.ListAsync(UserId, IsAdmin, storeId, status)));

    /// <summary>One transfer</summary>
    [HttpGet("{id}")]
    public Task<IActionResult> Get(int id) =>
        Run(async () =>
        {
            var t = await _transfers.GetAsync(UserId, IsAdmin, id);
            return t == null ? NotFound() : Ok(t);
        });

    /// <summary>Products with their free stock at the sending shop</summary>
    [HttpGet("products")]
    public Task<IActionResult> Products([FromQuery] int? storeId, [FromQuery] string? q) =>
        Run(async () => Ok(await _pos.SearchProductsAsync(UserId, IsAdmin, storeId, q)));

    /// <summary>Send stock: it leaves the sending shop now</summary>
    [HttpPost]
    public Task<IActionResult> Create([FromBody] CreateTransferRequest req) =>
        Run(async () =>
        {
            var t = await _transfers.CreateAsync(UserId, IsAdmin, req);
            return CreatedAtAction(nameof(Get), new { id = t.Id }, t);
        });

    /// <summary>Confirm the stock arrived: it's added to the receiving shop</summary>
    [HttpPost("{id}/receive")]
    public Task<IActionResult> Receive(int id) =>
        Run(async () => Ok(await _transfers.ReceiveAsync(UserId, IsAdmin, id)));

    /// <summary>Cancel while in transit: the stock goes back to the sending shop</summary>
    [HttpPost("{id}/cancel")]
    public Task<IActionResult> Cancel(int id) =>
        Run(async () => Ok(await _transfers.CancelAsync(UserId, IsAdmin, id)));

    // Same as PosController: working with another shop is a 403
    private async Task<IActionResult> Run(Func<Task<IActionResult>> action)
    {
        try { return await action(); }
        catch (PosForbiddenException ex) { return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message }); }
    }

    private int UserId =>
        int.Parse(User.FindFirstValue("userId") ?? User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private bool IsAdmin => User.IsInRole("Admin");
}
