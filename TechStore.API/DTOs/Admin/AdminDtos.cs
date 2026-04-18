namespace TechStore.API.DTOs.Admin;

public record DashboardStatsDto(
    int TotalOrders,
    int TodayOrders,
    decimal TotalRevenue,
    decimal MonthRevenue,
    int TotalProducts,
    int ActiveProducts,
    int LowStockProducts,
    int TotalCustomers,
    int NewCustomersThisMonth,
    List<RevenueByDayDto> RevenueChart,
    List<TopProductDto> TopProducts,
    List<OrdersByStatusDto> OrdersByStatus
);

public record RevenueByDayDto(string Date, decimal Revenue, int Orders);
public record TopProductDto(int ProductId, string ProductName, int TotalSold, decimal Revenue);
public record OrdersByStatusDto(string Status, int Count);

public record SalesReportDto(
    DateTime FromDate,
    DateTime ToDate,
    int TotalOrders,
    decimal TotalRevenue,
    decimal AverageOrderValue,
    List<RevenueByDayDto> DailyBreakdown,
    List<TopProductDto> TopProducts
);

public record UserAdminDto(
    int Id,
    string FirstName,
    string LastName,
    string Email,
    string? Phone,
    string Role,
    bool IsActive,
    int TotalOrders,
    decimal TotalSpent,
    DateTime CreatedAt
);
