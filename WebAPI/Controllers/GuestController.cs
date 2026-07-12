using Application.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/guest")]
public class GuestController : ControllerBase
{
    private readonly IGuestService _guestService;

    public GuestController(IGuestService guestService)
    {
        _guestService = guestService;
    }

    [HttpGet("books")]
    public async Task<IActionResult> GetBooks([FromQuery] string? keyword, [FromQuery] Guid? categoryId, [FromQuery] string? condition, [FromQuery] decimal? minPrice, [FromQuery] decimal? maxPrice, [FromQuery] string? sortBy, CancellationToken cancellationToken)
        => Ok(await _guestService.GetBooksAsync(keyword, categoryId, condition, minPrice, maxPrice, sortBy, cancellationToken));

    [HttpGet("books/{id:guid}")]
    public async Task<IActionResult> GetBook(Guid id, CancellationToken cancellationToken)
        => Ok(await _guestService.GetBookByIdAsync(id, cancellationToken));

    [HttpGet("accessories")]
    public async Task<IActionResult> GetAccessories([FromQuery] string? keyword, [FromQuery] Guid? brandId, [FromQuery] Guid? typeId, CancellationToken cancellationToken)
        => Ok(await _guestService.GetAccessoriesAsync(keyword, brandId, typeId, cancellationToken));

    [HttpGet("blindbox/tiers")]
    public async Task<IActionResult> GetBlindBoxTiers(CancellationToken cancellationToken)
        => Ok(await _guestService.GetBlindBoxTiersAsync(cancellationToken));

    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories(CancellationToken cancellationToken)
        => Ok(await _guestService.GetCategoriesAsync(cancellationToken));

    [HttpGet("search/live")]
    public async Task<IActionResult> LiveSearch([FromQuery] string keyword, CancellationToken cancellationToken)
        => Ok(await _guestService.LiveSearchAsync(keyword, cancellationToken));
}
