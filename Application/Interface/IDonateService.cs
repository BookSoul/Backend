using Application.DTO;

namespace Application.Interface;

public interface IDonateService
{
    Task<DonateRequestDto> CreateAsync(Guid customerId, CreateDonateRequest request, CancellationToken cancellationToken = default);
}
