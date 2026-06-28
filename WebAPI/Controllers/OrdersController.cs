using Application.DTO;
using Application.Interface;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Extensions;

namespace WebAPI.Controllers;

[ApiController]
[Authorize]
[Route("api/user/orders")]
public class OrdersController : ControllerBase
{
    private readonly ICheckoutService _checkoutService;

    public OrdersController(ICheckoutService checkoutService)
    {
        _checkoutService = checkoutService;
    }

    [HttpPost("checkout")]
    public async Task<IActionResult> Checkout([FromBody] CreateOrderRequest request, CancellationToken cancellationToken)
        => Ok(await _checkoutService.CreateOrderAsync(User.GetUserId(), request, HttpContext.Connection.RemoteIpAddress?.ToString(), cancellationToken));

    [HttpGet]
    public async Task<IActionResult> GetMyOrders(CancellationToken cancellationToken)
        => Ok(await _checkoutService.GetMyOrdersAsync(User.GetUserId(), cancellationToken));

    [HttpPost("{orderId:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid orderId, [FromBody] CancelOrderRequest request, CancellationToken cancellationToken)
        => Ok(await _checkoutService.CancelOrderAsync(User.GetUserId(), orderId, request, cancellationToken));

    [HttpPost("{orderId:guid}/return")]
    public async Task<IActionResult> RequestReturn(Guid orderId, [FromBody] RequestReturnOrderRequest request, CancellationToken cancellationToken)
        => Ok(await _checkoutService.RequestReturnAsync(User.GetUserId(), orderId, request, cancellationToken));

    [HttpPost("{orderId:guid}/reorder")]
    public async Task<IActionResult> Reorder(Guid orderId, CancellationToken cancellationToken)
        => Ok(await _checkoutService.ReorderAsync(User.GetUserId(), orderId, cancellationToken));
}
