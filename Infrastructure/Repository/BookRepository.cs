using Domain.Entities.Books;
using Domain.Enums;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repository;

public class BookRepository : IBookRepository
{
    private readonly AppDbContext _context;

    public BookRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Book>> SearchAsync(
        string? keyword,
        Guid? authorId,
        Guid? categoryId,
        BookCondition? condition,
        decimal? minPrice,
        decimal? maxPrice,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Books
            .Include(b => b.Category)
            .Where(b => b.IsActive)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var normalized = keyword.Trim().ToLower();
            query = query.Where(b =>
                b.Title.ToLower().Contains(normalized) ||
                b.AuthorName.ToLower().Contains(normalized) ||
                b.Category.Name.ToLower().Contains(normalized));
        }

        if (authorId.HasValue) query = query.Where(b => b.AuthorName.Contains(authorId.Value.ToString()));
        if (categoryId.HasValue) query = query.Where(b => b.CategoryId == categoryId.Value);
        if (condition.HasValue) query = query.Where(b => b.Condition == condition.Value);
        if (minPrice.HasValue) query = query.Where(b => b.Price >= minPrice.Value);
        if (maxPrice.HasValue) query = query.Where(b => b.Price <= maxPrice.Value);

        return await query.OrderBy(b => b.Title).ToListAsync(cancellationToken);
    }

    public Task<Book?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _context.Books
            .Include(b => b.Category)
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
    }
}
