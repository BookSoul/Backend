using Application.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers;

[ApiController]
[Authorize(Roles = "Shipper,Admin")]
[Route("api/shipper")]
public class ShipperController : ControllerBase
{
    private readonly IShipperService _shipperService;

    public ShipperController(IShipperService shipperService)
    {
        _shipperService = shipperService;
    }

    [HttpGet("pickups")]
    public async Task<IActionResult> GetPickups(CancellationToken cancellationToken)
        => Ok(await _shipperService.GetPickupTasksAsync(cancellationToken));

    [HttpPatch("pickups/{sourceType}/{requestId:guid}/picked-up")]
    public async Task<IActionResult> MarkPickedUp(string sourceType, Guid requestId, CancellationToken cancellationToken)
        => Ok(await _shipperService.MarkPickedUpAsync(sourceType, requestId, cancellationToken));
}
