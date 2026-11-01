using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechStore.API.DTOs.Order;
using TechStore.API.Services.Interfaces;

namespace TechStore.API.Controllers;

// Delivery fees per state. Anyone can read the ones offered at checkout; only admins change them.
[ApiController]
[Route("api/delivery-rates")]
public class DeliveryRatesController : ControllerBase
{
    private readonly IDeliveryRateService _rates;
    public DeliveryRatesController(IDeliveryRateService rates) => _rates = rates;

    /// <summary>The states we deliver to, with their fees</summary>
    [HttpGet]
    public async Task<IActionResult> Active() => Ok(await _rates.GetActiveAsync());

    /// <summary>Every state, with a blank fee where there's no delivery (Admin only)</summary>
    [HttpGet("all")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> All() => Ok(await _rates.GetAllStatesAsync());

    /// <summary>Save fees for the states sent; a blank fee stops delivery there (Admin only)</summary>
    [HttpPut]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Save([FromBody] List<SaveDeliveryRateRequest> rates) =>
        Ok(await _rates.SaveAsync(UserId, rates));

    private int UserId =>
        int.Parse(User.FindFirstValue("userId") ?? User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
