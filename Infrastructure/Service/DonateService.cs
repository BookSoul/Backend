using Application.DTO;
using Application.Interface;
using Domain.Entities.Donate;
using Domain.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Service;

public class DonateService : IDonateService
{
    private readonly AppDbContext _context;
    private readonly IImageStorageService _imageStorageService;

    public DonateService(AppDbContext context, IImageStorageService imageStorageService)
    {
        _context = context;
        _imageStorageService = imageStorageService;
    }

    public async Task<DonateRequestDto> CreateAsync(Guid customerId, CreateDonateRequest request, CancellationToken cancellationToken = default)
        => await CreateAsync(customerId, request, [], cancellationToken);

    public async Task<DonateRequestDto> CreateAsync(Guid customerId, CreateDonateRequest request, IReadOnlyList<ImageUploadPayload> images, CancellationToken cancellationToken = default)
    {
        var imageUrls = request.ImageUrls?.Where(url => !string.IsNullOrWhiteSpace(url)).Select(url => url.Trim()).ToList() ?? [];
        foreach (var image in images)
        {
            var url = await _imageStorageService.UploadAsync(image.FileName, image.ContentType, image.Content, cancellationToken);
            imageUrls.Add(url);
        }

        if (imageUrls.Count < 3)
        {
            throw new InvalidOperationException("At least 3 images are required.");
        }

        var entity = new DonateRequest
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            BookTitle = request.BookTitle.Trim(),
            Author = request.Author.Trim(),
            Genre = request.Genre.Trim(),
            Condition = request.Condition,
            ImageUrls = string.Join(';', imageUrls),
            CardTemplate = request.CardTemplate,
            MessageContent = request.MessageContent.Trim(),
            DonorName = request.DonorName.Trim(),
            DonorEmail = request.DonorEmail.Trim(),
            DonorPhone = request.DonorPhone.Trim(),
            DonorAddress = request.DonorAddress.Trim(),
            IsAnonymous = request.IsAnonymous,
            Status = DonateRequestStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        _context.DonateRequests.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<IReadOnlyList<DonateRequestDto>> GetMyRequestsAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        var requests = await _context.DonateRequests
            .AsNoTracking()
            .Where(request => request.CustomerId == customerId)
            .OrderByDescending(request => request.CreatedAt)
            .ToListAsync(cancellationToken);

        return requests.Select(Map).ToList();
    }

    public async Task<IReadOnlyList<DonateRequestDto>> GetRequestsAsync(CancellationToken cancellationToken = default)
    {
        var requests = await _context.DonateRequests
            .AsNoTracking()
            .OrderByDescending(request => request.CreatedAt)
            .ToListAsync(cancellationToken);

        return requests.Select(Map).ToList();
    }

    public async Task<DonateRequestDto> ReviewAsync(Guid requestId, ReviewDonateRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Status is DonateRequestStatus.Pending or DonateRequestStatus.Received)
        {
            throw new InvalidOperationException("Staff review can only approve or reject donate requests.");
        }

        var entity = await _context.DonateRequests.FirstOrDefaultAsync(item => item.Id == requestId, cancellationToken)
            ?? throw new KeyNotFoundException("Donate request not found.");

        if (entity.Status != DonateRequestStatus.Pending)
        {
            throw new InvalidOperationException("Donate request has already been reviewed.");
        }

        if (request.Status == DonateRequestStatus.Rejected && string.IsNullOrWhiteSpace(request.StaffNote))
        {
            throw new InvalidOperationException("Reject reason is required.");
        }

        entity.Status = request.Status;
        entity.StaffNote = string.IsNullOrWhiteSpace(request.StaffNote) ? null : request.StaffNote.Trim();
        entity.ReviewedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    private static DonateRequestDto Map(DonateRequest request) => new(
        request.Id,
        request.CustomerId,
        request.CustomerId.ToString(),
        request.BookTitle,
        request.Author,
        request.Genre,
        request.Condition,
        request.Condition.ToString(),
        (request.ImageUrls ?? string.Empty).Split(';', StringSplitOptions.RemoveEmptyEntries).ToList(),
        request.CardTemplate,
        ToFrontendCardTemplate(request.CardTemplate),
        request.MessageContent,
        request.DonorName,
        request.DonorEmail,
        request.DonorPhone,
        request.DonorAddress,
        request.IsAnonymous,
        request.Status,
        ToFrontendStatus(request.Status),
        request.StaffNote,
        request.ReviewedAt,
        request.CreatedAt);

    private static string ToFrontendStatus(DonateRequestStatus status) => status switch
    {
        DonateRequestStatus.Pending => "pending",
        DonateRequestStatus.Approved => "approved",
        DonateRequestStatus.Rejected => "rejected",
        DonateRequestStatus.Received => "received",
        _ => status.ToString().ToLowerInvariant()
    };

    private static string ToFrontendCardTemplate(DonateCardTemplate template) => template switch
    {
        DonateCardTemplate.VintageFlowers => "card1",
        DonateCardTemplate.MinimalistLines => "card2",
        DonateCardTemplate.WatercolorDream => "card3",
        DonateCardTemplate.AutumnLeaves => "card4",
        _ => "none"
    };
}
