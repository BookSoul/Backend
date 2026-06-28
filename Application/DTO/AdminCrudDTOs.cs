namespace Application.DTO;

public class BookUpsertRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Author { get; set; }
    public string? AuthorName { get; set; }
    public Guid? CategoryId { get; set; }
    public string? Category { get; set; }
    public decimal Price { get; set; }
    public decimal? OriginalPrice { get; set; }
    public int Stock { get; set; }
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

public class AccessoryUpsertRequest
{
    public string Name { get; set; } = string.Empty;
    public Guid? BrandId { get; set; }
    public string? Brand { get; set; }
    public Guid? TypeId { get; set; }
    public string? Category { get; set; }
    public decimal Price { get; set; }
    public decimal? OriginalPrice { get; set; }
    public int Stock { get; set; }
    public bool? InStock { get; set; }
    public string? Image { get; set; }
    public string? ImageUrl { get; set; }
    public string? Description { get; set; }
    public bool? IsActive { get; set; }
    public string? ApprovalStatus { get; set; }
    public string? RejectionNote { get; set; }
    public string? CreatedBy { get; set; }
    public string? CreatedByName { get; set; }
    public string? CreatedByRole { get; set; }
}
