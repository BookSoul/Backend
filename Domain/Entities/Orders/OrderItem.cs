using Domain.Enums;

namespace Domain.Entities.Orders;

public class OrderItem
{
    public Guid OrderId { get; set; }
    public Guid? BookId { get; set; }
    public Guid? AccessoryId { get; set; }
    public BlindBoxTier? BlindBoxTier { get; set; }
    public string? BlindBoxGenre { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; }

    public Order Order { get; set; } = null!;
}
