using Domain.Entities.Accessories;
using Domain.Entities.Books;
using Domain.Entities.Identity;
using Domain.Enums;

namespace Domain.Entities.Import;

public class ImportTicket
{
    public Guid Id { get; set; }
    public string TicketCode { get; set; } = string.Empty;
    public Guid StaffId { get; set; }
    public ImportTicketStatus Status { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? SubmittedAt { get; set; }

    public User Staff { get; set; } = null!;
    public ICollection<Book> Books { get; set; } = new List<Book>();
    public ICollection<Accessory> Accessories { get; set; } = new List<Accessory>();
}
