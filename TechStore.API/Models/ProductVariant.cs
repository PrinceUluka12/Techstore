namespace TechStore.API.Models;

public class ProductVariant
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public string Name { get; set; } = string.Empty;    // e.g. "128GB – Midnight Black"
    public string? SKU { get; set; }
    public decimal PriceAdjustment { get; set; } = 0;  // added to the base product price
    public string? Attributes { get; set; }              // JSON {"color":"Black","storage":"128GB"}
    public int StockQuantity { get; set; } = 0;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
