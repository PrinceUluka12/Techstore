using Microsoft.EntityFrameworkCore;
using TechStore.API.Data;
using TechStore.API.DTOs.Policy;
using TechStore.API.Models;
using TechStore.API.Services.Interfaces;

namespace TechStore.API.Services;

// The shop's policy pages, edited in Admin -> Policy Pages by staff with "Edit Policy Pages"
public class PolicyService : IPolicyService
{
    // Slug (as in the shop's infoLinks.js) -> title. Titles are fixed: the footer links use them.
    public static readonly IReadOnlyDictionary<string, string> Pages = new Dictionary<string, string>
    {
        ["terms"]      = "Terms of Service & Compliance",
        ["returns"]    = "Refund and Returns Policy",
        ["privacy"]    = "Privacy Policy",
        ["disclaimer"] = "Disclaimer",
    };

    // When the starting text was written: the "Last updated" date until a page is first saved
    private static readonly DateTime DefaultsWritten = new(2026, 11, 1, 12, 0, 0, DateTimeKind.Utc); // midday UTC: 1 November in Nigeria and nearly everywhere else

    // The starting text, built into the API from Content/Policies/{slug}.md
    private static readonly Lazy<Dictionary<string, string>> Defaults = new(() => Pages.Keys.ToDictionary(slug => slug, slug =>
    {
        using var stream = typeof(PolicyService).Assembly.GetManifestResourceStream($"TechStore.API.Content.Policies.{slug}.md")
            ?? throw new InvalidOperationException($"Missing starting text for the {slug} page.");
        return new StreamReader(stream).ReadToEnd();
    }));

    private readonly AppDbContext _db;
    public PolicyService(AppDbContext db) => _db = db;

    public async Task<PolicyPageDto?> GetAsync(string slug)
    {
        if (!Pages.TryGetValue(slug, out var title)) return null;
        var latest = await Versions(slug).FirstOrDefaultAsync();
        return latest == null
            ? new PolicyPageDto(slug, title, Defaults.Value[slug], DefaultsWritten, null, true)
            : new PolicyPageDto(slug, title, latest.Body, latest.CreatedAt, latest.CreatedByName, false);
    }

    public async Task<IEnumerable<PolicyPageDto>> ListAsync()
    {
        var pages = new List<PolicyPageDto>();
        foreach (var slug in Pages.Keys) pages.Add((await GetAsync(slug))!);
        return pages;
    }

    // Adds a version, unless it's the same as the current text
    public async Task<PolicyPageDto?> SaveAsync(string slug, string body, int userId)
    {
        var current = await GetAsync(slug);
        if (current == null) return null;
        body = body.Trim();
        if (body == current.Body.Trim()) return current;

        var name = await _db.Users.Where(u => u.Id == userId)
            .Select(u => u.FirstName + " " + u.LastName).SingleAsync();
        _db.PolicyVersions.Add(new PolicyVersion
        {
            Slug = slug, Body = body, CreatedAt = DateTime.UtcNow, CreatedById = userId, CreatedByName = name.Trim(),
        });
        await _db.SaveChangesAsync();
        return await GetAsync(slug);
    }

    // Newest first, with each version's text, so any can be read or brought back
    public async Task<IEnumerable<PolicyVersionDto>?> HistoryAsync(string slug)
    {
        if (!Pages.ContainsKey(slug)) return null;
        return await Versions(slug).Select(v => new PolicyVersionDto(v.Id, v.CreatedAt, v.CreatedByName, v.Body)).ToListAsync();
    }

    private IQueryable<PolicyVersion> Versions(string slug) =>
        _db.PolicyVersions.AsNoTracking().Where(v => v.Slug == slug).OrderByDescending(v => v.Id);
}
