using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace Ape.Services;

/// <summary>
/// SMTP email sender that retrieves credentials from SecureConfigurationService.
/// Credential keys expected in SystemCredentials table:
///   SMTP_SERVER, SMTP_PORT, SMTP_USERNAME, SMTP_PASSWORD, SMTP_SSL
/// </summary>
public class EmailService : IEmailSender
{
    private readonly SecureConfigurationService _configService;
    private readonly ILogger<EmailService> _logger;

    private string? _smtpHost;
    private int _smtpPort = 587;
    private string? _smtpUser;
    private string? _smtpPassword;
    private bool _enableSsl = true;
    private bool _credentialsInitialized;

    public EmailService(SecureConfigurationService configService, ILogger<EmailService> logger)
    {
        _configService = configService;
        _logger = logger;
    }

    private async Task EnsureCredentialsInitializedAsync()
    {
        if (_credentialsInitialized) return;

        _smtpHost = await _configService.GetCredentialAsync("SMTP_SERVER");
        _smtpUser = await _configService.GetCredentialAsync("SMTP_USERNAME");
        _smtpPassword = await _configService.GetCredentialAsync("SMTP_PASSWORD");

        var portString = await _configService.GetCredentialAsync("SMTP_PORT");
        if (int.TryParse(portString, out var port))
            _smtpPort = port;

        var sslString = await _configService.GetCredentialAsync("SMTP_SSL");
        if (bool.TryParse(sslString, out var ssl))
            _enableSsl = ssl;

        _credentialsInitialized = true;

        if (string.IsNullOrEmpty(_smtpHost))
            _logger.LogWarning("SMTP_SERVER credential is not configured");
        if (string.IsNullOrEmpty(_smtpUser))
            _logger.LogWarning("SMTP_USERNAME credential is not configured");
        if (string.IsNullOrEmpty(_smtpPassword))
            _logger.LogWarning("SMTP_PASSWORD credential is not configured");
    }

    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        await EnsureCredentialsInitializedAsync();

        if (string.IsNullOrEmpty(_smtpHost) || string.IsNullOrEmpty(_smtpUser) || string.IsNullOrEmpty(_smtpPassword))
        {
            _logger.LogError("SMTP credentials are not fully configured. Cannot send email to {Email}", email);
            throw new InvalidOperationException(
                "SMTP credentials not configured. Ensure SMTP_SERVER, SMTP_USERNAME, and SMTP_PASSWORD are set in System Credentials.");
        }

        using var message = new MailMessage();
        message.From = new MailAddress(_smtpUser);
        message.To.Add(new MailAddress(email));
        message.Subject = subject;
        message.Body = htmlMessage;
        message.IsBodyHtml = true;

        using var client = new SmtpClient(_smtpHost, _smtpPort);
        client.Credentials = new NetworkCredential(_smtpUser, _smtpPassword);
        client.EnableSsl = _enableSsl;

        try
        {
            await client.SendMailAsync(message);
            _logger.LogInformation("Email sent successfully to {Email} via SMTP - Subject: {Subject}", email, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email} via SMTP ({Host}:{Port}) - Subject: {Subject}",
                email, _smtpHost, _smtpPort, subject);
            throw;
        }
    }
}
