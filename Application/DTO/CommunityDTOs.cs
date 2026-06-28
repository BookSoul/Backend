using Domain.Enums;

namespace Application.DTO;

public record ReviewDto(
    Guid Id,
    Guid CustomerId,
    Guid? BookId,
    Guid? AccessoryId,
    int Rating,
    string Comment,
    DateTime CreatedAt
);

public record CreateReviewRequest(
    Guid? BookId,
    Guid? AccessoryId,
    int Rating,
    string Comment
);

public record UpdateReviewRequest(
    int Rating,
    string Comment
);

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
