namespace Ape.Models.ViewModels
{
    public enum ActivityStatus
    {
        ActiveNow,          // Last 5 minutes - DON'T DEPLOY
        ActiveRecently,     // 5-30 minutes - Probably safe
        Idle,               // 30 min - 12 hours - Safe to deploy
        SessionExpired      // 12+ hours - Definitely safe
    }

    public class UserActivityInfo
    {
        public string UserId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Roles { get; set; } = string.Empty;
        public DateTime? LastLogin { get; set; }
        public DateTime? LastActivity { get; set; }
        public ActivityStatus ActivityStatus { get; set; }
        public bool IsCurrentUser { get; set; }
    }

    public class ActiveUsersViewModel
    {
        public List<UserActivityInfo> ActiveUsers { get; set; } = [];
        public int ActiveNowCount { get; set; }
        public int ActiveNowExcludingSelfCount { get; set; }
        public int ActiveRecentlyCount { get; set; }
        public int IdleCount { get; set; }
        public int ExpiredCount { get; set; }
        public string CurrentUserId { get; set; } = string.Empty;
    }
}
