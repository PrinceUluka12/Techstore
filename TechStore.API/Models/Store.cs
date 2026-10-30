namespace TechStore.API.Models;

// A physical shop. Each shop keeps its own stock (Inventory rows); exactly one
// shop has SendsOnlineOrders set, and website orders reserve its stock.
public class Store
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty; // Short unique code, e.g. "IKJ"
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Phone { get; set; }
    public bool IsActive { get; set; } = true;
    public bool SendsOnlineOrders { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Inventory> Inventories { get; set; } = new List<Inventory>();
}
