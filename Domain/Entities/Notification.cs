using Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Domain.Entities;

public class Notification
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    // If null, it could mean it's a global notification.
    // However, to keep "IsRead" simple per user, we will just create a notification record for each user.
    public Guid UserId { get; set; } 
    
    [Required]
    public string Title { get; set; } = null!;
    
    [Required]
    public string Message { get; set; } = null!;
    
    // Optional link for promotions (e.g. facebook, minigame)
    public string? Link { get; set; }
    
    public NotificationType Type { get; set; }
    
    public bool IsRead { get; set; } = false;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
