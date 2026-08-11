using Application.DTO;
using Application.Interface;
using Domain.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Service;

public class AnalyticsService : IAnalyticsService
{
    private readonly AppDbContext _context;

    public AnalyticsService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardAnalyticsDto> GetDashboardAsync(DateTime? from, DateTime? to, CancellationToken cancellationToken = default)
    {
        var start = from ?? DateTime.UtcNow.AddMonths(-6);
        var end = to ?? DateTime.UtcNow;

        var ordersQuery = _context.Orders.AsNoTracking()
            .Where(o => o.OrderDate >= start && o.OrderDate <= end);

        var totalRevenue = await ordersQuery
            .Where(o => o.Status == OrderStatus.Delivered)
            .SumAsync(o => o.TotalAmount, cancellationToken);

        var totalOrders = await ordersQuery.CountAsync(cancellationToken);
        var pendingOrders = await ordersQuery.CountAsync(o => o.Status == OrderStatus.Pending || o.Status == OrderStatus.AwaitingPreparation || o.Status == OrderStatus.ReadyForDelivery, cancellationToken);
        var pendingImportTickets = await _context.ImportTickets.CountAsync(
            t => t.Status == ImportTicketStatus.Pending && t.SubmittedAt != null, cancellationToken);
        var pendingBuyback = await _context.BuybackRequests.CountAsync(r => r.Status == BuybackRequestStatus.Pending, cancellationToken);

        var totalReviews = await _context.Reviews
            .Where(r => r.CreatedAt >= start && r.CreatedAt <= end)
            .CountAsync(cancellationToken);

        var revenueByMonth = await ordersQuery
            .Where(o => o.Status == OrderStatus.Delivered)
            .GroupBy(o => new { o.OrderDate.Year, o.OrderDate.Month })
            .Select(g => new MonthlyRevenueDto(g.Key.Year, g.Key.Month, g.Sum(x => x.TotalAmount)))
            .OrderBy(x => x.Year)
            .ThenBy(x => x.Month)
            .ToListAsync(cancellationToken);

        return new DashboardAnalyticsDto(
            totalRevenue,
            totalOrders,
            pendingOrders,
            pendingImportTickets,
            pendingBuyback,
            totalReviews,
            revenueByMonth);
    }
}
