namespace TechStore.API.Models;

public class OrderStatusLog
{
    public int Id { get; set; }

    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;

    // Who made the change
    public int ChangedByUserId { get; set; }
    public string ChangedByName { get; set; } = string.Empty;  // snapshot — survives user deletion
    public string ChangedByEmail { get; set; } = string.Empty;

    // What changed
    public OrderStatus FromStatus { get; set; }
    public OrderStatus ToStatus { get; set; }

    // Optional note from the admin
    public string? Note { get; set; }

    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
}