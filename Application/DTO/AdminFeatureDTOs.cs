namespace Application.DTO;

public record AdminDashboardSummaryDto(decimal TotalRevenue, int TotalOrders, int PendingOrders, int NewBuybackRequests);
public record AdminOrderDto(Guid Id, string Status, decimal TotalAmount, DateTime OrderDate);
public record AdminBuybackDto(Guid Id, string RequestCode, string Status, decimal? ProposedPrice, decimal? ApprovedPrice, DateTime CreatedAt);
public record AdminChartPointDto(string Label, decimal Value);
