using Application.DTO;
using Domain.Enums;

namespace Application.Interface;

public interface ICheckoutService
{
    Task<CheckoutResponseDto> CreateOrderAsync(Guid customerId, CreateOrderRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OrderSummaryDto>> GetMyOrdersAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task<OrderSummaryDto> GetOrderDetailAsync(Guid customerId, Guid orderId, CancellationToken cancellationToken = default);
    Task<OrderSummaryDto> UpdateOrderStatusAsync(Guid orderId, OrderStatus status, CancellationToken cancellationToken = default);
    Task<OrderSummaryDto> ReorderAsync(Guid customerId, Guid orderId, CancellationToken cancellationToken = default);
}
