using Application.DTO;
using Application.Interface;
using Domain.Entities.Identity;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Application.Features.Auth;

public record LoginUserQuery(LoginRequest Request) : IRequest<AuthResponse>;

public class LoginUserHandler : IRequestHandler<LoginUserQuery, AuthResponse>
{
    private readonly UserManager<User> _userManager;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public LoginUserHandler(UserManager<User> userManager, IJwtTokenGenerator jwtTokenGenerator)
    {
        _userManager = userManager;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<AuthResponse> Handle(LoginUserQuery query, CancellationToken cancellationToken)
    {
        var request = query.Request;

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null || !await _userManager.CheckPasswordAsync(user, request.Password))
        {
            return new AuthResponse(false, "Invalid email or password");
        }

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
            "Login successful",
            token,
            user.FullName,
            new UserProfileDto(
                user.Id.ToString(),
                user.UserName ?? user.FullName,
                user.Email ?? request.Email,
                user.AvatarUrl,
                role,
                user.Address,
                user.PhoneNumber,
                user.FullName,
                user.UserName));
    }
}
