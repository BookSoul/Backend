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
                b.Id, ProductType.Book, b.Title, b.Price, b.Stock, b.ImageUrl, b.Category.Name, null))
            .ToListAsync(cancellationToken);

        var accessories = await _context.Accessories
            .AsNoTracking()
            .Include(a => a.Brand)
            .Where(a => a.IsActive)
            .OrderByDescending(a => a.Stock)
            .Take(8)
            .Select(a => new ProductListItemDto(
                a.Id, ProductType.Accessory, a.Name, a.Price, a.Stock, a.ImageUrl, null, a.Brand.Name))
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
