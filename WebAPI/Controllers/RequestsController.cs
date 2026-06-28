using Application.DTO;
using Application.Interface;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Extensions;

namespace WebAPI.Controllers;

[ApiController]
[Authorize]
[Route("api/requests")]
public class RequestsController : ControllerBase
{
    private readonly IBuybackService _buybackService;

    public RequestsController(IBuybackService buybackService)
    {
        _buybackService = buybackService;
    }

    [HttpPost]
    [Authorize(Roles = "Customer,Admin")]
    public async Task<IActionResult> CreateRequest(
        [FromForm] BuybackType type,
        [FromForm] decimal proposedPrice,
        [FromForm] string? bookTitle,
        [FromForm] string? author,
        [FromForm] string? category,
        [FromForm] string? condition,
        [FromForm] string? publishYear,
        [FromForm] string? description,
        [FromForm] string? orderId,
        [FromForm] string? blindBoxTier,
        [FromForm] string? blindBoxCategory,
        [FromForm] decimal? buybackPrice,
        [FromForm] decimal? originalPrice,
        [FromForm] string? reason,
        [FromForm] string? userName,
        [FromForm] string? userEmail,
        [FromForm] string? userPhone,
        [FromForm] string? userAddress,
        [FromForm] List<IFormFile>? images,
        CancellationToken cancellationToken)
    {
        var payloads = new List<ImageUploadPayload>();
        if (images is not null)
        {
            foreach (var file in images)
            {
                await using var ms = new MemoryStream();
                await file.CopyToAsync(ms, cancellationToken);
                payloads.Add(new ImageUploadPayload(file.FileName, file.ContentType, ms.ToArray()));
            }
        }

        var request = new CreateBuybackRequest(
            type,
            proposedPrice,
            bookTitle,
            author,
            category,
            condition,
            publishYear,
            description,
            orderId,
            blindBoxTier,
            blindBoxCategory,
            buybackPrice,
            originalPrice,
            reason,
            userName,
            userEmail,
            userPhone,
            userAddress);
        var result = await _buybackService.CreateRequestAsync(User.GetUserId(), request, payloads, cancellationToken);
        return Ok(result);
    }

    [HttpGet("my-requests")]
    public async Task<IActionResult> GetMyRequests(CancellationToken cancellationToken)
    {
        var result = await _buybackService.GetMyRequestsAsync(User.GetUserId(), cancellationToken);
        return Ok(result);
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> GetRequests(CancellationToken cancellationToken)
    {
        var result = await _buybackService.GetRequestsAsync(cancellationToken);
        return Ok(result);
    }

    [HttpPatch("{id:guid}/approve")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> ReviewRequest(Guid id, [FromBody] ApproveBuybackRequest request, CancellationToken cancellationToken)
    {
        var result = await _buybackService.ReviewRequestAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpGet("pending")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> GetPending(CancellationToken cancellationToken)
    {
        var result = await _buybackService.GetPendingRequestsAsync(cancellationToken);
        return Ok(result);
    }
}
