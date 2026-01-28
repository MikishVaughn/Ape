#nullable disable

using Ape.Data;
using Ape.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace Ape.Areas.Identity.Pages.Account
{
    public class ConfirmEmailModel(
                UserManager<IdentityUser> userManager,
                IEmailSender emailSender,
                ApplicationDbContext dbContext,
                RoleManager<IdentityRole> roleManager,
                SecureConfigurationService configService,
                ILogger<ConfirmEmailModel> logger) : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager = userManager;
        private readonly IEmailSender _emailSender = emailSender;
        private readonly ApplicationDbContext _dbContext = dbContext;
        private readonly RoleManager<IdentityRole> _roleManager = roleManager;
        private readonly SecureConfigurationService _configService = configService;
        private readonly ILogger<ConfirmEmailModel> _logger = logger;

        [TempData]
        public string StatusMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(string userId, string code)
        {
            if (userId == null || code == null)
            {
                return RedirectToPage("/Index");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{userId}'.");
            }

            code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
            var result = await _userManager.ConfirmEmailAsync(user, code);
            StatusMessage = result.Succeeded ? "Thank you for confirming your email." : "Error confirming your email.";

            if (result.Succeeded)
            {
                // Automatically assign Member role upon email confirmation
                if (!await _userManager.IsInRoleAsync(user, "Member"))
                {
                    // Ensure the Member role exists
                    if (!await _roleManager.RoleExistsAsync("Member"))
                    {
                        await _roleManager.CreateAsync(new IdentityRole("Member"));
                        _logger.LogInformation("Created 'Member' role during email confirmation for user {UserId}", user.Id);
                    }

                    var roleResult = await _userManager.AddToRoleAsync(user, "Member");
                    if (roleResult.Succeeded)
                    {
                        _logger.LogInformation("User {UserId} automatically assigned to 'Member' role after email confirmation", user.Id);
                    }
                    else
                    {
                        _logger.LogError("Failed to assign 'Member' role to user {UserId} after email confirmation: {Errors}",
                            user.Id, string.Join(", ", roleResult.Errors.Select(e => e.Description)));
                    }
                }

                // Get site name from secure configuration
                string siteName = await _configService.GetCredentialAsync("SITE_NAME") ?? "Ape";

                string emailSubjectUser;
                string emailBodyUser;

                // Check if the user has the "Member" role
                bool isMember = await _userManager.IsInRoleAsync(user, "Member");

                if (isMember)
                {
                    emailSubjectUser = $"Welcome to {siteName} - Your Account is Confirmed";
                    emailBodyUser = $"<!DOCTYPE html>" +
                      "<html lang=\"en\">" +
                      "<head>" +
                      "    <meta charset=\"UTF-8\">" +
                      "    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">" +
                      $"    <title>Welcome to {siteName} </title>" +
                      "</head>" +
                      "<body style=\"font-family: sans-serif; line-height: 1.6; margin: 20px;\">" +
                      "    <p style=\"margin-bottom: 1em;\">Dear Member,</p>" +
                      $"   <p style=\"margin-bottom: 1em;\">Welcome to the {siteName}!</p>" +
                      "    <p style=\"margin-bottom: 1em;\">Thank you for confirming your email address. Your account is now <strong style=\"font-weight: bold;\">Active</strong>.</p>" +
                      $"   <p style=\"margin-bottom: 1em;\">You can log in to the community portal at <a href=\"https://{siteName}.com\" style=\"color: #007bff; text-decoration: none;\">https://{siteName}.com</a>.</p>" +
                      "    <p style=\"margin-bottom: 0;\">Thank you for being a part of our community.</p>" +
                      "    <p style=\"margin-top: 0;\">Sincerely,</p>" +
                      $"   <p style=\"margin-top: 0;\">The {siteName} Team</p>" +
                      "</body>" +
                      "</html>";
                }
                else
                {
                    emailSubjectUser = "Email Address Confirmed - Account Pending Authorization";
                    emailBodyUser = $"<!DOCTYPE html>" +
                        "<html lang=\"en\">" +
                        "<head>" +
                        "    <meta charset=\"UTF-8\">" +
                        "    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">" +
                        $"    <title>Email Address Confirmed - {siteName} </title>" +
                        "</head>" +
                        "<body style=\"font-family: sans-serif; line-height: 1.6; margin: 20px;\">" +
                        "    <p style=\"margin-bottom: 1em;\">Dear Member,</p>" +
                        "    <p style=\"margin-bottom: 1em;\">Thank you for confirming your email address.</p>" +
                        "    <p style=\"margin-bottom: 1em;\">Your account registration is now complete. However, a staff member must authorize your account, and this process could take up to 24 hours.</p>" +
                        "    <p style=\"margin-bottom: 1em;\">Once your account has been authorized, you will receive a separate <strong style=\"font-weight: bold;\">Welcome Email</strong> with login instructions. Please be patient as we are a small team of volunteers.</p>" +
                        "    <p style=\"margin-bottom: 0;\">Thank you for your understanding.</p>" +
                        "    <p style=\"margin-top: 0;\">Sincerely,</p>" +
                        $"    <p style=\"margin-top: 0;\">The {siteName} Team</p>" +
                        "</body>" +
                        "</html>";
                }

                // Send confirmation email to the user
                await _emailSender.SendEmailAsync(
                    user.Email,
                    emailSubjectUser,
                    emailBodyUser
                );

                // Send notification email to admin
                var userProfile = await _dbContext.UserProfiles.FirstOrDefaultAsync(up => up.UserId == user.Id);
                if (userProfile != null)
                {
                    string emailSubjectAdmin = $"{siteName} - Email Confirmation Notification";
                    string emailBodyAdmin;
                    string adminEmail = await _configService.GetCredentialAsync("SMTP_USERNAME");

                    if (string.IsNullOrEmpty(adminEmail))
                    {
                        _logger.LogError("SMTP_USERNAME credential is not configured. Cannot send admin notification.");
                        return Page();
                    }

                    if (isMember)
                    {
                        emailBodyAdmin = $"<!DOCTYPE html>" +
                                         "<html lang=\"en\">" +
                                         "<head>" +
                                         "    <meta charset=\"UTF-8\">" +
                                         "    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">" +
                                         "    <title>Email Confirmation Notification</title>" +
                                         "</head>" +
                                         "<body style=\"font-family: sans-serif; line-height: 1.6; margin: 20px;\">" +
                                         $"    <p style=\"margin-bottom: 1em;\">Dear {siteName} Administrator,</p>" +
                                         "    <p style=\"margin-bottom: 1em;\">This is a notification that a user has confirmed their email address.</p>" +
                                         "    <p style=\"margin-bottom: 1em;\"><strong>Member Account Activated:</strong></p>" +
                                         "    <ul style=\"margin-left: 20px; margin-bottom: 1em;\">" +
                                         $"        <li><strong>Name:</strong> {userProfile.FirstName} {userProfile.MiddleName} {userProfile.LastName}</li>" +
                                         $"        <li><strong>Email:</strong> {user.Email}</li>" +
                                         "    </ul>" +
                                         "    <p style=\"margin-bottom: 1em;\">The user's email address has been verified, and their account is now live with Member access.</p>" +
                                         "    <p style=\"margin-bottom: 0;\">Sincerely,</p>" +
                                         $"    <p style=\"margin-top: 0;\">{siteName} System</p>" +
                                         "</body>" +
                                         "</html>";
                    }
                    else
                    {
                        emailBodyAdmin = $"<!DOCTYPE html>" +
                                         "<html lang=\"en\">" +
                                         "<head>" +
                                         "    <meta charset=\"UTF-8\">" +
                                         "    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">" +
                                         "    <title>Email Confirmation Notification - Action Required</title>" +
                                         "</head>" +
                                         "<body style=\"font-family: sans-serif; line-height: 1.6; margin: 20px;\">" +
                                         $"    <p style=\"margin-bottom: 1em;\">Dear {siteName} Administrator,</p>" +
                                         "    <p style=\"margin-bottom: 1em;\">This is a notification that a user has confirmed their email address.</p>" +
                                         "    <p style=\"margin-bottom: 1em;\"><strong>Account Requires Member Role Assignment:</strong></p>" +
                                         "    <ul style=\"margin-left: 20px; margin-bottom: 1em;\">" +
                                         $"        <li><strong>Name:</strong> {userProfile.FirstName} {userProfile.MiddleName} {userProfile.LastName}</li>" +
                                         $"        <li><strong>Email:</strong> {user.Email}</li>" +
                                         "    </ul>" +
                                         "    <p style=\"margin-bottom: 1em;\">The user with the email address above has confirmed their email. Please review their account and assign the 'Member' role as appropriate.</p>" +
                                         "    <p style=\"margin-bottom: 0;\">Sincerely,</p>" +
                                         $"    <p style=\"margin-top: 0;\">{siteName} System</p>" +
                                         "</body>" +
                                         "</html>";
                    }

                    await _emailSender.SendEmailAsync(
                        adminEmail,
                        emailSubjectAdmin,
                        emailBodyAdmin
                    );
                }
                else
                {
                    // Handle the case where the UserProfile might be missing
                    _logger.LogError("UserProfile not found for user ID: {UserId}", userId);
                }
            }

            return Page();
        }
    }
}