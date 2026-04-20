namespace TechStore.API.Models;

public enum DiscountType
{
    Percentage,    // e.g., 20% off subtotal
    FixedAmount,   // e.g., $10 off subtotal
    FreeShipping   // Waives shipping cost
}

public class Coupon
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }

    // Discount configuration
    public DiscountType DiscountType { get; set; }
    public decimal DiscountValue { get; set; }
    public decimal? MinOrderAmount { get; set; }
    public decimal? MaxDiscountAmount { get; set; }

    // Validity
    public DateTime StartsAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public int? UsageLimit { get; set; }
    public int TimesUsed { get; set; }
    public bool IsActive { get; set; } = true;

    // Tracking
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
