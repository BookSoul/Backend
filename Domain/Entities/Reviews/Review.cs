using Domain.Entities.Accessories;
using Domain.Entities.Books;
using Domain.Entities.Identity;

namespace Domain.Entities.Reviews;

public class Review
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public Guid? BookId { get; set; }
    public Guid? AccessoryId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsHidden { get; set; } = false;

    public User Customer { get; set; } = null!;
    public Book? Book { get; set; }
    public Accessory? Accessory { get; set; }
}
