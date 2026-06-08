using Application.DTO;
using Application.Interface;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Extensions;

namespace WebAPI.Controllers;

[ApiController]
[Authorize]
[Route("api/user")]
public class UserController : ControllerBase
{
    private readonly ICartService _cartService;
    private readonly ICheckoutService _checkoutService;
    private readonly IBuybackService _buybackService;

    public UserController(ICartService cartService, ICheckoutService checkoutService, IBuybackService buybackService)
    {
        _cartService = cartService;
        _checkoutService = checkoutService;
        _buybackService = buybackService;
    }

    [HttpGet("cart")]
    public async Task<IActionResult> GetCart(CancellationToken cancellationToken) => Ok(await _cartService.GetCartAsync(User.GetUserId(), cancellationToken));

    [HttpPost("cart")]
    public async Task<IActionResult> AddToCart([FromBody] AddCartItemBody body, CancellationToken cancellationToken)
        => Ok(await _cartService.AddItemAsync(User.GetUserId(), body.ProductId, body.ProductType, body.Quantity, cancellationToken));

    [HttpPut("cart/{itemId:guid}")]
    public async Task<IActionResult> UpdateCartItem(Guid itemId, [FromBody] UpdateCartBody body, CancellationToken cancellationToken)
        => Ok(await _cartService.UpdateItemAsync(User.GetUserId(), body.ProductId, body.ProductType, body.Quantity, cancellationToken));

    // Order endpoints are exposed via OrdersController under /api/user/orders

    [HttpPost("buyback/regular")]
    public async Task<IActionResult> RegularBuyback([FromBody] BuybackRegularBody body, CancellationToken cancellationToken)
        => Ok(await _buybackService.CreateRequestAsync(User.GetUserId(), BuybackType.Regular, body.ProposedPrice, [], cancellationToken));

    [HttpPost("buyback/blindbox")]
    public async Task<IActionResult> BlindBoxBuyback([FromBody] BuybackBlindBoxBody body, CancellationToken cancellationToken)
        => Ok(await _buybackService.CreateRequestAsync(User.GetUserId(), BuybackType.BlindBox, body.ProposedPrice, [], cancellationToken));

    public record AddCartItemBody(Guid ProductId, ProductType ProductType, int Quantity);
    public record UpdateCartBody(Guid ProductId, ProductType ProductType, int Quantity);
    public record BuybackRegularBody(decimal ProposedPrice);
    public record BuybackBlindBoxBody(decimal ProposedPrice);
}
