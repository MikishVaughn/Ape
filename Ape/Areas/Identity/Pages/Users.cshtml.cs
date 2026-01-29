using Ape.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Ape.Areas.Identity.Pages
{
    [Authorize(Roles = "Admin,Manager")]
    public class UsersModel(UserManager<IdentityUser> userManager, ApplicationDbContext dbContext) : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager = userManager;
        private readonly ApplicationDbContext _dbContext = dbContext;

        public class UserModel
        {
            public required string Id { get; set; }
            public required string UserName { get; set; }
            public string? FullName { get; set; }
            public required string Email { get; set; }
            public bool EmailConfirmed { get; set; }
            public string? PhoneNumber { get; set; }
            public IList<string>? Roles { get; set; }
            public DateTime? LastLogin { get; set; }
            public bool IsActive { get; set; } = true;
            public DateTime? DeactivatedDate { get; set; }
            public string? DeactivatedBy { get; set; }
            public string? DeactivationReason { get; set; }
        }

        public required List<UserModel> Users { get; set; } = [];

        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public int PageSize { get; set; } = 20;

        public int TotalUsers { get; set; }
        public int TotalPages { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SortColumn { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SortOrder { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? StatusFilter { get; set; } = "Active";

        public async Task<IActionResult> OnGetAsync(string? searchTerm, int pageNumber = 1, int pageSize = 20, string? sortColumn = null, string? sortOrder = null, string? statusFilter = "Active")
        {
            SearchTerm = searchTerm;
            PageNumber = pageNumber;
            PageSize = pageSize;
            SortColumn = sortColumn ?? "fullname";
            SortOrder = sortOrder ?? "asc";
            StatusFilter = statusFilter;
            await LoadUsersDataAsync();
            return Page();
        }

        private async Task LoadUsersDataAsync()
        {
            IQueryable<IdentityUser> usersQuery = _userManager.Users.AsNoTracking().AsQueryable();

            // Apply search filtering
            if (!string.IsNullOrEmpty(SearchTerm))
            {
                string searchTerm = SearchTerm.Trim().ToLower();
                var filterCondition = PredicateBuilder.False<IdentityUser>();

                if (searchTerm.Equals("no role", StringComparison.OrdinalIgnoreCase))
                {
                    filterCondition = filterCondition.Or(u => !_dbContext.UserRoles.Any(ur => ur.UserId == u.Id));
                }
                else if (searchTerm.Equals("not confirmed", StringComparison.OrdinalIgnoreCase))
                {
                    filterCondition = filterCondition.Or(u => !u.EmailConfirmed);
                }
                else
                {
                    filterCondition = filterCondition.Or(u => u.Email != null && u.Email.ToLower().Contains(searchTerm));
                    filterCondition = filterCondition.Or(u => u.PhoneNumber != null && u.PhoneNumber.ToLower().Contains(searchTerm));
                    filterCondition = filterCondition.Or(u => _dbContext.UserProfiles.Any(up => up.UserId == u.Id &&
                        (
                            (up.FirstName != null && up.FirstName.ToLower().Contains(searchTerm)) ||
                            (up.MiddleName != null && up.MiddleName.ToLower().Contains(searchTerm)) ||
                            (up.LastName != null && up.LastName.ToLower().Contains(searchTerm))
                        )
                    ));
                    // Search in Roles
                    filterCondition = filterCondition.Or(u => _dbContext.UserRoles.Any(ur => ur.UserId == u.Id &&
                        _dbContext.Roles.Any(r => r.Id == ur.RoleId && r.Name != null && r.Name.ToLower().Contains(searchTerm))));
                }

                usersQuery = usersQuery.Where(filterCondition);
            }

            // Join with UserProfiles
            var joinedQuery = usersQuery.GroupJoin(
                _dbContext.UserProfiles,
                user => user.Id,
                userProfile => userProfile.UserId,
                (user, userProfiles) => new { User = user, UserProfile = userProfiles.FirstOrDefault() }
            ).AsQueryable();

            // Apply status filtering
            if (!string.IsNullOrEmpty(StatusFilter))
            {
                switch (StatusFilter.ToLower())
                {
                    case "active":
                        joinedQuery = joinedQuery.Where(x =>
                            (x.User.LockoutEnd == null || x.User.LockoutEnd <= DateTimeOffset.UtcNow) &&
                            (x.UserProfile == null || x.UserProfile.IsActive));
                        break;
                    case "inactive":
                        joinedQuery = joinedQuery.Where(x =>
                            (x.User.LockoutEnd != null && x.User.LockoutEnd > DateTimeOffset.UtcNow) ||
                            (x.UserProfile != null && !x.UserProfile.IsActive));
                        break;
                    case "all":
                        break;
                    default:
                        joinedQuery = joinedQuery.Where(x =>
                            (x.User.LockoutEnd == null || x.User.LockoutEnd <= DateTimeOffset.UtcNow) &&
                            (x.UserProfile == null || x.UserProfile.IsActive));
                        break;
                }
            }

            TotalUsers = await joinedQuery.CountAsync();
            TotalPages = (int)Math.Ceiling(TotalUsers / (double)PageSize);

            if (PageNumber < 1)
                PageNumber = 1;
            else if (PageNumber > TotalPages && TotalPages > 0)
                PageNumber = TotalPages;
            else if (TotalPages == 0)
                PageNumber = 1;

            // Apply sorting
            IOrderedQueryable<dynamic> orderedQuery;

            if (string.IsNullOrEmpty(SortColumn))
            {
                orderedQuery = joinedQuery.OrderBy(x => x.UserProfile != null ? x.UserProfile.LastName : null)
                    .ThenBy(x => x.UserProfile != null ? x.UserProfile.FirstName : null);
            }
            else
            {
                orderedQuery = SortColumn.ToLower() switch
                {
                    "fullname" => SortOrder?.ToLower() == "asc"
                        ? joinedQuery.OrderBy(x => x.UserProfile != null ? x.UserProfile.LastName : null).ThenBy(x => x.UserProfile != null ? x.UserProfile.FirstName : null)
                        : joinedQuery.OrderByDescending(x => x.UserProfile != null ? x.UserProfile.LastName : null).ThenByDescending(x => x.UserProfile != null ? x.UserProfile.FirstName : null),
                    "email" => SortOrder?.ToLower() == "asc"
                        ? joinedQuery.OrderBy(x => x.User.Email)
                        : joinedQuery.OrderByDescending(x => x.User.Email),
                    "emailconfirmed" => SortOrder?.ToLower() == "asc"
                        ? joinedQuery.OrderBy(x => x.User.EmailConfirmed)
                        : joinedQuery.OrderByDescending(x => x.User.EmailConfirmed),
                    "phonenumber" => SortOrder?.ToLower() == "asc"
                        ? joinedQuery.OrderBy(x => x.User.PhoneNumber)
                        : joinedQuery.OrderByDescending(x => x.User.PhoneNumber),
                    "roles" => SortOrder?.ToLower() == "asc"
                        ? joinedQuery.OrderBy(x => x.User.Email)
                        : joinedQuery.OrderByDescending(x => x.User.Email),
                    "lastlogin" => SortOrder?.ToLower() == "desc"
                        ? joinedQuery.OrderBy(x => x.UserProfile != null ? x.UserProfile.LastLogin : null)
                        : joinedQuery.OrderByDescending(x => x.UserProfile != null ? x.UserProfile.LastLogin : null),
                    "status" => SortOrder?.ToLower() == "asc"
                        ? joinedQuery.OrderBy(x => (x.User.LockoutEnd == null || x.User.LockoutEnd <= DateTimeOffset.UtcNow) && (x.UserProfile == null || x.UserProfile.IsActive))
                        : joinedQuery.OrderByDescending(x => (x.User.LockoutEnd == null || x.User.LockoutEnd <= DateTimeOffset.UtcNow) && (x.UserProfile == null || x.UserProfile.IsActive)),
                    _ => joinedQuery.OrderBy(x => x.UserProfile != null ? x.UserProfile.LastName : null)
                        .ThenBy(x => x.UserProfile != null ? x.UserProfile.FirstName : null),
                };
            }

            // Apply pagination
            var paginatedJoinedUsers = await orderedQuery
                .Skip((PageNumber - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            // Batch load all roles
            var userIds = paginatedJoinedUsers.Select(x => (string)x.User.Id).ToList();

            var allUserRoles = await _dbContext.UserRoles
                .AsNoTracking()
                .Where(ur => userIds.Contains(ur.UserId))
                .Join(_dbContext.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, r.Name })
                .ToListAsync();

            var userRolesDict = allUserRoles
                .GroupBy(x => x.UserId)
                .ToDictionary(g => g.Key, g => g.Select(x => x.Name).Where(n => n != null).Cast<string>().ToList());

            // Map to UserModel
            Users = [];
            foreach (var item in paginatedJoinedUsers)
            {
                var user = item.User;
                var userProfile = item.UserProfile;
                string userId = user.Id;

                IList<string> roles = userRolesDict.TryGetValue(userId, out var value) ? value : new List<string>();

                string? fullName = null;
                if (userProfile != null)
                {
                    fullName = $"{userProfile.FirstName} {(string.IsNullOrEmpty(userProfile.MiddleName) ? "" : userProfile.MiddleName + " ")}{userProfile.LastName}".Trim();
                }

                Users.Add(new UserModel
                {
                    Id = user.Id,
                    UserName = user.UserName ?? string.Empty,
                    Email = user.Email ?? string.Empty,
                    EmailConfirmed = user.EmailConfirmed,
                    PhoneNumber = user.PhoneNumber,
                    FullName = fullName ?? "No Info",
                    Roles = roles,
                    LastLogin = userProfile?.LastLogin,
                    IsActive = (user.LockoutEnd == null || user.LockoutEnd <= DateTimeOffset.UtcNow) &&
                              (userProfile?.IsActive ?? true),
                    DeactivatedDate = userProfile?.DeactivatedDate,
                    DeactivatedBy = userProfile?.DeactivatedBy,
                    DeactivationReason = userProfile?.DeactivationReason
                });
            }
        }
    }

    public static class PredicateBuilder
    {
        public static Expression<Func<T, bool>> True<T>() { return f => true; }
        public static Expression<Func<T, bool>> False<T>() { return f => false; }

        public static Expression<Func<T, bool>> Or<T>(this Expression<Func<T, bool>> expr1, Expression<Func<T, bool>> expr2)
        {
            var invokedExpr = Expression.Invoke(expr2, expr1.Parameters.Cast<Expression>());
            return Expression.Lambda<Func<T, bool>>(Expression.OrElse(expr1.Body, invokedExpr), expr1.Parameters);
        }

        public static Expression<Func<T, bool>> And<T>(this Expression<Func<T, bool>> expr1, Expression<Func<T, bool>> expr2)
        {
            var invokedExpr = Expression.Invoke(expr2, expr1.Parameters.Cast<Expression>());
            return Expression.Lambda<Func<T, bool>>(Expression.AndAlso(expr1.Body, invokedExpr), expr1.Parameters);
        }
    }
}
