namespace Domain.Enums;

public enum OrderStatus
{
    Pending = 0,
    Confirmed = 1,
    Processing = 1,
    Packing = 2,
    Shipping = 3,
    Shipped = 3,
    Delivered = 4,
    Cancelled = 5
}
