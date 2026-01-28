using Ape.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Ape.Areas.Identity.Pages.Account.Manage
{
    public class DeleteAccountModel(
        UserManager<IdentityUser> userManager,
        SignInManager<IdentityUser> signInManager,
        ApplicationDbContext dbContext,
        ILogger<DeleteAccountModel> logger) : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager = userManager;
        private readonly SignInManager<IdentityUser> _signInManager = signInManager;
        private readonly ApplicationDbContext _dbContext = dbContext;
        private readonly ILogger<DeleteAccountModel> _logger = logger;

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            [Required(ErrorMessage = "Password is required to confirm account deletion.")]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; } = string.Empty;

            [Display(Name = "I understand this action is permanent")]
            public bool ConfirmDeletion { get; set; }
        }

        public bool RequirePassword { get; set; }

        public bool HasBillingHistory { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            RequirePassword = await _userManager.HasPasswordAsync(user);
            HasBillingHistory = false;

            return Page();
        }

        public async Task<IActionResult> OnPostDeleteAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            RequirePassword = await _userManager.HasPasswordAsync(user);

            if (RequirePassword)
            {
                if (string.IsNullOrEmpty(Input.Password) || !await _userManager.CheckPasswordAsync(user, Input.Password))
                {
                    ModelState.AddModelError(string.Empty, "Incorrect password.");
                    return Page();
                }
            }

            if (!Input.ConfirmDeletion)
            {
                ModelState.AddModelError(string.Empty, "You must confirm that you understand this action is permanent.");
                return Page();
            }

            var userId = user.Id;
            var userName = user.UserName;
            _logger.LogInformation("User {UserId} ({UserName}) is deleting their account.", userId, userName);

            try
            {
                // Delete user profile data
                var userProfile = await _dbContext.UserProfiles.FindAsync(userId);
                if (userProfile != null)
                {
                    _dbContext.UserProfiles.Remove(userProfile);
                    await _dbContext.SaveChangesAsync();
                }

                // Delete the Identity user
                var result = await _userManager.DeleteAsync(user);
                if (!result.Succeeded)
                {
                    ModelState.AddModelError(string.Empty, "An error occurred while deleting your account.");
                    _logger.LogError("Failed to delete user {UserId}: {Errors}", userId,
                        string.Join(", ", result.Errors.Select(e => e.Description)));
                    return Page();
                }

                // Sign out
                await _signInManager.SignOutAsync();
                _logger.LogInformation("User {UserId} successfully deleted their account.", userId);

                TempData["AccountDeleted"] = "Your account has been permanently deleted.";
                return Redirect("~/");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting account for user {UserId}", userId);
                ModelState.AddModelError(string.Empty, "An unexpected error occurred. Please try again.");
                return Page();
            }
        }
    }
}
