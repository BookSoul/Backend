using Application.DTO;
using Application.Features.Auth;
using Application.Validation;
using FluentAssertions;

namespace ApplicationTest;

public class AuthValidatorTests
{
    [Fact]
    public void RegisterUserCommandValidator_Should_Fail_When_Email_Invalid_And_Password_Too_Short()
    {
        var validator = new RegisterUserCommandValidator();
        var command = new RegisterUserCommand(
            new RegisterRequest(
                FullName: "Test User",
                Email: "invalid-email",
                Password: "123",
                UserName: "tester",
                Address: "HCM"));

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == "Request.Email");
        result.Errors.Should().Contain(x => x.PropertyName == "Request.Password");
    }

    [Fact]
    public void LoginUserQueryValidator_Should_Pass_When_Request_Is_Valid()
    {
        var validator = new LoginUserQueryValidator();
        var query = new LoginUserQuery(
            new LoginRequest(
                Email: "user@example.com",
                Password: "valid-password"));

        var result = validator.Validate(query);

        result.IsValid.Should().BeTrue();
    }
}
