using Application.DTO;
using Application.Interface;
using Domain.Entities.Community;
using Domain.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Service;

public class WishlistService : IWishlistService
{
    private readonly AppDbContext _context;

    public WishlistService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<ProductListItemDto>> GetWishlistAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        var items = await _context.WishlistItems.AsNoTracking().Where(x => x.CustomerId == customerId).ToListAsync(cancellationToken);
        var result = new List<ProductListItemDto>();
        foreach (var item in items)
        {
            if (item.BookId.HasValue)
            {
                var book = await _context.Books.AsNoTracking().FirstOrDefaultAsync(b => b.Id == item.BookId.Value, cancellationToken);
                if (book is not null) result.Add(new ProductListItemDto(book.Id, ProductType.Book, book.Title, book.Price, book.Stock, book.ImageUrl, null, null));
            }
            else if (item.AccessoryId.HasValue)
            {
                var accessory = await _context.Accessories.AsNoTracking().FirstOrDefaultAsync(a => a.Id == item.AccessoryId.Value, cancellationToken);
                if (accessory is not null) result.Add(new ProductListItemDto(accessory.Id, ProductType.Accessory, accessory.Name, accessory.Price, accessory.Stock, accessory.ImageUrl, null, null));
            }
        }
        return result;
    }

    public async Task ToggleAsync(Guid customerId, Guid productId, ProductType productType, CancellationToken cancellationToken = default)
    {
        var existing = await _context.WishlistItems.FirstOrDefaultAsync(x => x.CustomerId == customerId && x.BookId == (productType == ProductType.Book ? productId : null) && x.AccessoryId == (productType == ProductType.Accessory ? productId : null), cancellationToken);
        if (existing is null)
        {
            _context.WishlistItems.Add(new WishlistItem { Id = Guid.NewGuid(), CustomerId = customerId, BookId = productType == ProductType.Book ? productId : null, AccessoryId = productType == ProductType.Accessory ? productId : null });
        }
        else
        {
            _context.WishlistItems.Remove(existing);
        }
        await _context.SaveChangesAsync(cancellationToken);
    }
}
