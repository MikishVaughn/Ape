using Ape.Models;
using Ape.Services;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace Ape.Controllers
{
    public class InfoController(
        IEmailSender emailSender,
        ILogger<InfoController> logger,
        ISystemSettingsService systemSettingsService,
        SecureConfigurationService configService) : Controller
    {
        private readonly IEmailSender _emailSender = emailSender;
        private readonly ILogger<InfoController> _logger = logger;
        private readonly ISystemSettingsService _systemSettingsService = systemSettingsService;
        private readonly SecureConfigurationService _configService = configService;

        public IActionResult Index()
        {
            ViewBag.Message = "Home";
            return View();
        }

        public IActionResult Contact()
        {
            ViewBag.Message = "Contact Us";
            return View();
        }

        public IActionResult TOS()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult About()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SendEmail(string Name, string Email, string Subject, string Message,
            string Comment, string Website, string Company, string Url, string FormLoadTime, string JsToken)
        {
            // === ANTI-SPAM VALIDATION LAYER 1: Honeypot Fields ===
            if (!string.IsNullOrEmpty(Comment) || !string.IsNullOrEmpty(Website) ||
                !string.IsNullOrEmpty(Company) || !string.IsNullOrEmpty(Url))
            {
                _logger.LogWarning("Spam detected: Honeypot field filled. IP: {IP}", HttpContext.Connection.RemoteIpAddress);
                TempData["SuccessMessage"] = "Your message has been sent successfully!";
                return RedirectToAction("Contact");
            }

            // === ANTI-SPAM VALIDATION LAYER 2: JavaScript Token ===
            if (string.IsNullOrEmpty(JsToken))
            {
                _logger.LogWarning("Spam detected: Missing JS token. IP: {IP}", HttpContext.Connection.RemoteIpAddress);
                TempData["ErrorMessage"] = "Please enable JavaScript to submit this form.";
                return RedirectToAction("Contact");
            }

            // === ANTI-SPAM VALIDATION LAYER 3: Minimum Time Validation ===
            if (!string.IsNullOrEmpty(FormLoadTime) && long.TryParse(FormLoadTime, out long loadTime))
            {
                long currentTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                long elapsedSeconds = (currentTime - loadTime) / 1000;

                if (elapsedSeconds < 3)
                {
                    _logger.LogWarning("Spam detected: Form submitted too quickly ({Seconds}s). IP: {IP}", elapsedSeconds, HttpContext.Connection.RemoteIpAddress);
                    TempData["SuccessMessage"] = "Your message has been sent successfully!";
                    return RedirectToAction("Contact");
                }
            }

            // === LEGITIMATE SUBMISSION ===
            try
            {
                var recipients = await _systemSettingsService.GetEmailListAsync(SystemSettingKeys.ContactFormEmails);

                if (recipients.Count == 0)
                {
                    string fallbackEmail = await _configService.GetCredentialAsync("SMTP_USERNAME") ?? "admin@example.com";
                    recipients = [fallbackEmail];
                    _logger.LogWarning("No contact form emails configured in SystemSettings, using fallback: {Email}", fallbackEmail);
                }

                string htmlBody = "<!DOCTYPE html>" +
                                  "<html lang=\"en\">" +
                                  "<head>" +
                                  "<meta charset=\"utf-8\">" +
                                  "<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">" +
                                  "<title>Contact Form Submission</title>" +
                                  "</head>" +
                                  "<body style=\"font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;\">" +
                                  "<div style=\"background: #f8f9fa; padding: 20px; border-radius: 10px; margin-bottom: 20px;\">" +
                                  "<h2 style=\"color: #2c5aa0; margin: 0 0 20px 0;\">Contact Form Submission</h2>" +
                                  "</div>" +
                                  "<div style=\"background: white; padding: 20px; border: 1px solid #dee2e6; border-radius: 5px;\">" +
                                  $"<p><strong>From:</strong> {System.Net.WebUtility.HtmlEncode(Name)}</p>" +
                                  $"<p><strong>Email:</strong> <a href=\"mailto:{System.Net.WebUtility.HtmlEncode(Email)}\">{System.Net.WebUtility.HtmlEncode(Email)}</a></p>" +
                                  $"<p><strong>Subject:</strong> {System.Net.WebUtility.HtmlEncode(Subject)}</p>" +
                                  "<div style=\"margin-top: 20px;\"><strong>Message:</strong></div>" +
                                  $"<div style=\"background: #f8f9fa; padding: 15px; border-radius: 5px; margin-top: 10px; white-space: pre-wrap;\">{System.Net.WebUtility.HtmlEncode(Message)}</div>" +
                                  "</div>" +
                                  "<div style=\"text-align: center; margin-top: 20px; padding: 10px; font-size: 12px; color: #6c757d;\">" +
                                  "<p>This message was sent via the Ape contact form.</p>" +
                                  $"<p>To reply, respond directly to: <a href=\"mailto:{System.Net.WebUtility.HtmlEncode(Email)}\">{System.Net.WebUtility.HtmlEncode(Email)}</a></p>" +
                                  "</div>" +
                                  "</body>" +
                                  "</html>";

                int successCount = 0;
                foreach (var recipient in recipients)
                {
                    if (!string.IsNullOrWhiteSpace(recipient))
                    {
                        try
                        {
                            await _emailSender.SendEmailAsync(
                                recipient.Trim(),
                                $"Contact Form: {Subject}",
                                htmlBody
                            );
                            successCount++;
                            _logger.LogInformation("Contact form email sent successfully to {Recipient}", recipient);
                        }
                        catch (Exception recipientEx)
                        {
                            _logger.LogError(recipientEx, "Failed to send contact form email to {Recipient}", recipient);
                        }
                    }
                }

                if (successCount > 0)
                {
                    _logger.LogInformation("Contact form submitted by {Name} ({Email})", Name, Email);
                    TempData["SuccessMessage"] = "Your message has been sent successfully! We'll get back to you soon.";
                }
                else
                {
                    TempData["ErrorMessage"] = "There was an error sending your message. Please try again later.";
                }

                return RedirectToAction("Contact");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing contact form submission");
                TempData["ErrorMessage"] = "There was an error sending your message. Please try again later.";
                return RedirectToAction("Contact");
            }
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }

}
