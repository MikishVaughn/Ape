using System.ComponentModel.DataAnnotations;

namespace Ape.Models
{
    public class SystemSetting
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string SettingKey { get; set; } = string.Empty;

        [Required]
        [MaxLength(1000)]
        public string SettingValue { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;

        [MaxLength(100)]
        public string? UpdatedBy { get; set; }
    }

    public static class SystemSettingKeys
    {
        public const string ContactFormEmails = "ContactFormEmails";
        public const string SiteName = "SiteName";
        public const string AdminEmail = "AdminEmail";
    }
}
