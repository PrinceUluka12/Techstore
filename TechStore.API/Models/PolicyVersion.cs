namespace TechStore.API.Models;

// One saved version of a policy page (terms, returns, privacy, disclaimer). Saving adds a
// version rather than changing one, so what a page said on any date can be looked up; the
// newest is what the shop shows. A page with no versions yet shows its starting text
// (Content/Policies/{slug}.md).
public class PolicyVersion
{
    public int Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty; // Markdown
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int? CreatedById { get; set; }
    public string CreatedByName { get; set; } = string.Empty; // as it was then, if the account changes later
}
