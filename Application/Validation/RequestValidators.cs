using Application.Features.Auth;
using FluentValidation;

namespace Application.Validation;

public class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        RuleFor(x => x.Request.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Request.Password).NotEmpty().MinimumLength(6);
        RuleFor(x => x.Request.DisplayName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Request.LoginName).NotEmpty().MaximumLength(50);
    }
}

public class LoginUserQueryValidator : AbstractValidator<LoginUserQuery>
{
    public LoginUserQueryValidator()
    {
        RuleFor(x => x.Request.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Request.Password).NotEmpty();
    }
}
