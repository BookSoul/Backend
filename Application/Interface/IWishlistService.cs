using Application.DTO;

namespace Application.Interface;

public interface IWishlistService
{
    Task<IReadOnlyList<ProductListItemDto>> GetWishlistAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task ToggleAsync(Guid customerId, Guid productId, Domain.Enums.ProductType productType, CancellationToken cancellationToken = default);
}
