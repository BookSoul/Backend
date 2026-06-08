using Domain.Enums;

namespace Application.DTO;

public record BuybackRequestDto(
    Guid Id,
    Guid CustomerId,
    BuybackType Type,
    BuybackRequestStatus Status,
    decimal ProposedPrice,
    decimal? FinalPrice,
    string? AdminNote,
    IReadOnlyList<string> ImageUrls,
    DateTime CreatedAt
);

public record ApproveBuybackRequest(
    BuybackRequestStatus Status,
    decimal? FinalPrice,
    string? AdminNote
);
