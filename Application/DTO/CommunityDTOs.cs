using Domain.Enums;

namespace Application.DTO;

// ─────────────────────────────────────────────
// Review DTOs
// ─────────────────────────────────────────────

/// <summary>Response DTO đầy đủ cho một review, bao gồm thông tin người dùng và sản phẩm.</summary>
public record ReviewDto(
    Guid Id,
    Guid CustomerId,
    string CustomerName,
    string? CustomerAvatar,
    Guid? BookId,
    Guid? AccessoryId,
    string? ProductName,
    string? ProductImage,
    int Rating,
    string? Comment,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    bool IsHidden = false
);

/// <summary>Request tạo review mới. Phải cung cấp BookId HOẶC AccessoryId (không null cả hai).</summary>
public record CreateReviewRequest(
    Guid? BookId,
    Guid? AccessoryId,
    int Rating,
    string? Comment
);

/// <summary>Trả về kết quả kiểm tra quyền đánh giá sách của user.</summary>
public record ReviewEligibilityDto(
    bool CanReview,
    ReviewDto? ExistingReview
);

/// <summary>Request cập nhật review. Chỉ Rating và Comment được phép thay đổi.</summary>
public record UpdateReviewRequest(
    int Rating,
    string Comment
);

/// <summary>Tóm tắt đánh giá cho một sản phẩm: điểm trung bình, tổng số review và phân phối rating.</summary>
public record AdminReviewStatisticsDto(int TotalReviews, double AverageRating, int[] RatingDistribution);

public record ProductReviewSummaryDto(
    double AverageRating,
    int TotalReviews,
    int[] RatingDistribution  // index 0 = 1 sao, index 4 = 5 sao
);

// ─────────────────────────────────────────────
// Donate DTOs
// ─────────────────────────────────────────────

public record DonateRequestDto(
    Guid Id,
    Guid CustomerId,
    string UserId,
    string BookTitle,
    string Author,
    string Genre,
    BookCondition Condition,
    string ConditionText,
    IReadOnlyList<string> ImageUrls,
    DonateCardTemplate CardTemplate,
    string CardTemplateKey,
    string MessageContent,
    string DonorName,
    string DonorEmail,
    string DonorPhone,
    string DonorAddress,
    bool IsAnonymous,
    DonateRequestStatus Status,
    string StatusKey,
    string? StaffNote,
    DateTime? ReviewedAt,
    DateTime CreatedAt
);

public record CreateDonateRequest(
    string BookTitle,
    string Author,
    string Genre,
    BookCondition Condition,
    IReadOnlyList<string> ImageUrls,
    DonateCardTemplate CardTemplate,
    string MessageContent,
    string DonorName,
    string DonorEmail,
    string DonorPhone,
    string DonorAddress,
    bool IsAnonymous
);

public record ReviewDonateRequest(
    DonateRequestStatus Status,
    string? StaffNote
);

