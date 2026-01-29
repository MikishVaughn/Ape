using Ape.Data;
using Ape.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ape.Controllers
{
    [Authorize(Roles = "Admin,Manager")]
    public class ActiveUsersController(
        ApplicationDbContext context,
        UserManager<IdentityUser> userManager) : Controller
    {
        private readonly ApplicationDbContext _context = context;
        private readonly UserManager<IdentityUser> _userManager = userManager;

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var currentUserId = _userManager.GetUserId(User) ?? string.Empty;

            // Get all active user profiles
            var userProfiles = await _context.UserProfiles
                .Where(up => up.IsActive)
                .ToListAsync();

            // Get corresponding identity users
            var userIds = userProfiles.Select(up => up.UserId).ToList();
            var allUsers = await _context.Users
                .Where(u => userIds.Contains(u.Id))
                .ToListAsync();

            var activeUsersData = new List<UserActivityInfo>();

            foreach (var user in allUsers)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var userProfile = userProfiles.FirstOrDefault(up => up.UserId == user.Id);

                if (userProfile != null)
                {
                    activeUsersData.Add(new UserActivityInfo
                    {
                        UserId = user.Id,
                        Email = user.Email ?? "Unknown",
                        FullName = $"{userProfile.FirstName} {userProfile.LastName}".Trim(),
                        Roles = roles.Count != 0 ? string.Join(", ", roles) : "Member",
                        LastLogin = userProfile.LastLogin,
                        LastActivity = userProfile.LastActivity,
                        ActivityStatus = CalculateActivityStatus(userProfile.LastActivity),
                        IsCurrentUser = user.Id == currentUserId
                    });
                }
            }

            var viewModel = new ActiveUsersViewModel
            {
                ActiveUsers = activeUsersData
                    .OrderByDescending(u => u.LastActivity ?? DateTime.MinValue)
                    .ToList(),
                CurrentUserId = currentUserId
            };

            viewModel.ActiveNowCount = viewModel.ActiveUsers.Count(u => u.ActivityStatus == ActivityStatus.ActiveNow);
            viewModel.ActiveNowExcludingSelfCount = viewModel.ActiveUsers.Count(u => u.ActivityStatus == ActivityStatus.ActiveNow && !u.IsCurrentUser);
            viewModel.ActiveRecentlyCount = viewModel.ActiveUsers.Count(u => u.ActivityStatus == ActivityStatus.ActiveRecently);
            viewModel.IdleCount = viewModel.ActiveUsers.Count(u => u.ActivityStatus == ActivityStatus.Idle);
            viewModel.ExpiredCount = viewModel.ActiveUsers.Count(u => u.ActivityStatus == ActivityStatus.SessionExpired);

            return View(viewModel);
        }

        private static ActivityStatus CalculateActivityStatus(DateTime? lastActivity)
        {
            if (!lastActivity.HasValue)
                return ActivityStatus.SessionExpired;

            var timeSinceActivity = DateTime.UtcNow - lastActivity.Value;

            if (timeSinceActivity.TotalMinutes <= 5)
                return ActivityStatus.ActiveNow;

            if (timeSinceActivity.TotalMinutes <= 30)
                return ActivityStatus.ActiveRecently;

            if (timeSinceActivity.TotalHours <= 12)
                return ActivityStatus.Idle;

            return ActivityStatus.SessionExpired;
        }
    }
}
