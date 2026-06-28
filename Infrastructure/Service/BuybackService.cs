using Application.DTO;
using Application.Interface;
using Domain.Entities.Buyback;
using Domain.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Infrastructure.Service;

public class BuybackService : IBuybackService
{
    private readonly AppDbContext _context;
    private readonly IImageStorageService _imageStorageService;

    public BuybackService(AppDbContext context, IImageStorageService imageStorageService)
    {
        _context = context;
        _imageStorageService = imageStorageService;
    }

    public async Task<BuybackRequestDto> CreateRequestAsync(
        Guid customerId,
        BuybackType type,
        decimal proposedPrice,
        IReadOnlyList<ImageUploadPayload> images,
        CancellationToken cancellationToken = default)
        => await CreateRequestAsync(
            customerId,
            new CreateBuybackRequest(type, proposedPrice, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null),
            images,
            cancellationToken);

    public async Task<BuybackRequestDto> CreateRequestAsync(
        Guid customerId,
        CreateBuybackRequest createRequest,
        IReadOnlyList<ImageUploadPayload> images,
        CancellationToken cancellationToken = default)
    {
        var proposedPrice = createRequest.ProposedPrice ?? createRequest.BuybackPrice ?? 1m;
        if (proposedPrice <= 0)
        {
            throw new InvalidOperationException("ProposedPrice must be greater than 0.");
        }

        var request = new BuybackRequest
        {
            Id = Guid.NewGuid(),
            RequestCode = $"BR-{DateTime.UtcNow:yyyyMMddHHmmssfff}",
            CustomerId = customerId,
            Type = createRequest.Type,
            Status = BuybackRequestStatus.Pending,
            ProposedPrice = proposedPrice,
            CreatedAt = DateTime.UtcNow,
            ImageUrls = string.Empty,
            Reason = createRequest.Reason ?? string.Empty,
            RefundInfo = string.Empty,
            BookTitle = createRequest.BookTitle,
            Author = createRequest.Author,
            Category = createRequest.Category,
            Condition = ParseCondition(createRequest.Condition),
            ConditionText = createRequest.Condition,
            PublishYear = createRequest.PublishYear,
            Description = createRequest.Description,
            OriginalOrderId = Guid.TryParse(createRequest.OrderId, out var orderId) ? orderId : null,
            BlindBoxTier = ParseBlindBoxTier(createRequest.BlindBoxTier),
            BlindBoxCategory = createRequest.BlindBoxCategory,
            OriginalPrice = createRequest.OriginalPrice,
            ContactName = createRequest.UserName,
            ContactEmail = createRequest.UserEmail,
            ContactPhone = createRequest.UserPhone,
            ContactAddress = createRequest.UserAddress
        };

        var imageUrls = new List<string>();
        foreach (var image in images)
        {
            var url = await _imageStorageService.UploadAsync(image.FileName, image.ContentType, image.Content, cancellationToken);
            imageUrls.Add(url);
        }
        request.ImageUrls = string.Join(';', imageUrls);

        _context.BuybackRequests.Add(request);
        await _context.SaveChangesAsync(cancellationToken);
        return await MapAsync(request.Id, cancellationToken);
    }

    public async Task<BuybackRequestDto> ReviewRequestAsync(Guid requestId, ApproveBuybackRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Status == BuybackRequestStatus.Pending)
        {
            throw new InvalidOperationException("Review action cannot set status to Pending.");
        }

        var entity = await _context.BuybackRequests
            .FirstOrDefaultAsync(r => r.Id == requestId, cancellationToken)
            ?? throw new KeyNotFoundException("Buyback request not found.");

        if (request.Status is not (BuybackRequestStatus.Approved or BuybackRequestStatus.Rejected))
        {
            throw new InvalidOperationException("Buyback request can only be approved or rejected from review.");
        }

        entity.Status = request.Status;
        entity.AdminNotes = string.IsNullOrWhiteSpace(request.AdminNote) ? null : request.AdminNote.Trim();

        if (request.Status == BuybackRequestStatus.Approved)
        {
            if (!request.FinalPrice.HasValue || request.FinalPrice <= 0)
            {
                throw new InvalidOperationException("FinalPrice is required when approving.");
            }

            entity.ApprovedPrice = request.FinalPrice.Value;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return await MapAsync(requestId, cancellationToken);
    }

    public async Task<IReadOnlyList<BuybackRequestDto>> GetMyRequestsAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        var ids = await _context.BuybackRequests
            .AsNoTracking()
            .Where(r => r.CustomerId == customerId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => r.Id)
            .ToListAsync(cancellationToken);

        var result = new List<BuybackRequestDto>();
        foreach (var id in ids)
        {
            result.Add(await MapAsync(id, cancellationToken));
        }

        return result;
    }

    public async Task<IReadOnlyList<BuybackRequestDto>> GetRequestsAsync(CancellationToken cancellationToken = default)
    {
        var ids = await _context.BuybackRequests
            .AsNoTracking()
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => r.Id)
            .ToListAsync(cancellationToken);

        var result = new List<BuybackRequestDto>();
        foreach (var id in ids)
        {
            result.Add(await MapAsync(id, cancellationToken));
        }

        return result;
    }

    public async Task<IReadOnlyList<BuybackRequestDto>> GetPendingRequestsAsync(CancellationToken cancellationToken = default)
    {
        var ids = await _context.BuybackRequests
            .AsNoTracking()
            .Where(r => r.Status == BuybackRequestStatus.Pending)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => r.Id)
            .ToListAsync(cancellationToken);

        var result = new List<BuybackRequestDto>();
        foreach (var id in ids)
        {
            result.Add(await MapAsync(id, cancellationToken));
        }

        return result;
    }

    private async Task<BuybackRequestDto> MapAsync(Guid requestId, CancellationToken cancellationToken)
    {
        var request = await _context.BuybackRequests
            .AsNoTracking()
            .Include(r => r.Customer)
            .FirstAsync(r => r.Id == requestId, cancellationToken);

        return new BuybackRequestDto(
            request.Id,
            ToFrontendType(request.Type),
            request.CustomerId,
            request.CustomerId.ToString(),
            request.ContactName ?? request.Customer?.FullName,
            request.ContactEmail ?? request.Customer?.Email,
            request.ContactPhone ?? request.Customer?.PhoneNumber,
            request.ContactAddress ?? request.Customer?.Address,
            request.Type,
            ToFrontendStatus(request.Status),
            request.Status,
            request.ProposedPrice ?? 0m,
            request.ApprovedPrice ?? request.ProposedPrice ?? 0m,
            request.ApprovedPrice ?? 0m,
            request.AdminNotes,
            (request.ImageUrls ?? string.Empty).Split(';', StringSplitOptions.RemoveEmptyEntries).ToList(),
            request.CreatedAt,
            request.BookTitle,
            request.Author,
            request.Category,
            request.ConditionText ?? request.Condition?.ToString(),
            request.PublishYear,
            request.Description,
            request.OriginalOrderId?.ToString(),
            request.BlindBoxTier?.ToString(),
            request.BlindBoxCategory,
            request.OriginalPrice,
            request.Reason);
    }

    private static BookCondition? ParseCondition(string? condition)
    {
        if (string.IsNullOrWhiteSpace(condition)) return null;
        var normalized = condition.Trim().ToLowerInvariant();
        if (normalized.Contains("như") || normalized.Contains("mới") || normalized.Contains("nhu") || normalized.Contains("moi") || normalized.Contains("new")) return BookCondition.LikeNew;
        if (normalized.Contains("rat") || normalized.Contains("very")) return BookCondition.Good;
        if (normalized.Contains("kha") || normalized.Contains("acceptable")) return BookCondition.Acceptable;
        return BookCondition.Good;
    }

    private static BlindBoxTier? ParseBlindBoxTier(string? tier)
    {
        if (string.IsNullOrWhiteSpace(tier)) return null;
        var normalized = tier.Trim().ToLowerInvariant();
        if (normalized.Contains("deluxe")) return BlindBoxTier.Deluxe;
        if (normalized.Contains("pro")) return BlindBoxTier.Pro;
        return BlindBoxTier.Normal;
    }

    private static string ToFrontendType(BuybackType type) => type == BuybackType.BlindBox ? "blindbox" : "regular";

    private static string ToFrontendStatus(BuybackRequestStatus status) => status switch
    {
        BuybackRequestStatus.Pending => "pending",
        BuybackRequestStatus.Approved => "approved",
        BuybackRequestStatus.Rejected => "rejected",
        BuybackRequestStatus.Received => "received",
        _ => status.ToString().ToLowerInvariant()
    };
}
