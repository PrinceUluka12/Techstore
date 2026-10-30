namespace TechStore.API.Models;

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public bool RequiresImei { get; set; } = false; // in-store sales record an IMEI per unit
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Product> Products { get; set; } = new List<Product>();
}

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string SKU { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal? CompareAtPrice { get; set; }
    public string? Brand { get; set; }
    public string? ImageUrl { get; set; }
    public string? AdditionalImages { get; set; } // JSON array of URLs
    public bool IsActive { get; set; } = true;
    public bool IsFeatured { get; set; } = false;
    public double? Rating { get; set; }
    public int ReviewCount { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    public ICollection<Inventory> Inventories { get; set; } = new List<Inventory>(); // One per shop

    // Stock of the shop that sends online orders. Only set when the query used
    // IncludeOnlineStock() (Repositories/StockQueries.cs), which loads that row and its Store.
    public Inventory? OnlineInventory => Inventories.FirstOrDefault(i => i.Store?.SendsOnlineOrders == true);

    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
}
