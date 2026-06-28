namespace Domain.Entities.Books;

public class Author
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Biography { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
