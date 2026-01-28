using Ape.Models;
using Ape.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ape.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ContactFormSettingsController(
        ISystemSettingsService systemSettingsService,
        ILogger<ContactFormSettingsController> logger) : Controller
    {
        private readonly ISystemSettingsService _systemSettingsService = systemSettingsService;
        private readonly ILogger<ContactFormSettingsController> _logger = logger;

        public async Task<IActionResult> Index()
        {
            try
            {
                var contactFormEmails = await _systemSettingsService.GetSettingAsync(SystemSettingKeys.ContactFormEmails, "");
                var allSettings = await _systemSettingsService.GetAllSettingsAsync();

                ViewBag.ContactFormEmails = contactFormEmails;
                ViewBag.AllSettings = allSettings;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading contact form settings");
                TempData["ErrorMessage"] = $"Error loading settings: {ex.Message}";
                ViewBag.ContactFormEmails = "";
                ViewBag.AllSettings = new List<SystemSetting>();
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> UpdateContactEmails(string contactFormEmails)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(contactFormEmails))
                {
                    TempData["ErrorMessage"] = "Contact form emails cannot be empty.";
                    return RedirectToAction(nameof(Index));
                }

                var emailList = contactFormEmails
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(email => email.Trim())
                    .Where(email => !string.IsNullOrWhiteSpace(email))
                    .ToList();

                if (emailList.Count == 0)
                {
                    TempData["ErrorMessage"] = "Please provide at least one valid email address.";
                    return RedirectToAction(nameof(Index));
                }

                var invalidEmails = emailList.Where(email => !IsValidEmail(email)).ToList();
                if (invalidEmails.Count != 0)
                {
                    TempData["ErrorMessage"] = $"Invalid email format(s): {string.Join(", ", invalidEmails)}";
                    return RedirectToAction(nameof(Index));
                }

                var cleanedEmails = string.Join(",", emailList);

                await _systemSettingsService.SetSettingAsync(
                    SystemSettingKeys.ContactFormEmails,
                    cleanedEmails,
                    "Comma-separated list of email addresses to receive contact form submissions",
                    User.Identity?.Name ?? "Admin"
                );

                TempData["StatusMessage"] = $"Contact form email settings updated successfully!\n" +
                                            $"{emailList.Count} email address(es) configured:\n" +
                                            $"{string.Join("\n", emailList.Select(e => $"  {e}"))}";

                _logger.LogInformation("Contact form emails updated by {User}: {Emails}",
                    User.Identity?.Name ?? "Unknown", cleanedEmails);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating contact form emails");
                TempData["ErrorMessage"] = "Error updating contact form email settings. Please try again.";
            }

            return RedirectToAction(nameof(Index));
        }

        private static bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
    }
}
