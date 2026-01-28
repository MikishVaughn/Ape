using Ape.Data; // Add using for ApplicationDbContext
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Ape.Areas.Identity.Pages.Account
{
    public class LoginModel(
        SignInManager<IdentityUser> signInManager,
        ILogger<LoginModel> logger,
        UserManager<IdentityUser> userManager, // Inject UserManager
        ApplicationDbContext dbContext) : PageModel // Inject ApplicationDbContext
    {
        private readonly SignInManager<IdentityUser> _signInManager = signInManager;
        private readonly ILogger<LoginModel> _logger = logger;
        private readonly UserManager<IdentityUser> _userManager = userManager; // Inject UserManager
        private readonly ApplicationDbContext _dbContext = dbContext; // Inject ApplicationDbContext

        [BindProperty]
        public required InputModel Input { get; set; }

        public required IList<AuthenticationScheme> ExternalLogins { get; set; }

        public required string ReturnUrl { get; set; }

        [TempData]
        public required string ErrorMessage { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            public required string Email { get; set; }

            [Required]
            [DataType(DataType.Password)]
            public required string Password { get; set; }

            [Display(Name = "Remember me?")]
            public bool RememberMe { get; set; }
        }

        public async Task OnGetAsync(string? returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            returnUrl ??= Url.Content("~/");

            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            ExternalLogins = [.. (await _signInManager.GetExternalAuthenticationSchemesAsync())];

            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            returnUrl ??= Url.Content("~/"); // Default to homepage if no returnUrl

            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(Input.Email, Input.Password, Input.RememberMe, lockoutOnFailure: true);
                if (result.Succeeded)
                {
                    // Get the logged-in user
                    var user = await _userManager.FindByEmailAsync(Input.Email);
                    if (user != null)
                    {
                        // Find the corresponding UserProfile
                        var userProfile = await _dbContext.UserProfiles
                            .FindAsync(user.Id); // Assuming UserId is the primary key in UserProfile

                        if (userProfile != null)
                        {
                            // Double-check that the UserProfile is active (defense-in-depth)
                            if (!userProfile.IsActive)
                            {
                                _logger.LogWarning("User {Email} with inactive UserProfile attempted to log in.", Input.Email);
                                await _signInManager.SignOutAsync(); // Sign them out immediately
                                ModelState.AddModelError(string.Empty, "Your account has been deactivated. Please contact the administrator for assistance.");
                                return Page();
                            }

                            // Update the LastLogin field
                            userProfile.LastLogin = DateTime.UtcNow;
                            await _dbContext.SaveChangesAsync();
                        }
                        else
                        {
                            _logger.LogWarning("UserProfile not found for user {Email}", Input.Email);
                            // Consider if you want to create a UserProfile here if it doesn't exist
                        }
                    }

                    _logger.LogInformation("User {Email} logged in successfully.", Input.Email);

                    // Use returnUrl if provided, otherwise default to Info/Index
                    if (Url.IsLocalUrl(returnUrl))
                    {
                        return LocalRedirect(returnUrl);
                    }
                    else
                    {
                        return RedirectToAction("Index", "Info");
                    }
                }
                if (result.RequiresTwoFactor)
                {
                    return RedirectToPage("./LoginWith2fa", new { ReturnUrl = returnUrl, Input.RememberMe });
                }
                if (result.IsLockedOut)
                {
                    // Check if this is a deactivated user (lockout end is far in the future) or temporary lockout
                    var user = await _userManager.FindByEmailAsync(Input.Email);
                    if (user != null && user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow.AddYears(10))
                    {
                        // This is a deactivated user
                        _logger.LogWarning("Deactivated user {Email} attempted to log in.", Input.Email);
                        ModelState.AddModelError(string.Empty, "Your account has been deactivated. Please contact the administrator for assistance.");
                        return Page();
                    }
                    else
                    {
                        // This is a temporarily locked out user
                        _logger.LogWarning("User account {Email} temporarily locked out.", Input.Email);
                        return RedirectToPage("./Lockout");
                    }
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    return Page();
                }
            }

            return Page();
        }
    }
}