namespace Domain.Entities.Books;

public class Category
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    // Navigation properties
    public ICollection<Book> Books { get; set; } = new List<Book>();
}
