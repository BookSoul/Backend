using Application.DTO;

namespace Application.Interface;

public interface IDonateService
{
    Task<DonateRequestDto> CreateAsync(Guid customerId, CreateDonateRequest request, CancellationToken cancellationToken = default);
    Task<DonateRequestDto> CreateAsync(Guid customerId, CreateDonateRequest request, IReadOnlyList<ImageUploadPayload> images, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DonateRequestDto>> GetMyRequestsAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DonateRequestDto>> GetRequestsAsync(CancellationToken cancellationToken = default);
    Task<DonateRequestDto> ReviewAsync(Guid requestId, ReviewDonateRequest request, CancellationToken cancellationToken = default);
}
