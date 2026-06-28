using Domain.Entities.Identity;
using Domain.Enums;

namespace Domain.Entities.Donate;

public class DonateRequest
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public string BookTitle { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Genre { get; set; } = string.Empty;
    public BookCondition Condition { get; set; }
    public string ImageUrls { get; set; } = string.Empty;
    public DonateCardTemplate CardTemplate { get; set; }
    public string MessageContent { get; set; } = string.Empty;
    public string DonorName { get; set; } = string.Empty;
    public string DonorEmail { get; set; } = string.Empty;
    public string DonorPhone { get; set; } = string.Empty;
    public string DonorAddress { get; set; } = string.Empty;
    public bool IsAnonymous { get; set; }
    public DonateRequestStatus Status { get; set; } = DonateRequestStatus.Pending;
    public string? StaffNote { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public DateTime CreatedAt { get; set; }

    public User Customer { get; set; } = null!;
}
