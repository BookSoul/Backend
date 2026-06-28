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

            var books = await bookQuery.OrderByDescending(b => b.CreatedAt).ToListAsync(cancellationToken);
            results.AddRange(books.Select(b => new ProductListItemDto(
                b.Id,
                "book",
                b.Code,
                b.Title,
                b.Title,
                b.AuthorName,
                b.AuthorName,
                b.Price,
                b.OriginalPrice,
                b.Stock,
                b.Stock > 0,
                b.ImageUrl,
                b.ImageUrl,
                b.Category.Name,
                b.Category.Name,
                null,
                null)));
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
                a.Id,
                "accessory",
                a.Id.ToString(),
                null,
                a.Name,
                null,
                null,
                a.Price,
                a.OriginalPrice,
                a.Stock,
                a.Stock > 0,
                a.ImageUrl,
                a.ImageUrl,
                a.Type.Name,
                a.Type.Name,
                a.Brand.Name,
                a.Brand.Name)));
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
        if (request.ImportTicketId.HasValue)
        {
            await EnsureImportTicketEditableAsync(request.ImportTicketId.Value, cancellationToken);
        }

        var categoryId = await ResolveCategoryIdAsync(request.CategoryId, request.Category, cancellationToken);

        var book = new Book
        {
            Id = Guid.NewGuid(),
            Title = request.Title.Trim(),
            AuthorName = (request.AuthorName ?? request.Author ?? string.Empty).Trim(),
            CategoryId = categoryId,
            Price = request.Price,
            OriginalPrice = request.OriginalPrice,
            Condition = ParseCondition(request.Condition),
            Stock = request.Stock,
            ImageUrl = request.ImageUrl ?? request.Image,
            Description = request.Description,
            ImportTicketId = request.ImportTicketId,
            IsActive = request.IsActive ?? request.ApprovalStatus == "published",
            Publisher = request.Publisher,
            Year = request.Year,
            Pages = request.Pages,
            Language = request.Language,
            Seller = request.Seller,
            SellerNote = request.SellerNote,
            Featured = request.Featured ?? false,
            ApprovalStatus = request.ApprovalStatus ?? "published",
            RejectionNote = request.RejectionNote,
            CreatedBy = request.CreatedBy,
            CreatedByName = request.CreatedByName,
            CreatedByRole = request.CreatedByRole
        };

        _context.Books.Add(book);
        await _context.SaveChangesAsync(cancellationToken);
        return (await GetProductByIdAsync(book.Id, ProductType.Book, cancellationToken))!;
    }

    public async Task<ProductDetailDto> CreateAccessoryAsync(CreateAccessoryProductRequest request, CancellationToken cancellationToken = default)
    {
        if (request.ImportTicketId.HasValue)
        {
            await EnsureImportTicketEditableAsync(request.ImportTicketId.Value, cancellationToken);
        }

        var brandId = await ResolveBrandIdAsync(request.BrandId, request.Brand, cancellationToken);
        var typeId = await ResolveAccessoryTypeIdAsync(request.TypeId, request.Category, cancellationToken);

        var accessory = new Accessory
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            BrandId = brandId,
            TypeId = typeId,
            Price = request.Price,
            OriginalPrice = request.OriginalPrice,
            Stock = request.Stock,
            ImageUrl = request.ImageUrl ?? request.Image,
            Description = request.Description,
            ImportTicketId = request.ImportTicketId,
            IsActive = request.IsActive ?? request.ApprovalStatus == "published",
            ApprovalStatus = request.ApprovalStatus ?? "published",
            RejectionNote = request.RejectionNote,
            CreatedBy = request.CreatedBy,
            CreatedByName = request.CreatedByName,
            CreatedByRole = request.CreatedByRole
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

            if (!string.IsNullOrWhiteSpace(request.Title ?? request.Name)) book.Title = (request.Title ?? request.Name)!.Trim();
            if (!string.IsNullOrWhiteSpace(request.AuthorName ?? request.Author)) book.AuthorName = (request.AuthorName ?? request.Author)!.Trim();
            if (request.CategoryId.HasValue || !string.IsNullOrWhiteSpace(request.Category))
                book.CategoryId = await ResolveCategoryIdAsync(request.CategoryId, request.Category, cancellationToken);
            if (request.Price.HasValue) book.Price = request.Price.Value;
            if (request.OriginalPrice.HasValue) book.OriginalPrice = request.OriginalPrice;
            if (request.Stock.HasValue) book.Stock = request.Stock.Value;
            if (request.ImageUrl is not null || request.Image is not null) book.ImageUrl = request.ImageUrl ?? request.Image;
            if (request.Description is not null) book.Description = request.Description;
            if (request.Condition is not null) book.Condition = ParseCondition(request.Condition);
            if (request.Publisher is not null) book.Publisher = request.Publisher;
            if (request.Year is not null) book.Year = request.Year;
            if (request.Pages is not null) book.Pages = request.Pages;
            if (request.Language is not null) book.Language = request.Language;
            if (request.Seller is not null) book.Seller = request.Seller;
            if (request.SellerNote is not null) book.SellerNote = request.SellerNote;
            if (request.Featured.HasValue) book.Featured = request.Featured.Value;
            if (request.IsActive.HasValue) book.IsActive = request.IsActive.Value;
            if (request.ApprovalStatus is not null)
            {
                book.ApprovalStatus = request.ApprovalStatus;
                book.IsActive = request.ApprovalStatus == "published";
            }
            if (request.RejectionNote is not null) book.RejectionNote = request.RejectionNote;
            if (request.CreatedBy is not null) book.CreatedBy = request.CreatedBy;
            if (request.CreatedByName is not null) book.CreatedByName = request.CreatedByName;
            if (request.CreatedByRole is not null) book.CreatedByRole = request.CreatedByRole;
        }
        else
        {
            var accessory = await _context.Accessories.FirstOrDefaultAsync(a => a.Id == id, cancellationToken)
                ?? throw new KeyNotFoundException("Accessory not found.");

            if (!string.IsNullOrWhiteSpace(request.Name)) accessory.Name = request.Name.Trim();
            if (request.BrandId.HasValue || !string.IsNullOrWhiteSpace(request.Brand))
                accessory.BrandId = await ResolveBrandIdAsync(request.BrandId, request.Brand, cancellationToken);
            if (request.TypeId.HasValue || !string.IsNullOrWhiteSpace(request.Category))
                accessory.TypeId = await ResolveAccessoryTypeIdAsync(request.TypeId, request.Category, cancellationToken);
            if (request.Price.HasValue) accessory.Price = request.Price.Value;
            if (request.OriginalPrice.HasValue) accessory.OriginalPrice = request.OriginalPrice;
            if (request.Stock.HasValue) accessory.Stock = request.Stock.Value;
            if (request.InStock.HasValue && !request.Stock.HasValue && !request.InStock.Value) accessory.Stock = 0;
            if (request.ImageUrl is not null || request.Image is not null) accessory.ImageUrl = request.ImageUrl ?? request.Image;
            if (request.Description is not null) accessory.Description = request.Description;
            if (request.IsActive.HasValue) accessory.IsActive = request.IsActive.Value;
            if (request.ApprovalStatus is not null)
            {
                accessory.ApprovalStatus = request.ApprovalStatus;
                accessory.IsActive = request.ApprovalStatus == "published";
            }
            if (request.RejectionNote is not null) accessory.RejectionNote = request.RejectionNote;
            if (request.CreatedBy is not null) accessory.CreatedBy = request.CreatedBy;
            if (request.CreatedByName is not null) accessory.CreatedByName = request.CreatedByName;
            if (request.CreatedByRole is not null) accessory.CreatedByRole = request.CreatedByRole;
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

    private async Task<Guid> ResolveCategoryIdAsync(Guid? categoryId, string? categoryName, CancellationToken cancellationToken)
    {
        if (categoryId.HasValue) return categoryId.Value;
        var name = string.IsNullOrWhiteSpace(categoryName) ? "Khác" : categoryName.Trim();
        var existing = await _context.Categories.FirstOrDefaultAsync(c => c.Name == name, cancellationToken);
        if (existing is not null) return existing.Id;

        var category = new Category { Id = Guid.NewGuid(), Name = name, Code = SlugCode(name) };
        _context.Categories.Add(category);
        await _context.SaveChangesAsync(cancellationToken);
        return category.Id;
    }

    private async Task<Guid> ResolveBrandIdAsync(Guid? brandId, string? brandName, CancellationToken cancellationToken)
    {
        if (brandId.HasValue) return brandId.Value;
        var name = string.IsNullOrWhiteSpace(brandName) ? "BookSoul" : brandName.Trim();
        var existing = await _context.Brands.FirstOrDefaultAsync(b => b.Name == name, cancellationToken);
        if (existing is not null) return existing.Id;

        var brand = new Brand { Id = Guid.NewGuid(), Name = name };
        _context.Brands.Add(brand);
        await _context.SaveChangesAsync(cancellationToken);
        return brand.Id;
    }

    private async Task<Guid> ResolveAccessoryTypeIdAsync(Guid? typeId, string? typeName, CancellationToken cancellationToken)
    {
        if (typeId.HasValue) return typeId.Value;
        var name = string.IsNullOrWhiteSpace(typeName) ? "Phụ kiện" : typeName.Trim();
        var existing = await _context.AccessoryTypes.FirstOrDefaultAsync(t => t.Name == name, cancellationToken);
        if (existing is not null) return existing.Id;

        var type = new AccessoryType { Id = Guid.NewGuid(), Name = name };
        _context.AccessoryTypes.Add(type);
        await _context.SaveChangesAsync(cancellationToken);
        return type.Id;
    }

    private static BookCondition ParseCondition(string? condition)
    {
        if (string.IsNullOrWhiteSpace(condition)) return BookCondition.Good;
        var normalized = condition.Trim().ToLowerInvariant();
        if (normalized.Contains("new") || normalized.Contains("mới") || normalized.Contains("moi")) return BookCondition.LikeNew;
        if (normalized.Contains("rất") || normalized.Contains("rat") || normalized.Contains("very")) return BookCondition.Good;
        if (normalized.Contains("khá") || normalized.Contains("kha") || normalized.Contains("acceptable")) return BookCondition.Acceptable;
        return BookCondition.Good;
    }

    private static string SlugCode(string value)
    {
        var code = new string(value.ToUpperInvariant().Where(char.IsLetterOrDigit).Take(12).ToArray());
        return string.IsNullOrWhiteSpace(code) ? Guid.NewGuid().ToString("N")[..8].ToUpperInvariant() : code;
    }

    private static ProductDetailDto MapBook(Book book) => new(
        book.Id, "book", book.Title, book.Title, book.AuthorName, book.Price, book.Stock, book.Stock > 0,
        book.ImageUrl, book.ImageUrl, book.IsActive, book.Description, null, book.AuthorName, book.CategoryId, book.Category.Name,
        null, null, null, null, book.Condition.ToString(),
        book.OriginalPrice, book.Featured, book.Publisher, book.Year, book.Pages, book.Language, book.Seller ?? "BookSoul", book.SellerNote,
        Code: book.Code, CreatedAt: book.CreatedAt, ImportTicketId: book.ImportTicketId);

    private static ProductDetailDto MapAccessory(Accessory accessory) => new(
        accessory.Id, "accessory", null, accessory.Name, null, accessory.Price, accessory.Stock, accessory.Stock > 0,
        accessory.ImageUrl, accessory.ImageUrl, accessory.IsActive, accessory.Description, null, null, null, null,
        accessory.BrandId, accessory.Brand.Name, accessory.TypeId, accessory.Type.Name, null,
        accessory.OriginalPrice, null, null, null, null, null, null, null);
}
