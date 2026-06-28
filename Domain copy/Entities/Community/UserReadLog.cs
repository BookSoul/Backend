namespace Domain.Entities.Community;

public class UserReadLog
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public Guid BookId { get; set; }
    public DateTime ReadAt { get; set; }
}
