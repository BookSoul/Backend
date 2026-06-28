using Domain.Entities.Import;
using Domain.Entities.Orders;
using Domain.Enums;

namespace Domain.Entities.Books;

public class Book
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string Title { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
    public Guid CategoryId { get; set; }
    public decimal Price { get; set; }
    public decimal? OriginalPrice { get; set; }
    public BookCondition Condition { get; set; }
    public int Stock { get; set; }
    public string? ImageUrl { get; set; }
    public Guid? ImportTicketId { get; set; }
    public bool IsActive { get; set; }
    public string? Description { get; set; }
    public string? Publisher { get; set; }
    public string? Year { get; set; }
    public string? Pages { get; set; }
    public string? Language { get; set; }
    public string? Seller { get; set; }
    public string? SellerNote { get; set; }
    public bool Featured { get; set; }
    public string ApprovalStatus { get; set; } = "published";
    public string? RejectionNote { get; set; }
    public string? CreatedBy { get; set; }
    public string? CreatedByName { get; set; }
    public string? CreatedByRole { get; set; }

    public Category Category { get; set; } = null!;
    public ImportTicket? ImportTicket { get; set; }
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
