namespace TechStore.API.Models;

public class Cart
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<CartItem> Items { get; set; } = new List<CartItem>();
}

public class CartItem
{
    public int Id { get; set; }
    public int CartId { get; set; }
    public Cart Cart { get; set; } = null!;
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int Quantity { get; set; }
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
}

public class Inventory
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public int QuantityOnHand { get; set; }
    public int QuantityReserved { get; set; } // In pending orders
    public int LowStockThreshold { get; set; } = 10;
    public string? WarehouseLocation { get; set; }

    public int QuantityAvailable => QuantityOnHand - QuantityReserved;

    public DateTime LastRestockedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
