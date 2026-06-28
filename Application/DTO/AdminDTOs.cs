namespace Application.DTO;

public record UserAdminDto(
    Guid Id,
    string Email,
    string FullName,
    bool IsLocked,
    IReadOnlyList<string> Roles,
    string? UserName = null,
    string? Phone = null,
    string? Address = null,
    string? Avatar = null);
public record CreateStaffRequest(string FullName, string Email, string Password, string? Role = null, string? UserName = null, string? Phone = null, string? Address = null, string? Avatar = null);
public record CreateCustomerRequest(string FullName, string Email, string Password, string? UserName = null, string? Phone = null, string? Address = null, string? Avatar = null);
public record UpdateAdminUserRequest(string FullName, string Email, string? UserName = null, string? Phone = null, string? Address = null, string? Avatar = null, string? Password = null, string? Role = null);
public record CategoryDto(Guid Id, string Name, string? Description);
public record BrandDto(Guid Id, string Name, string? Description);
public record AccessoryTypeDto(Guid Id, string Name, string? Description);
public record AuthorDto(Guid Id, string Name, string? Biography);
public record ShippingFeeDto(decimal ShippingFee);
public record VoucherDto(Guid Id, string Code, decimal DiscountAmount, DateTime ExpiryDate, decimal MinOrderValue, bool IsActive);
public record AdminBannerDto(Guid Id, string Title, string ImageUrl, string? LinkUrl, bool IsActive, int DisplayOrder);
