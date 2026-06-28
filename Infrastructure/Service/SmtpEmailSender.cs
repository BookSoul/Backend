using System.Net;
using System.Net.Mail;
using Application.Interface;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Service;

public class SmtpEmailSender : IEmailSender
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IConfiguration configuration, ILogger<SmtpEmailSender> logger)
    {
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
        var host = _configuration["Email:Smtp:Host"]?.Trim();
        var port = int.TryParse(_configuration["Email:Smtp:Port"], out var configuredPort) ? configuredPort : 587;
        var enableSsl = !bool.TryParse(_configuration["Email:Smtp:EnableSsl"], out var configuredSsl) || configuredSsl;
        var userName = _configuration["Email:Smtp:UserName"]?.Trim();
        var password = (_configuration["Email:Smtp:Password"] ?? string.Empty).Replace(" ", string.Empty).Trim();
        var fromAddress = (_configuration["Email:Smtp:FromAddress"] ?? userName)?.Trim();
        var fromName = _configuration["Email:Smtp:FromName"]?.Trim();
        if (string.IsNullOrWhiteSpace(fromName)) fromName = "BookSoul";

        if (string.IsNullOrWhiteSpace(host) ||
            string.IsNullOrWhiteSpace(userName) ||
            string.IsNullOrWhiteSpace(password) ||
            string.IsNullOrWhiteSpace(fromAddress))
        {
            _logger.LogWarning("SMTP configuration is incomplete. Password reset email was not sent.");
            throw new InvalidOperationException("Email hệ thống chưa được cấu hình SMTP đầy đủ.");
        }

        using var message = new MailMessage
        {
            From = new MailAddress(fromAddress, fromName),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };
        message.To.Add(new MailAddress(toEmail));

        if (!string.IsNullOrWhiteSpace(textBody))
        {
            message.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(textBody, null, "text/plain"));
            message.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(htmlBody, null, "text/html"));
        }

        using var client = new SmtpClient(host, port)
        {
            DeliveryMethod = SmtpDeliveryMethod.Network,
            EnableSsl = enableSsl,
            Credentials = new NetworkCredential(userName, password)
        };

        await client.SendMailAsync(message, cancellationToken);
    }
}
