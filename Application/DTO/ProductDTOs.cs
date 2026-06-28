using Domain.Enums;

namespace Application.DTO;

public record ProductListItemDto(
    Guid Id,
    string Type,
    string Code,
    string? Title,
    string Name,
    string? Author,
    string? AuthorName,
    decimal Price,
    decimal? OriginalPrice,
    int Stock,
    bool InStock,
    string? Image,
    string? ImageUrl,
    string? Category,
    string? CategoryName,
    string? Brand,
    string? BrandName
);

public record ProductDetailDto(
    Guid Id,
    string Type,
    string? Title,
    string Name,
    string? Author,
    decimal Price,
    int Stock,
    bool InStock,
    string? Image,
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
    string? Condition,
    decimal? OriginalPrice = null,
    bool? Featured = null,
    string? Publisher = null,
    string? Year = null,
    string? Pages = null,
    string? Language = null,
    string? Seller = null,
    string? SellerNote = null,
    string? ApprovalStatus = null,
    string? RejectionNote = null,
    string? CreatedBy = null,
    string? CreatedByName = null,
    string? CreatedByRole = null,
    string? Code = null,
    DateTime? CreatedAt = null,
    Guid? ImportTicketId = null
);

public class CreateBookProductRequest : BookUpsertRequest
{
    public Guid? ImportTicketId { get; set; }
}

public class CreateAccessoryProductRequest : AccessoryUpsertRequest
{
    public Guid? ImportTicketId { get; set; }
}

public class UpdateProductRequest
{
    public string? Name { get; set; }
    public string? Title { get; set; }
    public string? Author { get; set; }
    public string? AuthorName { get; set; }
    public Guid? CategoryId { get; set; }
    public string? Category { get; set; }
    public Guid? BrandId { get; set; }
    public string? Brand { get; set; }
    public Guid? TypeId { get; set; }
    public decimal? Price { get; set; }
    public decimal? OriginalPrice { get; set; }
    public int? Stock { get; set; }
    public bool? InStock { get; set; }
    public string? Image { get; set; }
    public string? ImageUrl { get; set; }
    public string? Description { get; set; }
    public string? Condition { get; set; }
    public string? Publisher { get; set; }
    public string? Year { get; set; }
    public string? Pages { get; set; }
    public string? Language { get; set; }
    public string? Seller { get; set; }
    public string? SellerNote { get; set; }
    public bool? Featured { get; set; }
    public bool? IsActive { get; set; }
    public string? ApprovalStatus { get; set; }
    public string? RejectionNote { get; set; }
    public string? CreatedBy { get; set; }
    public string? CreatedByName { get; set; }
    public string? CreatedByRole { get; set; }
}
