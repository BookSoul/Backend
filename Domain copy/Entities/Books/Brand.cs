using Domain.Entities.Accessories;

namespace Domain.Entities.Books;

public class Brand
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public ICollection<Accessory> Accessories { get; set; } = new List<Accessory>();
}
