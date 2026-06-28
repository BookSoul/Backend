namespace Domain.Entities.System;

public class Banner
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string? LinkUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; }
}
