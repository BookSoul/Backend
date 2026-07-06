using Application.DTO;
using Application.Features.Auth;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("signup")]
    public async Task<IActionResult> Signup([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var origin = $"{Request.Scheme}://{Request.Host}";
        var result = await _mediator.Send(new RegisterUserCommand(request, origin), cancellationToken);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpPost("signup/verify-otp")]
    public async Task<IActionResult> VerifySignupOtp([FromBody] VerifySignupOtpRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new VerifySignupOtpCommand(request), cancellationToken);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpGet("confirm-email")]
    public async Task<IActionResult> ConfirmEmail([FromQuery] string userId, [FromQuery] string token, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ConfirmEmailCommand(userId, token), cancellationToken);
        
        var backgroundColor = result.Success ? "#f0fdf4" : "#fef2f2";
        var textColor = result.Success ? "#166534" : "#991b1b";
        var borderColor = result.Success ? "#bbf7d0" : "#fecaca";

        var html = $@"
        <!DOCTYPE html>
        <html lang='vi'>
        <head>
            <meta charset='UTF-8'>
            <meta name='viewport' content='width=device-width, initial-scale=1.0'>
            <title>Xác nhận Email</title>
            <style>
                body {{ font-family: system-ui, -apple-system, sans-serif; display: flex; justify-content: center; align-items: center; height: 100vh; margin: 0; background-color: #f9fafb; }}
                .container {{ text-align: center; padding: 2rem; border-radius: 0.5rem; background-color: {backgroundColor}; color: {textColor}; border: 1px solid {borderColor}; max-width: 400px; box-shadow: 0 4px 6px -1px rgb(0 0 0 / 0.1); }}
                h1 {{ margin-top: 0; font-size: 1.5rem; }}
                p {{ margin-bottom: 0; line-height: 1.5; }}
            </style>
        </head>
        <body>
            <div class='container'>
                <h1>{(result.Success ? "Thành công!" : "Lỗi!")}</h1>
                <p>{result.Message}</p>
            </div>
        </body>
        </html>";

        return Content(html, "text/html");
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new LoginUserQuery(request), cancellationToken);
        if (!result.Success)
        {
            return Unauthorized(result);
        }

        return Ok(result);
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ForgotPasswordCommand(request), cancellationToken);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ResetForgotPasswordCommand(request), cancellationToken);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }
}
