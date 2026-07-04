using Application.DTO;
using Application.Interface;
using Domain.Entities.Identity;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Application.Features.Auth;

public record RegisterUserCommand(RegisterRequest Request, string BaseUrl) : IRequest<AuthResponse>;

public class RegisterUserHandler : IRequestHandler<RegisterUserCommand, AuthResponse>
{
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
            return new AuthResponse(false, "Email already exists");
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

        // Generate email confirmation token
        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var confirmationLink = $"{command.BaseUrl.TrimEnd('/')}/api/auth/confirm-email?userId={user.Id}&token={Uri.EscapeDataString(token)}";

        // Send email
        var emailBody = $@"
            <h2>Chào mừng bạn đến với BookSoul!</h2>
            <p>Xin chào {user.FullName},</p>
            <p>Cảm ơn bạn đã đăng ký tài khoản. Vui lòng bấm vào đường dẫn bên dưới để xác nhận địa chỉ email của bạn và kích hoạt tài khoản:</p>
            <p><a href='{confirmationLink}'>{confirmationLink}</a></p>
            <p>Trân trọng,<br>BookSoul Team</p>";
            
        try
        {
            await _emailSender.SendAsync(user.Email, "Xác nhận địa chỉ email của bạn", emailBody);
        }
        catch
        {
            // If email fails, we might want to delete the user or just inform them.
            // For now, we just return an error so they know. 
            // In production, might be better to have a retry mechanism.
            await _userManager.DeleteAsync(user);
            return new AuthResponse(false, "Không thể gửi email xác nhận. Vui lòng thử lại sau.");
        }

        return new AuthResponse(
            true,
            "Đăng ký thành công. Vui lòng kiểm tra email của bạn để xác nhận tài khoản trước khi đăng nhập.",
            null,
            user.FullName,
            new UserProfileDto(
                user.Id.ToString(),
                user.UserName ?? user.FullName,
                user.Email ?? request.Email,
                user.AvatarUrl,
                "user",
                user.Address,
                user.PhoneNumber,
                user.FullName,
                user.UserName));
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
}
