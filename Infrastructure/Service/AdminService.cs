using Application.DTO;
using Application.Interface;
using Domain.Entities.Accessories;
using Domain.Entities.Books;
using Domain.Entities.Identity;
using Domain.Entities.System;
using Domain.Enums;
using Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Service;

public class AdminService : IAdminService
{
    private readonly AppDbContext _context;
    private readonly UserManager<User> _userManager;

    public AdminService(AppDbContext context, UserManager<User> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<UserAdminDto> LockUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString()) ?? throw new KeyNotFoundException("User not found.");
        user.LockoutEnd = DateTimeOffset.UtcNow.AddYears(100);
        await _userManager.UpdateAsync(user);
        return await MapUser(user);
    }

    public async Task<UserAdminDto> UnlockUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString()) ?? throw new KeyNotFoundException("User not found.");
        user.LockoutEnd = null;
        user.AccessFailedCount = 0;
        await _userManager.UpdateAsync(user);
        return await MapUser(user);
    }

    public async Task<UserAdminDto> UpdateUserRolesAsync(Guid userId, IReadOnlyList<string> roles, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString()) ?? throw new KeyNotFoundException("User not found.");
        var currentRoles = await _userManager.GetRolesAsync(user);
        if (currentRoles.Count > 0) await _userManager.RemoveFromRolesAsync(user, currentRoles);
        if (roles.Count > 0) await _userManager.AddToRolesAsync(user, roles);
        return await MapUser(user);
    }

    public async Task<CategoryDto> UpsertCategoryAsync(Guid? id, string name, string? description, CancellationToken cancellationToken = default)
    {
        Category category;
        if (id.HasValue)
        {
            category = await _context.Categories.FirstOrDefaultAsync(x => x.Id == id.Value, cancellationToken)
                ?? throw new KeyNotFoundException("Category not found.");
            category.Name = name.Trim();
            category.Description = description?.Trim();
        }
        else
        {
            category = new Category { Id = Guid.NewGuid(), Name = name.Trim(), Description = description?.Trim() };
            _context.Categories.Add(category);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return new CategoryDto(category.Id, category.Name, category.Description);
    }

    public async Task DeleteCategoryAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var category = await _context.Categories.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("Category not found.");
        _context.Categories.Remove(category);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<BrandDto> UpsertBrandAsync(Guid? id, string name, string? description, CancellationToken cancellationToken = default)
    {
        Brand brand;
        if (id.HasValue)
        {
            brand = await _context.Brands.FirstOrDefaultAsync(x => x.Id == id.Value, cancellationToken)
                ?? throw new KeyNotFoundException("Brand not found.");
            brand.Name = name.Trim();
            brand.Description = description?.Trim();
        }
        else
        {
            brand = new Brand { Id = Guid.NewGuid(), Name = name.Trim(), Description = description?.Trim() };
            _context.Brands.Add(brand);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return new BrandDto(brand.Id, brand.Name, brand.Description);
    }

    public async Task DeleteBrandAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var brand = await _context.Brands.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("Brand not found.");
        _context.Brands.Remove(brand);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<AccessoryTypeDto> UpsertAccessoryTypeAsync(Guid? id, string name, string? description, CancellationToken cancellationToken = default)
    {
        AccessoryType type;
        if (id.HasValue)
        {
            type = await _context.AccessoryTypes.FirstOrDefaultAsync(x => x.Id == id.Value, cancellationToken)
                ?? throw new KeyNotFoundException("Accessory type not found.");
            type.Name = name.Trim();
            type.Description = description?.Trim();
        }
        else
        {
            type = new AccessoryType { Id = Guid.NewGuid(), Name = name.Trim(), Description = description?.Trim() };
            _context.AccessoryTypes.Add(type);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return new AccessoryTypeDto(type.Id, type.Name, type.Description);
    }

    public async Task<ShippingFeeDto> UpdateShippingFeeAsync(decimal shippingFee, CancellationToken cancellationToken = default)
    {
        var setting = await _context.SystemSettings.FirstOrDefaultAsync(cancellationToken);
        if (setting is null)
        {
            setting = new SystemSetting { Id = Guid.NewGuid(), ShippingFee = shippingFee };
            _context.SystemSettings.Add(setting);
        }
        else
        {
            setting.ShippingFee = shippingFee;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return new ShippingFeeDto(setting.ShippingFee);
    }

    public async Task<VoucherDto> CreateVoucherAsync(string code, decimal discountAmount, DateTime expiryDate, decimal minOrderValue, CancellationToken cancellationToken = default)
    {
        var voucher = new Voucher
        {
            Id = Guid.NewGuid(),
            Code = code.Trim().ToUpperInvariant(),
            DiscountAmount = discountAmount,
            ExpiryDate = expiryDate,
            MinOrderValue = minOrderValue,
            IsActive = true
        };
        _context.Vouchers.Add(voucher);
        await _context.SaveChangesAsync(cancellationToken);
        return MapVoucher(voucher);
    }

    public async Task<AdminBannerDto> ManageBannerAsync(Guid? id, string title, string imageUrl, string? linkUrl, bool isActive, int displayOrder, CancellationToken cancellationToken = default)
    {
        Banner banner;
        if (id.HasValue)
        {
            banner = await _context.Banners.FirstOrDefaultAsync(x => x.Id == id.Value, cancellationToken)
                ?? throw new KeyNotFoundException("Banner not found.");
            banner.Title = title.Trim();
            banner.ImageUrl = imageUrl.Trim();
            banner.LinkUrl = linkUrl?.Trim();
            banner.IsActive = isActive;
            banner.DisplayOrder = displayOrder;
        }
        else
        {
            banner = new Banner
            {
                Id = Guid.NewGuid(),
                Title = title.Trim(),
                ImageUrl = imageUrl.Trim(),
                LinkUrl = linkUrl?.Trim(),
                IsActive = isActive,
                DisplayOrder = displayOrder
            };
            _context.Banners.Add(banner);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return new AdminBannerDto(banner.Id, banner.Title, banner.ImageUrl, banner.LinkUrl, banner.IsActive, banner.DisplayOrder);
    }

    private async Task<UserAdminDto> MapUser(User user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var isLocked = user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow;
        return new UserAdminDto(user.Id, user.Email ?? string.Empty, user.FullName, isLocked, roles.ToList());
    }

    public async Task<AdminDashboardSummaryDto> GetDashboardSummaryAsync(CancellationToken cancellationToken = default)
    {
        var totalRevenue = await _context.Orders.SumAsync(o => (decimal?)o.TotalAmount, cancellationToken) ?? 0m;
        var totalOrders = await _context.Orders.CountAsync(cancellationToken);
        var pendingOrders = await _context.Orders.CountAsync(o => o.Status == OrderStatus.Pending, cancellationToken);
        var newBuybackRequests = await _context.BuybackRequests.CountAsync(r => r.Status == BuybackRequestStatus.Pending, cancellationToken);
        return new AdminDashboardSummaryDto(totalRevenue, totalOrders, pendingOrders, newBuybackRequests);
    }

    public async Task<ProductDetailDto> CreateBookAsync(BookUpsertRequest request, CancellationToken cancellationToken = default)
    {
        var book = new Book
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            AuthorName = request.AuthorName,
            CategoryId = request.CategoryId,
            Price = request.Price,
            Stock = request.Stock,
            ImageUrl = request.ImageUrl,
            Description = request.Description,
            IsActive = request.IsActive
        };
        _context.Books.Add(book);
        await _context.SaveChangesAsync(cancellationToken);
        return new ProductDetailDto(book.Id, ProductType.Book, book.Title, book.Price, book.Stock, book.ImageUrl, book.IsActive, book.Description, null, book.AuthorName, book.CategoryId, null, null, null, null, null, null);
    }

    public async Task<ProductDetailDto> UpdateBookAsync(Guid id, BookUpsertRequest request, CancellationToken cancellationToken = default)
    {
        var book = await _context.Books.FirstOrDefaultAsync(b => b.Id == id, cancellationToken) ?? throw new KeyNotFoundException("Book not found.");
        book.Title = request.Title;
        book.AuthorName = request.AuthorName;
        book.CategoryId = request.CategoryId;
        book.Price = request.Price;
        book.Stock = request.Stock;
        book.ImageUrl = request.ImageUrl;
        book.Description = request.Description;
        book.IsActive = request.IsActive;
        await _context.SaveChangesAsync(cancellationToken);
        return new ProductDetailDto(book.Id, ProductType.Book, book.Title, book.Price, book.Stock, book.ImageUrl, book.IsActive, book.Description, null, book.AuthorName, book.CategoryId, null, null, null, null, null, null);
    }

    public async Task DeleteBookAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var book = await _context.Books.FirstOrDefaultAsync(b => b.Id == id, cancellationToken) ?? throw new KeyNotFoundException("Book not found.");
        _context.Books.Remove(book);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<ProductDetailDto> CreateAccessoryAsync(AccessoryUpsertRequest request, CancellationToken cancellationToken = default)
    {
        var accessory = new Accessory
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            BrandId = request.BrandId,
            TypeId = request.TypeId,
            Price = request.Price,
            Stock = request.Stock,
            ImageUrl = request.ImageUrl,
            IsActive = request.IsActive
        };
        _context.Accessories.Add(accessory);
        await _context.SaveChangesAsync(cancellationToken);
        return new ProductDetailDto(accessory.Id, ProductType.Accessory, accessory.Name, accessory.Price, accessory.Stock, accessory.ImageUrl, accessory.IsActive, null, null, null, null, null, accessory.BrandId, null, accessory.TypeId, null, null);
    }

    public async Task<ProductDetailDto> UpdateAccessoryAsync(Guid id, AccessoryUpsertRequest request, CancellationToken cancellationToken = default)
    {
        var accessory = await _context.Accessories.FirstOrDefaultAsync(a => a.Id == id, cancellationToken) ?? throw new KeyNotFoundException("Accessory not found.");
        accessory.Name = request.Name;
        accessory.BrandId = request.BrandId;
        accessory.TypeId = request.TypeId;
        accessory.Price = request.Price;
        accessory.Stock = request.Stock;
        accessory.ImageUrl = request.ImageUrl;
        accessory.IsActive = request.IsActive;
        await _context.SaveChangesAsync(cancellationToken);
        return new ProductDetailDto(accessory.Id, ProductType.Accessory, accessory.Name, accessory.Price, accessory.Stock, accessory.ImageUrl, accessory.IsActive, null, null, null, null, null, accessory.BrandId, null, accessory.TypeId, null, null);
    }

    public async Task DeleteAccessoryAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var accessory = await _context.Accessories.FirstOrDefaultAsync(a => a.Id == id, cancellationToken) ?? throw new KeyNotFoundException("Accessory not found.");
        _context.Accessories.Remove(accessory);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AdminOrderDto>> GetOrdersAsync(CancellationToken cancellationToken = default)
        => await _context.Orders.AsNoTracking().OrderByDescending(o => o.OrderDate).Select(o => new AdminOrderDto(o.Id, o.Status.ToString(), o.TotalAmount, o.OrderDate)).ToListAsync(cancellationToken);

    public async Task<AdminOrderDto> UpdateOrderStatusAsync(Guid orderId, OrderStatus status, CancellationToken cancellationToken = default)
    {
        var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken) ?? throw new KeyNotFoundException("Order not found.");
        order.Status = status;
        await _context.SaveChangesAsync(cancellationToken);
        return new AdminOrderDto(order.Id, order.Status.ToString(), order.TotalAmount, order.OrderDate);
    }

    public async Task<IReadOnlyList<AdminBuybackDto>> GetBuybacksAsync(CancellationToken cancellationToken = default)
        => await _context.BuybackRequests.AsNoTracking().OrderByDescending(r => r.CreatedAt).Select(r => new AdminBuybackDto(r.Id, r.RequestCode, r.Status.ToString(), r.ProposedPrice, r.ApprovedPrice, r.CreatedAt)).ToListAsync(cancellationToken);

    public async Task<AdminBuybackDto> ApproveBuybackAsync(Guid id, decimal? approvedPrice, string? adminNotes, CancellationToken cancellationToken = default)
    {
        var request = await _context.BuybackRequests.FirstOrDefaultAsync(r => r.Id == id, cancellationToken) ?? throw new KeyNotFoundException("Buyback request not found.");
        request.Status = BuybackRequestStatus.Approved;
        request.ApprovedPrice = approvedPrice;
        request.AdminNotes = adminNotes;
        await _context.SaveChangesAsync(cancellationToken);
        return new AdminBuybackDto(request.Id, request.RequestCode, request.Status.ToString(), request.ProposedPrice, request.ApprovedPrice, request.CreatedAt);
    }

    public async Task<AdminBuybackDto> RejectBuybackAsync(Guid id, string reason, CancellationToken cancellationToken = default)
    {
        var request = await _context.BuybackRequests.FirstOrDefaultAsync(r => r.Id == id, cancellationToken) ?? throw new KeyNotFoundException("Buyback request not found.");
        request.Status = BuybackRequestStatus.Rejected;
        request.AdminNotes = reason;
        await _context.SaveChangesAsync(cancellationToken);
        return new AdminBuybackDto(request.Id, request.RequestCode, request.Status.ToString(), request.ProposedPrice, request.ApprovedPrice, request.CreatedAt);
    }

    public Task<IReadOnlyList<AdminChartPointDto>> GetChartDataAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<AdminChartPointDto>>(Array.Empty<AdminChartPointDto>());

    public Task<byte[]> ExportStatisticsAsync(string format, CancellationToken cancellationToken = default)
        => Task.FromResult(Array.Empty<byte>());

    private static VoucherDto MapVoucher(Voucher voucher) =>
        new(voucher.Id, voucher.Code, voucher.DiscountAmount, voucher.ExpiryDate, voucher.MinOrderValue, voucher.IsActive);
}
