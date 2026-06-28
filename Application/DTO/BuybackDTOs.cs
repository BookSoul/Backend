using Domain.Enums;

namespace Application.DTO;

public record BuybackRequestDto(
    Guid Id,
    string TypeKey,
    Guid CustomerId,
    string UserId,
    string? UserName,
    string? UserEmail,
    string? UserPhone,
    string? UserAddress,
    BuybackType Type,
    string StatusKey,
    BuybackRequestStatus Status,
    decimal ProposedPrice,
    decimal? BuybackPrice,
    decimal? FinalPrice,
    string? AdminNote,
    IReadOnlyList<string> ImageUrls,
    DateTime CreatedAt,
    string? BookTitle = null,
    string? Author = null,
    string? Category = null,
    string? Condition = null,
    string? PublishYear = null,
    string? Description = null,
    string? OrderId = null,
    string? BlindBoxTier = null,
    string? BlindBoxCategory = null,
    decimal? OriginalPrice = null,
    string? Reason = null
);

public record CreateBuybackRequest(
    BuybackType Type,
    decimal? ProposedPrice,
    string? BookTitle,
    string? Author,
    string? Category,
    string? Condition,
    string? PublishYear,
    string? Description,
    string? OrderId,
    string? BlindBoxTier,
    string? BlindBoxCategory,
    decimal? BuybackPrice,
    decimal? OriginalPrice,
    string? Reason,
    string? UserName,
    string? UserEmail,
    string? UserPhone,
    string? UserAddress
);

public record ApproveBuybackRequest(
    BuybackRequestStatus Status,
    decimal? FinalPrice,
    string? AdminNote
);
