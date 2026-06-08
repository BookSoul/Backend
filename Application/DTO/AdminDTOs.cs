namespace Application.DTO;

public record UserAdminDto(Guid Id, string Email, string FullName, bool IsLocked, IReadOnlyList<string> Roles);
public record CategoryDto(Guid Id, string Name, string? Description);
public record BrandDto(Guid Id, string Name, string? Description);
public record AccessoryTypeDto(Guid Id, string Name, string? Description);
public record AuthorDto(Guid Id, string Name, string? Biography);
public record ShippingFeeDto(decimal ShippingFee);
public record VoucherDto(Guid Id, string Code, decimal DiscountAmount, DateTime ExpiryDate, decimal MinOrderValue, bool IsActive);
public record AdminBannerDto(Guid Id, string Title, string ImageUrl, string? LinkUrl, bool IsActive, int DisplayOrder);
