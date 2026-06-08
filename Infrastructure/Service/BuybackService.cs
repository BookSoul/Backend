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
    {
        if (proposedPrice <= 0)
        {
            throw new InvalidOperationException("ProposedPrice must be greater than 0.");
        }

        var request = new BuybackRequest
        {
            Id = Guid.NewGuid(),
            RequestCode = $"BR-{DateTime.UtcNow:yyyyMMddHHmmssfff}",
            CustomerId = customerId,
            Type = type,
            Status = BuybackRequestStatus.Pending,
            ProposedPrice = proposedPrice,
            CreatedAt = DateTime.UtcNow,
            ImageUrls = string.Empty,
            Reason = string.Empty,
            RefundInfo = string.Empty
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
            .FirstAsync(r => r.Id == requestId, cancellationToken);

        return new BuybackRequestDto(
            request.Id,
            request.CustomerId,
            request.Type,
            request.Status,
            request.ProposedPrice ?? 0m,
            request.ApprovedPrice ?? 0m,
            request.AdminNotes,
            (request.ImageUrls ?? string.Empty).Split(';', StringSplitOptions.RemoveEmptyEntries).ToList(),
            request.CreatedAt);
    }
}
