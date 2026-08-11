namespace Application.DTO;

public record DashboardAnalyticsDto(
    decimal TotalRevenue,
    int TotalOrders,
    int PendingOrders,
    int PendingImportTickets,
    int PendingBuybackRequests,
    int TotalReviews,
    IReadOnlyList<MonthlyRevenueDto> RevenueByMonth
);

public record MonthlyRevenueDto(int Year, int Month, decimal Revenue);
