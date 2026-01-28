using Ape.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Ape.Middleware
{
    public class ActivityTrackingMiddleware(RequestDelegate next, ILogger<ActivityTrackingMiddleware> logger)
    {
        private readonly RequestDelegate _next = next;
        private readonly ILogger<ActivityTrackingMiddleware> _logger = logger;

        // Only update LastActivity every 2 minutes to reduce database writes
        private static readonly TimeSpan UpdateInterval = TimeSpan.FromMinutes(2);

        public async Task InvokeAsync(HttpContext context, UserManager<IdentityUser> userManager, ApplicationDbContext dbContext)
        {
            if (context.User.Identity?.IsAuthenticated == true)
            {
                try
                {
                    var user = await userManager.GetUserAsync(context.User);
                    if (user != null)
                    {
                        var shouldUpdate = await ShouldUpdateActivity(dbContext, user.Id);
                        if (shouldUpdate)
                        {
                            await UpdateUserActivity(dbContext, user.Id);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating user activity tracking");
                }
            }

            await _next(context);
        }

        private static async Task<bool> ShouldUpdateActivity(ApplicationDbContext dbContext, string userId)
        {
            var userProfile = await dbContext.UserProfiles
                .AsNoTracking()
                .Where(u => u.UserId == userId)
                .Select(u => new { u.LastActivity })
                .FirstOrDefaultAsync();

            if (userProfile == null) return false;
            if (!userProfile.LastActivity.HasValue) return true;

            var timeSinceLastUpdate = DateTime.UtcNow - userProfile.LastActivity.Value;
            return timeSinceLastUpdate >= UpdateInterval;
        }

        private async Task UpdateUserActivity(ApplicationDbContext dbContext, string userId)
        {
            try
            {
                await dbContext.UserProfiles
                    .Where(u => u.UserId == userId)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(u => u.LastActivity, DateTime.UtcNow));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update LastActivity for user {UserId}", userId);
            }
        }
    }
}
