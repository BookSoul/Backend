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
                return new AuthResponse(false, "Email đã tồn tại trong hệ thống.");
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
            return new AuthResponse(false, "Vui lòng nhập email và mã OTP.");
        }

        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
        {
            return new AuthResponse(false, "Email hoặc mã OTP chưa đúng.");
        }

        if (await _userManager.IsEmailConfirmedAsync(user))
        {
            return await BuildLoggedInResponseAsync(user, "Tài khoản đã được xác thực.");
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
            return new AuthResponse(false, "Mã OTP đã hết hạn. Vui lòng yêu cầu gửi mã mới.");
        }

        var incomingCodeHash = HashSignupCode(user.Id, otpCode);
        if (!FixedTimeEquals(storedCodeHash, incomingCodeHash))
        {
            attempts++;
            if (attempts >= MaxSignupAttempts)
            {
                await ClearSignupTokensAsync(user);
                return new AuthResponse(false, "Bạn đã nhập sai mã quá số lần cho phép. Vui lòng yêu cầu mã OTP mới.");
            }

            await _userManager.SetAuthenticationTokenAsync(user, LoginProvider, SignupAttemptsTokenName, attempts.ToString());
            return new AuthResponse(false, $"Mã OTP chưa đúng. Bạn còn {MaxSignupAttempts - attempts} lần thử.");
        }

        user.EmailConfirmed = true;
        await _userManager.UpdateAsync(user);
        await ClearSignupTokensAsync(user);
        return await BuildLoggedInResponseAsync(user, "Xác thực email thành công.");
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
                return new AuthResponse(false, $"Vui lòng đợi {waitSeconds:0} giây trước khi yêu cầu mã OTP mới.");
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
                "Mã OTP xác thực tài khoản BookSoul",
                BuildSignupOtpEmail(user.FullName, otpCode, expiresAt),
                $"Mã OTP xác thực tài khoản BookSoul của bạn là {otpCode}. Mã có hiệu lực đến {expiresAt.LocalDateTime:HH:mm dd/MM/yyyy}.",
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
            return new AuthResponse(true, $"Email provider chưa sẵn sàng ({ex.Message}). Mã OTP dev đã được tạo để bạn test local.", null, user.FullName, null, otpCode, expiresAt);
#else
            await ClearSignupTokensAsync(user);
            return new AuthResponse(false, ex.Message);
#endif
        }
        catch
        {
            await ClearSignupTokensAsync(user);
            return new AuthResponse(false, "Không gửi được mã OTP xác thực. Vui lòng kiểm tra cấu hình email hệ thống.");
        }

        return new AuthResponse(true, "Mã OTP đã được gửi đến email của bạn. Vui lòng nhập mã trong 15 phút.", null, user.FullName, null, null, expiresAt);
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
        var displayName = string.IsNullOrWhiteSpace(fullName) ? "bạn" : fullName.Trim();
        return $$"""
            <!doctype html>
            <html lang="vi">
            <body style="margin:0;padding:0;background:#f7f0df;font-family:Arial,Helvetica,sans-serif;color:#2d3727;">
              <div style="max-width:560px;margin:32px auto;padding:28px;background:#fffaf0;border:1px solid #d8c79b;border-radius:8px;">
                <p style="margin:0 0 12px;font-size:14px;color:#6e795c;">BookSoul</p>
                <h1 style="margin:0 0 18px;font-size:24px;color:#2d3727;">Mã OTP xác thực tài khoản</h1>
                <p style="font-size:15px;line-height:1.7;">Xin chào {{WebUtility.HtmlEncode(displayName)}},</p>
                <p style="font-size:15px;line-height:1.7;">Cảm ơn bạn đã đăng ký BookSoul. Nhập mã OTP dưới đây để kích hoạt tài khoản:</p>
                <div style="margin:24px 0;padding:18px;text-align:center;background:#efe4c7;border:1px dashed #b3914b;border-radius:6px;">
                  <strong style="font-size:32px;letter-spacing:10px;color:#2d3727;">{{otpCode}}</strong>
                </div>
                <p style="font-size:14px;line-height:1.7;color:#6e795c;">Mã có hiệu lực đến {{expiresAt.LocalDateTime:HH:mm dd/MM/yyyy}}.</p>
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

