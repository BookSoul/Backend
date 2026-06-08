using Application.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Extensions;

namespace WebAPI.Controllers;

[ApiController]
[Authorize]
[Route("api/user/wishlist")]
public class WishlistController : ControllerBase
{
    private readonly IWishlistService _wishlistService;

    public WishlistController(IWishlistService wishlistService)
    {
        _wishlistService = wishlistService;
    }

    [HttpGet]
    public async Task<IActionResult> GetWishlist(CancellationToken cancellationToken)
        => Ok(await _wishlistService.GetWishlistAsync(User.GetUserId(), cancellationToken));

    [HttpPost("{productId:guid}")]
    public async Task<IActionResult> ToggleWishlist(Guid productId, [FromQuery] Domain.Enums.ProductType productType, CancellationToken cancellationToken)
    {
        await _wishlistService.ToggleAsync(User.GetUserId(), productId, productType, cancellationToken);
        return Ok();
    }
}
