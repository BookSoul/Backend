using Domain.Entities.Identity;
using Domain.Enums;

namespace Domain.Entities.Buyback;

public class BuybackRequest
{
    public Guid Id { get; set; }
    public string RequestCode { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public BuybackType Type { get; set; }
    public Guid? OriginalOrderId { get; set; }
    public BlindBoxTier? BlindBoxTier { get; set; }
    public string? BlindBoxCategory { get; set; }
    public string? BookTitle { get; set; }
    public string? Author { get; set; }
    public string? Category { get; set; }
    public BookCondition? Condition { get; set; }
    public string? ConditionText { get; set; }
    public string? PublishYear { get; set; }
    public string? Description { get; set; }
    public string? ImageUrls { get; set; }
    public string Reason { get; set; } = string.Empty;
    public decimal? ProposedPrice { get; set; }
    public decimal? ApprovedPrice { get; set; }
    public decimal? OriginalPrice { get; set; }
    public string? ContactName { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public string RefundInfo { get; set; } = string.Empty;
    public BuybackRequestStatus Status { get; set; }
    public string? AdminNotes { get; set; }
    public DateTime CreatedAt { get; set; }

    public User Customer { get; set; } = null!;
}
