using Application.DTO;
using Application.Interface;
using Domain.Entities.Donate;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Service;

public class DonateService : IDonateService
{
    private readonly AppDbContext _context;

    public DonateService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<DonateRequestDto> CreateAsync(Guid customerId, CreateDonateRequest request, CancellationToken cancellationToken = default)
    {
        if (request.ImageUrls.Count < 3)
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
            ImageUrls = string.Join(';', request.ImageUrls),
            CardTemplate = request.CardTemplate,
            MessageContent = request.MessageContent.Trim(),
            DonorName = request.DonorName.Trim(),
            DonorEmail = request.DonorEmail.Trim(),
            DonorPhone = request.DonorPhone.Trim(),
            DonorAddress = request.DonorAddress.Trim(),
            IsAnonymous = request.IsAnonymous,
            CreatedAt = DateTime.UtcNow
        };

        _context.DonateRequests.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return new DonateRequestDto(entity.Id, entity.CustomerId, entity.BookTitle, entity.Author, entity.Genre, entity.Condition, entity.CardTemplate, entity.IsAnonymous, entity.CreatedAt);
    }
}
