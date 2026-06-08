using Domain.Entities.Identity;
using Domain.Enums;

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

    public User Customer { get; set; } = null!;
}
