namespace Application.DTO;

public record BlindBoxTierDto(int Tier, string Name, decimal Price, string Description);
public record LiveSearchItemDto(Guid Id, string Type, string Name, string? SubTitle);
