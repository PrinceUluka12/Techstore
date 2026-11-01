namespace TechStore.API.Models;

public class User
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = "Customer"; // Customer | Admin
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public bool IsActive { get; set; } = true;
    public int FailedLoginAttempts { get; set; } = 0;
    public DateTime? LockoutUntil { get; set; }
    public string? PasswordResetToken { get; set; }
    public DateTime? PasswordResetTokenExpiry { get; set; }
    // Adding orders placed without an account to this one: a hash of the token in the emailed link
    public string? OrdersClaimTokenHash { get; set; }
    public DateTime? OrdersClaimTokenExpiry { get; set; }
    public int? StoreId { get; set; } // Staff home shop; null for customers
    public Store? Store { get; set; }

    // PIN a manager enters on the till to approve a discount or price change
    public string? ApprovalPinHash { get; set; }
    public int FailedPinAttempts { get; set; } = 0;
    public DateTime? PinLockoutUntil { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<Wishlist> Wishlists { get; set; } = new List<Wishlist>();
    public Cart? Cart { get; set; }
}
