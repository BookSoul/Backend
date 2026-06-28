using Domain.Enums;

namespace Application.DTO;

public record CartItemDto(
    Guid ProductId,
    ProductType ProductType,
    string Name,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal,
    bool IsBlindBox,
    string? Id = null,
    string? Type = null,
    string? Title = null,
    decimal? Price = null,
    string? Image = null,
    string? Author = null,
    string? Brand = null,
    string? Category = null,
    string? Tier = null,
    bool Selected = true
);

public record CartDto(IReadOnlyList<CartItemDto> Items, decimal SubTotal);

public record CheckoutResponseDto(
    Guid OrderId,
    decimal SubTotal,
    decimal ShippingFee,
    decimal DiscountAmount,
    decimal FinalTotal,
    string Status,
    string? AppliedVoucherCode,
    string? PaymentUrl = null,
    string? PaymentStatus = null,
    string? PaymentProvider = null,
    string? PaymentTxnRef = null
);

public record CheckoutRequestDto(
    string ReceiverName,
    string ReceiverPhone,
    string ReceiverEmail,
    string ShippingAddress,
    string? Notes,
    string? VoucherCode,
    PaymentMethod PaymentMethod,
    IReadOnlyList<BlindBoxOrderLine>? BlindBoxLines
);

public record OrderItemDto(
    Guid? ProductId,
    ProductType? ProductType,
    string Name,
    int Quantity,
    decimal UnitPrice,
    bool IsBlindBox,
    string? Id = null,
    string? Type = null,
    string? Title = null,
    decimal? Price = null,
    string? Image = null,
    string? Author = null,
    string? Brand = null,
    string? Category = null,
    string? Tier = null
);

public record OrderSummaryDto(
    Guid Id,
    DateTime OrderDate,
    decimal TotalAmount,
    string Status,
    string PaymentMethod,
    IReadOnlyList<OrderItemDto> Items,
    string? UserId = null,
    string? UserName = null,
    string? UserEmail = null,
    string? Date = null,
    decimal? Total = null,
    ShippingAddressDto? ShippingAddress = null,
    string? CancellationReason = null,
    DateTime? CancelledAt = null,
    string? ReturnReason = null,
    string? ReturnReasonDetail = null,
    string? ReturnReviewNote = null,
    DateTime? ReturnRequestedAt = null,
    DateTime? ReturnReviewedAt = null,
    string? PaymentStatus = null,
    string? PaymentProvider = null,
    string? PaymentTxnRef = null,
    string? PaymentTransactionNo = null,
    string? PaymentResponseCode = null,
    DateTime? PaidAt = null
);

public record ShippingAddressDto(
    string Name,
    string Phone,
    string Address,
    string City
);

public class CreateOrderRequest
{
    public string? ReceiverName { get; set; }
    public string? ReceiverPhone { get; set; }
    public string? ReceiverEmail { get; set; }
    public string? UserName { get; set; }
    public string? UserEmail { get; set; }
    public ShippingAddressDto? ShippingAddress { get; set; }
    public string? ShippingAddressText { get; set; }
    public string? Notes { get; set; }
    public string? Note { get; set; }
    public string? VoucherCode { get; set; }
    public string? PaymentMethod { get; set; }
    public IReadOnlyList<FrontendOrderItemDto>? Items { get; set; }
    public decimal? Total { get; set; }
    public IReadOnlyList<BlindBoxOrderLine>? BlindBoxLines { get; set; }
}

public record FrontendOrderItemDto(
    string? Id,
    string? Type,
    string? Name,
    string? Title,
    decimal Price,
    int Quantity,
    string? Image,
    string? Author,
    string? Brand,
    string? Category,
    string? Tier
);

public record BlindBoxOrderLine(int Quantity, decimal UnitPrice);

public record AddCartItemRequest(Guid ProductId, ProductType ProductType, int Quantity);

public record UpdateOrderStatusRequest(OrderStatus Status, string? Reason = null);

public record CancelOrderRequest(string Reason);

public record RequestReturnOrderRequest(string Reason, string? Detail);

public record ReviewReturnOrderRequest(string? Note);

public record AssignBlindBoxProductRequest(Guid OrderItemId, Guid ProductId, ProductType ProductType);
