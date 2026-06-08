namespace Application.DTO;

public record BookUpsertRequest(string Title, string AuthorName, Guid CategoryId, decimal Price, int Stock, string? ImageUrl, string? Description, bool IsActive);
public record AccessoryUpsertRequest(string Name, Guid BrandId, Guid TypeId, decimal Price, int Stock, string? ImageUrl, bool IsActive);
