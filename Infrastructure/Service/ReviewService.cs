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

    // ─────────────────────────────────────────────────────────────────────────
    // USER: CUD
    // ─────────────────────────────────────────────────────────────────────────

    public async Task<ReviewDto> CreateAsync(Guid customerId, CreateReviewRequest request, CancellationToken cancellationToken = default)
    {
        // Validate rating
        if (request.Rating is < 1 or > 5)
            throw new InvalidOperationException("Rating phải từ 1 đến 5.");

        // Validate ít nhất một trong hai FK
        if (request.BookId is null && request.AccessoryId is null)
            throw new InvalidOperationException("Phải cung cấp BookId hoặc AccessoryId.");

        var productId = request.BookId ?? request.AccessoryId;
        Guid? correctBookId = request.BookId;
        Guid? correctAccessoryId = request.AccessoryId;

        // Auto-correct mismatch between BookId and AccessoryId from frontend
        if (productId.HasValue)
        {
            var isBook = await _context.Books.AnyAsync(b => b.Id == productId, cancellationToken);
            if (isBook)
            {
                correctBookId = productId;
                correctAccessoryId = null;
            }
            else
            {
                var isAccessory = await _context.Accessories.AnyAsync(a => a.Id == productId, cancellationToken);
                if (isAccessory)
                {
                    correctAccessoryId = productId;
                    correctBookId = null;
                }
            }
        }

        // Validate comment
        if (string.IsNullOrWhiteSpace(request.Comment))
            throw new InvalidOperationException("Nội dung bình luận không được để trống.");
        if (request.Comment.Trim().Length > 2000)
            throw new InvalidOperationException("Nội dung bình luận không được vượt quá 2000 ký tự.");

        // Business rule: phải có đơn hàng đã giao chứa sản phẩm đó
        var delivered = await _context.Orders
            .AsNoTracking()
            .Include(o => o.OrderItems)
            .AnyAsync(o =>
                o.CustomerId == customerId &&
                o.Status == Domain.Enums.OrderStatus.Delivered &&
                o.OrderItems.Any(i => i.BookId == productId || i.AccessoryId == productId),
                cancellationToken);

        if (!delivered)
            throw new InvalidOperationException("Bạn chỉ có thể đánh giá sản phẩm sau khi nhận được đơn hàng.");

        // Duplicate check: mỗi user chỉ review một sản phẩm một lần
        var isDuplicate = await _context.Reviews.AnyAsync(r =>
            r.CustomerId == customerId &&
            (r.BookId == productId || r.AccessoryId == productId),
            cancellationToken);

        if (isDuplicate)
            throw new InvalidOperationException("Bạn đã đánh giá sản phẩm này rồi. Vui lòng chỉnh sửa review hiện tại.");

        var review = new Review
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            BookId = correctBookId,
            AccessoryId = correctAccessoryId,
            Rating = request.Rating,
            Comment = request.Comment.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        _context.Reviews.Add(review);
        await _context.SaveChangesAsync(cancellationToken);

        // Load đầy đủ để trả về DTO
        await _context.Entry(review).Reference(r => r.Customer).LoadAsync(cancellationToken);
        if (review.BookId.HasValue)
            await _context.Entry(review).Reference(r => r.Book).LoadAsync(cancellationToken);
        if (review.AccessoryId.HasValue)
            await _context.Entry(review).Reference(r => r.Accessory).LoadAsync(cancellationToken);
        await _context.Entry(review).Reference(r => r.Customer).LoadAsync(cancellationToken);

        return MapToDto(review);
    }

    public async Task<ReviewDto> UpdateAsync(Guid customerId, Guid reviewId, UpdateReviewRequest request, CancellationToken cancellationToken = default)
    {
        // Validate rating
        if (request.Rating is < 1 or > 5)
            throw new InvalidOperationException("Rating phải từ 1 đến 5.");

        // Validate comment
        if (string.IsNullOrWhiteSpace(request.Comment))
            throw new InvalidOperationException("Nội dung bình luận không được để trống.");
        if (request.Comment.Trim().Length > 2000)
            throw new InvalidOperationException("Nội dung bình luận không được vượt quá 2000 ký tự.");

        var review = await _context.Reviews
            .Include(r => r.Customer)
            .Include(r => r.Book)
            .Include(r => r.Accessory)
            .FirstOrDefaultAsync(x => x.Id == reviewId && x.CustomerId == customerId, cancellationToken)
            ?? throw new KeyNotFoundException("Review không tìm thấy hoặc bạn không có quyền chỉnh sửa.");

        review.Rating = request.Rating;
        review.Comment = request.Comment.Trim();
        review.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return MapToDto(review);
    }

    public async Task DeleteAsync(Guid customerId, Guid reviewId, CancellationToken cancellationToken = default)
    {
        var review = await _context.Reviews
            .FirstOrDefaultAsync(x => x.Id == reviewId && x.CustomerId == customerId, cancellationToken)
            ?? throw new KeyNotFoundException("Review không tìm thấy hoặc bạn không có quyền xóa.");

        _context.Reviews.Remove(review);
        await _context.SaveChangesAsync(cancellationToken);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // USER: READ MY REVIEWS
    // ─────────────────────────────────────────────────────────────────────────

    public async Task<PagedResult<ReviewDto>> GetMyReviewsAsync(Guid customerId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var query = _context.Reviews
            .AsNoTracking()
            .Include(r => r.Customer)
            .Include(r => r.Book)
            .Include(r => r.Accessory)
            .Where(r => r.CustomerId == customerId)
            .OrderByDescending(r => r.CreatedAt);

        var totalItems = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<ReviewDto>(
            items.Select(MapToDto).ToList(),
            page,
            pageSize,
            totalItems,
            (int)Math.Ceiling((double)totalItems / pageSize)
        );
    }

    public async Task<ReviewEligibilityDto> CheckEligibilityAsync(Guid customerId, Guid? bookId, Guid? accessoryId, CancellationToken cancellationToken = default)
    {
        var productId = bookId ?? accessoryId;
        if (productId is null)
            throw new InvalidOperationException("Phải cung cấp BookId hoặc AccessoryId.");

        // Check if user has received the product
        var delivered = await _context.Orders
            .AsNoTracking()
            .Include(o => o.OrderItems)
            .AnyAsync(o =>
                o.CustomerId == customerId &&
                o.Status == Domain.Enums.OrderStatus.Delivered &&
                o.OrderItems.Any(i => i.BookId == productId || i.AccessoryId == productId),
                cancellationToken);

        if (!delivered)
            return new ReviewEligibilityDto(false, null);

        // Check if user has already reviewed
        var existingReview = await _context.Reviews
            .AsNoTracking()
            .Include(r => r.Customer)
            .Include(r => r.Book)
            .Include(r => r.Accessory)
            .FirstOrDefaultAsync(r =>
                r.CustomerId == customerId &&
                (r.BookId == productId || r.AccessoryId == productId),
                cancellationToken);

        return new ReviewEligibilityDto(true, existingReview != null ? MapToDto(existingReview) : null);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PUBLIC: GET REVIEWS BY PRODUCT
    // ─────────────────────────────────────────────────────────────────────────

    public async Task<PagedResult<ReviewDto>> GetByBookIdAsync(Guid bookId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var query = _context.Reviews
            .AsNoTracking()
            .Include(r => r.Customer)
            .Include(r => r.Book)
            .Where(r => (r.BookId == bookId || r.AccessoryId == bookId) && !r.IsHidden)
            .OrderByDescending(r => r.CreatedAt);

        var totalItems = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<ReviewDto>(
            items.Select(MapToDto).ToList(),
            page,
            pageSize,
            totalItems,
            (int)Math.Ceiling((double)totalItems / pageSize)
        );
    }

    public async Task<PagedResult<ReviewDto>> GetByAccessoryIdAsync(Guid accessoryId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var query = _context.Reviews
            .AsNoTracking()
            .Include(r => r.Customer)
            .Include(r => r.Accessory)
            .Where(r => (r.AccessoryId == accessoryId || r.BookId == accessoryId) && !r.IsHidden)
            .OrderByDescending(r => r.CreatedAt);

        var totalItems = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<ReviewDto>(
            items.Select(MapToDto).ToList(),
            page,
            pageSize,
            totalItems,
            (int)Math.Ceiling((double)totalItems / pageSize)
        );
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PUBLIC: RATING SUMMARY
    // ─────────────────────────────────────────────────────────────────────────

    public async Task<ProductReviewSummaryDto> GetRatingSummaryAsync(Guid? bookId, Guid? accessoryId, CancellationToken cancellationToken = default)
    {
        if (bookId is null && accessoryId is null)
            throw new InvalidOperationException("Phải cung cấp BookId hoặc AccessoryId.");
        
        var productId = bookId ?? accessoryId;

        var ratings = await _context.Reviews
            .AsNoTracking()
            .Where(r => !r.IsHidden && (r.BookId == productId || r.AccessoryId == productId))
            .Select(r => r.Rating)
            .ToListAsync(cancellationToken);

        if (ratings.Count == 0)
            return new ProductReviewSummaryDto(0, 0, new int[5]);

        var distribution = new int[5];
        foreach (var r in ratings)
        {
            var idx = Math.Clamp(r - 1, 0, 4);
            distribution[idx]++;
        }

        var average = Math.Round(ratings.Average(), 1);
        return new ProductReviewSummaryDto(average, ratings.Count, distribution);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ADMIN
    // ─────────────────────────────────────────────────────────────────────────

        public async Task<AdminReviewStatisticsDto> GetAdminStatisticsAsync(CancellationToken cancellationToken = default)
    {
        var total = await _context.Reviews.CountAsync(cancellationToken);
        if (total == 0) return new AdminReviewStatisticsDto(0, 0, new int[5]);

        var avg = await _context.Reviews.AverageAsync(r => (double)r.Rating, cancellationToken);
        
        var dist = new int[5];
        var groups = await _context.Reviews
            .GroupBy(r => r.Rating)
            .Select(g => new { Rating = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);
            
        foreach (var g in groups)
        {
            if (g.Rating >= 1 && g.Rating <= 5)
                dist[g.Rating - 1] = g.Count;
        }

        return new AdminReviewStatisticsDto(total, Math.Round(avg, 1), dist);
    }

    public async Task<PagedResult<ReviewDto>> AdminGetAllAsync(Guid? bookId, Guid? accessoryId, int? rating, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _context.Reviews
            .AsNoTracking()
            .Include(r => r.Customer)
            .Include(r => r.Book)
            .Include(r => r.Accessory)
            .AsQueryable();

        if (bookId.HasValue)
            query = query.Where(r => r.BookId == bookId.Value);
        if (accessoryId.HasValue)
            query = query.Where(r => r.AccessoryId == accessoryId.Value);
        if (rating.HasValue)
            query = query.Where(r => r.Rating == rating.Value);

        query = query.OrderByDescending(r => r.CreatedAt);

        var totalItems = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<ReviewDto>(
            items.Select(MapToDto).ToList(),
            page,
            pageSize,
            totalItems,
            (int)Math.Ceiling((double)totalItems / pageSize)
        );
    }

    public async Task AdminDeleteAsync(Guid reviewId, CancellationToken cancellationToken = default)
    {
        var review = await _context.Reviews
            .FirstOrDefaultAsync(r => r.Id == reviewId, cancellationToken)
            ?? throw new KeyNotFoundException("Review không tìm thấy.");

        _context.Reviews.Remove(review);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<ReviewDto> AdminToggleHideAsync(Guid reviewId, CancellationToken cancellationToken = default)
    {
        var review = await _context.Reviews
            .Include(r => r.Customer)
            .Include(r => r.Book)
            .Include(r => r.Accessory)
            .FirstOrDefaultAsync(r => r.Id == reviewId, cancellationToken)
            ?? throw new KeyNotFoundException("Review không tìm thấy.");

        review.IsHidden = !review.IsHidden;
        await _context.SaveChangesAsync(cancellationToken);
        return MapToDto(review);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PRIVATE HELPERS
    // ─────────────────────────────────────────────────────────────────────────

    private static ReviewDto MapToDto(Domain.Entities.Reviews.Review r) => new(
        r.Id,
        r.CustomerId,
        string.IsNullOrWhiteSpace(r.Customer?.FullName) ? (r.Customer?.UserName ?? r.Customer?.Email ?? "Người dùng") : r.Customer.FullName,
        r.Customer?.AvatarUrl,
        r.BookId,
        r.AccessoryId,
        r.Book?.Title ?? r.Accessory?.Name,
        r.Book?.ImageUrl ?? r.Accessory?.ImageUrl,
        r.Rating,
        r.Comment,
        r.CreatedAt,
        r.UpdatedAt,
        r.IsHidden
    );
}




