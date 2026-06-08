using Domain.Entities.Import;
using Domain.Entities.Orders;
using Domain.Enums;

namespace Domain.Entities.Books;

public class Book
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
    public Guid CategoryId { get; set; }
    public decimal Price { get; set; }
    public BookCondition Condition { get; set; }
    public int Stock { get; set; }
    public string? ImageUrl { get; set; }
    public Guid? ImportTicketId { get; set; }
    public bool IsActive { get; set; }
    public string? Description { get; set; }

    public Category Category { get; set; } = null!;
    public ImportTicket? ImportTicket { get; set; }
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
