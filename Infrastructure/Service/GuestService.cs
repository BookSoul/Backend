using Application.DTO;
using Application.Interface;
using Domain.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Service;

public class GuestService : IGuestService
{
    private readonly AppDbContext _context;

    public GuestService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<ProductListItemDto>> GetBooksAsync(string? keyword, Guid? categoryId, string? condition, decimal? minPrice, decimal? maxPrice, string? sortBy, CancellationToken cancellationToken = default)
    {
        var query = _context.Books.AsNoTracking().Include(b => b.Category).Where(b => b.IsActive).AsQueryable();
        if (!string.IsNullOrWhiteSpace(keyword)) query = query.Where(b => b.Title.Contains(keyword) || b.AuthorName.Contains(keyword));
        if (categoryId.HasValue) query = query.Where(b => b.CategoryId == categoryId.Value);
        if (minPrice.HasValue) query = query.Where(b => b.Price >= minPrice.Value);
        if (maxPrice.HasValue) query = query.Where(b => b.Price <= maxPrice.Value);
        var books = await query.ToListAsync(cancellationToken);
        return books.Select(b => new ProductListItemDto(b.Id, ProductType.Book, b.Title, b.Price, b.Stock, b.ImageUrl, b.Category.Name, null)).ToList();
    }

    public async Task<ProductDetailDto> GetBookByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var book = await _context.Books.AsNoTracking().Include(b => b.Category).FirstOrDefaultAsync(b => b.Id == id && b.IsActive, cancellationToken) ?? throw new KeyNotFoundException("Book not found.");
        return new ProductDetailDto(book.Id, ProductType.Book, book.Title, book.Price, book.Stock, book.ImageUrl, book.IsActive, book.Description, null, book.AuthorName, book.CategoryId, book.Category.Name, null, null, null, null, book.Condition.ToString());
    }

    public async Task<IReadOnlyList<ProductListItemDto>> GetAccessoriesAsync(string? keyword, Guid? brandId, Guid? typeId, CancellationToken cancellationToken = default)
    {
        var query = _context.Accessories.AsNoTracking().Include(a => a.Brand).Include(a => a.Type).Where(a => a.IsActive).AsQueryable();
        if (!string.IsNullOrWhiteSpace(keyword)) query = query.Where(a => a.Name.Contains(keyword));
        if (brandId.HasValue) query = query.Where(a => a.BrandId == brandId.Value);
        if (typeId.HasValue) query = query.Where(a => a.TypeId == typeId.Value);
        var items = await query.ToListAsync(cancellationToken);
        return items.Select(a => new ProductListItemDto(a.Id, ProductType.Accessory, a.Name, a.Price, a.Stock, a.ImageUrl, null, a.Brand.Name)).ToList();
    }

    public Task<IReadOnlyList<BlindBoxTierDto>> GetBlindBoxTiersAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<BlindBoxTierDto>>(new[]
        {
            new BlindBoxTierDto((int)BlindBoxTier.Normal, "Normal", 99000m, "Gói tiêu chuẩn"),
            new BlindBoxTierDto((int)BlindBoxTier.Pro, "Pro", 149000m, "Gói nâng cấp"),
            new BlindBoxTierDto((int)BlindBoxTier.Deluxe, "Deluxe", 249000m, "Gói cao cấp")
        });

    public async Task<IReadOnlyList<LiveSearchItemDto>> LiveSearchAsync(string keyword, CancellationToken cancellationToken = default)
    {
        keyword = keyword.Trim();
        var books = await _context.Books.AsNoTracking().Where(b => b.IsActive && b.Title.Contains(keyword)).Select(b => new LiveSearchItemDto(b.Id, "Book", b.Title, b.AuthorName)).Take(5).ToListAsync(cancellationToken);
        var accessories = await _context.Accessories.AsNoTracking().Where(a => a.IsActive && a.Name.Contains(keyword)).Select(a => new LiveSearchItemDto(a.Id, "Accessory", a.Name, a.Brand.Name)).Take(5).ToListAsync(cancellationToken);
        return books.Concat(accessories).Take(5).ToList();
    }
}
