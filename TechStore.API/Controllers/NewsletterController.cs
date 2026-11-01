using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TechStore.API.DTOs.Newsletter;
using TechStore.API.Helpers;
using TechStore.API.Services.Interfaces;

namespace TechStore.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NewsletterController : ControllerBase
{
    private readonly INewsletterService _newsletter;
    public NewsletterController(INewsletterService newsletter) => _newsletter = newsletter;

    /// <summary>Sign up from the shop's footer</summary>
    [HttpPost("subscribe")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Subscribe([FromBody] SubscribeRequest req)
    {
        await _newsletter.SubscribeAsync(req.Email);
        return Ok(new { message = "Thanks! You're on the list." });
    }

    /// <summary>The link at the bottom of every newsletter</summary>
    [HttpPost("unsubscribe")]
    public async Task<IActionResult> Unsubscribe([FromBody] UnsubscribeRequest req) =>
        await _newsletter.UnsubscribeAsync(req.Token)
            ? Ok(new { message = "You've been unsubscribed." })
            : NotFound(new { message = "That unsubscribe link isn't valid." });

    /// <summary>Everyone who's signed up, newest first</summary>
    [HttpGet]
    [Authorize]
    [RequirePermission(Perms.UsersView)]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? search = null) =>
        Ok(await _newsletter.ListAsync(page, pageSize, search));

    /// <summary>Current subscribers as CSV, with each one's unsubscribe link, for a mailing tool</summary>
    [HttpGet("export")]
    [Authorize]
    [RequirePermission(Perms.UsersView)]
    public async Task<IActionResult> Export() =>
        File(Encoding.UTF8.GetBytes(await _newsletter.ExportCsvAsync()), "text/csv", "newsletter-subscribers.csv");
}
