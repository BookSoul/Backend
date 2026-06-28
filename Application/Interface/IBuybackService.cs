using Application.DTO;
using Domain.Enums;

namespace Application.Interface;

public interface IBuybackService
{
    Task<BuybackRequestDto> CreateRequestAsync(
        Guid customerId,
        BuybackType type,
        decimal proposedPrice,
        IReadOnlyList<ImageUploadPayload> images,
        CancellationToken cancellationToken = default);

    Task<BuybackRequestDto> CreateRequestAsync(
        Guid customerId,
        CreateBuybackRequest request,
        IReadOnlyList<ImageUploadPayload> images,
        CancellationToken cancellationToken = default);

    Task<BuybackRequestDto> ReviewRequestAsync(Guid requestId, ApproveBuybackRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BuybackRequestDto>> GetMyRequestsAsync(Guid customerId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BuybackRequestDto>> GetRequestsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BuybackRequestDto>> GetPendingRequestsAsync(CancellationToken cancellationToken = default);
}
