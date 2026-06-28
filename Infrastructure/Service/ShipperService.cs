using Application.DTO;
using Application.Interface;
using Domain.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Service;

public class ShipperService : IShipperService
{
    private readonly AppDbContext _context;

    public ShipperService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<PickupTaskDto>> GetPickupTasksAsync(CancellationToken cancellationToken = default)
    {
        var buybacks = await _context.BuybackRequests
            .AsNoTracking()
            .Include(request => request.Customer)
            .Where(request => request.Status == BuybackRequestStatus.Approved || request.Status == BuybackRequestStatus.Received)
            .OrderByDescending(request => request.CreatedAt)
            .ToListAsync(cancellationToken);

        var donations = await _context.DonateRequests
            .AsNoTracking()
            .Include(request => request.Customer)
            .Where(request => request.Status == DonateRequestStatus.Approved || request.Status == DonateRequestStatus.Received)
            .OrderByDescending(request => request.CreatedAt)
            .ToListAsync(cancellationToken);

        return buybacks
            .Select(request => new PickupTaskDto(
                request.Id,
                "buyback",
                request.Type == BuybackType.BlindBox
                    ? ($"{request.BlindBoxTier?.ToString() ?? "Blind Box"} {request.BlindBoxCategory ?? string.Empty}").Trim()
                    : request.BookTitle ?? "Sách thu mua",
                request.ContactName ?? request.Customer?.FullName ?? request.Customer?.UserName ?? string.Empty,
                request.ContactEmail ?? request.Customer?.Email ?? string.Empty,
                request.ContactPhone ?? request.Customer?.PhoneNumber ?? string.Empty,
                request.ContactAddress ?? request.Customer?.Address ?? string.Empty,
                request.Status == BuybackRequestStatus.Received ? "pickedUp" : "waitingPickup",
                ToFrontendBuybackStatus(request.Status),
                request.AdminNotes,
                request.ApprovedPrice ?? request.ProposedPrice,
                request.CreatedAt))
            .Concat(donations.Select(request => new PickupTaskDto(
                request.Id,
                "donate",
                request.BookTitle,
                request.DonorName,
                request.DonorEmail,
                request.DonorPhone,
                request.DonorAddress,
                request.Status == DonateRequestStatus.Received ? "pickedUp" : "waitingPickup",
                ToFrontendDonateStatus(request.Status),
                request.StaffNote,
                null,
                request.CreatedAt)))
            .OrderBy(task => task.Status == "pickedUp" ? 1 : 0)
            .ThenByDescending(task => task.CreatedAt)
            .ToList();
    }

    public async Task<PickupTaskDto> MarkPickedUpAsync(string sourceType, Guid requestId, CancellationToken cancellationToken = default)
    {
        if (sourceType.Equals("buyback", StringComparison.OrdinalIgnoreCase))
        {
            var request = await _context.BuybackRequests.FirstOrDefaultAsync(item => item.Id == requestId, cancellationToken)
                ?? throw new KeyNotFoundException("Buyback request not found.");

            if (request.Status != BuybackRequestStatus.Approved && request.Status != BuybackRequestStatus.Received)
            {
                throw new InvalidOperationException("Buyback request must be approved before pickup.");
            }

            request.Status = BuybackRequestStatus.Received;
            await _context.SaveChangesAsync(cancellationToken);
        }
        else if (sourceType.Equals("donate", StringComparison.OrdinalIgnoreCase))
        {
            var request = await _context.DonateRequests.FirstOrDefaultAsync(item => item.Id == requestId, cancellationToken)
                ?? throw new KeyNotFoundException("Donate request not found.");

            if (request.Status != DonateRequestStatus.Approved && request.Status != DonateRequestStatus.Received)
            {
                throw new InvalidOperationException("Donate request must be approved before pickup.");
            }

            request.Status = DonateRequestStatus.Received;
            request.ReviewedAt ??= DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }
        else
        {
            throw new InvalidOperationException("Invalid pickup source type.");
        }

        var task = (await GetPickupTasksAsync(cancellationToken))
            .FirstOrDefault(item => item.Id == requestId && item.SourceType.Equals(sourceType, StringComparison.OrdinalIgnoreCase));

        return task ?? throw new KeyNotFoundException("Pickup task not found.");
    }

    private static string ToFrontendBuybackStatus(BuybackRequestStatus status) => status switch
    {
        BuybackRequestStatus.Pending => "pending",
        BuybackRequestStatus.Approved => "approved",
        BuybackRequestStatus.Rejected => "rejected",
        BuybackRequestStatus.Received => "received",
        _ => status.ToString().ToLowerInvariant()
    };

    private static string ToFrontendDonateStatus(DonateRequestStatus status) => status switch
    {
        DonateRequestStatus.Pending => "pending",
        DonateRequestStatus.Approved => "approved",
        DonateRequestStatus.Rejected => "rejected",
        DonateRequestStatus.Received => "received",
        _ => status.ToString().ToLowerInvariant()
    };
}
