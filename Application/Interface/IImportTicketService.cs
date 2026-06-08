using Application.DTO;

namespace Application.Interface;

public interface IImportTicketService
{
    Task<ImportTicketDto> CreateTicketAsync(Guid staffId, CreateImportTicketRequest request, CancellationToken cancellationToken = default);
    Task<ImportTicketDto> SubmitTicketAsync(Guid ticketId, Guid staffId, CancellationToken cancellationToken = default);
    Task<ImportTicketDto> ApproveTicketAsync(Guid ticketId, CancellationToken cancellationToken = default);
    Task<ImportTicketDto> RejectTicketAsync(Guid ticketId, string? note, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ImportTicketDto>> GetTicketsAsync(Guid? staffId, CancellationToken cancellationToken = default);
}
