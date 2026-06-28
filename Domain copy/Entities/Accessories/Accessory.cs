using Domain.Entities.Books;
using Domain.Entities.Import;

namespace Domain.Entities.Accessories;

public class Accessory
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid BrandId { get; set; }
    public Guid TypeId { get; set; }
    public decimal Price { get; set; }
    public decimal? OriginalPrice { get; set; }
    public int Stock { get; set; }
    public string? ImageUrl { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public Guid? ImportTicketId { get; set; }
    public string ApprovalStatus { get; set; } = "published";
    public string? RejectionNote { get; set; }
    public string? CreatedBy { get; set; }
    public string? CreatedByName { get; set; }
    public string? CreatedByRole { get; set; }

    public Brand Brand { get; set; } = null!;
    public AccessoryType Type { get; set; } = null!;
    public ImportTicket? ImportTicket { get; set; }
}
