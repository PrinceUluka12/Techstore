using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechStore.API.DTOs.BankAccount;
using TechStore.API.Services.Interfaces;

namespace TechStore.API.Controllers;

// Bank accounts customers pay into for online orders. Anyone can read the ones shown at
// checkout; only admins can change them, and every change goes in the history.
[ApiController]
[Route("api/bank-accounts")]
public class BankAccountsController : ControllerBase
{
    private readonly IBankAccountService _accounts;
    public BankAccountsController(IBankAccountService accounts) => _accounts = accounts;

    /// <summary>The accounts shown at checkout</summary>
    [HttpGet]
    public async Task<IActionResult> Active() => Ok(await _accounts.GetActiveAsync());

    /// <summary>Every account, including ones hidden from checkout (Admin only)</summary>
    [HttpGet("all")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> All() => Ok(await _accounts.GetAllAsync());

    /// <summary>Add an account (Admin only)</summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] SaveBankAccountRequest req) =>
        Ok(await _accounts.CreateAsync(UserId, req));

    /// <summary>Change an account, or show/hide it at checkout (Admin only)</summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] SaveBankAccountRequest req)
    {
        var account = await _accounts.UpdateAsync(UserId, id, req);
        return account == null ? NotFound() : Ok(account);
    }

    /// <summary>Remove an account (Admin only)</summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id) =>
        await _accounts.DeleteAsync(UserId, id) ? NoContent() : NotFound();

    /// <summary>Every change to the accounts, newest first (Admin only)</summary>
    [HttpGet("history")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> History([FromQuery] int page = 1, [FromQuery] int pageSize = 20) =>
        Ok(await _accounts.GetHistoryAsync(Math.Max(1, page), Math.Clamp(pageSize, 1, 100)));

    private int UserId =>
        int.Parse(User.FindFirstValue("userId") ?? User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
