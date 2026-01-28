using Ape.Data;
using Ape.Models;
using Microsoft.EntityFrameworkCore;

namespace Ape.Services
{
    public interface ISystemSettingsService
    {
        Task<string> GetSettingAsync(string key, string defaultValue = "");
        Task<List<string>> GetEmailListAsync(string key);
        Task SetSettingAsync(string key, string value, string description = "", string updatedBy = "System");
        Task<List<SystemSetting>> GetAllSettingsAsync();
    }

    public class SystemSettingsService(ApplicationDbContext context, ILogger<SystemSettingsService> logger) : ISystemSettingsService
    {
        private readonly ApplicationDbContext _context = context;
        private readonly ILogger<SystemSettingsService> _logger = logger;

        public async Task<string> GetSettingAsync(string key, string defaultValue = "")
        {
            try
            {
                var setting = await _context.SystemSettings
                    .FirstOrDefaultAsync(s => s.SettingKey == key);

                return setting?.SettingValue ?? defaultValue;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving system setting: {Key}", key);
                return defaultValue;
            }
        }

        public async Task<List<string>> GetEmailListAsync(string key)
        {
            try
            {
                var settingValue = await GetSettingAsync(key, "");
                if (string.IsNullOrWhiteSpace(settingValue))
                {
                    return [];
                }

                return settingValue
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(email => email.Trim())
                    .Where(email => !string.IsNullOrWhiteSpace(email))
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing email list for setting: {Key}", key);
                return [];
            }
        }

        public async Task SetSettingAsync(string key, string value, string description = "", string updatedBy = "System")
        {
            try
            {
                var setting = await _context.SystemSettings
                    .FirstOrDefaultAsync(s => s.SettingKey == key);

                if (setting != null)
                {
                    setting.SettingValue = value;
                    setting.UpdatedDate = DateTime.UtcNow;
                    setting.UpdatedBy = updatedBy;
                    if (!string.IsNullOrEmpty(description))
                    {
                        setting.Description = description;
                    }
                }
                else
                {
                    setting = new SystemSetting
                    {
                        SettingKey = key,
                        SettingValue = value,
                        Description = description,
                        CreatedDate = DateTime.UtcNow,
                        UpdatedDate = DateTime.UtcNow,
                        UpdatedBy = updatedBy
                    };
                    _context.SystemSettings.Add(setting);
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("System setting updated: {Key} by {UpdatedBy}", key, updatedBy);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting system setting: {Key}", key);
                throw;
            }
        }

        public async Task<List<SystemSetting>> GetAllSettingsAsync()
        {
            try
            {
                return await _context.SystemSettings
                    .OrderBy(s => s.SettingKey)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all system settings");
                return [];
            }
        }
    }
}
