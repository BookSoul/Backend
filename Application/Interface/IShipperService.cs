using Application.DTO;

namespace Application.Interface;

public interface IShipperService
{
    Task<IReadOnlyList<PickupTaskDto>> GetPickupTasksAsync(CancellationToken cancellationToken = default);
    Task<PickupTaskDto> MarkPickedUpAsync(string sourceType, Guid requestId, CancellationToken cancellationToken = default);
}
