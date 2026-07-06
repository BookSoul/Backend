using System.Net;
using System.Security.Cryptography;
using System.Text;
using Application.DTO;
using Application.Interface;
using Domain.Entities.Identity;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Application.Features.Auth;

public record ForgotPasswordCommand(ForgotPasswordRequest Request) : IRequest<ForgotPasswordResponse>;

public record ResetForgotPasswordCommand(ResetForgotPasswordRequest Request) : IRequest<ForgotPasswordResponse>;

public class ForgotPasswordHandler :
    IRequestHandler<ForgotPasswordCommand, ForgotPasswordResponse>,
    IRequestHandler<ResetForgotPasswordCommand, ForgotPasswordResponse>
{
    private const string LoginProvider = "BookSoul";
    private const string ResetCodeTokenName = "PasswordResetCode";
    private const string ResetExpiresTokenName = "PasswordResetExpiresAt";
    private const string ResetLastSentTokenName = "PasswordResetLastSentAt";
    private const string ResetAttemptsTokenName = "PasswordResetAttempts";
    private static readonly TimeSpan ResetCodeLifetime = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan ResendCooldown = TimeSpan.FromSeconds(60);
    private const int MaxResetAttempts = 5;

    private readonly UserManager<User> _userManager;
    private readonly IEmailSender _emailSender;

    public ForgotPasswordHandler(UserManager<User> userManager, IEmailSender emailSender)
    {
        _userManager = userManager;
        _emailSender = emailSender;
    }

    public async Task<ForgotPasswordResponse> Handle(ForgotPasswordCommand command, CancellationToken cancellationToken)
    {
        var email = command.Request.Email?.Trim();
        if (string.IsNullOrWhiteSpace(email))
        {
            return new ForgotPasswordResponse(false, "Vui lÃ²ng nháº­p email.");
        }

        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
        {
            return new ForgotPasswordResponse(false, "Email nÃ y chÆ°a Ä‘Æ°á»£c Ä‘Äƒng kÃ½ trong há»‡ thá»‘ng.");
        }

        var lastSentText = await _userManager.GetAuthenticationTokenAsync(user, LoginProvider, ResetLastSentTokenName);
        if (DateTimeOffset.TryParse(lastSentText, out var lastSentAt))
        {
            var elapsed = DateTimeOffset.UtcNow - lastSentAt;
            if (elapsed < ResendCooldown)
            {
                var waitSeconds = Math.Ceiling((ResendCooldown - elapsed).TotalSeconds);
                return new ForgotPasswordResponse(false, $"Vui lÃ²ng Ä‘á»£i {waitSeconds:0} giÃ¢y trÆ°á»›c khi yÃªu cáº§u mÃ£ OTP má»›i.");
            }
        }

        var resetCode = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
        var expiresAt = DateTimeOffset.UtcNow.Add(ResetCodeLifetime);
        var codeHash = HashResetCode(user.Id, resetCode);

        await _userManager.SetAuthenticationTokenAsync(user, LoginProvider, ResetCodeTokenName, codeHash);
        await _userManager.SetAuthenticationTokenAsync(user, LoginProvider, ResetExpiresTokenName, expiresAt.ToString("O"));
        await _userManager.SetAuthenticationTokenAsync(user, LoginProvider, ResetLastSentTokenName, DateTimeOffset.UtcNow.ToString("O"));
        await _userManager.SetAuthenticationTokenAsync(user, LoginProvider, ResetAttemptsTokenName, "0");
        try
        {
            await _emailSender.SendAsync(
                user.Email ?? email,
                "MÃ£ OTP Ä‘áº·t láº¡i máº­t kháº©u BookSoul",
                BuildPasswordResetEmail(user.FullName, resetCode, expiresAt),
                $"MÃ£ OTP Ä‘áº·t láº¡i máº­t kháº©u BookSoul cá»§a báº¡n lÃ  {resetCode}. MÃ£ cÃ³ hiá»‡u lá»±c Ä‘áº¿n {expiresAt.LocalDateTime:HH:mm dd/MM/yyyy}.",
                cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            if (!IsDevelopmentEnvironment())
            {
                await ClearResetTokensAsync(user);
                return new ForgotPasswordResponse(false, ex.Message, user.Email);
            }

#if DEBUG
            return new ForgotPasswordResponse(
                true,
                $"Email provider chÆ°a sáºµn sÃ ng ({ex.Message}). MÃ£ OTP dev Ä‘Ã£ Ä‘Æ°á»£c táº¡o Ä‘á»ƒ báº¡n test local.",
                user.Email,
                resetCode,
                expiresAt);
#else
            await ClearResetTokensAsync(user);
            return new ForgotPasswordResponse(false, ex.Message);
#endif
        }
        catch
        {
            await ClearResetTokensAsync(user);
            return new ForgotPasswordResponse(false, "KhÃ´ng gá»­i Ä‘Æ°á»£c mÃ£ OTP qua email. Vui lÃ²ng kiá»ƒm tra cáº¥u hÃ¬nh email há»‡ thá»‘ng.");
        }

        return new ForgotPasswordResponse(
            true,
            "MÃ£ OTP Ä‘Ã£ Ä‘Æ°á»£c gá»­i Ä‘áº¿n email cá»§a báº¡n. Vui lÃ²ng kiá»ƒm tra há»™p thÆ° vÃ  nháº­p mÃ£ trong 15 phÃºt.",
            user.Email,
            null,
            expiresAt);
    }

    public async Task<ForgotPasswordResponse> Handle(ResetForgotPasswordCommand command, CancellationToken cancellationToken)
    {
        var email = command.Request.Email?.Trim();
        var resetCode = command.Request.ResetCode?.Trim();
        var newPassword = command.Request.NewPassword;

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(resetCode))
        {
            return new ForgotPasswordResponse(false, "Vui lÃ²ng nháº­p email vÃ  mÃ£ xÃ¡c nháº­n.");
        }

        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
        {
            return new ForgotPasswordResponse(false, "Máº­t kháº©u má»›i pháº£i cÃ³ Ã­t nháº¥t 6 kÃ½ tá»±.");
        }

        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
        {
            return new ForgotPasswordResponse(false, "Email hoáº·c mÃ£ xÃ¡c nháº­n chÆ°a Ä‘Ãºng.");
        }

        var storedCodeHash = await _userManager.GetAuthenticationTokenAsync(user, LoginProvider, ResetCodeTokenName);
        var expiresAtText = await _userManager.GetAuthenticationTokenAsync(user, LoginProvider, ResetExpiresTokenName);
        var attemptsText = await _userManager.GetAuthenticationTokenAsync(user, LoginProvider, ResetAttemptsTokenName);
        var attempts = int.TryParse(attemptsText, out var parsedAttempts) ? parsedAttempts : 0;

        if (string.IsNullOrWhiteSpace(storedCodeHash) ||
            !DateTimeOffset.TryParse(expiresAtText, out var expiresAt) ||
            expiresAt < DateTimeOffset.UtcNow)
        {
            await ClearResetTokensAsync(user);
            return new ForgotPasswordResponse(false, "MÃ£ xÃ¡c nháº­n Ä‘Ã£ háº¿t háº¡n. Vui lÃ²ng yÃªu cáº§u mÃ£ má»›i.");
        }

        var incomingCodeHash = HashResetCode(user.Id, resetCode);
        if (!FixedTimeEquals(storedCodeHash, incomingCodeHash))
        {
            attempts++;
            if (attempts >= MaxResetAttempts)
            {
                await ClearResetTokensAsync(user);
                return new ForgotPasswordResponse(false, "Báº¡n Ä‘Ã£ nháº­p sai mÃ£ quÃ¡ sá»‘ láº§n cho phÃ©p. Vui lÃ²ng yÃªu cáº§u mÃ£ OTP má»›i.");
            }

            await _userManager.SetAuthenticationTokenAsync(user, LoginProvider, ResetAttemptsTokenName, attempts.ToString());
            var remainingAttempts = MaxResetAttempts - attempts;
            return new ForgotPasswordResponse(false, $"MÃ£ xÃ¡c nháº­n chÆ°a Ä‘Ãºng. Báº¡n cÃ²n {remainingAttempts} láº§n thá»­.");
        }

        var identityToken = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, identityToken, newPassword);
        if (!result.Succeeded)
        {
            return new ForgotPasswordResponse(false, string.Join(", ", result.Errors.Select(error => error.Description)));
        }

        await ClearResetTokensAsync(user);
        return new ForgotPasswordResponse(true, "Máº­t kháº©u Ä‘Ã£ Ä‘Æ°á»£c cáº­p nháº­t. Báº¡n cÃ³ thá»ƒ Ä‘Äƒng nháº­p láº¡i.", user.Email);
    }

    private async Task ClearResetTokensAsync(User user)
    {
        await _userManager.RemoveAuthenticationTokenAsync(user, LoginProvider, ResetCodeTokenName);
        await _userManager.RemoveAuthenticationTokenAsync(user, LoginProvider, ResetExpiresTokenName);
        await _userManager.RemoveAuthenticationTokenAsync(user, LoginProvider, ResetLastSentTokenName);
        await _userManager.RemoveAuthenticationTokenAsync(user, LoginProvider, ResetAttemptsTokenName);
    }

    private static string HashResetCode(Guid userId, string resetCode)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{userId:N}:{resetCode}"));
        return Convert.ToHexString(bytes);
    }

    private static string BuildPasswordResetEmail(string fullName, string resetCode, DateTimeOffset expiresAt)
    {
        var displayName = string.IsNullOrWhiteSpace(fullName) ? "báº¡n" : fullName.Trim();
        return $$"""
            <!doctype html>
            <html lang="vi">
            <body style="margin:0;padding:0;background:#f7f0df;font-family:Georgia,'Times New Roman',serif;color:#2d3727;">
              <div style="max-width:560px;margin:32px auto;padding:28px;background:#fffaf0;border:1px solid #d8c79b;border-radius:8px;">
                <p style="margin:0 0 12px;font-size:14px;color:#6e795c;">BookSoul</p>
                <h1 style="margin:0 0 18px;font-size:24px;color:#2d3727;">MÃ£ OTP Ä‘áº·t láº¡i máº­t kháº©u</h1>
                <p style="font-size:15px;line-height:1.7;">Xin chÃ o {{WebUtility.HtmlEncode(displayName)}},</p>
                <p style="font-size:15px;line-height:1.7;">Báº¡n vá»«a yÃªu cáº§u Ä‘áº·t láº¡i máº­t kháº©u cho tÃ i khoáº£n BookSoul. Nháº­p mÃ£ OTP dÆ°á»›i Ä‘Ã¢y Ä‘á»ƒ tiáº¿p tá»¥c:</p>
                <div style="margin:24px 0;padding:18px;text-align:center;background:#efe4c7;border:1px dashed #b3914b;border-radius:6px;">
                  <strong style="font-size:32px;letter-spacing:10px;color:#2d3727;">{{resetCode}}</strong>
                </div>
                <p style="font-size:14px;line-height:1.7;color:#6e795c;">MÃ£ cÃ³ hiá»‡u lá»±c Ä‘áº¿n {{expiresAt.LocalDateTime:HH:mm dd/MM/yyyy}}. Náº¿u báº¡n khÃ´ng yÃªu cáº§u thao tÃ¡c nÃ y, vui lÃ²ng bá» qua email.</p>
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

