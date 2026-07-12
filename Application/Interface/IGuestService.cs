using Application.DTO;

namespace Application.Interface;

public interface IGuestService
{
    Task<IReadOnlyList<ProductListItemDto>> GetBooksAsync(string? keyword, Guid? categoryId, string? condition, decimal? minPrice, decimal? maxPrice, string? sortBy, CancellationToken cancellationToken = default);
    Task<ProductDetailDto> GetBookByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductListItemDto>> GetAccessoriesAsync(string? keyword, Guid? brandId, Guid? typeId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BlindBoxTierDto>> GetBlindBoxTiersAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LiveSearchItemDto>> LiveSearchAsync(string keyword, CancellationToken cancellationToken = default);
}
