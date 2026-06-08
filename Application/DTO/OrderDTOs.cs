using Domain.Enums;

namespace Application.DTO;

public record CartItemDto(
    Guid ProductId,
    ProductType ProductType,
    string Name,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal,
    bool IsBlindBox
);

public record CartDto(IReadOnlyList<CartItemDto> Items, decimal SubTotal);

public record CheckoutResponseDto(
    Guid OrderId,
    decimal SubTotal,
    decimal ShippingFee,
    decimal DiscountAmount,
    decimal FinalTotal,
    string Status,
    string? AppliedVoucherCode
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
    bool IsBlindBox
);

public record OrderSummaryDto(
    Guid Id,
    DateTime OrderDate,
    decimal TotalAmount,
    string Status,
    string PaymentMethod,
    IReadOnlyList<OrderItemDto> Items
);

public record CreateOrderRequest(
    string ReceiverName,
    string ReceiverPhone,
    string ReceiverEmail,
    string ShippingAddress,
    string? Notes,
    string? VoucherCode,
    PaymentMethod PaymentMethod,
    IReadOnlyList<BlindBoxOrderLine>? BlindBoxLines
);

public record BlindBoxOrderLine(int Quantity, decimal UnitPrice);

public record AddCartItemRequest(Guid ProductId, ProductType ProductType, int Quantity);

public record UpdateOrderStatusRequest(OrderStatus Status);

public record AssignBlindBoxProductRequest(Guid OrderItemId, Guid ProductId, ProductType ProductType);
