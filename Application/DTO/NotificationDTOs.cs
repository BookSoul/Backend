using Domain.Enums;

namespace Application.DTO;

public record NotificationDto(
    Guid Id,
    Guid UserId,
    string Title,
    string Message,
    string? Link,
    NotificationType Type,
    bool IsRead,
    DateTime CreatedAt
);

public record SendNotificationRequest(
    Guid? UserId,
    string Title,
    string Message,
    string? Link
);
