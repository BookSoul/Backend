using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Application.Interface;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Service;

public class ResendEmailSender : IEmailSender
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ResendEmailSender> _logger;

    public ResendEmailSender(HttpClient httpClient, IConfiguration configuration, ILogger<ResendEmailSender> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendAsync(
        string toEmail,
        string subject,
        string htmlBody,
        string? textBody = null,
        CancellationToken cancellationToken = default)
    {
        var apiKey = _configuration["Email:Resend:ApiKey"]?.Trim();
        var fromAddress = _configuration["Email:Resend:FromAddress"]?.Trim();
        var fromName = _configuration["Email:Resend:FromName"]?.Trim();
        if (string.IsNullOrWhiteSpace(fromName)) fromName = "BookSoul";

        if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(fromAddress))
        {
            _logger.LogWarning("Resend email configuration is incomplete. Password reset email was not sent.");
            throw new InvalidOperationException("Email API Resend chưa được cấu hình đầy đủ.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.resend.com/emails");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString("N"));
        request.Content = JsonContent.Create(new
        {
            from = $"{fromName} <{fromAddress}>",
            to = new[] { toEmail },
            subject,
            html = htmlBody,
            text = textBody ?? string.Empty,
        });

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        _logger.LogWarning(
            "Resend email failed with status {StatusCode}. Response: {ResponseBody}",
            response.StatusCode,
            responseBody);

        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            throw new InvalidOperationException("Resend API key chưa hợp lệ hoặc chưa có quyền gửi email.");
        }

        throw new InvalidOperationException("Không gửi được email qua Resend. Vui lòng kiểm tra API key, sender email hoặc domain gửi mail.");
    }
}
