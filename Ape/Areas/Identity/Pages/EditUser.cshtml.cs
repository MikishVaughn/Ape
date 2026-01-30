using Ape.Data;
using Ape.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Ape.Areas.Identity.Pages
{
    [Authorize(Roles = "Admin,Manager")]
    public class EditUserModel(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager, ApplicationDbContext dbContext) : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager = userManager;
        private readonly RoleManager<IdentityRole> _roleManager = roleManager;
        private readonly ApplicationDbContext _dbContext = dbContext;

        [BindProperty]
        public InputModel Input { get; set; } = new InputModel();

        [BindProperty(SupportsGet = true)]
        public string? ReturnUrl { get; set; }

        [TempData]
        public string? StatusMessage { get; set; }

        [BindProperty]
        public List<RoleViewModel> AllRoles { get; set; } = [];

        public bool IsCurrentUserAdmin { get; set; }
        public bool IsEditedUserAdmin { get; set; }

        public class RoleViewModel
        {
            public required string Value { get; set; }
            public string? Text { get; set; }
            public bool Selected { get; set; }
        }

        public class InputModel
        {
            public string? Id { get; set; }

            [Display(Name = "Username")]
            public string? UserName { get; set; }

            [EmailAddress]
            [Display(Name = "Email")]
            public string? Email { get; set; }

            [Display(Name = "Email Confirmed")]
            public bool EmailConfirmed { get; set; }

            [Phone]
            [Display(Name = "Cell Phone")]
            [RegularExpression(@"^\(?\d{3}\)?[-. ]?\d{3}[-. ]?\d{4}$", ErrorMessage = "Not a valid format; try ### ###-####")]
            public string? PhoneNumber { get; set; }

            [Display(Name = "Cell Confirmed")]
            public bool PhoneNumberConfirmed { get; set; }

            [Phone]
            [Display(Name = "Home Phone")]
            [RegularExpression(@"^\(?\d{3}\)?[-. ]?\d{3}[-. ]?\d{4}$", ErrorMessage = "Not a valid format; try ### ###-####")]
            public string? HomePhoneNumber { get; set; }

            [Display(Name = "First Name")]
            public string? FirstName { get; set; }

            [Display(Name = "Middle Name")]
            public string? MiddleName { get; set; }

            [Display(Name = "Last Name")]
            public string? LastName { get; set; }

            [Display(Name = "Birthday")]
            [DataType(DataType.Date)]
            public DateTime? Birthday { get; set; }

            [Display(Name = "Anniversary")]
            [DataType(DataType.Date)]
            public DateTime? Anniversary { get; set; }

            [Display(Name = "Address Line 1")]
            public string? AddressLine1 { get; set; }

            [Display(Name = "Address Line 2")]
            public string? AddressLine2 { get; set; }

            [Display(Name = "City")]
            public string? City { get; set; }

            [Display(Name = "State")]
            public string? State { get; set; }

            [Display(Name = "Zip Code")]
            public string? ZipCode { get; set; }

            [Display(Name = "Is Two Factor On")]
            public bool TwoFactorEnabled { get; set; }

            [Display(Name = "Is Active")]
            public bool IsActive { get; set; } = true;

            [Display(Name = "Deactivated Date")]
            public DateTime? DeactivatedDate { get; set; }

            [Display(Name = "Deactivated By")]
            public string? DeactivatedBy { get; set; }

            [Display(Name = "Deactivation Reason")]
            public string? DeactivationReason { get; set; }

            [Display(Name = "Is Locked Out")]
            public bool IsLockedOut { get; set; }

            [Display(Name = "Lockout End Date")]
            public DateTimeOffset? LockoutEnd { get; set; }
        }

        private async Task LoadUserAsync(IdentityUser user)
        {
            var userProfile = await _dbContext.UserProfiles.FindAsync(user.Id);
            Input = new InputModel
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                EmailConfirmed = user.EmailConfirmed,
                PhoneNumber = user.PhoneNumber,
                PhoneNumberConfirmed = user.PhoneNumberConfirmed,
                TwoFactorEnabled = user.TwoFactorEnabled,
                HomePhoneNumber = userProfile?.HomePhoneNumber,
                FirstName = userProfile?.FirstName,
                MiddleName = userProfile?.MiddleName,
                LastName = userProfile?.LastName,
                Birthday = userProfile?.Birthday,
                Anniversary = userProfile?.Anniversary,
                AddressLine1 = userProfile?.AddressLine1,
                AddressLine2 = userProfile?.AddressLine2,
                City = userProfile?.City,
                State = userProfile?.State,
                ZipCode = userProfile?.ZipCode,
                IsActive = (user.LockoutEnd == null || user.LockoutEnd <= DateTimeOffset.UtcNow) &&
                          (userProfile?.IsActive ?? true),
                DeactivatedDate = userProfile?.DeactivatedDate,
                DeactivatedBy = userProfile?.DeactivatedBy,
                DeactivationReason = userProfile?.DeactivationReason,
                IsLockedOut = user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow &&
                             user.LockoutEnd < DateTimeOffset.MaxValue,
                LockoutEnd = user.LockoutEnd
            };
            await PopulateRoleViewModelsAsync(user);
        }

        private async Task PopulateRoleViewModelsAsync(IdentityUser user)
        {
            var roles = await _roleManager.Roles.ToListAsync();
            var userRoles = await _userManager.GetRolesAsync(user);
            AllRoles = [.. roles.Select(role => new RoleViewModel
            {
                Value = role.Name ?? string.Empty,
                Text = role.Name ?? string.Empty,
                Selected = userRoles.Contains(role.Name ?? string.Empty)
            }).OrderBy(r => r.Text)];
        }

        public async Task<IActionResult> OnGetAsync(string id, string? returnUrl)
        {
            ReturnUrl = returnUrl;

            if (string.IsNullOrEmpty(id))
            {
                StatusMessage = "Error: User ID is missing.";
                return GetReturnRedirect();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                StatusMessage = $"Error: Unable to load user with ID '{id}'.";
                return GetReturnRedirect();
            }

            await LoadUserAsync(user);
            IsCurrentUserAdmin = User.IsInRole("Admin");
            IsEditedUserAdmin = await _userManager.IsInRoleAsync(user, "Admin");
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var userOnPost = await _userManager.FindByIdAsync(Input.Id ?? string.Empty);
            if (userOnPost != null)
            {
                await PopulateRoleViewModelsAsync(userOnPost);
            }

            if (!ModelState.IsValid)
            {
                StatusMessage = "Error: Please fix the validation errors.";
                return Page();
            }

            var user = await _userManager.FindByIdAsync(Input.Id ?? string.Empty);
            if (user == null)
            {
                StatusMessage = $"Error: Unable to find user with ID '{Input.Id}' to update.";
                return GetReturnRedirect();
            }

            // Managers cannot modify Admin user profiles
            if (!User.IsInRole("Admin") && await _userManager.IsInRoleAsync(user, "Admin"))
            {
                StatusMessage = "Error: Only Admins can modify Admin user profiles.";
                return GetReturnRedirect();
            }

            user.UserName = Input.UserName;
            user.Email = Input.Email;
            user.EmailConfirmed = Input.EmailConfirmed;
            user.PhoneNumber = Input.PhoneNumber;
            user.PhoneNumberConfirmed = Input.PhoneNumberConfirmed;
            user.TwoFactorEnabled = Input.TwoFactorEnabled;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                StatusMessage = "Error: User update failed.";
                return Page();
            }

            var userProfile = await _dbContext.UserProfiles.FindAsync(Input.Id);
            if (userProfile == null)
            {
                userProfile = new UserProfiles { UserId = Input.Id ?? string.Empty, User = user };
                _dbContext.UserProfiles.Add(userProfile);
            }

            userProfile.FirstName = Input.FirstName;
            userProfile.MiddleName = Input.MiddleName;
            userProfile.LastName = Input.LastName;
            userProfile.Birthday = Input.Birthday;
            userProfile.Anniversary = Input.Anniversary;
            userProfile.AddressLine1 = Input.AddressLine1;
            userProfile.AddressLine2 = Input.AddressLine2;
            userProfile.City = Input.City;
            userProfile.State = Input.State;
            userProfile.ZipCode = Input.ZipCode;
            userProfile.HomePhoneNumber = Input.HomePhoneNumber;

            await _dbContext.SaveChangesAsync();
            StatusMessage = "User updated successfully.";

            return GetReturnRedirect();
        }

        public IActionResult OnPostCancel()
        {
            StatusMessage = "User Cancelled.";
            return GetReturnRedirect();
        }

        public async Task<IActionResult> OnPostUnlockAsync()
        {
            var userToUnlock = await _userManager.FindByIdAsync(Input.Id ?? string.Empty);
            if (userToUnlock == null)
            {
                StatusMessage = "Member not found.";
                return GetReturnRedirect();
            }

            if (userToUnlock.LockoutEnd == null || userToUnlock.LockoutEnd <= DateTimeOffset.UtcNow)
            {
                StatusMessage = $"Member '{userToUnlock.UserName}' is not currently locked out.";
                return GetReturnRedirect();
            }

            if (userToUnlock.LockoutEnd == DateTimeOffset.MaxValue)
            {
                StatusMessage = $"Member '{userToUnlock.UserName}' is deactivated. Use Reactivate instead of Unlock.";
                return GetReturnRedirect();
            }

            // Managers cannot unlock Admin users
            if (!User.IsInRole("Admin") && await _userManager.IsInRoleAsync(userToUnlock, "Admin"))
            {
                StatusMessage = "Error: Only Admins can unlock Admin users.";
                return GetReturnRedirect();
            }

            userToUnlock.LockoutEnd = null;
            userToUnlock.AccessFailedCount = 0;
            var result = await _userManager.UpdateAsync(userToUnlock);

            if (!result.Succeeded)
            {
                StatusMessage = "Failed to unlock account.";
                return Page();
            }

            StatusMessage = $"Account '{userToUnlock.UserName}' has been unlocked successfully.";
            return GetReturnRedirect();
        }

        public async Task<IActionResult> OnPostDeleteAsync()
        {
            var userToDelete = await _userManager.FindByIdAsync(Input.Id ?? string.Empty);
            if (userToDelete == null)
            {
                StatusMessage = $"Error: Unable to find user with ID '{Input.Id}'.";
                return GetReturnRedirect();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null && userToDelete.Id == currentUser.Id)
            {
                StatusMessage = "Error: You cannot delete your own account.";
                return GetReturnRedirect();
            }

            // Managers cannot delete Admin users
            if (!User.IsInRole("Admin") && await _userManager.IsInRoleAsync(userToDelete, "Admin"))
            {
                StatusMessage = "Error: Only Admins can delete Admin users.";
                return GetReturnRedirect();
            }

            try
            {
                // Delete UserProfile
                var userProfile = await _dbContext.UserProfiles.FindAsync(userToDelete.Id);
                if (userProfile != null)
                {
                    _dbContext.UserProfiles.Remove(userProfile);
                }

                // Delete AspNetUserRoles
                var userRoles = await _dbContext.UserRoles
                    .Where(ur => ur.UserId == userToDelete.Id)
                    .ToListAsync();
                if (userRoles.Count > 0)
                {
                    _dbContext.UserRoles.RemoveRange(userRoles);
                }

                // Delete AspNetUserClaims
                var userClaims = await _dbContext.UserClaims
                    .Where(uc => uc.UserId == userToDelete.Id)
                    .ToListAsync();
                if (userClaims.Count > 0)
                {
                    _dbContext.UserClaims.RemoveRange(userClaims);
                }

                // Delete AspNetUserLogins
                var userLogins = await _dbContext.UserLogins
                    .Where(ul => ul.UserId == userToDelete.Id)
                    .ToListAsync();
                if (userLogins.Count > 0)
                {
                    _dbContext.UserLogins.RemoveRange(userLogins);
                }

                // Delete AspNetUserTokens
                var userTokens = await _dbContext.UserTokens
                    .Where(ut => ut.UserId == userToDelete.Id)
                    .ToListAsync();
                if (userTokens.Count > 0)
                {
                    _dbContext.UserTokens.RemoveRange(userTokens);
                }

                // Delete from AspNetUsers
                var aspNetUser = await _dbContext.Users.FindAsync(userToDelete.Id);
                if (aspNetUser != null)
                {
                    _dbContext.Users.Remove(aspNetUser);
                }

                await _dbContext.SaveChangesAsync();

                StatusMessage = $"Member '{userToDelete.UserName}' deleted successfully.";
                return GetReturnRedirect();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error deleting user: {ex.Message}";
                return Page();
            }
        }

        public async Task<IActionResult> OnPostDeactivateAsync(string reason)
        {
            if (string.IsNullOrEmpty(reason?.Trim()))
            {
                StatusMessage = "Deactivation reason is required.";
                return Page();
            }

            var userToDeactivate = await _userManager.FindByIdAsync(Input.Id ?? string.Empty);
            if (userToDeactivate == null)
            {
                StatusMessage = "Member not found.";
                return GetReturnRedirect();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null && userToDeactivate.Id == currentUser.Id)
            {
                StatusMessage = "Error: You cannot deactivate your own account.";
                return Page();
            }

            // Managers cannot deactivate Admin users
            if (!User.IsInRole("Admin") && await _userManager.IsInRoleAsync(userToDeactivate, "Admin"))
            {
                StatusMessage = "Error: Only Admins can deactivate Admin users.";
                return GetReturnRedirect();
            }

            userToDeactivate.LockoutEnd = DateTimeOffset.MaxValue;
            var identityResult = await _userManager.UpdateAsync(userToDeactivate);

            if (!identityResult.Succeeded)
            {
                StatusMessage = "Failed to deactivate member in Identity system.";
                return Page();
            }

            var userProfile = await _dbContext.UserProfiles.FirstOrDefaultAsync(up => up.UserId == userToDeactivate.Id);
            if (userProfile != null)
            {
                userProfile.IsActive = false;
                userProfile.DeactivatedDate = DateTime.UtcNow;
                userProfile.DeactivatedBy = User.Identity?.Name ?? "System";
                userProfile.DeactivationReason = reason.Trim();

                try
                {
                    await _dbContext.SaveChangesAsync();
                    StatusMessage = $"Member '{userToDeactivate.UserName}' has been deactivated. Reason: {reason}";
                }
                catch (Exception)
                {
                    StatusMessage = "Failed to update member profile during deactivation.";
                    return Page();
                }
            }
            else
            {
                StatusMessage = $"Member '{userToDeactivate.UserName}' was deactivated in Identity system, but no profile record was found.";
            }

            return GetReturnRedirect();
        }

        public async Task<IActionResult> OnPostReactivateAsync()
        {
            var userToReactivate = await _userManager.FindByIdAsync(Input.Id ?? string.Empty);
            if (userToReactivate == null)
            {
                StatusMessage = "Member not found.";
                return GetReturnRedirect();
            }

            // Managers cannot reactivate Admin users
            if (!User.IsInRole("Admin") && await _userManager.IsInRoleAsync(userToReactivate, "Admin"))
            {
                StatusMessage = "Error: Only Admins can reactivate Admin users.";
                return GetReturnRedirect();
            }

            userToReactivate.LockoutEnd = null;
            var identityResult = await _userManager.UpdateAsync(userToReactivate);

            if (!identityResult.Succeeded)
            {
                StatusMessage = "Failed to reactivate member in Identity system.";
                return Page();
            }

            var userProfile = await _dbContext.UserProfiles.FirstOrDefaultAsync(up => up.UserId == userToReactivate.Id);
            if (userProfile != null)
            {
                userProfile.IsActive = true;
                userProfile.DeactivatedDate = null;
                userProfile.DeactivatedBy = null;
                userProfile.DeactivationReason = null;

                try
                {
                    await _dbContext.SaveChangesAsync();
                    StatusMessage = $"Member '{userToReactivate.UserName}' has been reactivated.";
                }
                catch (Exception)
                {
                    StatusMessage = "Failed to update member profile during reactivation.";
                    return Page();
                }
            }
            else
            {
                StatusMessage = $"Member '{userToReactivate.UserName}' was reactivated in Identity system, but no profile record was found.";
            }

            return GetReturnRedirect();
        }

        private IActionResult GetReturnRedirect()
        {
            if (!string.IsNullOrEmpty(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
            {
                return Redirect(ReturnUrl);
            }
            return RedirectToPage("./Users");
        }
    }
}
