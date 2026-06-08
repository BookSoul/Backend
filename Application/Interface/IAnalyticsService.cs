using Application.DTO;

namespace Application.Interface;

public interface IAnalyticsService
{
    Task<DashboardAnalyticsDto> GetDashboardAsync(DateTime? from, DateTime? to, CancellationToken cancellationToken = default);
}
