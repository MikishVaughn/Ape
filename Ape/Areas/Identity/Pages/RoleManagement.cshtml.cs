using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Ape.Areas.Identity.Pages
{
    [Authorize(Roles = "Admin,Manager")]
    public class RoleManagementModel(RoleManager<IdentityRole> roleManager, UserManager<IdentityUser> userManager) : PageModel
    {
        private readonly RoleManager<IdentityRole> _roleManager = roleManager;
        private readonly UserManager<IdentityUser> _userManager = userManager;

        public List<RoleInfo> Roles { get; set; } = [];

        [TempData]
        public string? StatusMessage { get; set; }

        public class RoleInfo
        {
            public required string Id { get; set; }
            public required string Name { get; set; }
            public int UserCount { get; set; }
        }

        public async Task OnGetAsync()
        {
            await LoadRolesAsync();
        }

        public async Task<IActionResult> OnPostCreateAsync(string roleName)
        {
            if (string.IsNullOrWhiteSpace(roleName))
            {
                StatusMessage = "Error: Role name cannot be empty.";
                return RedirectToPage();
            }

            roleName = roleName.Trim();

            var exists = await _roleManager.RoleExistsAsync(roleName);
            if (exists)
            {
                StatusMessage = $"Error: A role named \"{roleName}\" already exists.";
                return RedirectToPage();
            }

            var result = await _roleManager.CreateAsync(new IdentityRole(roleName));
            if (result.Succeeded)
            {
                StatusMessage = $"Role \"{roleName}\" created successfully.";
            }
            else
            {
                StatusMessage = "Error: " + string.Join(", ", result.Errors.Select(e => e.Description));
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostRenameAsync(string roleId, string newName)
        {
            if (string.IsNullOrWhiteSpace(newName))
            {
                StatusMessage = "Error: New role name cannot be empty.";
                return RedirectToPage();
            }

            newName = newName.Trim();

            var role = await _roleManager.FindByIdAsync(roleId);
            if (role == null)
            {
                StatusMessage = "Error: Role not found.";
                return RedirectToPage();
            }

            var oldName = role.Name;

            // Check if the new name already exists (different role)
            var existingRole = await _roleManager.FindByNameAsync(newName);
            if (existingRole != null && existingRole.Id != roleId)
            {
                StatusMessage = $"Error: A role named \"{newName}\" already exists.";
                return RedirectToPage();
            }

            role.Name = newName;
            var result = await _roleManager.UpdateAsync(role);
            if (result.Succeeded)
            {
                StatusMessage = $"Role renamed from \"{oldName}\" to \"{newName}\".";
            }
            else
            {
                StatusMessage = "Error: " + string.Join(", ", result.Errors.Select(e => e.Description));
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(string roleId)
        {
            var role = await _roleManager.FindByIdAsync(roleId);
            if (role == null)
            {
                StatusMessage = "Error: Role not found.";
                return RedirectToPage();
            }

            var roleName = role.Name ?? "Unknown";

            // Check if any users are assigned to this role
            var usersInRole = await _userManager.GetUsersInRoleAsync(roleName);
            if (usersInRole.Count > 0)
            {
                StatusMessage = $"Error: Cannot delete role \"{roleName}\" because {usersInRole.Count} user(s) are assigned to it. Remove all users from this role first.";
                return RedirectToPage();
            }

            var result = await _roleManager.DeleteAsync(role);
            if (result.Succeeded)
            {
                StatusMessage = $"Role \"{roleName}\" deleted successfully.";
            }
            else
            {
                StatusMessage = "Error: " + string.Join(", ", result.Errors.Select(e => e.Description));
            }

            return RedirectToPage();
        }

        private async Task LoadRolesAsync()
        {
            var roles = await _roleManager.Roles.OrderBy(r => r.Name).ToListAsync();
            Roles = [];

            foreach (var role in roles)
            {
                var name = role.Name ?? "Unknown";
                var usersInRole = await _userManager.GetUsersInRoleAsync(name);
                Roles.Add(new RoleInfo
                {
                    Id = role.Id,
                    Name = name,
                    UserCount = usersInRole.Count
                });
            }
        }
    }
}
