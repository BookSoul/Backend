using Application.DTO;
using Domain.Enums;

namespace Application.Interface;

public interface ICartService
{
    Task<CartDto> GetCartAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task<CartDto> AddItemAsync(Guid customerId, Guid productId, ProductType productType, int quantity, CancellationToken cancellationToken = default);
    Task<CartDto> UpdateItemAsync(Guid customerId, Guid productId, ProductType productType, int quantity, CancellationToken cancellationToken = default);
    Task<CartDto> RemoveItemAsync(Guid customerId, Guid productId, ProductType productType, CancellationToken cancellationToken = default);
    Task ClearCartAsync(Guid customerId, CancellationToken cancellationToken = default);
}
