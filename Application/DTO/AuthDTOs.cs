namespace Application.DTO;

public record RegisterRequest(
    string FullName,
    string Email,
    string Password,
    string UserName,
    string? Address
);

public record LoginRequest(
    string Email,
    string Password
);

public record AuthResponse(
    bool Success,
    string Message,
    string? Token = null,
    string? FullName = null
);
