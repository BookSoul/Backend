namespace Application.DTO;

public record HomePageDto(
    IReadOnlyList<ProductListItemDto> FeaturedBooks,
    IReadOnlyList<ProductListItemDto> FeaturedAccessories,
    IReadOnlyList<HomeBannerDto> Banners
);

public record HomeBannerDto(Guid Id, string Title, string ImageUrl, string? LinkUrl, int DisplayOrder);
