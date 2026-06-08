using Application.DTO;

namespace Application.Interface;

public interface IReviewService
{
    Task<ReviewDto> CreateAsync(Guid customerId, CreateReviewRequest request, CancellationToken cancellationToken = default);
    Task<ReviewDto> UpdateAsync(Guid customerId, Guid reviewId, UpdateReviewRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid customerId, Guid reviewId, CancellationToken cancellationToken = default);
}
