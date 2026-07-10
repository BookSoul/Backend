using Application.DTO;
using Domain.Enums;

namespace Application.Interface;

public interface INotificationService
{
    Task SendNotificationAsync(Guid userId, string title, string message, NotificationType type, string? link = null);
    Task BroadcastToRoleAsync(string roleName, string title, string message, NotificationType type, string? link = null);
    Task BroadcastToAllCustomersAsync(string title, string message, string? link = null);
    Task<List<NotificationDto>> GetUserNotificationsAsync(Guid userId);
    Task MarkAsReadAsync(Guid notificationId, Guid userId);
    Task MarkAllAsReadAsync(Guid userId);
}
