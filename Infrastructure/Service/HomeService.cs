using Application.DTO;
using Application.Interface;
using Domain.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Service;

public class HomeService : IHomeService
{
    private readonly AppDbContext _context;

    public HomeService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<HomePageDto> GetHomePageAsync(CancellationToken cancellationToken = default)
    {
        var books = await _context.Books
            .AsNoTracking()
            .Include(b => b.Category)
            .Where(b => b.IsActive)
            .OrderByDescending(b => b.Stock)
            .Take(8)
            .Select(b => new ProductListItemDto(
                b.Id,
                "book",
                b.Id.ToString(),
                b.Title,
                b.Title,
                b.AuthorName,
                b.AuthorName,
                b.Price,
                null,
                b.Stock,
                b.Stock > 0,
                b.ImageUrl,
                b.ImageUrl,
                b.Category.Name,
                b.Category.Name,
                null,
                null))
            .ToListAsync(cancellationToken);

        var accessories = await _context.Accessories
            .AsNoTracking()
            .Include(a => a.Brand)
            .Where(a => a.IsActive)
            .OrderByDescending(a => a.Stock)
            .Take(8)
            .Select(a => new ProductListItemDto(
                a.Id,
                "accessory",
                a.Id.ToString(),
                null,
                a.Name,
                null,
                null,
                a.Price,
                null,
                a.Stock,
                a.Stock > 0,
                a.ImageUrl,
                a.ImageUrl,
                null,
                null,
                a.Brand.Name,
                a.Brand.Name))
            .ToListAsync(cancellationToken);

        var banners = await _context.Banners
            .AsNoTracking()
            .Where(b => b.IsActive)
            .OrderBy(b => b.DisplayOrder)
            .Select(b => new HomeBannerDto(b.Id, b.Title, b.ImageUrl, b.LinkUrl, b.DisplayOrder))
            .ToListAsync(cancellationToken);

        return new HomePageDto(books, accessories, banners);
    }
}
