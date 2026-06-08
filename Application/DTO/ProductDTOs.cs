using Domain.Enums;

namespace Application.DTO;

public record ProductListItemDto(
    Guid Id,
    ProductType Type,
    string Name,
    decimal Price,
    int Stock,
    string? ImageUrl,
    string? CategoryName,
    string? BrandName
);

public record ProductDetailDto(
    Guid Id,
    ProductType Type,
    string Name,
    decimal Price,
    int Stock,
    string? ImageUrl,
    bool IsActive,
    string? Description,
    Guid? AuthorId,
    string? AuthorName,
    Guid? CategoryId,
    string? CategoryName,
    Guid? BrandId,
    string? BrandName,
    Guid? TypeId,
    string? AccessoryTypeName,
    string? Condition
);

public record CreateBookProductRequest(
    string Title,
    string AuthorName,
    Guid CategoryId,
    decimal Price,
    BookCondition Condition,
    int Stock,
    string? ImageUrl,
    string? Description,
    Guid ImportTicketId
);

public record CreateAccessoryProductRequest(
    string Name,
    Guid BrandId,
    Guid TypeId,
    decimal Price,
    int Stock,
    string? ImageUrl,
    Guid ImportTicketId
);

public record UpdateProductRequest(
    string? Name,
    decimal? Price,
    int? Stock,
    string? ImageUrl,
    string? Description,
    bool? IsActive
);
