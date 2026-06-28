using Application.DTO;
using Domain.Enums;

namespace Application.Interface;

public interface ICheckoutService
{
    Task<CheckoutResponseDto> CreateOrderAsync(Guid customerId, CreateOrderRequest request, string? clientIp = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OrderSummaryDto>> GetMyOrdersAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task<OrderSummaryDto> GetOrderDetailAsync(Guid customerId, Guid orderId, CancellationToken cancellationToken = default);
    Task<OrderSummaryDto> CancelOrderAsync(Guid customerId, Guid orderId, CancelOrderRequest request, CancellationToken cancellationToken = default);
    Task<OrderSummaryDto> RequestReturnAsync(Guid customerId, Guid orderId, RequestReturnOrderRequest request, CancellationToken cancellationToken = default);
    Task<OrderSummaryDto> UpdateOrderStatusAsync(Guid orderId, OrderStatus status, CancellationToken cancellationToken = default);
    Task<OrderSummaryDto> ReorderAsync(Guid customerId, Guid orderId, CancellationToken cancellationToken = default);
}
