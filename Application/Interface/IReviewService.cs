using Application.DTO;

namespace Application.Interface;

public interface IReviewService
{
    // ── USER: CUD ────────────────────────────────────────────────────────────
    Task<ReviewDto> CreateAsync(Guid customerId, CreateReviewRequest request, CancellationToken cancellationToken = default);
    Task<ReviewDto> UpdateAsync(Guid customerId, Guid reviewId, UpdateReviewRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid customerId, Guid reviewId, CancellationToken cancellationToken = default);

    // ── USER: READ MY REVIEWS ─────────────────────────────────────────────────
    Task<PagedResult<ReviewDto>> GetMyReviewsAsync(Guid customerId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<ReviewEligibilityDto> CheckEligibilityAsync(Guid customerId, Guid? bookId, Guid? accessoryId, CancellationToken cancellationToken = default);

    // ── PUBLIC: GET REVIEWS BY PRODUCT ───────────────────────────────────────
    Task<PagedResult<ReviewDto>> GetByBookIdAsync(Guid bookId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<PagedResult<ReviewDto>> GetByAccessoryIdAsync(Guid accessoryId, int page, int pageSize, CancellationToken cancellationToken = default);

    // ── PUBLIC: RATING SUMMARY ────────────────────────────────────────────────
    Task<ProductReviewSummaryDto> GetRatingSummaryAsync(Guid? bookId, Guid? accessoryId, CancellationToken cancellationToken = default);

    // ── ADMIN ─────────────────────────────────────────────────────────────────
    Task<PagedResult<ReviewDto>> AdminGetAllAsync(Guid? bookId, Guid? accessoryId, int? rating, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<ReviewDto> AdminToggleHideAsync(Guid reviewId, CancellationToken cancellationToken = default);
    Task<AdminReviewStatisticsDto> GetAdminStatisticsAsync(CancellationToken cancellationToken = default);
    Task AdminDeleteAsync(Guid reviewId, CancellationToken cancellationToken = default);
}


