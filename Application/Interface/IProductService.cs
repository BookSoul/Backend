using Application.DTO;
using Domain.Enums;

namespace Application.Interface;

public interface IProductService
{
    Task<IReadOnlyList<ProductListItemDto>> SearchProductsAsync(
        ProductType? type,
        string? keyword,
        Guid? categoryId,
        Guid? brandId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<ProductDetailDto?> GetProductByIdAsync(Guid id, ProductType type, CancellationToken cancellationToken = default);

    Task<ProductDetailDto> CreateBookAsync(CreateBookProductRequest request, CancellationToken cancellationToken = default);

    Task<ProductDetailDto> CreateAccessoryAsync(CreateAccessoryProductRequest request, CancellationToken cancellationToken = default);

    Task<ProductDetailDto> UpdateProductAsync(Guid id, ProductType type, UpdateProductRequest request, CancellationToken cancellationToken = default);
}
