namespace Application.DTO;

public class RegisterRequest
{
    public string? Name { get; set; }
    public string? FullName { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? UserName { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }

    public string DisplayName => string.IsNullOrWhiteSpace(FullName) ? Name?.Trim() ?? string.Empty : FullName!.Trim();
    public string LoginName => string.IsNullOrWhiteSpace(UserName) ? GetLastNamePart(DisplayName) ?? Email : UserName!.Trim();

    private static string? GetLastNamePart(string fullName)
    {
        var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Length == 0 ? null : parts[^1];
    }
}

public record LoginRequest(
    string Email,
    string Password
);

public record AuthResponse(
    bool Success,
    string Message,
    string? Token = null,
    string? FullName = null,
    UserProfileDto? User = null,
    string? OtpCode = null,
    DateTimeOffset? ExpiresAt = null
);

public record UserProfileDto(
    string Id,
    string Name,
    string Email,
    string? Avatar,
    string Role,
    string? Address,
    string? Phone,
    string? FullName = null,
    string? UserName = null
);

public class UpdateUserProfileRequest
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Avatar { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? FullName { get; set; }
    public string? UserName { get; set; }
}

public record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword
);

public record ForgotPasswordRequest(
    string Email
);

public record ForgotPasswordResponse(
    bool Success,
    string Message,
    string? Email = null,
    string? ResetCode = null,
    DateTimeOffset? ExpiresAt = null
);

public record ResetForgotPasswordRequest(
    string Email,
    string ResetCode,
    string NewPassword
);

public record VerifySignupOtpRequest(
    string Email,
    string OtpCode
);
