using Domain.Enums;

namespace Domain.Entities.Orders;

public class OrderItem
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid? BookId { get; set; }
    public Guid? AccessoryId { get; set; }
    public BlindBoxTier? BlindBoxTier { get; set; }
    public string? BlindBoxGenre { get; set; }
    public string? ProductName { get; set; }
    public string? ProductImage { get; set; }
    public string? ProductTypeText { get; set; }
    public string? Author { get; set; }
    public string? Brand { get; set; }
    public string? Category { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; }

    public Order Order { get; set; } = null!;
}
