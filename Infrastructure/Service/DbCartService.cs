using Application.DTO;
using Application.Interface;
using Domain.Entities.Orders;
using Domain.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Service;

public class DbCartService : ICartService
{
    private readonly AppDbContext _context;

    public DbCartService(AppDbContext context)
    {
        _context = context;
    }

    public Task<CartDto> GetCartAsync(Guid customerId, CancellationToken cancellationToken = default)
        => BuildCartAsync(customerId, cancellationToken);

    public async Task<CartDto> AddItemAsync(Guid customerId, Guid productId, ProductType productType, int quantity, CancellationToken cancellationToken = default)
    {
        if (quantity <= 0)
        {
            throw new InvalidOperationException("Quantity must be greater than 0.");
        }

        await EnsureProductExistsAsync(productId, productType, cancellationToken);

        var existing = await _context.ShoppingCartItems.FirstOrDefaultAsync(
            ci => ci.CustomerId == customerId && ci.BookId == (productType == ProductType.Book ? productId : null) && ci.AccessoryId == (productType == ProductType.Accessory ? productId : null),
            cancellationToken);

        if (existing is null)
        {
            _context.ShoppingCartItems.Add(new ShoppingCartItem
            {
                Id = Guid.NewGuid(),
                CustomerId = customerId,
                BookId = productType == ProductType.Book ? productId : null,
                AccessoryId = productType == ProductType.Accessory ? productId : null,
                Quantity = quantity,
                AddedAt = DateTime.UtcNow
            });
        }
        else
        {
            existing.Quantity += quantity;
            existing.AddedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return await BuildCartAsync(customerId, cancellationToken);
    }

    public async Task<CartDto> UpdateItemAsync(Guid customerId, Guid productId, ProductType productType, int quantity, CancellationToken cancellationToken = default)
    {
        var existing = await _context.ShoppingCartItems.FirstOrDefaultAsync(
            ci => ci.CustomerId == customerId && ci.BookId == (productType == ProductType.Book ? productId : null) && ci.AccessoryId == (productType == ProductType.Accessory ? productId : null),
            cancellationToken);

        if (existing is null)
        {
            throw new KeyNotFoundException("Cart item not found.");
        }

        if (quantity <= 0)
        {
            _context.ShoppingCartItems.Remove(existing);
        }
        else
        {
            existing.Quantity = quantity;
            existing.AddedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return await BuildCartAsync(customerId, cancellationToken);
    }

    public async Task<CartDto> RemoveItemAsync(Guid customerId, Guid productId, ProductType productType, CancellationToken cancellationToken = default)
    {
        var existing = await _context.ShoppingCartItems.FirstOrDefaultAsync(
            ci => ci.CustomerId == customerId && ci.BookId == (productType == ProductType.Book ? productId : null) && ci.AccessoryId == (productType == ProductType.Accessory ? productId : null),
            cancellationToken);

        if (existing is not null)
        {
            _context.ShoppingCartItems.Remove(existing);
            await _context.SaveChangesAsync(cancellationToken);
        }

        return await BuildCartAsync(customerId, cancellationToken);
    }

    public async Task ClearCartAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        var items = await _context.ShoppingCartItems.Where(ci => ci.CustomerId == customerId).ToListAsync(cancellationToken);
        if (items.Count == 0) return;
        _context.ShoppingCartItems.RemoveRange(items);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureProductExistsAsync(Guid productId, ProductType productType, CancellationToken cancellationToken)
    {
        var exists = productType switch
        {
            ProductType.Book => await _context.Books.AnyAsync(b => b.Id == productId && b.IsActive, cancellationToken),
            ProductType.Accessory => await _context.Accessories.AnyAsync(a => a.Id == productId && a.IsActive, cancellationToken),
            _ => false
        };

        if (!exists)
        {
            throw new KeyNotFoundException("Product not found or inactive.");
        }
    }

    private async Task<CartDto> BuildCartAsync(Guid customerId, CancellationToken cancellationToken)
    {
        var cartItems = await _context.ShoppingCartItems
            .AsNoTracking()
            .Where(ci => ci.CustomerId == customerId)
            .OrderByDescending(ci => ci.AddedAt)
            .ToListAsync(cancellationToken);

        var items = new List<CartItemDto>();
        foreach (var ci in cartItems)
        {
            if (ci.BookId.HasValue)
            {
                var book = await _context.Books.AsNoTracking()
                    .Include(b => b.Category)
                    .FirstOrDefaultAsync(b => b.Id == ci.BookId.Value, cancellationToken);
                if (book is null) continue;
                items.Add(new CartItemDto(
                    book.Id,
                    ProductType.Book,
                    book.Title,
                    ci.Quantity,
                    book.Price,
                    book.Price * ci.Quantity,
                    false,
                    book.Id.ToString(),
                    "book",
                    book.Title,
                    book.Price,
                    book.ImageUrl,
                    book.AuthorName,
                    null,
                    book.Category?.Name,
                    null));
            }
            else if (ci.AccessoryId.HasValue)
            {
                var accessory = await _context.Accessories.AsNoTracking()
                    .Include(a => a.Brand)
                    .Include(a => a.Type)
                    .FirstOrDefaultAsync(a => a.Id == ci.AccessoryId.Value, cancellationToken);
                if (accessory is null) continue;
                items.Add(new CartItemDto(
                    accessory.Id,
                    ProductType.Accessory,
                    accessory.Name,
                    ci.Quantity,
                    accessory.Price,
                    accessory.Price * ci.Quantity,
                    false,
                    accessory.Id.ToString(),
                    "accessory",
                    accessory.Name,
                    accessory.Price,
                    accessory.ImageUrl,
                    null,
                    accessory.Brand?.Name,
                    accessory.Type?.Name,
                    null));
            }
        }

        return new CartDto(items, items.Sum(x => x.LineTotal));
    }
}
