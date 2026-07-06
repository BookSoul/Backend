using Application.DTO;
using Application.Interface;
using Domain.Entities.Identity;
using MediatR;
using Microsoft.AspNetCore.Identity;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace Application.Features.Auth;

public record RegisterUserCommand(RegisterRequest Request, string BaseUrl) : IRequest<AuthResponse>;
public record VerifySignupOtpCommand(VerifySignupOtpRequest Request) : IRequest<AuthResponse>;

public class RegisterUserHandler :
    IRequestHandler<RegisterUserCommand, AuthResponse>,
    IRequestHandler<VerifySignupOtpCommand, AuthResponse>
{
    private const string LoginProvider = "BookSoul";
    private const string SignupCodeTokenName = "SignupOtpCode";
    private const string SignupExpiresTokenName = "SignupOtpExpiresAt";
    private const string SignupLastSentTokenName = "SignupOtpLastSentAt";
    private const string SignupAttemptsTokenName = "SignupOtpAttempts";
    private static readonly TimeSpan SignupCodeLifetime = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan ResendCooldown = TimeSpan.FromSeconds(60);
    private const int MaxSignupAttempts = 5;

    private readonly UserManager<User> _userManager;
    private readonly RoleManager<Role> _roleManager;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IEmailSender _emailSender;

    public RegisterUserHandler(UserManager<User> userManager, RoleManager<Role> roleManager, IJwtTokenGenerator jwtTokenGenerator, IEmailSender emailSender)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _jwtTokenGenerator = jwtTokenGenerator;
        _emailSender = emailSender;
    }

    public async Task<AuthResponse> Handle(RegisterUserCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;

        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            if (await _userManager.IsEmailConfirmedAsync(existingUser))
            {
                return new AuthResponse(false, "Email Ä‘Ã£ tá»“n táº¡i trong há»‡ thá»‘ng.");
            }

            return await SendSignupOtpAsync(existingUser, cancellationToken);
        }

        var userName = await GetAvailableUserNameAsync(request.LoginName);

        var user = new User
        {
            FullName = request.DisplayName,
            Email = request.Email,
            UserName = userName,
            Address = request.Address,
            PhoneNumber = request.Phone
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            return new AuthResponse(false, string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        // Add default role (Customer)
        if (!await _roleManager.RoleExistsAsync("Customer"))
        {
            await _roleManager.CreateAsync(new Role { Name = "Customer" });
        }
        await _userManager.AddToRoleAsync(user, "Customer");

        return await SendSignupOtpAsync(user, cancellationToken);
    }

    public async Task<AuthResponse> Handle(VerifySignupOtpCommand command, CancellationToken cancellationToken)
    {
        var email = command.Request.Email?.Trim();
        var otpCode = command.Request.OtpCode?.Trim();
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(otpCode))
        {
            return new AuthResponse(false, "Vui lÃ²ng nháº­p email vÃ  mÃ£ OTP.");
        }

        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
        {
            return new AuthResponse(false, "Email hoáº·c mÃ£ OTP chÆ°a Ä‘Ãºng.");
        }

        if (await _userManager.IsEmailConfirmedAsync(user))
        {
            return await BuildLoggedInResponseAsync(user, "TÃ i khoáº£n Ä‘Ã£ Ä‘Æ°á»£c xÃ¡c thá»±c.");
        }

        var storedCodeHash = await _userManager.GetAuthenticationTokenAsync(user, LoginProvider, SignupCodeTokenName);
        var expiresAtText = await _userManager.GetAuthenticationTokenAsync(user, LoginProvider, SignupExpiresTokenName);
        var attemptsText = await _userManager.GetAuthenticationTokenAsync(user, LoginProvider, SignupAttemptsTokenName);
        var attempts = int.TryParse(attemptsText, out var parsedAttempts) ? parsedAttempts : 0;

        if (string.IsNullOrWhiteSpace(storedCodeHash) ||
            !DateTimeOffset.TryParse(expiresAtText, out var expiresAt) ||
            expiresAt < DateTimeOffset.UtcNow)
        {
            await ClearSignupTokensAsync(user);
            return new AuthResponse(false, "MÃ£ OTP Ä‘Ã£ háº¿t háº¡n. Vui lÃ²ng yÃªu cáº§u gá»­i mÃ£ má»›i.");
        }

        var incomingCodeHash = HashSignupCode(user.Id, otpCode);
        if (!FixedTimeEquals(storedCodeHash, incomingCodeHash))
        {
            attempts++;
            if (attempts >= MaxSignupAttempts)
            {
                await ClearSignupTokensAsync(user);
                return new AuthResponse(false, "Báº¡n Ä‘Ã£ nháº­p sai mÃ£ quÃ¡ sá»‘ láº§n cho phÃ©p. Vui lÃ²ng yÃªu cáº§u mÃ£ OTP má»›i.");
            }

            await _userManager.SetAuthenticationTokenAsync(user, LoginProvider, SignupAttemptsTokenName, attempts.ToString());
            return new AuthResponse(false, $"MÃ£ OTP chÆ°a Ä‘Ãºng. Báº¡n cÃ²n {MaxSignupAttempts - attempts} láº§n thá»­.");
        }

        user.EmailConfirmed = true;
        await _userManager.UpdateAsync(user);
        await ClearSignupTokensAsync(user);
        return await BuildLoggedInResponseAsync(user, "XÃ¡c thá»±c email thÃ nh cÃ´ng.");
    }

    private async Task<string> GetAvailableUserNameAsync(string requestedUserName)
    {
        var baseName = string.IsNullOrWhiteSpace(requestedUserName) ? "user" : requestedUserName.Trim();
        var candidate = baseName;
        var suffix = 1;

        while (await _userManager.FindByNameAsync(candidate) is not null)
        {
            candidate = $"{baseName}{suffix++}";
        }

        return candidate;
    }

    private async Task<AuthResponse> SendSignupOtpAsync(User user, CancellationToken cancellationToken)
    {
        var lastSentText = await _userManager.GetAuthenticationTokenAsync(user, LoginProvider, SignupLastSentTokenName);
        if (DateTimeOffset.TryParse(lastSentText, out var lastSentAt))
        {
            var elapsed = DateTimeOffset.UtcNow - lastSentAt;
            if (elapsed < ResendCooldown)
            {
                var waitSeconds = Math.Ceiling((ResendCooldown - elapsed).TotalSeconds);
                return new AuthResponse(false, $"Vui lÃ²ng Ä‘á»£i {waitSeconds:0} giÃ¢y trÆ°á»›c khi yÃªu cáº§u mÃ£ OTP má»›i.");
            }
        }

        var otpCode = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
        var expiresAt = DateTimeOffset.UtcNow.Add(SignupCodeLifetime);
        var codeHash = HashSignupCode(user.Id, otpCode);

        await _userManager.SetAuthenticationTokenAsync(user, LoginProvider, SignupCodeTokenName, codeHash);
        await _userManager.SetAuthenticationTokenAsync(user, LoginProvider, SignupExpiresTokenName, expiresAt.ToString("O"));
        await _userManager.SetAuthenticationTokenAsync(user, LoginProvider, SignupLastSentTokenName, DateTimeOffset.UtcNow.ToString("O"));
        await _userManager.SetAuthenticationTokenAsync(user, LoginProvider, SignupAttemptsTokenName, "0");

        try
        {
            await _emailSender.SendAsync(
                user.Email ?? string.Empty,
                "MÃ£ OTP xÃ¡c thá»±c tÃ i khoáº£n BookSoul",
                BuildSignupOtpEmail(user.FullName, otpCode, expiresAt),
                $"MÃ£ OTP xÃ¡c thá»±c tÃ i khoáº£n BookSoul cá»§a báº¡n lÃ  {otpCode}. MÃ£ cÃ³ hiá»‡u lá»±c Ä‘áº¿n {expiresAt.LocalDateTime:HH:mm dd/MM/yyyy}.",
                cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            if (!IsDevelopmentEnvironment())
            {
                await ClearSignupTokensAsync(user);
                return new AuthResponse(false, ex.Message);
            }

#if DEBUG
            return new AuthResponse(true, $"Email provider chÆ°a sáºµn sÃ ng ({ex.Message}). MÃ£ OTP dev Ä‘Ã£ Ä‘Æ°á»£c táº¡o Ä‘á»ƒ báº¡n test local.", null, user.FullName, null, otpCode, expiresAt);
#else
            await ClearSignupTokensAsync(user);
            return new AuthResponse(false, ex.Message);
#endif
        }
        catch
        {
            await ClearSignupTokensAsync(user);
            return new AuthResponse(false, "KhÃ´ng gá»­i Ä‘Æ°á»£c mÃ£ OTP xÃ¡c thá»±c. Vui lÃ²ng kiá»ƒm tra cáº¥u hÃ¬nh email há»‡ thá»‘ng.");
        }

        return new AuthResponse(true, "MÃ£ OTP Ä‘Ã£ Ä‘Æ°á»£c gá»­i Ä‘áº¿n email cá»§a báº¡n. Vui lÃ²ng nháº­p mÃ£ trong 15 phÃºt.", null, user.FullName, null, null, expiresAt);
    }

    private async Task<AuthResponse> BuildLoggedInResponseAsync(User user, string message)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var token = _jwtTokenGenerator.GenerateToken(user, roles);
        var role = roles.Contains("Admin")
            ? "admin"
            : roles.Contains("Shipper")
                ? "shipper"
                : roles.Contains("Staff")
                    ? "staff"
                    : "user";

        return new AuthResponse(
            true,
            message,
            token,
            user.FullName,
            new UserProfileDto(
                user.Id.ToString(),
                user.UserName ?? user.FullName,
                user.Email ?? string.Empty,
                user.AvatarUrl,
                role,
                user.Address,
                user.PhoneNumber,
                user.FullName,
                user.UserName));
    }

    private async Task ClearSignupTokensAsync(User user)
    {
        await _userManager.RemoveAuthenticationTokenAsync(user, LoginProvider, SignupCodeTokenName);
        await _userManager.RemoveAuthenticationTokenAsync(user, LoginProvider, SignupExpiresTokenName);
        await _userManager.RemoveAuthenticationTokenAsync(user, LoginProvider, SignupLastSentTokenName);
        await _userManager.RemoveAuthenticationTokenAsync(user, LoginProvider, SignupAttemptsTokenName);
    }

    private static string HashSignupCode(Guid userId, string otpCode)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{userId:N}:{otpCode}"));
        return Convert.ToHexString(bytes);
    }

    private static string BuildSignupOtpEmail(string fullName, string otpCode, DateTimeOffset expiresAt)
    {
        var displayName = string.IsNullOrWhiteSpace(fullName) ? "báº¡n" : fullName.Trim();
        return $$"""
            <!doctype html>
            <html lang="vi">
            <body style="margin:0;padding:0;background:#f7f0df;font-family:Georgia,'Times New Roman',serif;color:#2d3727;">
              <div style="max-width:560px;margin:32px auto;padding:28px;background:#fffaf0;border:1px solid #d8c79b;border-radius:8px;">
                <p style="margin:0 0 12px;font-size:14px;color:#6e795c;">BookSoul</p>
                <h1 style="margin:0 0 18px;font-size:24px;color:#2d3727;">MÃ£ OTP xÃ¡c thá»±c tÃ i khoáº£n</h1>
                <p style="font-size:15px;line-height:1.7;">Xin chÃ o {{WebUtility.HtmlEncode(displayName)}},</p>
                <p style="font-size:15px;line-height:1.7;">Cáº£m Æ¡n báº¡n Ä‘Ã£ Ä‘Äƒng kÃ½ BookSoul. Nháº­p mÃ£ OTP dÆ°á»›i Ä‘Ã¢y Ä‘á»ƒ kÃ­ch hoáº¡t tÃ i khoáº£n:</p>
                <div style="margin:24px 0;padding:18px;text-align:center;background:#efe4c7;border:1px dashed #b3914b;border-radius:6px;">
                  <strong style="font-size:32px;letter-spacing:10px;color:#2d3727;">{{otpCode}}</strong>
                </div>
                <p style="font-size:14px;line-height:1.7;color:#6e795c;">MÃ£ cÃ³ hiá»‡u lá»±c Ä‘áº¿n {{expiresAt.LocalDateTime:HH:mm dd/MM/yyyy}}.</p>
                <p style="margin-top:24px;font-size:14px;color:#6e795c;">BookSoul Team</p>
              </div>
            </body>
            </html>
            """;
    }

    private static bool FixedTimeEquals(string left, string right)
    {
        var leftBytes = Encoding.UTF8.GetBytes(left);
        var rightBytes = Encoding.UTF8.GetBytes(right);
        return leftBytes.Length == rightBytes.Length &&
               CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }

    private static bool IsDevelopmentEnvironment()
        => string.Equals(
            Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
            "Development",
            StringComparison.OrdinalIgnoreCase);
}

