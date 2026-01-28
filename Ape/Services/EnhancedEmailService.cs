using Ape.Data;
using Ape.Models;
using Microsoft.AspNetCore.Identity.UI.Services;
using System.Net;
using System.Net.Mail;
using System.Text.RegularExpressions;
using Azure;
using Azure.Communication.Email;

namespace Ape.Services;

/// <summary>
/// Enhanced Email Service with Azure Communication Services primary and SMTP fallback.
/// Includes persistent database logging and failure detection.
/// Credentials loaded from encrypted database via SecureConfigurationService.
///
/// Required SystemCredentials keys:
///   Azure: AZURE_COMMUNICATION_CONNECTION_STRING, AZURE_EMAIL_FROM
///   SMTP:  SMTP_SERVER, SMTP_PORT, SMTP_USERNAME, SMTP_PASSWORD, SMTP_SSL
/// </summary>
public class EnhancedEmailService : IEmailSender
{
    private readonly ILogger<EnhancedEmailService> _logger;
    private readonly ApplicationDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly SecureConfigurationService _configService;

    // Lazy-loaded credentials and clients
    private EmailClient? _azureEmailClient;
    private string? _azureSenderAddress;
    private string? _smtpHost;
    private int _smtpPort = 587;
    private string? _smtpUser;
    private string? _smtpPassword;
    private bool _enableSsl = true;
    private bool _credentialsInitialized;
    private readonly object _initLock = new();

    public string LastEmailServer { get; private set; } = "Unknown";

    // Track consecutive Azure failures to avoid retrying when rate-limited
    private int _consecutiveAzureFailures;
    private DateTime _lastAzureFailureTime = DateTime.MinValue;
    private const int MaxConsecutiveFailuresBeforeSkip = 3;
    private static readonly TimeSpan AzureSkipDuration = TimeSpan.FromMinutes(2);

    public EnhancedEmailService(
        ILogger<EnhancedEmailService> logger,
        ApplicationDbContext context,
        IHttpContextAccessor httpContextAccessor,
        SecureConfigurationService configService)
    {
        _logger = logger;
        _context = context;
        _httpContextAccessor = httpContextAccessor;
        _configService = configService;
    }

    private async Task EnsureCredentialsInitializedAsync()
    {
        if (_credentialsInitialized) return;

        lock (_initLock)
        {
            if (_credentialsInitialized) return;
        }

        _logger.LogInformation("Initializing email credentials from secure database storage...");

        var missingCredentials = new List<string>();

        // Initialize Azure Email Client
        try
        {
            var azureConnectionString = await _configService.GetCredentialAsync("AZURE_COMMUNICATION_CONNECTION_STRING");
            _azureSenderAddress = await _configService.GetCredentialAsync("AZURE_EMAIL_FROM");

            if (!string.IsNullOrEmpty(azureConnectionString))
            {
                _azureEmailClient = new EmailClient(azureConnectionString);
                _logger.LogInformation("Azure Email Service initialized successfully");
            }
            else
            {
                _logger.LogWarning("AZURE_COMMUNICATION_CONNECTION_STRING not found - Azure email unavailable, SMTP required");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Azure Email Service");
        }

        // Initialize SMTP settings (required for fallback)
        _smtpHost = await _configService.GetCredentialAsync("SMTP_SERVER");
        _smtpUser = await _configService.GetCredentialAsync("SMTP_USERNAME");
        _smtpPassword = await _configService.GetCredentialAsync("SMTP_PASSWORD");

        string? portString = await _configService.GetCredentialAsync("SMTP_PORT");
        string? sslString = await _configService.GetCredentialAsync("SMTP_SSL");

        if (string.IsNullOrEmpty(_smtpHost)) missingCredentials.Add("SMTP_SERVER");
        if (string.IsNullOrEmpty(_smtpUser)) missingCredentials.Add("SMTP_USERNAME");
        if (string.IsNullOrEmpty(_smtpPassword)) missingCredentials.Add("SMTP_PASSWORD");

        if (string.IsNullOrEmpty(portString) || !int.TryParse(portString, out _smtpPort))
        {
            _smtpPort = 587;
            if (string.IsNullOrEmpty(portString))
                _logger.LogInformation("SMTP_PORT not configured, using default: 587");
        }

        if (string.IsNullOrEmpty(sslString) || !bool.TryParse(sslString, out _enableSsl))
        {
            _enableSsl = true;
            if (string.IsNullOrEmpty(sslString))
                _logger.LogInformation("SMTP_SSL not configured, using default: true");
        }

        if (missingCredentials.Count > 0)
        {
            _logger.LogError("SMTP credentials missing from SystemCredentials table: {Missing}. Add them via /SystemCredentials",
                string.Join(", ", missingCredentials));
        }
        else
        {
            _logger.LogInformation("SMTP service initialized: {Host}:{Port} (SSL: {SSL}, User: {User})",
                _smtpHost, _smtpPort, _enableSsl, _smtpUser);
        }

        if (_azureEmailClient == null && missingCredentials.Count > 0)
        {
            _logger.LogCritical("NO EMAIL SERVICE AVAILABLE - Both Azure and SMTP are unconfigured. Add credentials via /SystemCredentials");
        }

        _credentialsInitialized = true;
    }

    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        await EnsureCredentialsInitializedAsync();
        await SendEmailWithAttachmentsAsync(email, subject, htmlMessage, null);
    }

    public async Task SendEmailViaSmtpOnlyAsync(string email, string subject, string htmlMessage, List<EmailAttachment>? attachments = null)
    {
        await EnsureCredentialsInitializedAsync();

        var logEntry = new EmailLog
        {
            Timestamp = DateTime.UtcNow,
            ToEmail = email,
            FromEmail = _smtpUser,
            Subject = subject,
            SentBy = GetCurrentUser(),
            EmailType = DetermineEmailType(subject),
            RetryCount = 0
        };

        _logger.LogInformation("Sending email via SMTP ONLY (bypassing Azure) to {Email} from {From}", email, _smtpUser);
        await TrySmtpEmail(email, subject, htmlMessage, logEntry, "SMTP-only mode (Azure bypassed)", attachments);

        LastEmailServer = logEntry.EmailServer ?? "Unknown";
        await SaveEmailLog(logEntry);
    }

    public async Task SendEmailWithAttachmentsAsync(string email, string subject, string htmlMessage, List<EmailAttachment>? attachments)
    {
        await EnsureCredentialsInitializedAsync();

        var logEntry = new EmailLog
        {
            Timestamp = DateTime.UtcNow,
            ToEmail = email,
            FromEmail = _azureSenderAddress ?? _smtpUser,
            Subject = subject,
            SentBy = GetCurrentUser(),
            EmailType = DetermineEmailType(subject),
            RetryCount = 0
        };

        // Try Azure first, then SMTP fallback
        bool azureSuccess = await TryAzureEmail(email, subject, htmlMessage, logEntry, attachments);

        if (!azureSuccess)
        {
            _logger.LogWarning("Azure email failed, attempting SMTP fallback...");
            string azureFailureDetails = logEntry.Details ?? "Azure failure details not captured";
            await TrySmtpEmail(email, subject, htmlMessage, logEntry, azureFailureDetails, attachments);
        }

        LastEmailServer = logEntry.EmailServer ?? "Unknown";
        await SaveEmailLog(logEntry);
    }

    public class EmailAttachment
    {
        public required string FileName { get; set; }
        public required byte[] Content { get; set; }
        public required string ContentType { get; set; }
    }

    private async Task<bool> TryAzureEmail(string email, string subject, string htmlMessage, EmailLog logEntry, List<EmailAttachment>? attachments)
    {
        if (_azureEmailClient == null || string.IsNullOrEmpty(_azureSenderAddress))
        {
            logEntry.Details = "Azure Email Service not available";
            return false;
        }

        // Skip Azure if we've had too many consecutive failures recently
        if (_consecutiveAzureFailures >= MaxConsecutiveFailuresBeforeSkip)
        {
            var timeSinceLastFailure = DateTime.UtcNow - _lastAzureFailureTime;
            if (timeSinceLastFailure < AzureSkipDuration)
            {
                _logger.LogWarning("Skipping Azure (consecutive failures: {Failures}, last failure {Minutes:F1} min ago) - using SMTP directly for {Email}",
                    _consecutiveAzureFailures, timeSinceLastFailure.TotalMinutes, email);
                logEntry.Details = $"Skipped Azure due to {_consecutiveAzureFailures} consecutive failures - using SMTP directly";
                return false;
            }
            else
            {
                _logger.LogInformation("Azure cooldown period expired ({Minutes:F1} min) - retrying Azure for {Email}",
                    timeSinceLastFailure.TotalMinutes, email);
                _consecutiveAzureFailures = 0;
            }
        }

        try
        {
            _logger.LogInformation("Attempting Azure email send to {Email}", email);

            var emailContent = new EmailContent(subject)
            {
                Html = htmlMessage,
                PlainText = GetPlainTextFromHtml(htmlMessage)
            };

            var emailMessage = new EmailMessage(
                senderAddress: _azureSenderAddress,
                recipientAddress: email,
                content: emailContent
            );

            if (attachments != null && attachments.Count > 0)
            {
                foreach (var attachment in attachments)
                {
                    var binaryData = new BinaryData(attachment.Content);
                    var azureAttachment = new Azure.Communication.Email.EmailAttachment(attachment.FileName, attachment.ContentType, binaryData);
                    emailMessage.Attachments.Add(azureAttachment);
                }
            }

            using var sendTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            EmailSendOperation emailSendOperation;

            try
            {
                emailSendOperation = await _azureEmailClient.SendAsync(
                    WaitUntil.Started,
                    emailMessage,
                    sendTimeout.Token);
            }
            catch (OperationCanceledException)
            {
                _logger.LogError("Azure SendAsync timed out after 5 seconds for {Email} - triggering SMTP fallback", email);
                _consecutiveAzureFailures++;
                _lastAzureFailureTime = DateTime.UtcNow;
                logEntry.Details = "Azure SendAsync timed out - likely rate limited";
                return false;
            }

            if (!string.IsNullOrEmpty(emailSendOperation.Id))
            {
                _logger.LogInformation("Azure accepted email for {Email}. MessageId: {MessageId}", email, emailSendOperation.Id);
                _consecutiveAzureFailures = 0;

                logEntry.Success = true;
                logEntry.EmailServer = "Azure";
                logEntry.MessageId = emailSendOperation.Id;
                logEntry.Details = $"Azure Communication Services - Accepted. MessageId: {emailSendOperation.Id}";
                return true;
            }
            else
            {
                _logger.LogError("Azure did not accept email for {Email} - no MessageId returned", email);
                _consecutiveAzureFailures++;
                _lastAzureFailureTime = DateTime.UtcNow;

                logEntry.Success = false;
                logEntry.EmailServer = "Azure";
                logEntry.Details = "Azure Communication Services - Rejected (no MessageId)";
                return false;
            }
        }
        catch (RequestFailedException azureEx)
        {
            _logger.LogError(azureEx, "Azure RequestFailedException for {Email}. Status: {Status}, ErrorCode: {ErrorCode}",
                email, azureEx.Status, azureEx.ErrorCode);

            _consecutiveAzureFailures++;
            _lastAzureFailureTime = DateTime.UtcNow;

            logEntry.Details = $"Azure RequestFailed - Status: {azureEx.Status}, Error: {azureEx.ErrorCode}, Message: {azureEx.Message}";
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Azure email exception for {Email}", email);
            _consecutiveAzureFailures++;
            _lastAzureFailureTime = DateTime.UtcNow;

            logEntry.Details = $"Azure Communication Services - Exception: {ex.Message}";
            return false;
        }
    }

    private async Task TrySmtpEmail(string email, string subject, string htmlMessage, EmailLog logEntry, string azureFailureDetails = "", List<EmailAttachment>? attachments = null)
    {
        if (string.IsNullOrEmpty(_smtpHost) || string.IsNullOrEmpty(_smtpUser))
        {
            _logger.LogError("SMTP fallback not available - configuration missing");
            logEntry.Success = false;
            logEntry.EmailServer = "SMTP";
            logEntry.RetryCount = 1;
            logEntry.Details = "SMTP fallback failed - configuration missing";
            return;
        }

        try
        {
            _logger.LogInformation("Attempting SMTP send to {Email} via {Host}:{Port} (SSL: {SSL})",
                email, _smtpHost, _smtpPort, _enableSsl);

            using var client = new SmtpClient(_smtpHost, _smtpPort);
            client.Credentials = new NetworkCredential(_smtpUser, _smtpPassword);
            client.EnableSsl = _enableSsl;
            client.Timeout = 30000;

            var mailMessage = new MailMessage
            {
                From = new MailAddress(_smtpUser!),
                Subject = subject,
                Body = htmlMessage,
                IsBodyHtml = true
            };
            mailMessage.To.Add(email);

            if (attachments != null && attachments.Count > 0)
            {
                foreach (var attachment in attachments)
                {
                    var stream = new MemoryStream(attachment.Content);
                    var smtpAttachment = new Attachment(stream, attachment.FileName, attachment.ContentType);
                    mailMessage.Attachments.Add(smtpAttachment);
                }
            }

            await client.SendMailAsync(mailMessage);

            if (azureFailureDetails.Contains("SMTP-only mode"))
            {
                _logger.LogInformation("SMTP-only email sent successfully to {Email}", email);
                logEntry.Details = "Sent via SMTP (Azure intentionally bypassed)";
                logEntry.RetryCount = 0;
            }
            else
            {
                _logger.LogInformation("SMTP fallback email sent successfully to {Email}", email);
                logEntry.Details = $"SMTP fallback - Success after Azure failure. Azure error: {azureFailureDetails}";
                logEntry.RetryCount = 1;
            }

            logEntry.Success = true;
            logEntry.EmailServer = "SMTP";
        }
        catch (Exception ex)
        {
            string actualError = ex.Message;
            var innerEx = ex.InnerException;
            while (innerEx != null)
            {
                actualError = innerEx.Message;
                innerEx = innerEx.InnerException;
            }

            _logger.LogError(ex, "SMTP failed for {Email}. Server: {Host}:{Port}, SSL: {SSL}, User: {User}, Error: {Error}",
                email, _smtpHost, _smtpPort, _enableSsl, _smtpUser, actualError);

            if (azureFailureDetails.Contains("SMTP-only mode"))
            {
                logEntry.Details = $"SMTP send failed (Azure was bypassed intentionally). Error: {actualError}";
                logEntry.RetryCount = 0;
            }
            else
            {
                logEntry.Details = $"SMTP fallback failed - Exception: {actualError}. Original Azure error: {azureFailureDetails}";
                logEntry.RetryCount = 1;
            }

            logEntry.Success = false;
            logEntry.EmailServer = "SMTP";

            string errorMessage = azureFailureDetails.Contains("SMTP-only mode")
                ? $"SMTP email service failed: {actualError}"
                : $"Both Azure and SMTP email services failed. Last error: {actualError}";
            throw new InvalidOperationException(errorMessage, ex);
        }
    }

    private async Task SaveEmailLog(EmailLog logEntry)
    {
        try
        {
            _context.EmailLogs.Add(logEntry);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save email log to database");
        }
    }

    private string GetCurrentUser()
    {
        try
        {
            return _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "System";
        }
        catch
        {
            return "System";
        }
    }

    private static string DetermineEmailType(string subject)
    {
        var lowerSubject = subject.ToLower();

        if (lowerSubject.Contains("confirm")) return "Confirmation";
        if (lowerSubject.Contains("welcome")) return "Welcome";
        if (lowerSubject.Contains("reset")) return "PasswordReset";
        if (lowerSubject.Contains("registration")) return "Registration";
        if (lowerSubject.Contains("test email")) return "Test";

        return "Other";
    }

    private static string GetPlainTextFromHtml(string html)
    {
        if (string.IsNullOrEmpty(html))
            return string.Empty;

        var text = Regex.Replace(html, @"<script[^>]*>[\s\S]*?</script>", "", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"<style[^>]*>[\s\S]*?</style>", "", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"<br\s*/?>", "\n", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"</p>", "\n", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"<[^>]+>", "");
        text = WebUtility.HtmlDecode(text);
        text = Regex.Replace(text, @"[ \t]+", " ");
        text = Regex.Replace(text, @"\n\s*\n\s*\n", "\n\n");

        return text.Trim();
    }
}
