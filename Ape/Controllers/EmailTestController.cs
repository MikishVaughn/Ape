using Ape.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;

namespace Ape.Controllers
{
    [Authorize(Roles = "Admin")]
    public class EmailTestController(
        IEmailSender emailService,
        SecureConfigurationService configService,
        ILogger<EmailTestController> logger) : Controller
    {
        private readonly IEmailSender _emailService = emailService;
        private readonly SecureConfigurationService _configService = configService;
        private readonly ILogger<EmailTestController> _logger = logger;

        public async Task<IActionResult> Index()
        {
            await LoadCredentialsForView();
            return View();
        }

        private async Task LoadCredentialsForView()
        {
            ViewBag.AzureConnectionString = await _configService.GetCredentialAsync("AZURE_COMMUNICATION_CONNECTION_STRING");
            ViewBag.AzureSenderAddress = await _configService.GetCredentialAsync("AZURE_EMAIL_FROM");
            ViewBag.SmtpServer = await _configService.GetCredentialAsync("SMTP_SERVER");
            ViewBag.SmtpPort = await _configService.GetCredentialAsync("SMTP_PORT") ?? "587";
            ViewBag.SmtpUsername = await _configService.GetCredentialAsync("SMTP_USERNAME");
            ViewBag.SmtpSsl = await _configService.GetCredentialAsync("SMTP_SSL") ?? "true";
        }

        public async Task<IActionResult> ShowVars()
        {
            var connectionString = await _configService.GetCredentialAsync("AZURE_COMMUNICATION_CONNECTION_STRING");
            var azureFrom = await _configService.GetCredentialAsync("AZURE_EMAIL_FROM");
            var smtpServer = await _configService.GetCredentialAsync("SMTP_SERVER");
            var smtpUser = await _configService.GetCredentialAsync("SMTP_USERNAME");
            var smtpPassword = await _configService.GetCredentialAsync("SMTP_PASSWORD");
            var smtpPort = await _configService.GetCredentialAsync("SMTP_PORT") ?? "587";
            var smtpSsl = await _configService.GetCredentialAsync("SMTP_SSL") ?? "true";
            var siteName = await _configService.GetCredentialAsync("SITE_NAME") ?? "Ape";

            var output = $@"EMAIL CREDENTIALS (from database)
================================

AZURE:
  Connection String: {(string.IsNullOrEmpty(connectionString) ? "NOT SET" : $"SET ({connectionString.Length} chars)")}
  Sender Address: {azureFrom ?? "NOT SET"}

SMTP:
  Server: {smtpServer ?? "NOT SET"}
  Username: {smtpUser ?? "NOT SET"}
  Password: {(string.IsNullOrEmpty(smtpPassword) ? "NOT SET" : $"SET ({smtpPassword.Length} chars)")}
  Port: {smtpPort}
  SSL: {smtpSsl}

Site Name: {siteName}

Manage credentials: /SystemCredentials/Index
";
            return Content(output, "text/plain");
        }

        [HttpPost]
        public async Task<IActionResult> TestEmail(string testEmail)
        {
            if (string.IsNullOrEmpty(testEmail))
            {
                ViewBag.Message = "Please provide a test email address.";
                ViewBag.Success = false;
                await LoadCredentialsForView();
                return View("Index");
            }

            try
            {
                string siteName = await _configService.GetCredentialAsync("SITE_NAME") ?? "Ape";

                await _emailService.SendEmailAsync(
                    testEmail,
                    $"Test Email from {siteName}",
                    $"<h2>Test Email from {siteName}</h2>" +
                    "<p>This is a test email (Azure primary, SMTP fallback).</p>" +
                    $"<p>Sent at: {DateTime.Now:yyyy-MM-dd HH:mm:ss}</p>"
                );

                var serverUsed = (_emailService is EnhancedEmailService enhanced) ? enhanced.LastEmailServer : "Unknown";
                ViewBag.Message = $"Test email sent successfully to {testEmail} via {serverUsed}";
                ViewBag.Success = true;
            }
            catch (Exception ex)
            {
                ViewBag.Message = $"Failed to send test email: {ex.Message}";
                ViewBag.Success = false;
                _logger.LogError(ex, "Failed to send test email to {Email}", testEmail);
            }

            await LoadCredentialsForView();
            return View("Index");
        }

        [HttpPost]
        public async Task<IActionResult> TestSmtpEmail(string testEmail)
        {
            if (string.IsNullOrEmpty(testEmail))
            {
                ViewBag.Message = "Please provide a test email address.";
                ViewBag.Success = false;
                await LoadCredentialsForView();
                return View("Index");
            }

            try
            {
                string siteName = await _configService.GetCredentialAsync("SITE_NAME") ?? "Ape";

                // Cast to EnhancedEmailService to access SMTP-only method
                if (_emailService is EnhancedEmailService enhancedService)
                {
                    await enhancedService.SendEmailViaSmtpOnlyAsync(
                        testEmail,
                        $"SMTP Test Email from {siteName}",
                        $"<h2>SMTP Test Email from {siteName}</h2>" +
                        "<p>This email was sent via SMTP only (bypassing Azure).</p>" +
                        $"<p>Sent at: {DateTime.Now:yyyy-MM-dd HH:mm:ss}</p>"
                    );

                    ViewBag.Message = $"SMTP test email sent successfully to {testEmail}";
                    ViewBag.Success = true;
                }
                else
                {
                    ViewBag.Message = "SMTP-only test not available with current email service";
                    ViewBag.Success = false;
                }
            }
            catch (Exception ex)
            {
                ViewBag.Message = $"SMTP test failed: {ex.Message}";
                ViewBag.Success = false;
                _logger.LogError(ex, "Failed to send SMTP test email to {Email}", testEmail);
            }

            await LoadCredentialsForView();
            return View("Index");
        }
    }
}
