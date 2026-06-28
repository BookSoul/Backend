using Application.DTO;
using Application.Interface;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Extensions;

namespace WebAPI.Controllers;

[ApiController]
[Authorize]
[Route("api/user/donate")]
public class DonateController : ControllerBase
{
    private readonly IDonateService _donateService;

    public DonateController(IDonateService donateService)
    {
        _donateService = donateService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateDonateRequest(
        [FromForm] string bookTitle,
        [FromForm] string author,
        [FromForm] string genre,
        [FromForm] string condition,
        [FromForm] string? cardTemplate,
        [FromForm] string? messageContent,
        [FromForm] string donorName,
        [FromForm] string? donorEmail,
        [FromForm] string donorPhone,
        [FromForm] string donorAddress,
        [FromForm] bool isAnonymous,
        [FromForm] List<IFormFile>? images,
        CancellationToken cancellationToken)
    {
        var payloads = new List<ImageUploadPayload>();
        if (images is not null)
        {
            foreach (var file in images.Take(5))
            {
                await using var ms = new MemoryStream();
                await file.CopyToAsync(ms, cancellationToken);
                payloads.Add(new ImageUploadPayload(file.FileName, file.ContentType, ms.ToArray()));
            }
        }

        var request = new CreateDonateRequest(
            bookTitle,
            author,
            genre,
            ParseCondition(condition),
            [],
            ParseCardTemplate(cardTemplate),
            messageContent ?? string.Empty,
            donorName,
            donorEmail ?? string.Empty,
            donorPhone,
            donorAddress,
            isAnonymous);

        return Ok(await _donateService.CreateAsync(User.GetUserId(), request, payloads, cancellationToken));
    }

    [HttpGet("my-requests")]
    public async Task<IActionResult> GetMyDonateRequests(CancellationToken cancellationToken)
        => Ok(await _donateService.GetMyRequestsAsync(User.GetUserId(), cancellationToken));

    [HttpGet]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> GetDonateRequests(CancellationToken cancellationToken)
        => Ok(await _donateService.GetRequestsAsync(cancellationToken));

    [HttpPatch("{id:guid}/review")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> ReviewDonateRequest(Guid id, [FromBody] ReviewDonateRequest request, CancellationToken cancellationToken)
        => Ok(await _donateService.ReviewAsync(id, request, cancellationToken));

    private static BookCondition ParseCondition(string? condition)
    {
        if (string.IsNullOrWhiteSpace(condition)) return BookCondition.Good;
        var normalized = condition.Trim().ToLowerInvariant();
        if (normalized.Contains("new") || normalized.Contains("mới") || normalized.Contains("moi")) return BookCondition.LikeNew;
        if (normalized.Contains("khá") || normalized.Contains("kha") || normalized.Contains("acceptable")) return BookCondition.Acceptable;
        if (normalized.Contains("new100")) return BookCondition.New;
        return BookCondition.Good;
    }

    private static DonateCardTemplate ParseCardTemplate(string? cardTemplate)
    {
        if (string.IsNullOrWhiteSpace(cardTemplate)) return DonateCardTemplate.None;
        return cardTemplate.Trim().ToLowerInvariant() switch
        {
            "card1" or "vintageflowers" => DonateCardTemplate.VintageFlowers,
            "card2" or "minimalist" or "minimalistlines" => DonateCardTemplate.MinimalistLines,
            "card3" or "watercolor" or "watercolordream" => DonateCardTemplate.WatercolorDream,
            "card4" or "autumn" or "autumnleaves" => DonateCardTemplate.AutumnLeaves,
            _ => DonateCardTemplate.None
        };
    }
}
