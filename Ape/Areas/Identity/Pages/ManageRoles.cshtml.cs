using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Ape.Areas.Identity.Pages
{
    [Authorize(Roles = "Admin,Manager")]
    public class ManageRolesModel(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager) : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager = userManager;
        private readonly RoleManager<IdentityRole> _roleManager = roleManager;

        [BindProperty(SupportsGet = true)]
        public string? UserId { get; set; }

        public IdentityUser? UserToEdit { get; set; }

        [BindProperty]
        public List<RoleViewModel> AllRoles { get; set; } = [];

        [BindProperty(SupportsGet = true)]
        public string? ReturnUrl { get; set; }

        [TempData]
        public string? StatusMessage { get; set; }

        public class RoleViewModel
        {
            public required string Value { get; set; }
            public string? Text { get; set; }
            public bool Selected { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(string? userId, string? returnUrl)
        {
            if (string.IsNullOrEmpty(userId))
            {
                StatusMessage = "Error: User ID is missing for role management.";
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }
                return RedirectToPage("./Users");
            }

            UserId = userId;
            ReturnUrl = returnUrl;

            UserToEdit = await _userManager.FindByIdAsync(UserId);
            if (UserToEdit == null)
            {
                StatusMessage = $"Error: Unable to load user with ID '{UserId}' for role management.";
                if (!string.IsNullOrEmpty(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
                {
                    return Redirect(ReturnUrl);
                }
                return RedirectToPage("./Users");
            }

            await PopulateRoleViewModelsAsync(UserToEdit);
            return Page();
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

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrEmpty(UserId))
            {
                StatusMessage = "Error: User ID is missing on post.";
                if (!string.IsNullOrEmpty(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
                {
                    return Redirect(ReturnUrl);
                }
                return RedirectToPage("./Users");
            }

            UserToEdit = await _userManager.FindByIdAsync(UserId);
            if (UserToEdit == null)
            {
                StatusMessage = $"Error: Unable to find user with ID '{UserId}' to update roles.";
                if (!string.IsNullOrEmpty(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
                {
                    return Redirect(ReturnUrl);
                }
                return RedirectToPage("./Users");
            }

            if (!ModelState.IsValid)
            {
                await PopulateRoleViewModelsAsync(UserToEdit);
                StatusMessage = "Error: Please fix validation errors.";
                return Page();
            }

            var originalRoles = await _userManager.GetRolesAsync(UserToEdit);
            var selectedRoles = AllRoles.Where(r => r.Selected).Select(r => r.Value ?? string.Empty).ToList();

            var rolesToRemove = originalRoles.Except(selectedRoles).ToList();
            var rolesToAdd = selectedRoles.Except(originalRoles).ToList();

            if (rolesToRemove.Count > 0)
            {
                var removeResult = await _userManager.RemoveFromRolesAsync(UserToEdit, rolesToRemove);
                if (!removeResult.Succeeded)
                {
                    foreach (var error in removeResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    await PopulateRoleViewModelsAsync(UserToEdit);
                    StatusMessage = "Error: Failed to remove roles.";
                    return Page();
                }
            }

            if (rolesToAdd.Count > 0)
            {
                var addResult = await _userManager.AddToRolesAsync(UserToEdit, rolesToAdd);
                if (!addResult.Succeeded)
                {
                    foreach (var error in addResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    await PopulateRoleViewModelsAsync(UserToEdit);
                    StatusMessage = "Error: Failed to add roles.";
                    return Page();
                }
            }

            StatusMessage = $"Roles updated successfully for user '{UserToEdit.UserName}'.";

            if (!string.IsNullOrEmpty(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
            {
                return Redirect(ReturnUrl);
            }
            return RedirectToPage("./EditUser", new { id = UserId });
        }

        public IActionResult OnPostCancel()
        {
            StatusMessage = "Role changes cancelled.";

            if (!string.IsNullOrEmpty(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
            {
                return Redirect(ReturnUrl);
            }
            return RedirectToPage("./EditUser", new { id = UserId });
        }
    }
}
