namespace Application.DTO;

public record PickupTaskDto(
    Guid Id,
    string SourceType,
    string Title,
    string ContactName,
    string ContactEmail,
    string ContactPhone,
    string ContactAddress,
    string Status,
    string RequestStatus,
    string? Note,
    decimal? Amount,
    DateTime CreatedAt
);
