using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechStore.API.DTOs.Store;
using TechStore.API.Helpers;
using TechStore.API.Services.Interfaces;

namespace TechStore.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class StoresController : ControllerBase
{
    private readonly IStoreService _stores;
    public StoresController(IStoreService stores) => _stores = stores;

    /// <summary>List all shops (any signed-in user: shop names and addresses go on receipts)</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await _stores.GetAllAsync());

    /// <summary>Create a shop; every product gets a stock row in it, starting at 0</summary>
    [HttpPost]
    [RequirePermission(Perms.StoresManage)]
    public async Task<IActionResult> Create([FromBody] CreateStoreRequest req)
    {
        try { return Ok(await _stores.CreateAsync(req)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    /// <summary>Update a shop, including which shop sends online orders</summary>
    [HttpPut("{id}")]
    [RequirePermission(Perms.StoresManage)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateStoreRequest req)
    {
        try { return Ok(await _stores.UpdateAsync(id, req)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }
}
