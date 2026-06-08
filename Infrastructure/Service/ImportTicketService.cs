using Application.DTO;
using Application.Interface;
using Domain.Entities.Import;
using Domain.Enums;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Service;

public class ImportTicketService : IImportTicketService
{
    private readonly AppDbContext _context;
    private readonly IUnitOfWork _unitOfWork;

    public ImportTicketService(AppDbContext context, IUnitOfWork unitOfWork)
    {
        _context = context;
        _unitOfWork = unitOfWork;
    }

    public async Task<ImportTicketDto> CreateTicketAsync(Guid staffId, CreateImportTicketRequest request, CancellationToken cancellationToken = default)
    {
        var ticket = new ImportTicket
        {
            Id = Guid.NewGuid(),
            TicketCode = $"IMP-{DateTime.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(1000, 9999)}",
            StaffId = staffId,
            Status = ImportTicketStatus.Pending,
            Note = request.Note,
            CreatedDate = DateTime.UtcNow
        };

        _context.ImportTickets.Add(ticket);
        await _context.SaveChangesAsync(cancellationToken);
        return await MapAsync(ticket.Id, cancellationToken);
    }

    public async Task<ImportTicketDto> SubmitTicketAsync(Guid ticketId, Guid staffId, CancellationToken cancellationToken = default)
    {
        var ticket = await _context.ImportTickets.FirstOrDefaultAsync(t => t.Id == ticketId, cancellationToken)
            ?? throw new KeyNotFoundException("Import ticket not found.");

        if (ticket.StaffId != staffId)
        {
            throw new UnauthorizedAccessException("You can only submit your own import ticket.");
        }

        if (ticket.Status != ImportTicketStatus.Pending || ticket.SubmittedAt.HasValue)
        {
            throw new InvalidOperationException("Ticket cannot be submitted.");
        }

        var hasProducts = await _context.Books.AnyAsync(b => b.ImportTicketId == ticketId, cancellationToken)
            || await _context.Accessories.AnyAsync(a => a.ImportTicketId == ticketId, cancellationToken);

        if (!hasProducts)
        {
            throw new InvalidOperationException("Import ticket must contain at least one product.");
        }

        ticket.SubmittedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        return await MapAsync(ticketId, cancellationToken);
    }

    public async Task<ImportTicketDto> ApproveTicketAsync(Guid ticketId, CancellationToken cancellationToken = default)
    {
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var ticket = await _context.ImportTickets
                .FirstOrDefaultAsync(t => t.Id == ticketId, cancellationToken)
                ?? throw new KeyNotFoundException("Import ticket not found.");

            if (!ticket.SubmittedAt.HasValue || ticket.Status != ImportTicketStatus.Pending)
            {
                throw new InvalidOperationException("Only submitted pending tickets can be approved.");
            }

            var books = await _context.Books.Where(b => b.ImportTicketId == ticketId).ToListAsync(cancellationToken);
            var accessories = await _context.Accessories.Where(a => a.ImportTicketId == ticketId).ToListAsync(cancellationToken);

            if (books.Count == 0 && accessories.Count == 0)
            {
                throw new InvalidOperationException("Ticket has no products to approve.");
            }

            foreach (var book in books)
            {
                if (book.Price <= 0 || book.Stock < 0 || string.IsNullOrWhiteSpace(book.Title))
                {
                    throw new InvalidOperationException($"Book '{book.Title}' is invalid for approval.");
                }

                book.IsActive = true;
            }

            foreach (var accessory in accessories)
            {
                if (accessory.Price <= 0 || accessory.Stock < 0 || string.IsNullOrWhiteSpace(accessory.Name))
                {
                    throw new InvalidOperationException($"Accessory '{accessory.Name}' is invalid for approval.");
                }

                accessory.IsActive = true;
            }

            ticket.Status = ImportTicketStatus.Approved;
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
            return await MapAsync(ticketId, cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    public async Task<ImportTicketDto> RejectTicketAsync(Guid ticketId, string? note, CancellationToken cancellationToken = default)
    {
        var ticket = await _context.ImportTickets.FirstOrDefaultAsync(t => t.Id == ticketId, cancellationToken)
            ?? throw new KeyNotFoundException("Import ticket not found.");

        ticket.Status = ImportTicketStatus.Rejected;
        ticket.Note = string.IsNullOrWhiteSpace(note) ? ticket.Note : note.Trim();
        await _context.SaveChangesAsync(cancellationToken);
        return await MapAsync(ticketId, cancellationToken);
    }

    public async Task<IReadOnlyList<ImportTicketDto>> GetTicketsAsync(Guid? staffId, CancellationToken cancellationToken = default)
    {
        var query = _context.ImportTickets.AsNoTracking().AsQueryable();
        if (staffId.HasValue)
        {
            query = query.Where(t => t.StaffId == staffId.Value);
        }

        var ids = await query.OrderByDescending(t => t.CreatedDate).Select(t => t.Id).ToListAsync(cancellationToken);
        var result = new List<ImportTicketDto>();
        foreach (var id in ids)
        {
            result.Add(await MapAsync(id, cancellationToken));
        }

        return result;
    }

    private async Task<ImportTicketDto> MapAsync(Guid ticketId, CancellationToken cancellationToken)
    {
        var ticket = await _context.ImportTickets.AsNoTracking().FirstAsync(t => t.Id == ticketId, cancellationToken);
        var bookCount = await _context.Books.CountAsync(b => b.ImportTicketId == ticketId, cancellationToken);
        var accessoryCount = await _context.Accessories.CountAsync(a => a.ImportTicketId == ticketId, cancellationToken);

        return new ImportTicketDto(
            ticket.Id,
            ticket.TicketCode,
            ticket.StaffId,
            ticket.Status,
            ticket.Note,
            ticket.CreatedDate,
            ticket.SubmittedAt,
            bookCount,
            accessoryCount);
    }
}
