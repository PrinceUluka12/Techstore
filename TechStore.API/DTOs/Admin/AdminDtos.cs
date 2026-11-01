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
    List<OrdersByStatusDto> OrdersByStatus,
    NeedsAttentionDto? Attention = null
);

// What staff should act on, shown at the top of the dashboard
public record NeedsAttentionDto(
    int AwaitingPayment,    // online orders not paid yet
    int PaymentDueSoon,     // ...of those, cancelled within the next 6 hours if still unpaid
    int Paid,               // paid online orders nobody has started on (Confirmed)
    int BeingPrepared,      // Processing
    int ReadyForPickup,     // waiting for the customer to collect
    int LowStock,           // products at or below their low-stock level, across shops
    int TransfersInTransit  // stock sent between shops, not yet received
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
    List<TopProductDto> TopProducts,
    List<SalesBreakdownDto> ByShop,
    List<SalesBreakdownDto> ByChannel // "Online" / "InStore"
);

public record SalesBreakdownDto(string Name, int Orders, decimal Revenue);

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
    DateTime CreatedAt,
    int? StoreId,
    string? StoreName
);
