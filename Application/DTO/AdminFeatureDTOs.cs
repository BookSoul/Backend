namespace Application.DTO;

public record AdminDashboardSummaryDto(decimal TotalRevenue, int TotalOrders, int PendingOrders, int NewBuybackRequests);
public record AdminOrderDto(Guid Id, string Status, decimal TotalAmount, DateTime OrderDate);
public record AdminBuybackDto(Guid Id, string RequestCode, string Status, decimal? ProposedPrice, decimal? ApprovedPrice, DateTime CreatedAt);
public record AdminChartPointDto(string Label, decimal Value);
public record AdminAnalyticsDto(
    AdminAnalyticsSummaryDto Summary,
    IReadOnlyList<AdminTrendPointDto> RevenueByMonth,
    IReadOnlyList<AdminTrendPointDto> OrdersByMonth,
    IReadOnlyList<AdminBreakdownDto> OrderStatusBreakdown,
    IReadOnlyList<AdminBreakdownDto> ProductApprovalBreakdown,
    IReadOnlyList<AdminBreakdownDto> ProductVisibilityBreakdown,
    IReadOnlyList<AdminBreakdownDto> InventoryByCategory,
    IReadOnlyList<AdminTopProductDto> TopProducts,
    IReadOnlyList<AdminBreakdownDto> BuybackStatusBreakdown,
    IReadOnlyList<AdminBreakdownDto> AccountBreakdown);
public record AdminAnalyticsSummaryDto(
    decimal TotalRevenue,
    int TotalOrders,
    int DeliveredOrders,
    int PendingOrders,
    int CancelledOrders,
    int ReturnRequests,
    decimal AverageOrderValue,
    int TotalBooks,
    int TotalAccessories,
    int ActiveProducts,
    int HiddenProducts,
    int PendingProducts,
    int OutOfStockProducts,
    int TotalStaff,
    int TotalCustomers,
    int NewBuybackRequests);
public record AdminTrendPointDto(string Label, int Year, int Month, decimal Revenue, int Orders);
public record AdminBreakdownDto(string Label, int Value, decimal? Amount = null);
public record AdminTopProductDto(string Name, string Type, int Quantity, decimal Revenue);
