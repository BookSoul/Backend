namespace Domain.Entities.Accessories;

public class AccessoryType
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public ICollection<Accessory> Accessories { get; set; } = new List<Accessory>();
}
