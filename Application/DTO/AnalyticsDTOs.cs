namespace Application.DTO;

public record DashboardAnalyticsDto(
    decimal TotalRevenue,
    int TotalOrders,
    int PendingOrders,
    int PendingImportTickets,
    int PendingBuybackRequests,
    IReadOnlyList<MonthlyRevenueDto> RevenueByMonth
);

public record MonthlyRevenueDto(int Year, int Month, decimal Revenue);
