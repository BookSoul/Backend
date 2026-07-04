using Application.DTO;
using Application.Interface;
using Domain.Entities.Accessories;
using Domain.Entities.Books;
using Domain.Entities.Identity;
using Domain.Entities.Orders;
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

    public async Task<IReadOnlyList<UserAdminDto>> GetStaffUsersAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var staffUsers = await _userManager.GetUsersInRoleAsync("Staff");
        var shipperUsers = await _userManager.GetUsersInRoleAsync("Shipper");
        var results = new List<UserAdminDto>();
        foreach (var user in staffUsers.Concat(shipperUsers).DistinctBy(u => u.Id).OrderBy(u => u.FullName).ThenBy(u => u.Email))
        {
            results.Add(await MapUser(user));
        }

        return results;
    }

    public async Task<UserAdminDto> CreateStaffAsync(CreateStaffRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var fullName = request.FullName?.Trim();
        var email = request.Email?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new InvalidOperationException("Vui lòng nhập họ và tên nhân viên.");
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new InvalidOperationException("Vui lòng nhập email nhân viên.");
        }

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 6)
        {
            throw new InvalidOperationException("Mật khẩu phải có ít nhất 6 ký tự.");
        }

        if (await _userManager.FindByEmailAsync(email) is not null)
        {
            throw new InvalidOperationException("Email này đã được sử dụng.");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            UserName = email,
            FullName = fullName,
            EmailConfirmed = true,
            LockoutEnabled = true
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(string.Join(" ", result.Errors.Select(e => e.Description)));
        }

        var employeeRole = NormalizeEmployeeRole(request.Role);
        var roleResult = await _userManager.AddToRoleAsync(user, employeeRole);
        if (!roleResult.Succeeded)
        {
            await _userManager.DeleteAsync(user);
            throw new InvalidOperationException(string.Join(" ", roleResult.Errors.Select(e => e.Description)));
        }

        return await MapUser(user);
    }

    public async Task DeleteStaffAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var user = await _userManager.FindByIdAsync(userId.ToString()) ?? throw new KeyNotFoundException("Employee not found.");
        if (!await IsEmployeeAsync(user))
        {
            throw new InvalidOperationException("Chỉ được xóa tài khoản Staff tại màn hình này.");
        }

        if (await _userManager.IsInRoleAsync(user, "Admin"))
        {
            throw new InvalidOperationException("Không thể xóa tài khoản Admin tại màn hình Staff.");
        }

        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(string.Join(" ", result.Errors.Select(e => e.Description)));
        }
    }

    public async Task<UserAdminDto> UpdateStaffAsync(Guid userId, UpdateAdminUserRequest request, CancellationToken cancellationToken = default)
    {
        var user = await GetEmployeeUserAsync(userId, cancellationToken);
        if (await _userManager.IsInRoleAsync(user, "Admin"))
        {
            throw new InvalidOperationException("Cannot update an Admin account from Staff management.");
        }

        var updated = await UpdateUserAsync(user, request, cancellationToken);
        if (!string.IsNullOrWhiteSpace(request.Role))
        {
            await SetEmployeeRoleAsync(user, NormalizeEmployeeRole(request.Role));
            updated = await MapUser(user);
        }

        return updated;
    }

    public async Task<IReadOnlyList<UserAdminDto>> GetCustomerUsersAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var customerUsers = await _userManager.GetUsersInRoleAsync("Customer");
        var results = new List<UserAdminDto>();
        foreach (var user in customerUsers.OrderBy(u => u.FullName).ThenBy(u => u.Email))
        {
            if (await _userManager.IsInRoleAsync(user, "Admin") || await IsEmployeeAsync(user))
            {
                continue;
            }

            results.Add(await MapUser(user));
        }

        return results;
    }

    public async Task<UserAdminDto> CreateCustomerAsync(CreateCustomerRequest request, CancellationToken cancellationToken = default)
    {
        return await CreateUserWithRoleAsync(
            request.FullName,
            request.Email,
            request.Password,
            "Customer",
            request.UserName,
            request.Phone,
            request.Address,
            request.Avatar,
            cancellationToken);
    }

    public async Task<UserAdminDto> UpdateCustomerAsync(Guid userId, UpdateAdminUserRequest request, CancellationToken cancellationToken = default)
    {
        var user = await GetUserInRoleAsync(userId, "Customer", cancellationToken);
        if (await _userManager.IsInRoleAsync(user, "Admin") || await IsEmployeeAsync(user))
        {
            throw new InvalidOperationException("Cannot update an admin or staff account from Customer management.");
        }

        return await UpdateUserAsync(user, request, cancellationToken);
    }

    public async Task DeleteCustomerAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await GetUserInRoleAsync(userId, "Customer", cancellationToken);
        if (await _userManager.IsInRoleAsync(user, "Admin") || await IsEmployeeAsync(user))
        {
            throw new InvalidOperationException("Cannot delete an admin or staff account from Customer management.");
        }

        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(string.Join(" ", result.Errors.Select(e => e.Description)));
        }
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

    public async Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Categories
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new CategoryDto(c.Id, c.Name, c.Description))
            .ToListAsync(cancellationToken);
    }

    public async Task<CategoryDto> UpsertCategoryAsync(Guid? id, string name, string? description, CancellationToken cancellationToken = default)
    {
        var normalizedName = NormalizeCatalogName(name, "Tên thể loại không được để trống.");
        var duplicate = await _context.Categories
            .AnyAsync(x => x.Name == normalizedName && (!id.HasValue || x.Id != id.Value), cancellationToken);
        if (duplicate)
        {
            throw new InvalidOperationException("Thể loại này đã tồn tại.");
        }

        Category category;
        if (id.HasValue)
        {
            category = await _context.Categories.FirstOrDefaultAsync(x => x.Id == id.Value, cancellationToken)
                ?? throw new KeyNotFoundException("Category not found.");
            category.Name = normalizedName;
            category.Code = SlugCode(normalizedName);
            category.Description = description?.Trim();
        }
        else
        {
            category = new Category { Id = Guid.NewGuid(), Name = normalizedName, Code = SlugCode(normalizedName), Description = description?.Trim() };
            _context.Categories.Add(category);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return new CategoryDto(category.Id, category.Name, category.Description);
    }

    public async Task DeleteCategoryAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var category = await _context.Categories.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("Category not found.");
        var isUsed = await _context.Books.AnyAsync(b => b.CategoryId == id, cancellationToken);
        if (isUsed)
        {
            throw new InvalidOperationException("Không thể xóa thể loại đang được sử dụng bởi sách.");
        }

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AuthorDto>> GetAuthorsAsync(CancellationToken cancellationToken = default)
    {
        var catalogAuthors = await _context.Authors
            .AsNoTracking()
            .OrderBy(a => a.Name)
            .Select(a => new AuthorDto(a.Id, a.Name, a.Biography))
            .ToListAsync(cancellationToken);

        if (catalogAuthors.Count > 0)
        {
            return catalogAuthors;
        }

        var authorNames = await _context.Books
            .AsNoTracking()
            .Select(b => b.AuthorName)
            .Where(name => name != string.Empty)
            .Distinct()
            .OrderBy(name => name)
            .ToListAsync(cancellationToken);

        return authorNames.Select(name => new AuthorDto(Guid.Empty, name, null)).ToList();
    }

    public async Task<AuthorDto> UpsertAuthorAsync(Guid? id, string name, string? biography, CancellationToken cancellationToken = default)
    {
        var normalizedName = NormalizeCatalogName(name, "Tên tác giả không được để trống.");
        var duplicate = await _context.Authors
            .AnyAsync(x => x.Name == normalizedName && (!id.HasValue || x.Id != id.Value), cancellationToken);
        if (duplicate)
        {
            throw new InvalidOperationException("Tác giả này đã tồn tại.");
        }

        Author author;
        if (id.HasValue && id.Value != Guid.Empty)
        {
            author = await _context.Authors.FirstOrDefaultAsync(x => x.Id == id.Value, cancellationToken)
                ?? throw new KeyNotFoundException("Author not found.");
            var oldName = author.Name;
            author.Name = normalizedName;
            author.Biography = biography?.Trim();

            if (!string.Equals(oldName, normalizedName, StringComparison.Ordinal))
            {
                var books = await _context.Books.Where(b => b.AuthorName == oldName).ToListAsync(cancellationToken);
                foreach (var book in books)
                {
                    book.AuthorName = normalizedName;
                }
            }
        }
        else
        {
            author = new Author { Id = Guid.NewGuid(), Name = normalizedName, Biography = biography?.Trim() };
            _context.Authors.Add(author);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return new AuthorDto(author.Id, author.Name, author.Biography);
    }

    public async Task DeleteAuthorAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var author = await _context.Authors.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("Author not found.");
        var isUsed = await _context.Books.AnyAsync(b => b.AuthorName == author.Name, cancellationToken);
        if (isUsed)
        {
            throw new InvalidOperationException("Không thể xóa tác giả đang được sử dụng bởi sách.");
        }

        _context.Authors.Remove(author);
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

    private async Task<User> GetUserInRoleAsync(Guid userId, string role, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var user = await _userManager.FindByIdAsync(userId.ToString()) ?? throw new KeyNotFoundException("User not found.");
        if (!await _userManager.IsInRoleAsync(user, role))
        {
            throw new InvalidOperationException($"User is not in role {role}.");
        }

        return user;
    }

    private async Task<User> GetEmployeeUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var user = await _userManager.FindByIdAsync(userId.ToString()) ?? throw new KeyNotFoundException("Employee not found.");
        if (!await IsEmployeeAsync(user))
        {
            throw new InvalidOperationException("User is not an employee.");
        }

        return user;
    }

    private async Task<bool> IsEmployeeAsync(User user)
    {
        return await _userManager.IsInRoleAsync(user, "Staff") || await _userManager.IsInRoleAsync(user, "Shipper");
    }

    private static string NormalizeEmployeeRole(string? role)
    {
        if (string.IsNullOrWhiteSpace(role))
        {
            return "Staff";
        }

        if (role.Equals("staff", StringComparison.OrdinalIgnoreCase))
        {
            return "Staff";
        }

        if (role.Equals("shipper", StringComparison.OrdinalIgnoreCase))
        {
            return "Shipper";
        }

        throw new InvalidOperationException("Vai trò nhân viên không hợp lệ.");
    }

    private async Task SetEmployeeRoleAsync(User user, string role)
    {
        var employeeRoles = (await _userManager.GetRolesAsync(user))
            .Where(currentRole =>
                currentRole.Equals("Staff", StringComparison.OrdinalIgnoreCase) ||
                currentRole.Equals("Shipper", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (employeeRoles.Length > 0)
        {
            var removeResult = await _userManager.RemoveFromRolesAsync(user, employeeRoles);
            if (!removeResult.Succeeded)
            {
                throw new InvalidOperationException(string.Join(" ", removeResult.Errors.Select(e => e.Description)));
            }
        }

        var addResult = await _userManager.AddToRoleAsync(user, role);
        if (!addResult.Succeeded)
        {
            throw new InvalidOperationException(string.Join(" ", addResult.Errors.Select(e => e.Description)));
        }
    }

    private async Task<UserAdminDto> CreateUserWithRoleAsync(
        string fullName,
        string email,
        string password,
        string role,
        string? userName,
        string? phone,
        string? address,
        string? avatar,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var normalizedFullName = fullName?.Trim();
        var normalizedEmail = email?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalizedFullName))
        {
            throw new InvalidOperationException("Vui lòng nhập họ và tên.");
        }

        if (string.IsNullOrWhiteSpace(normalizedEmail))
        {
            throw new InvalidOperationException("Vui lòng nhập email.");
        }

        if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
        {
            throw new InvalidOperationException("Mật khẩu phải có ít nhất 6 ký tự.");
        }

        if (await _userManager.FindByEmailAsync(normalizedEmail) is not null)
        {
            throw new InvalidOperationException("Email này đã được sử dụng.");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = normalizedEmail,
            UserName = string.IsNullOrWhiteSpace(userName) ? normalizedEmail : userName.Trim(),
            FullName = normalizedFullName,
            PhoneNumber = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim(),
            Address = string.IsNullOrWhiteSpace(address) ? null : address.Trim(),
            AvatarUrl = string.IsNullOrWhiteSpace(avatar) ? null : avatar.Trim(),
            EmailConfirmed = true,
            LockoutEnabled = true
        };

        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(string.Join(" ", result.Errors.Select(e => e.Description)));
        }

        var roleResult = await _userManager.AddToRoleAsync(user, role);
        if (!roleResult.Succeeded)
        {
            await _userManager.DeleteAsync(user);
            throw new InvalidOperationException(string.Join(" ", roleResult.Errors.Select(e => e.Description)));
        }

        return await MapUser(user);
    }

    private async Task<UserAdminDto> UpdateUserAsync(User user, UpdateAdminUserRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var fullName = request.FullName?.Trim();
        var email = request.Email?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new InvalidOperationException("Vui lòng nhập họ và tên.");
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new InvalidOperationException("Vui lòng nhập email.");
        }

        var existingByEmail = await _userManager.FindByEmailAsync(email);
        if (existingByEmail is not null && existingByEmail.Id != user.Id)
        {
            throw new InvalidOperationException("Email này đã được sử dụng.");
        }

        user.FullName = fullName;
        user.Email = email;
        user.NormalizedEmail = _userManager.NormalizeEmail(email);
        user.UserName = string.IsNullOrWhiteSpace(request.UserName) ? email : request.UserName.Trim();
        user.NormalizedUserName = _userManager.NormalizeName(user.UserName);
        user.PhoneNumber = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim();
        user.Address = string.IsNullOrWhiteSpace(request.Address) ? null : request.Address.Trim();
        user.AvatarUrl = string.IsNullOrWhiteSpace(request.Avatar) ? null : request.Avatar.Trim();

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            throw new InvalidOperationException(string.Join(" ", updateResult.Errors.Select(e => e.Description)));
        }

        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            if (request.Password.Length < 6)
            {
                throw new InvalidOperationException("Mật khẩu phải có ít nhất 6 ký tự.");
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var passwordResult = await _userManager.ResetPasswordAsync(user, token, request.Password);
            if (!passwordResult.Succeeded)
            {
                throw new InvalidOperationException(string.Join(" ", passwordResult.Errors.Select(e => e.Description)));
            }
        }

        return await MapUser(user);
    }

    private async Task<UserAdminDto> MapUser(User user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var isLocked = user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow;
        return new UserAdminDto(
            user.Id,
            user.Email ?? string.Empty,
            user.FullName,
            isLocked,
            roles.ToList(),
            user.UserName,
            user.PhoneNumber,
            user.Address,
            user.AvatarUrl);
    }

    public async Task<AdminDashboardSummaryDto> GetDashboardSummaryAsync(CancellationToken cancellationToken = default)
    {
        var totalRevenue = await _context.Orders.SumAsync(o => (decimal?)o.TotalAmount, cancellationToken) ?? 0m;
        var totalOrders = await _context.Orders.CountAsync(cancellationToken);
        var pendingOrders = await _context.Orders.CountAsync(o =>
            o.Status == OrderStatus.Pending ||
            o.Status == OrderStatus.AwaitingPreparation ||
            o.Status == OrderStatus.ReadyForDelivery ||
            o.Status == OrderStatus.ReturnRequested,
            cancellationToken);
        var newBuybackRequests = await _context.BuybackRequests.CountAsync(r => r.Status == BuybackRequestStatus.Pending, cancellationToken);
        return new AdminDashboardSummaryDto(totalRevenue, totalOrders, pendingOrders, newBuybackRequests);
    }

    public async Task<IReadOnlyList<ProductDetailDto>> GetBooksAsync(CancellationToken cancellationToken = default)
    {
        var books = await _context.Books
            .AsNoTracking()
            .Include(b => b.Category)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync(cancellationToken);

        return books.Select(MapBook).ToList();
    }

    public async Task<ProductDetailDto> CreateBookAsync(BookUpsertRequest request, CancellationToken cancellationToken = default)
    {
        var approvalStatus = request.ApprovalStatus ?? "published";
        var authorName = NormalizeCatalogName(request.AuthorName ?? request.Author ?? string.Empty, "Tên tác giả không được để trống.");
        await EnsureAuthorCatalogAsync(authorName, cancellationToken);

        var book = new Book
        {
            Id = Guid.NewGuid(),
            Title = request.Title.Trim(),
            AuthorName = authorName,
            CategoryId = await ResolveCategoryIdAsync(request.CategoryId, request.Category, cancellationToken),
            Price = request.Price,
            OriginalPrice = request.OriginalPrice,
            Stock = request.Stock,
            ImageUrl = request.ImageUrl ?? request.Image,
            Description = request.Description,
            Condition = ParseCondition(request.Condition),
            IsActive = ResolveProductActive(request.IsActive, approvalStatus, true),
            Publisher = request.Publisher,
            Year = request.Year,
            Pages = request.Pages,
            Language = request.Language,
            Seller = request.Seller,
            SellerNote = request.SellerNote,
            Featured = request.Featured ?? false,
            ApprovalStatus = approvalStatus,
            RejectionNote = request.RejectionNote,
            CreatedBy = request.CreatedBy,
            CreatedByName = request.CreatedByName,
            CreatedByRole = request.CreatedByRole
        };
        _context.Books.Add(book);
        await _context.SaveChangesAsync(cancellationToken);
        return MapBook(book);
    }

    public async Task<ProductDetailDto> UpdateBookAsync(Guid id, BookUpsertRequest request, CancellationToken cancellationToken = default)
    {
        var book = await _context.Books
            .Include(b => b.Category)
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("Book not found.");
        var approvalStatus = request.ApprovalStatus ?? book.ApprovalStatus;
        var authorName = NormalizeCatalogName(request.AuthorName ?? request.Author ?? book.AuthorName, "Tên tác giả không được để trống.");
        await EnsureAuthorCatalogAsync(authorName, cancellationToken);

        book.Title = request.Title.Trim();
        book.AuthorName = authorName;
        book.CategoryId = await ResolveCategoryIdAsync(request.CategoryId, request.Category, cancellationToken);
        book.Price = request.Price;
        book.OriginalPrice = request.OriginalPrice;
        book.Stock = request.Stock;
        book.ImageUrl = request.ImageUrl ?? request.Image;
        book.Description = request.Description;
        book.Condition = ParseCondition(request.Condition);
        book.IsActive = ResolveProductActive(request.IsActive, approvalStatus, book.IsActive);
        book.Publisher = request.Publisher;
        book.Year = request.Year;
        book.Pages = request.Pages;
        book.Language = request.Language;
        book.Seller = request.Seller;
        book.SellerNote = request.SellerNote;
        book.Featured = request.Featured ?? book.Featured;
        book.ApprovalStatus = approvalStatus;
        book.RejectionNote = request.RejectionNote;
        book.CreatedBy = request.CreatedBy ?? book.CreatedBy;
        book.CreatedByName = request.CreatedByName ?? book.CreatedByName;
        book.CreatedByRole = request.CreatedByRole ?? book.CreatedByRole;
        await _context.SaveChangesAsync(cancellationToken);
        return MapBook(book);
    }

    public async Task DeleteBookAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var book = await _context.Books.FirstOrDefaultAsync(b => b.Id == id, cancellationToken) ?? throw new KeyNotFoundException("Book not found.");
        _context.Books.Remove(book);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProductDetailDto>> GetAccessoriesAsync(CancellationToken cancellationToken = default)
    {
        var accessories = await _context.Accessories
            .AsNoTracking()
            .Include(a => a.Brand)
            .Include(a => a.Type)
            .OrderBy(a => a.Name)
            .ToListAsync(cancellationToken);

        return accessories.Select(MapAccessory).ToList();
    }

    public async Task<ProductDetailDto> CreateAccessoryAsync(AccessoryUpsertRequest request, CancellationToken cancellationToken = default)
    {
        var approvalStatus = request.ApprovalStatus ?? "published";
        var accessory = new Accessory
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            BrandId = await ResolveBrandIdAsync(request.BrandId, request.Brand, cancellationToken),
            TypeId = await ResolveAccessoryTypeIdAsync(request.TypeId, request.Category, cancellationToken),
            Price = request.Price,
            OriginalPrice = request.OriginalPrice,
            Stock = request.Stock,
            ImageUrl = request.ImageUrl ?? request.Image,
            Description = request.Description,
            IsActive = ResolveProductActive(request.IsActive, approvalStatus, true),
            ApprovalStatus = approvalStatus,
            RejectionNote = request.RejectionNote,
            CreatedBy = request.CreatedBy,
            CreatedByName = request.CreatedByName,
            CreatedByRole = request.CreatedByRole
        };
        _context.Accessories.Add(accessory);
        await _context.SaveChangesAsync(cancellationToken);
        return MapAccessory(accessory);
    }

    public async Task<ProductDetailDto> UpdateAccessoryAsync(Guid id, AccessoryUpsertRequest request, CancellationToken cancellationToken = default)
    {
        var accessory = await _context.Accessories
            .Include(a => a.Brand)
            .Include(a => a.Type)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("Accessory not found.");
        var approvalStatus = request.ApprovalStatus ?? accessory.ApprovalStatus;
        accessory.Name = request.Name;
        accessory.BrandId = await ResolveBrandIdAsync(request.BrandId, request.Brand, cancellationToken);
        accessory.TypeId = await ResolveAccessoryTypeIdAsync(request.TypeId, request.Category, cancellationToken);
        accessory.Price = request.Price;
        accessory.OriginalPrice = request.OriginalPrice;
        accessory.Stock = request.Stock;
        if (request.InStock.HasValue && !request.InStock.Value) accessory.Stock = 0;
        accessory.ImageUrl = request.ImageUrl ?? request.Image;
        accessory.Description = request.Description;
        accessory.IsActive = ResolveProductActive(request.IsActive, approvalStatus, accessory.IsActive);
        accessory.ApprovalStatus = approvalStatus;
        accessory.RejectionNote = request.RejectionNote;
        accessory.CreatedBy = request.CreatedBy ?? accessory.CreatedBy;
        accessory.CreatedByName = request.CreatedByName ?? accessory.CreatedByName;
        accessory.CreatedByRole = request.CreatedByRole ?? accessory.CreatedByRole;
        await _context.SaveChangesAsync(cancellationToken);
        return MapAccessory(accessory);
    }

    public async Task DeleteAccessoryAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var accessory = await _context.Accessories.FirstOrDefaultAsync(a => a.Id == id, cancellationToken) ?? throw new KeyNotFoundException("Accessory not found.");
        _context.Accessories.Remove(accessory);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task<Dictionary<Guid, string>> GetBlindBoxBookTitlesAsync(IEnumerable<Order> orders, CancellationToken cancellationToken)
    {
        var blindBoxBookIds = orders
            .SelectMany(o => o.OrderItems)
            .Where(oi => oi.BlindBoxTier.HasValue && oi.BookId.HasValue)
            .Select(oi => oi.BookId!.Value)
            .Distinct()
            .ToList();
            
        if (blindBoxBookIds.Count == 0) return new Dictionary<Guid, string>();
            
        return await _context.Books
            .Where(b => blindBoxBookIds.Contains(b.Id))
            .ToDictionaryAsync(b => b.Id, b => b.Title, cancellationToken);
    }

    public async Task<IReadOnlyList<OrderSummaryDto>> GetOrdersAsync(CancellationToken cancellationToken = default)
    {
        var orders = await _context.Orders
            .AsNoTracking()
            .Include(o => o.OrderItems)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync(cancellationToken);

        var bookTitles = await GetBlindBoxBookTitlesAsync(orders, cancellationToken);
        return orders.Select(o => MapOrder(o, bookTitles)).ToList();
    }

    public async Task<OrderSummaryDto> UpdateOrderStatusAsync(Guid orderId, OrderStatus status, string? reason = null, CancellationToken cancellationToken = default)
    {
        var order = await _context.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken)
            ?? throw new KeyNotFoundException("Order not found.");

        if (!CanTransitionOrder(order.Status, status))
        {
            throw new InvalidOperationException($"Cannot move order from {order.Status} to {status}.");
        }

        if (status == OrderStatus.Cancelled)
        {
            if (string.IsNullOrWhiteSpace(reason))
            {
                throw new InvalidOperationException("Cancellation reason is required.");
            }

            order.CancellationReason = reason.Trim();
            order.CancelledAt = DateTime.UtcNow;
        }

        order.Status = status;
        await _context.SaveChangesAsync(cancellationToken);
        
        var bookTitles = await GetBlindBoxBookTitlesAsync(new[] { order }, cancellationToken);
        return MapOrder(order, bookTitles);
    }

    public async Task<OrderSummaryDto> ApproveReturnAsync(Guid orderId, string? note, CancellationToken cancellationToken = default)
    {
        var order = await _context.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken)
            ?? throw new KeyNotFoundException("Order not found.");

        if (order.Status != OrderStatus.ReturnRequested)
        {
            throw new InvalidOperationException("Only return requests can be approved.");
        }

        order.Status = OrderStatus.Returned;
        order.ReturnReviewNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        order.ReturnReviewedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        
        var bookTitles = await GetBlindBoxBookTitlesAsync(new[] { order }, cancellationToken);
        return MapOrder(order, bookTitles);
    }

    public async Task<OrderSummaryDto> RejectReturnAsync(Guid orderId, string? note, CancellationToken cancellationToken = default)
    {
        var order = await _context.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken)
            ?? throw new KeyNotFoundException("Order not found.");

        if (order.Status != OrderStatus.ReturnRequested)
        {
            throw new InvalidOperationException("Only return requests can be rejected.");
        }

        order.Status = OrderStatus.ReturnRejected;
        order.ReturnReviewNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        order.ReturnReviewedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        
        var bookTitles = await GetBlindBoxBookTitlesAsync(new[] { order }, cancellationToken);
        return MapOrder(order, bookTitles);
    }

    public async Task<IReadOnlyList<AdminBuybackDto>> GetBuybacksAsync(CancellationToken cancellationToken = default)
        => await _context.BuybackRequests.AsNoTracking().OrderByDescending(r => r.CreatedAt).Select(r => new AdminBuybackDto(r.Id, r.RequestCode, ToFrontendBuybackStatus(r.Status), r.ProposedPrice, r.ApprovedPrice, r.CreatedAt)).ToListAsync(cancellationToken);

    public async Task<AdminBuybackDto> ApproveBuybackAsync(Guid id, decimal? approvedPrice, string? adminNotes, CancellationToken cancellationToken = default)
    {
        var request = await _context.BuybackRequests.FirstOrDefaultAsync(r => r.Id == id, cancellationToken) ?? throw new KeyNotFoundException("Buyback request not found.");
        request.Status = BuybackRequestStatus.Approved;
        request.ApprovedPrice = approvedPrice;
        request.AdminNotes = adminNotes;
        await _context.SaveChangesAsync(cancellationToken);
        return new AdminBuybackDto(request.Id, request.RequestCode, ToFrontendBuybackStatus(request.Status), request.ProposedPrice, request.ApprovedPrice, request.CreatedAt);
    }

    public async Task<AdminBuybackDto> RejectBuybackAsync(Guid id, string reason, CancellationToken cancellationToken = default)
    {
        var request = await _context.BuybackRequests.FirstOrDefaultAsync(r => r.Id == id, cancellationToken) ?? throw new KeyNotFoundException("Buyback request not found.");
        request.Status = BuybackRequestStatus.Rejected;
        request.AdminNotes = reason;
        await _context.SaveChangesAsync(cancellationToken);
        return new AdminBuybackDto(request.Id, request.RequestCode, ToFrontendBuybackStatus(request.Status), request.ProposedPrice, request.ApprovedPrice, request.CreatedAt);
    }

    public async Task<AdminAnalyticsDto> GetAnalyticsAsync(DateTime? from, DateTime? to, CancellationToken cancellationToken = default)
    {
        var establishedAt = new DateTime(2026, 1, 1);
        var fromDate = from?.Date ?? establishedAt;
        if (fromDate < establishedAt)
        {
            fromDate = establishedAt;
        }

        var toDate = to?.Date ?? DateTime.UtcNow.Date;
        if (toDate < fromDate)
        {
            toDate = fromDate;
        }

        var toExclusive = toDate.AddDays(1);

        var orders = await _context.Orders
            .AsNoTracking()
            .Where(o => o.OrderDate >= fromDate && o.OrderDate < toExclusive)
            .Select(o => new { o.Id, o.Status, o.TotalAmount, o.OrderDate })
            .ToListAsync(cancellationToken);

        var orderItems = await _context.OrderItems
            .AsNoTracking()
            .Include(oi => oi.Order)
            .Where(oi => oi.Order.OrderDate >= fromDate && oi.Order.OrderDate < toExclusive)
            .Select(oi => new
            {
                oi.ProductName,
                oi.ProductTypeText,
                oi.Quantity,
                oi.Price,
                OrderStatus = oi.Order.Status
            })
            .ToListAsync(cancellationToken);

        var books = await _context.Books
            .AsNoTracking()
            .Include(b => b.Category)
            .Select(b => new
            {
                b.Stock,
                b.IsActive,
                b.ApprovalStatus,
                Category = b.Category.Name
            })
            .ToListAsync(cancellationToken);

        var accessories = await _context.Accessories
            .AsNoTracking()
            .Include(a => a.Type)
            .Select(a => new
            {
                a.Stock,
                a.IsActive,
                a.ApprovalStatus,
                Category = a.Type.Name
            })
            .ToListAsync(cancellationToken);

        var buybacks = await _context.BuybackRequests
            .AsNoTracking()
            .Where(b => b.CreatedAt >= fromDate && b.CreatedAt < toExclusive)
            .Select(b => new { b.Status, b.ProposedPrice, b.ApprovedPrice })
            .ToListAsync(cancellationToken);

        var staffCount = (await _userManager.GetUsersInRoleAsync("Staff")).Count;
        var customerCount = (await _userManager.GetUsersInRoleAsync("Customer")).Count;
        var adminCount = (await _userManager.GetUsersInRoleAsync("Admin")).Count;

        var deliveredOrders = orders.Where(o => o.Status == OrderStatus.Delivered).ToList();
        var pendingOrders = orders.Count(o =>
            o.Status == OrderStatus.Pending ||
            o.Status == OrderStatus.AwaitingPreparation ||
            o.Status == OrderStatus.ReadyForDelivery);
        var totalRevenue = deliveredOrders.Sum(o => o.TotalAmount);
        var totalProducts = books.Count + accessories.Count;
        var activeProducts = books.Count(b => b.ApprovalStatus == "published" && b.IsActive)
            + accessories.Count(a => a.ApprovalStatus == "published" && a.IsActive);
        var hiddenProducts = books.Count(b => b.ApprovalStatus == "published" && !b.IsActive)
            + accessories.Count(a => a.ApprovalStatus == "published" && !a.IsActive);
        var pendingProducts = books.Count(b => b.ApprovalStatus == "pending")
            + accessories.Count(a => a.ApprovalStatus == "pending");
        var outOfStockProducts = books.Count(b => b.Stock <= 0) + accessories.Count(a => a.Stock <= 0);

        var summary = new AdminAnalyticsSummaryDto(
            totalRevenue,
            orders.Count,
            deliveredOrders.Count,
            pendingOrders,
            orders.Count(o => o.Status == OrderStatus.Cancelled),
            orders.Count(o => o.Status == OrderStatus.ReturnRequested),
            deliveredOrders.Count > 0 ? Math.Round(totalRevenue / deliveredOrders.Count, 2) : 0m,
            books.Count,
            accessories.Count,
            activeProducts,
            hiddenProducts,
            pendingProducts,
            outOfStockProducts,
            staffCount,
            customerCount,
            buybacks.Count(b => b.Status == BuybackRequestStatus.Pending));

        var monthStart = new DateTime(fromDate.Year, fromDate.Month, 1);
        var monthEnd = new DateTime(toDate.Year, toDate.Month, 1);
        var monthCount = ((monthEnd.Year - monthStart.Year) * 12) + monthEnd.Month - monthStart.Month + 1;
        var months = Enumerable.Range(0, Math.Max(monthCount, 1))
            .Select(offset => monthStart.AddMonths(offset))
            .ToList();

        var revenueByMonth = months.Select(month =>
        {
            var monthOrders = deliveredOrders.Where(o => o.OrderDate.Year == month.Year && o.OrderDate.Month == month.Month).ToList();
            return new AdminTrendPointDto(
                $"{month:MM/yyyy}",
                month.Year,
                month.Month,
                monthOrders.Sum(o => o.TotalAmount),
                monthOrders.Count);
        }).ToList();

        var ordersByMonth = months.Select(month =>
        {
            var monthOrders = orders.Where(o => o.OrderDate.Year == month.Year && o.OrderDate.Month == month.Month).ToList();
            return new AdminTrendPointDto(
                $"{month:MM/yyyy}",
                month.Year,
                month.Month,
                monthOrders.Sum(o => o.TotalAmount),
                monthOrders.Count);
        }).ToList();

        var orderStatusBreakdown = orders
            .GroupBy(o => o.Status)
            .Select(g => new AdminBreakdownDto(ToOrderStatusLabel(g.Key), g.Count(), g.Sum(o => o.TotalAmount)))
            .OrderByDescending(x => x.Value)
            .ToList();

        var productApprovals = books.Select(b => b.ApprovalStatus)
            .Concat(accessories.Select(a => a.ApprovalStatus))
            .ToList();
        var productApprovalBreakdown = new List<AdminBreakdownDto>
        {
            new("Đã duyệt", productApprovals.Count(x => string.IsNullOrWhiteSpace(x) || x == "published")),
            new("Chờ duyệt", productApprovals.Count(x => x == "pending")),
            new("Từ chối", productApprovals.Count(x => x == "rejected"))
        };

        var productVisibilityBreakdown = new List<AdminBreakdownDto>
        {
            new("Đang hiển thị", activeProducts),
            new("Đang ẩn", hiddenProducts),
            new("Chờ duyệt", pendingProducts),
            new("Hết hàng", outOfStockProducts)
        };

        var inventoryByCategory = books.Select(b => new { b.Category, b.Stock })
            .Concat(accessories.Select(a => new { a.Category, a.Stock }))
            .GroupBy(x => string.IsNullOrWhiteSpace(x.Category) ? "Khác" : x.Category)
            .Select(g => new AdminBreakdownDto(g.Key, g.Sum(x => x.Stock)))
            .OrderByDescending(x => x.Value)
            .Take(8)
            .ToList();

        var topProducts = orderItems
            .Where(oi => oi.OrderStatus == OrderStatus.Delivered)
            .GroupBy(oi => new
            {
                Name = string.IsNullOrWhiteSpace(oi.ProductName) ? "Sản phẩm" : oi.ProductName,
                Type = string.IsNullOrWhiteSpace(oi.ProductTypeText) ? "product" : oi.ProductTypeText
            })
            .Select(g => new AdminTopProductDto(
                g.Key.Name,
                g.Key.Type,
                g.Sum(x => x.Quantity),
                g.Sum(x => x.Price * x.Quantity)))
            .OrderByDescending(x => x.Quantity)
            .ThenByDescending(x => x.Revenue)
            .Take(8)
            .ToList();

        var buybackStatusBreakdown = buybacks
            .GroupBy(b => b.Status)
            .Select(g => new AdminBreakdownDto(ToBuybackStatusLabel(g.Key), g.Count(), g.Sum(x => x.ApprovedPrice ?? x.ProposedPrice ?? 0m)))
            .OrderByDescending(x => x.Value)
            .ToList();

        var accountBreakdown = new List<AdminBreakdownDto>
        {
            new("Admin", adminCount),
            new("Staff", staffCount),
            new("Khách hàng", customerCount)
        };

        return new AdminAnalyticsDto(
            summary,
            revenueByMonth,
            ordersByMonth,
            orderStatusBreakdown,
            productApprovalBreakdown,
            productVisibilityBreakdown,
            inventoryByCategory,
            topProducts,
            buybackStatusBreakdown,
            accountBreakdown);
    }

    public Task<IReadOnlyList<AdminChartPointDto>> GetChartDataAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<AdminChartPointDto>>(Array.Empty<AdminChartPointDto>());

    public Task<byte[]> ExportStatisticsAsync(string format, CancellationToken cancellationToken = default)
        => Task.FromResult(Array.Empty<byte>());

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

    private async Task EnsureAuthorCatalogAsync(string authorName, CancellationToken cancellationToken)
    {
        var exists = await _context.Authors.AnyAsync(a => a.Name == authorName, cancellationToken);
        if (exists)
        {
            return;
        }

        _context.Authors.Add(new Author { Id = Guid.NewGuid(), Name = authorName });
    }

    private static string NormalizeCatalogName(string value, string errorMessage)
    {
        var normalized = value.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new InvalidOperationException(errorMessage);
        }

        return normalized;
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
        if (normalized.Contains("khá") || normalized.Contains("kha") || normalized.Contains("acceptable")) return BookCondition.Acceptable;
        return BookCondition.Good;
    }

    private static string SlugCode(string value)
    {
        var code = new string(value.ToUpperInvariant().Where(char.IsLetterOrDigit).Take(12).ToArray());
        return string.IsNullOrWhiteSpace(code) ? Guid.NewGuid().ToString("N")[..8].ToUpperInvariant() : code;
    }

    private static string ToFrontendOrderStatus(OrderStatus status) => (int)status switch
    {
        0 => "pending",
        1 => "awaitingPreparation",
        2 => "readyForDelivery",
        3 => "readyForDelivery",
        4 => "delivered",
        5 => "cancelled",
        6 => "returnRequested",
        7 => "returned",
        8 => "returnRejected",
        _ => status.ToString().ToLowerInvariant()
    };

    private static bool CanTransitionOrder(OrderStatus current, OrderStatus next)
    {
        if (current == next) return true;

        return (current, next) switch
        {
            (OrderStatus.Pending, OrderStatus.AwaitingPreparation) => true,
            (OrderStatus.Pending, OrderStatus.Cancelled) => true,
            (OrderStatus.AwaitingPreparation, OrderStatus.ReadyForDelivery) => true,
            (OrderStatus.AwaitingPreparation, OrderStatus.Cancelled) => true,
            (OrderStatus.ReadyForDelivery, OrderStatus.Delivered) => true,
            (OrderStatus.Shipping, OrderStatus.Delivered) => true,
            _ => false
        };
    }

    private static string ToFrontendPaymentMethod(PaymentMethod method) => method switch
    {
        PaymentMethod.CashOnDelivery => "Thanh toán khi nhận hàng",
        PaymentMethod.BankTransfer => "Chuyển khoản ngân hàng",
        PaymentMethod.EWallet => "Ví điện tử",
        _ => method.ToString()
    };

    private static string GetAdminProductName(OrderItem oi, Dictionary<Guid, string>? blindBoxTitles)
    {
        var defaultName = oi.ProductName ?? oi.BlindBoxGenre ?? "Sản phẩm";
        if (oi.BlindBoxTier.HasValue && oi.BookId.HasValue && blindBoxTitles != null && blindBoxTitles.TryGetValue(oi.BookId.Value, out var realTitle))
        {
            return $"{defaultName} (Thực tế: {realTitle})";
        }
        return defaultName;
    }

    private static OrderSummaryDto MapOrder(Order order, Dictionary<Guid, string>? blindBoxTitles = null) => new(
        order.Id,
        order.OrderDate,
        order.TotalAmount,
        ToFrontendOrderStatus(order.Status),
        ToFrontendPaymentMethod(order.PaymentMethod),
        order.OrderItems.Select(oi => new OrderItemDto(
            oi.BookId ?? oi.AccessoryId,
            oi.BookId.HasValue ? ProductType.Book : oi.AccessoryId.HasValue ? ProductType.Accessory : null,
            GetAdminProductName(oi, blindBoxTitles),
            oi.Quantity,
            oi.Price,
            oi.BlindBoxTier.HasValue || string.Equals(oi.ProductTypeText, "blindbox", StringComparison.OrdinalIgnoreCase),
            (oi.BookId ?? oi.AccessoryId)?.ToString(),
            oi.ProductTypeText ?? (oi.BlindBoxTier.HasValue ? "blindbox" : oi.BookId.HasValue ? "book" : "accessory"),
            GetAdminProductName(oi, blindBoxTitles),
            oi.Price,
            oi.ProductImage,
            oi.Author,
            oi.Brand,
            oi.Category ?? oi.BlindBoxGenre,
            oi.BlindBoxTier?.ToString())).ToList(),
        order.CustomerId.ToString(),
        order.ReceiverName,
        order.ReceiverEmail,
        order.OrderDate.ToString("O"),
        order.TotalAmount,
        new ShippingAddressDto(order.ReceiverName, order.ReceiverPhone, order.ShippingAddress, string.Empty),
        order.CancellationReason,
        order.CancelledAt,
        order.ReturnReason,
        order.ReturnReasonDetail,
        order.ReturnReviewNote,
        order.ReturnRequestedAt,
        order.ReturnReviewedAt);

    private static string ToFrontendBuybackStatus(BuybackRequestStatus status) => status switch
    {
        BuybackRequestStatus.Pending => "pending",
        BuybackRequestStatus.Approved => "approved",
        BuybackRequestStatus.Rejected => "rejected",
        BuybackRequestStatus.Received => "received",
        _ => status.ToString().ToLowerInvariant()
    };

    private static string ToOrderStatusLabel(OrderStatus status) => ToFrontendOrderStatus(status) switch
    {
        "pending" => "Chờ xác nhận",
        "awaitingPreparation" => "Chờ chuẩn bị",
        "readyForDelivery" => "Chờ giao hàng",
        "delivered" => "Đã giao",
        "cancelled" => "Đã hủy",
        "returnRequested" => "Chờ duyệt trả hàng",
        "returned" => "Đã trả hàng",
        "returnRejected" => "Từ chối trả hàng",
        _ => status.ToString()
    };

    private static string ToBuybackStatusLabel(BuybackRequestStatus status) => status switch
    {
        BuybackRequestStatus.Pending => "Chờ duyệt",
        BuybackRequestStatus.Approved => "Đã duyệt",
        BuybackRequestStatus.Rejected => "Từ chối",
        BuybackRequestStatus.Received => "Đã nhận sách",
        _ => status.ToString()
    };

    private static VoucherDto MapVoucher(Voucher voucher) =>
        new(voucher.Id, voucher.Code, voucher.DiscountAmount, voucher.ExpiryDate, voucher.MinOrderValue, voucher.IsActive);

    private static bool ResolveProductActive(bool? requestedIsActive, string approvalStatus, bool currentIsActive)
    {
        return approvalStatus switch
        {
            "pending" => false,
            "rejected" => false,
            "published" => requestedIsActive ?? currentIsActive,
            _ => requestedIsActive ?? currentIsActive
        };
    }

    private static ProductDetailDto MapBook(Book book) => new(
        book.Id,
        "book",
        book.Title,
        book.Title,
        book.AuthorName,
        book.Price,
        book.Stock,
        book.Stock > 0,
        book.ImageUrl,
        book.ImageUrl,
        book.IsActive,
        book.Description,
        null,
        book.AuthorName,
        book.CategoryId,
        book.Category?.Name,
        null,
        null,
        null,
        null,
        book.Condition.ToString(),
        book.OriginalPrice,
        book.Featured,
        book.Publisher,
        book.Year,
        book.Pages,
        book.Language,
        book.Seller ?? "BookSoul",
        book.SellerNote,
        book.ApprovalStatus,
        book.RejectionNote,
        book.CreatedBy,
        book.CreatedByName,
        book.CreatedByRole,
        book.Code,
        book.CreatedAt,
        book.ImportTicketId);

    private static ProductDetailDto MapAccessory(Accessory accessory) => new(
        accessory.Id,
        "accessory",
        null,
        accessory.Name,
        null,
        accessory.Price,
        accessory.Stock,
        accessory.Stock > 0,
        accessory.ImageUrl,
        accessory.ImageUrl,
        accessory.IsActive,
        accessory.Description,
        null,
        null,
        null,
        null,
        accessory.BrandId,
        accessory.Brand?.Name,
        accessory.TypeId,
        accessory.Type?.Name,
        null,
        accessory.OriginalPrice,
        null,
        null,
        null,
        null,
        null,
        null,
        null,
        accessory.ApprovalStatus,
        accessory.RejectionNote,
        accessory.CreatedBy,
        accessory.CreatedByName,
        accessory.CreatedByRole);
}
