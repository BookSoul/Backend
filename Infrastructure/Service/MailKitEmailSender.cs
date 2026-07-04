using System.Net.Security;
using Application.Interface;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace Infrastructure.Service;

public class MailKitEmailSender : IEmailSender
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<MailKitEmailSender> _logger;

    public MailKitEmailSender(IConfiguration configuration, ILogger<MailKitEmailSender> logger)
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
            _logger.LogWarning("MailKit SMTP configuration is incomplete. Password reset email was not sent.");
            throw new InvalidOperationException("Email hệ thống chưa được cấu hình MailKit SMTP đầy đủ.");
        }

        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(fromName, fromAddress));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;

            var builder = new BodyBuilder
            {
                HtmlBody = htmlBody
            };

            if (!string.IsNullOrWhiteSpace(textBody))
            {
                builder.TextBody = textBody;
            }

            message.Body = builder.ToMessageBody();

            using var client = new SmtpClient();
            
            // Connect to SMTP server (port 587 with StartTLS is standard for Gmail)
            await client.ConnectAsync(host, port, SecureSocketOptions.StartTls, cancellationToken);
            
            // Authenticate with App Password
            await client.AuthenticateAsync(userName, password, cancellationToken);
            
            // Send email
            await client.SendAsync(message, cancellationToken);
            
            // Disconnect cleanly
            await client.DisconnectAsync(true, cancellationToken);
            
            _logger.LogInformation("Email sent successfully to {ToEmail} using MailKit.", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {ToEmail} using MailKit.", toEmail);
            throw new InvalidOperationException("Không gửi được email qua MailKit. Vui lòng kiểm tra lại cấu hình SMTP.", ex);
        }
    }
}
