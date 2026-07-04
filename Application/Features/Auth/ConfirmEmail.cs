using System.Net;
using Application.DTO;
using Domain.Entities.Identity;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Application.Features.Auth;

public record ConfirmEmailCommand(string UserId, string Token) : IRequest<AuthResponse>;

public class ConfirmEmailHandler : IRequestHandler<ConfirmEmailCommand, AuthResponse>
{
    private readonly UserManager<User> _userManager;

    public ConfirmEmailHandler(UserManager<User> userManager)
    {
        _userManager = userManager;
    }

    public async Task<AuthResponse> Handle(ConfirmEmailCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.UserId) || string.IsNullOrWhiteSpace(command.Token))
        {
            return new AuthResponse(false, "Mã xác nhận hoặc thông tin người dùng không hợp lệ.");
        }

        var user = await _userManager.FindByIdAsync(command.UserId);
        if (user == null)
        {
            return new AuthResponse(false, "Không tìm thấy người dùng.");
        }

        if (await _userManager.IsEmailConfirmedAsync(user))
        {
            return new AuthResponse(true, "Tài khoản của bạn đã được xác nhận trước đó. Vui lòng đăng nhập.");
        }

        var result = await _userManager.ConfirmEmailAsync(user, command.Token);
        if (result.Succeeded)
        {
            return new AuthResponse(true, "Xác nhận email thành công. Bạn có thể đăng nhập ngay bây giờ.");
        }

        var errors = string.Join(", ", result.Errors.Select(e => e.Description));
        return new AuthResponse(false, $"Xác nhận email thất bại: {errors}");
    }
}
