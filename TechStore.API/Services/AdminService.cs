using Microsoft.EntityFrameworkCore;
using TechStore.API.Data;
using TechStore.API.DTOs.Admin;
using TechStore.API.Models;
using TechStore.API.Repositories.Interfaces;
using TechStore.API.Services.Interfaces;

namespace TechStore.API.Services;

public class AdminService : IAdminService
{
    private readonly AppDbContext _db;
    private readonly IOrderRepository _orders;
    private readonly IInventoryRepository _inventory;
    private readonly IUserRepository _users;
    private readonly IProductRepository _products;

    public AdminService(
        AppDbContext db,
        IOrderRepository orders,
        IInventoryRepository inventory,
        IUserRepository users,
        IProductRepository products)
    {
        _db = db;
        _orders = orders;
        _inventory = inventory;
        _users = users;
        _products = products;
    }

    public async Task<DashboardStatsDto> GetDashboardStatsAsync()
    {
        var totalOrders    = await _orders.CountAsync();
        var todayOrders    = await _orders.CountTodayAsync();
        var totalRevenue   = await _orders.GetTotalRevenueAsync();
        var monthRevenue   = await _orders.GetMonthRevenueAsync();
        var totalProducts  = await _products.CountAsync();
        var lowStock       = await _inventory.CountLowStockAsync();
        var totalCustomers = await _users.CountAsync();
        var newCustomers   = await _users.CountNewThisMonthAsync();

        var revenueChart   = await GetLast30DaysRevenueAsync();
        var topProducts    = await GetTopProductsAsync(5);
        var ordersByStatus = await GetOrdersByStatusAsync();

        return new DashboardStatsDto(
            totalOrders, todayOrders,
            totalRevenue, monthRevenue,
            totalProducts, totalProducts,
            lowStock,
            totalCustomers, newCustomers,
            revenueChart, topProducts, ordersByStatus
        );
    }

    public async Task<SalesReportDto> GetSalesReportAsync(DateTime from, DateTime to)
    {
        var orders = await _db.Orders
            .Include(o => o.Items)
            .Where(o => o.CreatedAt >= from && o.CreatedAt <= to
                     && o.PaymentStatus == PaymentStatus.Paid)
            .ToListAsync();

        var daily = orders
            .GroupBy(o => o.CreatedAt.Date)
            .OrderBy(g => g.Key)
            .Select(g => new RevenueByDayDto(
                g.Key.ToString("yyyy-MM-dd"),
                g.Sum(o => o.Total),
                g.Count()))
            .ToList();

        // Pull raw order items to memory first, then group
        var rawItems = await _db.OrderItems
            .Where(i => i.Order.CreatedAt >= from && i.Order.CreatedAt <= to
                     && i.Order.PaymentStatus == PaymentStatus.Paid)
            .Select(i => new { i.ProductId, i.ProductName, i.Quantity, i.LineTotal })
            .ToListAsync();

        var topProducts = rawItems
            .GroupBy(i => new { i.ProductId, i.ProductName })
            .Select(g => new TopProductDto(
                g.Key.ProductId,
                g.Key.ProductName,
                g.Sum(i => i.Quantity),
                g.Sum(i => i.LineTotal)))
            .OrderByDescending(t => t.Revenue)
            .Take(10)
            .ToList();

        var totalRevenue   = orders.Sum(o => o.Total);
        var avgOrderValue  = orders.Count > 0 ? totalRevenue / orders.Count : 0;

        return new SalesReportDto(from, to, orders.Count, totalRevenue, avgOrderValue, daily, topProducts);
    }

    public async Task<PagedResult<UserAdminDto>> GetUsersAsync(int page, int pageSize)
    {
        var total = await _db.Users.CountAsync();

        var users = await _db.Users
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new UserAdminDto(
                u.Id, u.FirstName, u.LastName, u.Email, u.Phone,
                u.Role, u.IsActive,
                u.Orders.Count,
                u.Orders.Where(o => o.PaymentStatus == PaymentStatus.Paid).Sum(o => o.Total),
                u.CreatedAt))
            .ToListAsync();

        return new PagedResult<UserAdminDto>(users, total, page, pageSize);
    }

    public async Task<bool> ToggleUserStatusAsync(int userId)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return false;
        user.IsActive  = !user.IsActive;
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private async Task<List<RevenueByDayDto>> GetLast30DaysRevenueAsync()
    {
        var from = DateTime.UtcNow.AddDays(-29).Date;

        // Fetch to memory first — EF can't translate .Date grouping on all providers
        var rows = await _db.Orders
            .Where(o => o.CreatedAt >= from && o.PaymentStatus == PaymentStatus.Paid)
            .Select(o => new { o.CreatedAt, o.Total })
            .ToListAsync();

        return rows
            .GroupBy(o => o.CreatedAt.Date)
            .OrderBy(g => g.Key)
            .Select(g => new RevenueByDayDto(
                g.Key.ToString("yyyy-MM-dd"),
                g.Sum(o => o.Total),
                g.Count()))
            .ToList();
    }

    private async Task<List<TopProductDto>> GetTopProductsAsync(int count)
    {
        // Pull the raw fields MySQL can give us, then group in memory
        var rows = await _db.OrderItems
            .Select(i => new { i.ProductId, i.ProductName, i.Quantity, i.LineTotal })
            .ToListAsync();

        return rows
            .GroupBy(i => new { i.ProductId, i.ProductName })
            .Select(g => new TopProductDto(
                g.Key.ProductId,
                g.Key.ProductName,
                g.Sum(i => i.Quantity),
                g.Sum(i => i.LineTotal)))
            .OrderByDescending(t => t.TotalSold)
            .Take(count)
            .ToList();
    }

    private async Task<List<OrdersByStatusDto>> GetOrdersByStatusAsync()
    {
        // Pull statuses to memory, then group — avoids enum-to-string translation issues
        var rows = await _db.Orders
            .Select(o => o.Status)
            .ToListAsync();

        return rows
            .GroupBy(s => s)
            .Select(g => new OrdersByStatusDto(g.Key.ToString(), g.Count()))
            .ToList();
    }
}