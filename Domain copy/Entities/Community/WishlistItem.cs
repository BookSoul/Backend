namespace Domain.Entities.Community;

public class WishlistItem
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public Guid? BookId { get; set; }
    public Guid? AccessoryId { get; set; }
}
