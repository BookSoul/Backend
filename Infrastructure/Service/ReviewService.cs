using Application.DTO;
using Application.Interface;
using Domain.Entities.Reviews;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Service;

public class ReviewService : IReviewService
{
    private readonly AppDbContext _context;

    public ReviewService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ReviewDto> CreateAsync(Guid customerId, CreateReviewRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Rating is < 1 or > 5) throw new InvalidOperationException("Rating must be between 1 and 5.");
        if (request.BookId is null && request.AccessoryId is null) throw new InvalidOperationException("BookId or AccessoryId is required.");

        var delivered = await _context.Orders
            .AsNoTracking()
            .Include(o => o.OrderItems)
            .AnyAsync(o => o.CustomerId == customerId && o.Status == Domain.Enums.OrderStatus.Delivered && o.OrderItems.Any(i => i.BookId == request.BookId || i.AccessoryId == request.AccessoryId), cancellationToken);
        if (!delivered) throw new InvalidOperationException("You can only review products after a delivered order.");

        var review = new Review { Id = Guid.NewGuid(), CustomerId = customerId, BookId = request.BookId, AccessoryId = request.AccessoryId, Rating = request.Rating, Comment = request.Comment.Trim(), CreatedAt = DateTime.UtcNow };
        _context.Reviews.Add(review);
        await _context.SaveChangesAsync(cancellationToken);
        return new ReviewDto(review.Id, review.CustomerId, review.BookId, review.AccessoryId, review.Rating, review.Comment, review.CreatedAt);
    }

    public async Task<ReviewDto> UpdateAsync(Guid customerId, Guid reviewId, UpdateReviewRequest request, CancellationToken cancellationToken = default)
    {
        var review = await _context.Reviews.FirstOrDefaultAsync(x => x.Id == reviewId && x.CustomerId == customerId, cancellationToken) ?? throw new KeyNotFoundException("Review not found.");
        review.Rating = request.Rating;
        review.Comment = request.Comment.Trim();
        await _context.SaveChangesAsync(cancellationToken);
        return new ReviewDto(review.Id, review.CustomerId, review.BookId, review.AccessoryId, review.Rating, review.Comment, review.CreatedAt);
    }

    public async Task DeleteAsync(Guid customerId, Guid reviewId, CancellationToken cancellationToken = default)
    {
        var review = await _context.Reviews.FirstOrDefaultAsync(x => x.Id == reviewId && x.CustomerId == customerId, cancellationToken) ?? throw new KeyNotFoundException("Review not found.");
        _context.Reviews.Remove(review);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
