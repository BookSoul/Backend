using Application.DTO;

namespace Application.Interface;

public interface IAdminService
{
    Task<UserAdminDto> LockUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<UserAdminDto> UnlockUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<UserAdminDto> UpdateUserRolesAsync(Guid userId, IReadOnlyList<string> roles, CancellationToken cancellationToken = default);

    Task<CategoryDto> UpsertCategoryAsync(Guid? id, string name, string? description, CancellationToken cancellationToken = default);
    Task DeleteCategoryAsync(Guid id, CancellationToken cancellationToken = default);
    Task<BrandDto> UpsertBrandAsync(Guid? id, string name, string? description, CancellationToken cancellationToken = default);
    Task DeleteBrandAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AccessoryTypeDto> UpsertAccessoryTypeAsync(Guid? id, string name, string? description, CancellationToken cancellationToken = default);

    Task<ShippingFeeDto> UpdateShippingFeeAsync(decimal shippingFee, CancellationToken cancellationToken = default);
    Task<VoucherDto> CreateVoucherAsync(string code, decimal discountAmount, DateTime expiryDate, decimal minOrderValue, CancellationToken cancellationToken = default);
    Task<AdminBannerDto> ManageBannerAsync(Guid? id, string title, string imageUrl, string? linkUrl, bool isActive, int displayOrder, CancellationToken cancellationToken = default);

    Task<ProductDetailDto> CreateBookAsync(BookUpsertRequest request, CancellationToken cancellationToken = default);
    Task<ProductDetailDto> UpdateBookAsync(Guid id, BookUpsertRequest request, CancellationToken cancellationToken = default);
    Task DeleteBookAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ProductDetailDto> CreateAccessoryAsync(AccessoryUpsertRequest request, CancellationToken cancellationToken = default);
    Task<ProductDetailDto> UpdateAccessoryAsync(Guid id, AccessoryUpsertRequest request, CancellationToken cancellationToken = default);
    Task DeleteAccessoryAsync(Guid id, CancellationToken cancellationToken = default);

    Task<AdminDashboardSummaryDto> GetDashboardSummaryAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AdminOrderDto>> GetOrdersAsync(CancellationToken cancellationToken = default);
    Task<AdminOrderDto> UpdateOrderStatusAsync(Guid orderId, Domain.Enums.OrderStatus status, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AdminBuybackDto>> GetBuybacksAsync(CancellationToken cancellationToken = default);
    Task<AdminBuybackDto> ApproveBuybackAsync(Guid id, decimal? approvedPrice, string? adminNotes, CancellationToken cancellationToken = default);
    Task<AdminBuybackDto> RejectBuybackAsync(Guid id, string reason, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AdminChartPointDto>> GetChartDataAsync(CancellationToken cancellationToken = default);
    Task<byte[]> ExportStatisticsAsync(string format, CancellationToken cancellationToken = default);
}
