using Domain.Enums;

namespace Application.DTO;

public record ImportTicketDto(
    Guid Id,
    string TicketCode,
    Guid StaffId,
    ImportTicketStatus Status,
    string? Note,
    DateTime CreatedDate,
    DateTime? SubmittedAt,
    int BookCount,
    int AccessoryCount
);

public record CreateImportTicketRequest(string? Note);
