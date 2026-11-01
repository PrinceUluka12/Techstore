using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using TechStore.API.DTOs.Policy;
using TechStore.API.Helpers;
using TechStore.API.Services.Interfaces;

namespace TechStore.API.Controllers;

// Terms, returns, privacy and disclaimer: shown in the shop, edited by staff with "Edit Policy Pages"
[ApiController]
[Route("api/[controller]")]
public class PoliciesController : ControllerBase
{
    private readonly IPolicyService _policies;
    public PoliciesController(IPolicyService policies) => _policies = policies;

    /// <summary>A page as the shop shows it</summary>
    [HttpGet("{slug}")]
    public async Task<IActionResult> Get(string slug) =>
        await _policies.GetAsync(slug) is { } page ? Ok(page) : NotFound();

    /// <summary>All four pages, for the editor</summary>
    [HttpGet]
    [RequirePermission(Perms.PagesManage)]
    public async Task<IActionResult> List() => Ok(await _policies.ListAsync());

    /// <summary>Save new text: adds a version, which the shop then shows</summary>
    [HttpPut("{slug}")]
    [RequirePermission(Perms.PagesManage)]
    public async Task<IActionResult> Save(string slug, [FromBody] SavePolicyRequest req) =>
        await _policies.SaveAsync(slug, req.Body, GetUserId()) is { } page ? Ok(page) : NotFound();

    /// <summary>Every saved version, newest first</summary>
    [HttpGet("{slug}/history")]
    [RequirePermission(Perms.PagesManage)]
    public async Task<IActionResult> History(string slug) =>
        await _policies.HistoryAsync(slug) is { } versions ? Ok(versions) : NotFound();

    private int GetUserId() =>
        int.Parse(User.FindFirstValue("userId") ?? User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
