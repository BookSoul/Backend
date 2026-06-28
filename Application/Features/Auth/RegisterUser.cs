using Application.DTO;
using Application.Interface;
using Domain.Entities.Identity;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Application.Features.Auth;

public record RegisterUserCommand(RegisterRequest Request) : IRequest<AuthResponse>;

public class RegisterUserHandler : IRequestHandler<RegisterUserCommand, AuthResponse>
{
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<Role> _roleManager;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public RegisterUserHandler(UserManager<User> userManager, RoleManager<Role> roleManager, IJwtTokenGenerator jwtTokenGenerator)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _jwtTokenGenerator = jwtTokenGenerator;
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
        var roles = await _userManager.GetRolesAsync(user);
        var token = _jwtTokenGenerator.GenerateToken(user, roles);

        return new AuthResponse(
            true,
            "User registered successfully",
            token,
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
