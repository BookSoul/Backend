using Domain.Entities.Identity;
using Domain.Enums;

namespace Domain.Entities.Orders;

public class ShoppingCartItem
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public Guid? BookId { get; set; }
    public Guid? AccessoryId { get; set; }
    public int Quantity { get; set; }
    public DateTime AddedAt { get; set; }

    public User Customer { get; set; } = null!;
}
