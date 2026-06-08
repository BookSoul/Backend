using Application.DTO;
using Application.Interface;
using Domain.Entities.Accessories;
using Domain.Entities.Books;
using Domain.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Service;

public class ProductService : IProductService
{
    private readonly AppDbContext _context;

    public ProductService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<ProductListItemDto>> SearchProductsAsync(
        ProductType? type,
        string? keyword,
        Guid? categoryId,
        Guid? brandId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var results = new List<ProductListItemDto>();

        if (type is null or ProductType.Book)
        {
            var bookQuery = _context.Books
                .AsNoTracking()
                .Include(b => b.Category)
                .Where(b => b.IsActive);

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var normalized = keyword.Trim().ToLower();
                bookQuery = bookQuery.Where(b =>
                    b.Title.ToLower().Contains(normalized) ||
                    b.AuthorName.ToLower().Contains(normalized));
            }

            if (categoryId.HasValue)
            {
                bookQuery = bookQuery.Where(b => b.CategoryId == categoryId.Value);
            }

            var books = await bookQuery.OrderBy(b => b.Title).ToListAsync(cancellationToken);
            results.AddRange(books.Select(b => new ProductListItemDto(
                b.Id, ProductType.Book, b.Title, b.Price, b.Stock, b.ImageUrl, b.Category.Name, null)));
        }

        if (type is null or ProductType.Accessory)
        {
            var accQuery = _context.Accessories
                .AsNoTracking()
                .Include(a => a.Brand)
                .Include(a => a.Type)
                .Where(a => a.IsActive);

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var normalized = keyword.Trim().ToLower();
                accQuery = accQuery.Where(a => a.Name.ToLower().Contains(normalized));
            }

            if (brandId.HasValue)
            {
                accQuery = accQuery.Where(a => a.BrandId == brandId.Value);
            }

            var accessories = await accQuery.OrderBy(a => a.Name).ToListAsync(cancellationToken);
            results.AddRange(accessories.Select(a => new ProductListItemDto(
                a.Id, ProductType.Accessory, a.Name, a.Price, a.Stock, a.ImageUrl, null, a.Brand.Name)));
        }

        var safePage = page < 1 ? 1 : page;
        var safeSize = pageSize < 1 ? 20 : pageSize;
        return results
            .Skip((safePage - 1) * safeSize)
            .Take(safeSize)
            .ToList();
    }

    public async Task<ProductDetailDto?> GetProductByIdAsync(Guid id, ProductType type, CancellationToken cancellationToken = default)
    {
        if (type == ProductType.Book)
        {
            var book = await _context.Books
                .AsNoTracking()
                .Include(b => b.Category)
                .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

            return book is null ? null : MapBook(book);
        }

        var accessory = await _context.Accessories
            .AsNoTracking()
            .Include(a => a.Brand)
            .Include(a => a.Type)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        return accessory is null ? null : MapAccessory(accessory);
    }

    public async Task<ProductDetailDto> CreateBookAsync(CreateBookProductRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureImportTicketEditableAsync(request.ImportTicketId, cancellationToken);

        var book = new Book
        {
            Id = Guid.NewGuid(),
            Title = request.Title.Trim(),
            AuthorName = request.AuthorName.Trim(),
            CategoryId = request.CategoryId,
            Price = request.Price,
            Condition = request.Condition,
            Stock = request.Stock,
            ImageUrl = request.ImageUrl,
            Description = request.Description,
            ImportTicketId = request.ImportTicketId,
            IsActive = false
        };

        _context.Books.Add(book);
        await _context.SaveChangesAsync(cancellationToken);
        return (await GetProductByIdAsync(book.Id, ProductType.Book, cancellationToken))!;
    }

    public async Task<ProductDetailDto> CreateAccessoryAsync(CreateAccessoryProductRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureImportTicketEditableAsync(request.ImportTicketId, cancellationToken);

        var accessory = new Accessory
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            BrandId = request.BrandId,
            TypeId = request.TypeId,
            Price = request.Price,
            Stock = request.Stock,
            ImageUrl = request.ImageUrl,
            ImportTicketId = request.ImportTicketId,
            IsActive = false
        };

        _context.Accessories.Add(accessory);
        await _context.SaveChangesAsync(cancellationToken);
        return (await GetProductByIdAsync(accessory.Id, ProductType.Accessory, cancellationToken))!;
    }

    public async Task<ProductDetailDto> UpdateProductAsync(Guid id, ProductType type, UpdateProductRequest request, CancellationToken cancellationToken = default)
    {
        if (type == ProductType.Book)
        {
            var book = await _context.Books.FirstOrDefaultAsync(b => b.Id == id, cancellationToken)
                ?? throw new KeyNotFoundException("Book not found.");

            if (!string.IsNullOrWhiteSpace(request.Name)) book.Title = request.Name.Trim();
            if (request.Price.HasValue) book.Price = request.Price.Value;
            if (request.Stock.HasValue) book.Stock = request.Stock.Value;
            if (request.ImageUrl is not null) book.ImageUrl = request.ImageUrl;
            if (request.Description is not null) book.Description = request.Description;
            if (request.IsActive.HasValue) book.IsActive = request.IsActive.Value;
        }
        else
        {
            var accessory = await _context.Accessories.FirstOrDefaultAsync(a => a.Id == id, cancellationToken)
                ?? throw new KeyNotFoundException("Accessory not found.");

            if (!string.IsNullOrWhiteSpace(request.Name)) accessory.Name = request.Name.Trim();
            if (request.Price.HasValue) accessory.Price = request.Price.Value;
            if (request.Stock.HasValue) accessory.Stock = request.Stock.Value;
            if (request.ImageUrl is not null) accessory.ImageUrl = request.ImageUrl;
            if (request.IsActive.HasValue) accessory.IsActive = request.IsActive.Value;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return (await GetProductByIdAsync(id, type, cancellationToken))!;
    }

    private async Task EnsureImportTicketEditableAsync(Guid ticketId, CancellationToken cancellationToken)
    {
        var ticket = await _context.ImportTickets.FirstOrDefaultAsync(t => t.Id == ticketId, cancellationToken);
        if (ticket is null)
        {
            throw new KeyNotFoundException("Import ticket not found.");
        }

        if (ticket.Status != ImportTicketStatus.Pending || ticket.SubmittedAt.HasValue)
        {
            throw new InvalidOperationException("Import ticket is not editable.");
        }
    }

    private static ProductDetailDto MapBook(Book book) => new(
        book.Id, ProductType.Book, book.Title, book.Price, book.Stock, book.ImageUrl, book.IsActive,
        book.Description, null, book.AuthorName, book.CategoryId, book.Category.Name,
        null, null, null, null, book.Condition.ToString());

    private static ProductDetailDto MapAccessory(Accessory accessory) => new(
        accessory.Id, ProductType.Accessory, accessory.Name, accessory.Price, accessory.Stock,
        accessory.ImageUrl, accessory.IsActive, null, null, null, null, null,
        accessory.BrandId, accessory.Brand.Name, accessory.TypeId, accessory.Type.Name, null);
}
