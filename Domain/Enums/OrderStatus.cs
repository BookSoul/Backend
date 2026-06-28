namespace Domain.Enums;

public enum OrderStatus
{
    Pending = 0,
    AwaitingPreparation = 1,
    Confirmed = 1,
    Processing = 1,
    ReadyForDelivery = 2,
    Packing = 2,
    Shipping = 3,
    Shipped = 3,
    Delivered = 4,
    Cancelled = 5,
    ReturnRequested = 6,
    Returned = 7,
    ReturnRejected = 8
}
