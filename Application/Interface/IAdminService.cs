using Application.DTO;

namespace Application.Interface;

public interface IAdminService
{
    Task<IReadOnlyList<UserAdminDto>> GetStaffUsersAsync(CancellationToken cancellationToken = default);
    Task<UserAdminDto> CreateStaffAsync(CreateStaffRequest request, CancellationToken cancellationToken = default);
    Task<UserAdminDto> UpdateStaffAsync(Guid userId, UpdateAdminUserRequest request, CancellationToken cancellationToken = default);
    Task DeleteStaffAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserAdminDto>> GetCustomerUsersAsync(CancellationToken cancellationToken = default);
    Task<UserAdminDto> CreateCustomerAsync(CreateCustomerRequest request, CancellationToken cancellationToken = default);
    Task<UserAdminDto> UpdateCustomerAsync(Guid userId, UpdateAdminUserRequest request, CancellationToken cancellationToken = default);
    Task DeleteCustomerAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<UserAdminDto> LockUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<UserAdminDto> UnlockUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<UserAdminDto> UpdateUserRolesAsync(Guid userId, IReadOnlyList<string> roles, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default);
    Task<CategoryDto> UpsertCategoryAsync(Guid? id, string name, string? description, CancellationToken cancellationToken = default);
    Task DeleteCategoryAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AuthorDto>> GetAuthorsAsync(CancellationToken cancellationToken = default);
    Task<AuthorDto> UpsertAuthorAsync(Guid? id, string name, string? biography, CancellationToken cancellationToken = default);
    Task DeleteAuthorAsync(Guid id, CancellationToken cancellationToken = default);
    Task<BrandDto> UpsertBrandAsync(Guid? id, string name, string? description, CancellationToken cancellationToken = default);
    Task DeleteBrandAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AccessoryTypeDto> UpsertAccessoryTypeAsync(Guid? id, string name, string? description, CancellationToken cancellationToken = default);

    Task<ShippingFeeDto> GetShippingFeeAsync(CancellationToken cancellationToken = default);
    Task<ShippingFeeDto> UpdateShippingFeeAsync(decimal shippingFee, CancellationToken cancellationToken = default);
    Task<VoucherDto> CreateVoucherAsync(string code, decimal discountAmount, DateTime expiryDate, decimal minOrderValue, CancellationToken cancellationToken = default);
    Task<AdminBannerDto> ManageBannerAsync(Guid? id, string title, string imageUrl, string? linkUrl, bool isActive, int displayOrder, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProductDetailDto>> GetBooksAsync(CancellationToken cancellationToken = default);
    Task<ProductDetailDto> CreateBookAsync(BookUpsertRequest request, CancellationToken cancellationToken = default);
    Task<ProductDetailDto> UpdateBookAsync(Guid id, BookUpsertRequest request, CancellationToken cancellationToken = default);
    Task DeleteBookAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductDetailDto>> GetAccessoriesAsync(CancellationToken cancellationToken = default);
    Task<ProductDetailDto> CreateAccessoryAsync(AccessoryUpsertRequest request, CancellationToken cancellationToken = default);
    Task<ProductDetailDto> UpdateAccessoryAsync(Guid id, AccessoryUpsertRequest request, CancellationToken cancellationToken = default);
    Task DeleteAccessoryAsync(Guid id, CancellationToken cancellationToken = default);

    Task<AdminDashboardSummaryDto> GetDashboardSummaryAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OrderSummaryDto>> GetOrdersAsync(CancellationToken cancellationToken = default);
    Task<OrderSummaryDto> UpdateOrderStatusAsync(Guid orderId, Domain.Enums.OrderStatus status, string? reason = null, CancellationToken cancellationToken = default);
    Task<OrderSummaryDto> ApproveReturnAsync(Guid orderId, string? note, CancellationToken cancellationToken = default);
    Task<OrderSummaryDto> RejectReturnAsync(Guid orderId, string? note, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AdminBuybackDto>> GetBuybacksAsync(CancellationToken cancellationToken = default);
    Task<AdminBuybackDto> ApproveBuybackAsync(Guid id, decimal? approvedPrice, string? adminNotes, CancellationToken cancellationToken = default);
    Task<AdminBuybackDto> RejectBuybackAsync(Guid id, string reason, CancellationToken cancellationToken = default);
    Task<AdminAnalyticsDto> GetAnalyticsAsync(DateTime? from, DateTime? to, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AdminChartPointDto>> GetChartDataAsync(CancellationToken cancellationToken = default);
    Task<byte[]> ExportStatisticsAsync(string format, CancellationToken cancellationToken = default);
}
