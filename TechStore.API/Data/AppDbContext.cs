using Microsoft.EntityFrameworkCore;
using TechStore.API.Models;

namespace TechStore.API.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Inventory> Inventories => Set<Inventory>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Coupon> Coupons => Set<Coupon>();
    public DbSet<Review> Reviews => Set<Review>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // User
        builder.Entity<User>(e =>
        {
            e.HasIndex(u => u.Email).IsUnique();
        });

        // RefreshToken
        builder.Entity<RefreshToken>(e =>
        {
            e.HasIndex(rt => rt.Token).IsUnique();
            e.HasOne(rt => rt.User).WithMany(u => u.RefreshTokens)
                .HasForeignKey(rt => rt.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        // Product
        builder.Entity<Product>(e =>
        {
            e.HasIndex(p => p.SKU).IsUnique();
            e.Property(p => p.Price).HasColumnType("decimal(18,2)");
            e.Property(p => p.CompareAtPrice).HasColumnType("decimal(18,2)");
            e.HasOne(p => p.Category).WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(p => p.Inventory).WithOne(i => i.Product)
                .HasForeignKey<Inventory>(i => i.ProductId).OnDelete(DeleteBehavior.Cascade);
        });

        // Order
        builder.Entity<Order>(e =>
        {
            e.HasIndex(o => o.OrderNumber).IsUnique();
            e.Property(o => o.SubTotal).HasColumnType("decimal(18,2)");
            e.Property(o => o.Tax).HasColumnType("decimal(18,2)");
            e.Property(o => o.ShippingCost).HasColumnType("decimal(18,2)");
            e.Property(o => o.DiscountAmount).HasColumnType("decimal(18,2)");
            e.Property(o => o.Total).HasColumnType("decimal(18,2)");
            e.Property(o => o.Status).HasConversion<string>();
            e.Property(o => o.PaymentStatus).HasConversion<string>();
            e.HasOne(o => o.User).WithMany(u => u.Orders)
                .HasForeignKey(o => o.UserId).OnDelete(DeleteBehavior.Restrict);
        });

        // OrderItem
        builder.Entity<OrderItem>(e =>
        {
            e.Property(i => i.UnitPrice).HasColumnType("decimal(18,2)");
            e.Property(i => i.LineTotal).HasColumnType("decimal(18,2)");
            e.HasOne(i => i.Order).WithMany(o => o.Items)
                .HasForeignKey(i => i.OrderId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(i => i.Product).WithMany(p => p.OrderItems)
                .HasForeignKey(i => i.ProductId).OnDelete(DeleteBehavior.Restrict);
        });

        // Coupon
        builder.Entity<Coupon>(e =>
        {
            e.HasIndex(c => c.Code).IsUnique();
            e.Property(c => c.Value).HasColumnType("decimal(18,2)");
            e.Property(c => c.MinimumOrderAmount).HasColumnType("decimal(18,2)");
            e.Property(c => c.MaximumDiscountAmount).HasColumnType("decimal(18,2)");
            e.Property(c => c.Type).HasConversion<string>();
        });

        // Review
        builder.Entity<Review>(e =>
        {
            e.HasIndex(r => new { r.ProductId, r.UserId }).IsUnique();
            e.HasOne(r => r.Product).WithMany().HasForeignKey(r => r.ProductId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(r => r.User).WithMany().HasForeignKey(r => r.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        // Cart
        builder.Entity<Cart>(e =>
        {
            e.HasOne(c => c.User).WithOne(u => u.Cart)
                .HasForeignKey<Cart>(c => c.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<CartItem>(e =>
        {
            e.HasOne(ci => ci.Cart).WithMany(c => c.Items)
                .HasForeignKey(ci => ci.CartId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(ci => ci.Product).WithMany(p => p.CartItems)
                .HasForeignKey(ci => ci.ProductId).OnDelete(DeleteBehavior.Cascade);
        });

        // Seed categories
        builder.Entity<Category>().HasData(
            new Category { Id = 1, Name = "Smartphones", Description = "Latest smartphones and accessories", IsActive = true, CreatedAt = DateTime.UtcNow },
            new Category { Id = 2, Name = "Tablets", Description = "Tablets and e-readers", IsActive = true, CreatedAt = DateTime.UtcNow },
            new Category { Id = 3, Name = "Smart Watches", Description = "Wearable technology", IsActive = true, CreatedAt = DateTime.UtcNow },
            new Category { Id = 4, Name = "Laptops", Description = "Laptops and ultrabooks", IsActive = true, CreatedAt = DateTime.UtcNow },
            new Category { Id = 5, Name = "Accessories", Description = "Cases, chargers, and more", IsActive = true, CreatedAt = DateTime.UtcNow }
        );
    }
}
