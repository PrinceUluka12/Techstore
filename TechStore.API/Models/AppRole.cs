namespace TechStore.API.Models;

public class AppRole
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public bool IsSystem { get; set; }          // Admin & Customer — cannot be deleted or renamed
    public List<string> Permissions { get; set; } = [];
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
