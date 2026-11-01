using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using TechStore.API.Helpers;
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
    public DbSet<OrderStatusLog> OrderStatusLogs => Set<OrderStatusLog>();
    public DbSet<Wishlist> Wishlists => Set<Wishlist>();
    public DbSet<AppRole> AppRoles => Set<AppRole>();
    public DbSet<Store> Stores => Set<Store>();
    public DbSet<OrderPayment> OrderPayments => Set<OrderPayment>();
    public DbSet<OrderItemSerial> OrderItemSerials => Set<OrderItemSerial>();
    public DbSet<StockTransfer> StockTransfers => Set<StockTransfer>();
    public DbSet<StockTransferItem> StockTransferItems => Set<StockTransferItem>();
    public DbSet<BankAccount> BankAccounts => Set<BankAccount>();
    public DbSet<BankAccountLog> BankAccountLogs => Set<BankAccountLog>();
    public DbSet<DeliveryRate> DeliveryRates => Set<DeliveryRate>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // User
        builder.Entity<User>(e =>
        {
            e.HasIndex(u => u.Email).IsUnique();
            e.HasOne(u => u.Store).WithMany()
                .HasForeignKey(u => u.StoreId).OnDelete(DeleteBehavior.SetNull);
        });

        // Store
        builder.Entity<Store>(e =>
        {
            e.HasIndex(s => s.Code).IsUnique();
            e.Property(s => s.Name).HasMaxLength(100);
            e.Property(s => s.Code).HasMaxLength(10);
        });

        // Inventory — one row per product per shop
        builder.Entity<Inventory>(e =>
        {
            e.HasIndex(i => new { i.StoreId, i.ProductId }).IsUnique();
            e.HasOne(i => i.Store).WithMany(s => s.Inventories)
                .HasForeignKey(i => i.StoreId).OnDelete(DeleteBehavior.Restrict);
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
            e.Property(p => p.Warranty).HasMaxLength(100);
            e.HasOne(p => p.Category).WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(p => p.Inventories).WithOne(i => i.Product)
                .HasForeignKey(i => i.ProductId).OnDelete(DeleteBehavior.Cascade);
        });

        // Order
        builder.Entity<Order>(e =>
        {
            e.HasIndex(o => o.OrderNumber).IsUnique();
            e.Property(o => o.GuestToken).HasMaxLength(64);
            e.HasIndex(o => o.GuestToken).IsUnique().HasFilter("[GuestToken] IS NOT NULL");
            e.Property(o => o.SubTotal).HasColumnType("decimal(18,2)");
            e.Property(o => o.DiscountAmount).HasColumnType("decimal(18,2)");
            e.Property(o => o.Tax).HasColumnType("decimal(18,2)");
            e.Property(o => o.ShippingCost).HasColumnType("decimal(18,2)");
            e.Property(o => o.Total).HasColumnType("decimal(18,2)");
            e.Property(o => o.Status).HasConversion<string>();
            e.Property(o => o.PaymentStatus).HasConversion<string>();
            e.Property(o => o.Fulfilment).HasConversion<string>().HasMaxLength(20)
                .HasDefaultValue(FulfilmentMethod.Delivery); // orders from before pickup were all delivered
            e.HasOne(o => o.User).WithMany(u => u.Orders)
                .HasForeignKey(o => o.UserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(o => o.Store).WithMany()
                .HasForeignKey(o => o.StoreId).OnDelete(DeleteBehavior.Restrict);
            e.Property(o => o.Channel).HasConversion<string>().HasMaxLength(20);
            e.HasIndex(o => new { o.Channel, o.StoreId, o.CreatedAt });
            e.HasOne(o => o.SoldBy).WithMany()
                .HasForeignKey(o => o.SoldByUserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(o => o.DiscountApprovedBy).WithMany()
                .HasForeignKey(o => o.DiscountApprovedByUserId).OnDelete(DeleteBehavior.Restrict);
        });

        // OrderPayment — the parts a sale was paid in
        builder.Entity<OrderPayment>(e =>
        {
            e.Property(p => p.Amount).HasColumnType("decimal(18,2)");
            e.Property(p => p.Tendered).HasColumnType("decimal(18,2)");
            e.Property(p => p.Method).HasMaxLength(20);
            e.HasOne(p => p.Order).WithMany(o => o.Payments)
                .HasForeignKey(p => p.OrderId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(p => p.ReceivedBy).WithMany()
                .HasForeignKey(p => p.ReceivedByUserId).OnDelete(DeleteBehavior.Restrict);
        });

        // StockTransfer — stock moving between shops
        builder.Entity<StockTransfer>(e =>
        {
            e.HasIndex(t => t.TransferNumber).IsUnique();
            e.HasIndex(t => new { t.Status, t.CreatedAt });
            e.Property(t => t.TransferNumber).HasMaxLength(40);
            e.Property(t => t.Status).HasConversion<string>().HasMaxLength(20);
            e.HasOne(t => t.FromStore).WithMany()
                .HasForeignKey(t => t.FromStoreId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(t => t.ToStore).WithMany()
                .HasForeignKey(t => t.ToStoreId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(t => t.CreatedBy).WithMany()
                .HasForeignKey(t => t.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(t => t.CompletedBy).WithMany()
                .HasForeignKey(t => t.CompletedByUserId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<StockTransferItem>(e =>
        {
            e.HasOne(i => i.StockTransfer).WithMany(t => t.Items)
                .HasForeignKey(i => i.StockTransferId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(i => i.Product).WithMany()
                .HasForeignKey(i => i.ProductId).OnDelete(DeleteBehavior.Restrict);
        });

        // BankAccount — where customers pay for online orders; BankAccountLog — its history
        builder.Entity<DeliveryRate>(e =>
        {
            e.HasIndex(r => r.State).IsUnique();
            e.Property(r => r.State).HasMaxLength(50);
            e.Property(r => r.Fee).HasColumnType("decimal(18,2)");
            e.Property(r => r.UpdatedByName).HasMaxLength(150);
        });

        builder.Entity<BankAccount>(e =>
        {
            e.Property(b => b.BankName).HasMaxLength(100);
            e.Property(b => b.AccountName).HasMaxLength(150);
            e.Property(b => b.AccountNumber).HasMaxLength(10);
        });
        builder.Entity<BankAccountLog>(e =>
        {
            e.Property(l => l.Action).HasMaxLength(20);
            e.Property(l => l.Details).HasMaxLength(1000);
            e.Property(l => l.ChangedByName).HasMaxLength(200);
            e.Property(l => l.ChangedByEmail).HasMaxLength(256);
            e.HasIndex(l => l.ChangedAt);
        });

        // OrderItemSerial — IMEI / serial number per unit sold
        builder.Entity<OrderItemSerial>(e =>
        {
            e.Property(s => s.SerialNumber).HasMaxLength(50);
            e.HasIndex(s => s.SerialNumber);
            e.HasOne(s => s.OrderItem).WithMany(i => i.Serials)
                .HasForeignKey(s => s.OrderItemId).OnDelete(DeleteBehavior.Cascade);
        });

        // OrderItem
        builder.Entity<OrderItem>(e =>
        {
            e.Property(i => i.UnitPrice).HasColumnType("decimal(18,2)");
            e.Property(i => i.LineTotal).HasColumnType("decimal(18,2)");
            e.Property(i => i.OriginalUnitPrice).HasColumnType("decimal(18,2)");
            e.HasOne(i => i.Order).WithMany(o => o.Items)
                .HasForeignKey(i => i.OrderId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(i => i.Product).WithMany(p => p.OrderItems)
                .HasForeignKey(i => i.ProductId).OnDelete(DeleteBehavior.Restrict);
        });

        // OrderStatusLog ← NEW
        builder.Entity<OrderStatusLog>(e =>
        {
            e.Property(l => l.FromStatus).HasConversion<string>();
            e.Property(l => l.ToStatus).HasConversion<string>();
            e.HasOne(l => l.Order).WithMany()
                .HasForeignKey(l => l.OrderId).OnDelete(DeleteBehavior.Cascade);
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

        // Wishlist
        builder.Entity<Wishlist>(e =>
        {
            e.HasIndex(w => new { w.UserId, w.ProductId }).IsUnique();
            e.HasOne(w => w.User).WithMany(u => u.Wishlists)
                .HasForeignKey(w => w.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(w => w.Product).WithMany()
                .HasForeignKey(w => w.ProductId).OnDelete(DeleteBehavior.Cascade);
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

        // AppRole — store permissions as JSON
        builder.Entity<AppRole>(e =>
        {
            e.HasIndex(r => r.Name).IsUnique();
            e.Property(r => r.Permissions)
             .HasConversion(
                 v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                 v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>())
             .Metadata.SetValueComparer(new ValueComparer<List<string>>(
                 (a, b) => (a == null && b == null) || (a != null && b != null && a.SequenceEqual(b)),
                 v => v.Aggregate(0, (h, s) => HashCode.Combine(h, s.GetHashCode())),
                 v => v.ToList()));
        });

        // Seed system roles
        builder.Entity<AppRole>().HasData(
            new AppRole { Id = 1, Name = "Admin",    IsSystem = true, Description = "Full access to all features.",      Permissions = Perms.All.ToList(),  CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new AppRole { Id = 2, Name = "Customer", IsSystem = true, Description = "Standard customer account.",        Permissions = [],                  CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
        );

        // Seed the main shop. Existing stock and orders move here in the AddStores
        // migration; rename it and add the other shops from the admin Shops page.
        builder.Entity<Store>().HasData(
            new Store { Id = 1, Name = "Main Shop", Code = "MAIN", IsActive = true, SendsOnlineOrders = true,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
        );

        // Seed categories. The date is fixed: seed data that changes on every run (DateTime.UtcNow)
        // counts as an unmigrated model change, and EF Core 9+ refuses to migrate on startup.
        builder.Entity<Category>().HasData(
            new Category { Id = 1, Name = "Smartphones", Description = "Latest smartphones and accessories", IsActive = true, RequiresImei = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new Category { Id = 2, Name = "Tablets", Description = "Tablets and e-readers", IsActive = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new Category { Id = 3, Name = "Smart Watches", Description = "Wearable technology", IsActive = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new Category { Id = 4, Name = "Laptops", Description = "Laptops and ultrabooks", IsActive = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new Category { Id = 5, Name = "Accessories", Description = "Cases, chargers, and more", IsActive = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
        );
    }
}