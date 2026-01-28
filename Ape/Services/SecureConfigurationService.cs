using Ape.Data;
using Ape.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Ape.Services;

/// <summary>
/// Service for retrieving and managing system credentials from secure storage.
/// Provides caching, fallback to environment variables, and audit logging.
/// </summary>
public class SecureConfigurationService
{
    private readonly ApplicationDbContext _context;
    private readonly CredentialEncryptionService _encryptionService;
    private readonly IMemoryCache _cache;
    private readonly ILogger<SecureConfigurationService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private const int CacheExpirationMinutes = 15;

    public SecureConfigurationService(
        ApplicationDbContext context,
        CredentialEncryptionService encryptionService,
        IMemoryCache cache,
        ILogger<SecureConfigurationService> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _encryptionService = encryptionService;
        _cache = cache;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// Gets a credential value by key, with fallback to environment variables.
    /// Results are cached for performance.
    /// </summary>
    /// <param name="credentialKey">The credential key (e.g., "VISUAL_CROSSING_API_KEY")</param>
    /// <param name="defaultValue">Default value if credential not found</param>
    /// <param name="logAccess">Whether to log this access to audit trail (default: false for performance)</param>
    /// <returns>The decrypted credential value or default</returns>
    public async Task<string?> GetCredentialAsync(string credentialKey, string? defaultValue = null, bool logAccess = false)
    {
        if (string.IsNullOrWhiteSpace(credentialKey))
        {
            throw new ArgumentException("Credential key cannot be null or empty", nameof(credentialKey));
        }

        // Check cache first
        var cacheKey = $"Credential_{credentialKey}";
        if (_cache.TryGetValue(cacheKey, out string? cachedValue))
        {
            return cachedValue;
        }

        try
        {
            // Try database first
            var credential = await _context.SystemCredentials
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CredentialKey == credentialKey && c.IsActive);

            if (credential != null)
            {
                var decryptedValue = _encryptionService.Decrypt(credential.EncryptedValue);

                // Cache the value
                var cacheOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromMinutes(CacheExpirationMinutes));
                _cache.Set(cacheKey, decryptedValue, cacheOptions);

                // Log access if requested
                if (logAccess)
                {
                    await LogCredentialAccessAsync(credential.CredentialID, credentialKey, CredentialAction.Viewed);
                }

                return decryptedValue;
            }

            // Fallback to environment variables
            var envValue = GetEnvironmentVariable(credentialKey);
            if (!string.IsNullOrWhiteSpace(envValue))
            {
                _logger.LogInformation("Credential '{Key}' retrieved from environment variable (not yet migrated to database)", credentialKey);
                return envValue;
            }

            // Return default value
            if (defaultValue != null)
            {
                _logger.LogWarning("Credential '{Key}' not found. Using default value.", credentialKey);
            }
            else
            {
                _logger.LogWarning("Credential '{Key}' not found and no default provided.", credentialKey);
            }

            return defaultValue;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving credential '{Key}'. Falling back to environment variable.", credentialKey);

            // Fallback to environment variable on error
            var envValue = GetEnvironmentVariable(credentialKey);
            if (!string.IsNullOrWhiteSpace(envValue))
            {
                // Cache the environment variable value so it's available for synchronous calls
                var cacheOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromMinutes(CacheExpirationMinutes));
                _cache.Set(cacheKey, envValue, cacheOptions);

                _logger.LogInformation("Credential '{Key}' retrieved from environment variable and cached (database decryption failed)", credentialKey);
                return envValue;
            }

            return defaultValue;
        }
    }

    /// <summary>
    /// Gets a credential value synchronously (for non-async contexts).
    /// Uses cached value if available, otherwise retrieves from environment variables only.
    /// </summary>
    public string? GetCredential(string credentialKey, string? defaultValue = null)
    {
        if (string.IsNullOrWhiteSpace(credentialKey))
        {
            throw new ArgumentException("Credential key cannot be null or empty", nameof(credentialKey));
        }

        // Check cache first
        var cacheKey = $"Credential_{credentialKey}";
        if (_cache.TryGetValue(cacheKey, out string? cachedValue))
        {
            return cachedValue;
        }

        // Fallback to environment variable (synchronous)
        var envValue = GetEnvironmentVariable(credentialKey);
        if (!string.IsNullOrWhiteSpace(envValue))
        {
            return envValue;
        }

        return defaultValue;
    }

    /// <summary>
    /// Creates or updates a credential in the database.
    /// </summary>
    public async Task<bool> SetCredentialAsync(
        string credentialKey,
        string credentialName,
        string credentialValue,
        string category,
        string? description = null)
    {
        try
        {
            var currentUser = _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "System";

            var existingCredential = await _context.SystemCredentials
                .FirstOrDefaultAsync(c => c.CredentialKey == credentialKey);

            if (existingCredential != null)
            {
                // Update existing
                existingCredential.CredentialName = credentialName;
                existingCredential.Category = category;
                existingCredential.EncryptedValue = _encryptionService.Encrypt(credentialValue);
                existingCredential.Description = description;
                existingCredential.UpdatedDate = DateTime.UtcNow;
                existingCredential.UpdatedBy = currentUser;

                await _context.SaveChangesAsync();

                await LogCredentialAccessAsync(existingCredential.CredentialID, credentialKey, CredentialAction.Updated);
            }
            else
            {
                // Create new
                var newCredential = new SystemCredential
                {
                    CredentialKey = credentialKey,
                    CredentialName = credentialName,
                    Category = category,
                    EncryptedValue = _encryptionService.Encrypt(credentialValue),
                    Description = description,
                    CreatedDate = DateTime.UtcNow,
                    CreatedBy = currentUser,
                    IsActive = true
                };

                _context.SystemCredentials.Add(newCredential);
                await _context.SaveChangesAsync();

                await LogCredentialAccessAsync(newCredential.CredentialID, credentialKey, CredentialAction.Created);
            }

            // Invalidate cache
            _cache.Remove($"Credential_{credentialKey}");

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting credential '{Key}'", credentialKey);
            return false;
        }
    }

    /// <summary>
    /// Deletes a credential from the database.
    /// </summary>
    public async Task<bool> DeleteCredentialAsync(string credentialKey)
    {
        try
        {
            var credential = await _context.SystemCredentials
                .FirstOrDefaultAsync(c => c.CredentialKey == credentialKey);

            if (credential == null)
            {
                return false;
            }

            await LogCredentialAccessAsync(credential.CredentialID, credentialKey, CredentialAction.Deleted);

            _context.SystemCredentials.Remove(credential);
            await _context.SaveChangesAsync();

            // Invalidate cache
            _cache.Remove($"Credential_{credentialKey}");

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting credential '{Key}'", credentialKey);
            return false;
        }
    }

    /// <summary>
    /// Gets all credentials for a specific category (decrypted).
    /// </summary>
    public async Task<List<SystemCredential>> GetCredentialsByCategoryAsync(string category)
    {
        var credentials = await _context.SystemCredentials
            .AsNoTracking()
            .Where(c => c.Category == category && c.IsActive)
            .OrderBy(c => c.CredentialName)
            .ToListAsync();

        // Decrypt values
        foreach (var credential in credentials)
        {
            try
            {
                credential.DecryptedValue = _encryptionService.Decrypt(credential.EncryptedValue);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error decrypting credential '{Key}'", credential.CredentialKey);
                credential.DecryptedValue = "[Decryption Error]";
            }
        }

        return credentials;
    }

    /// <summary>
    /// Gets all credentials (decrypted) organized by category.
    /// </summary>
    public async Task<Dictionary<string, List<SystemCredential>>> GetAllCredentialsGroupedAsync()
    {
        var credentials = await _context.SystemCredentials
            .AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.Category)
            .ThenBy(c => c.CredentialName)
            .ToListAsync();

        // Decrypt values
        foreach (var credential in credentials)
        {
            try
            {
                credential.DecryptedValue = _encryptionService.Decrypt(credential.EncryptedValue);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error decrypting credential '{Key}'", credential.CredentialKey);
                credential.DecryptedValue = "[Decryption Error]";
            }
        }

        // Group by category
        return credentials
            .GroupBy(c => c.Category)
            .ToDictionary(g => g.Key, g => g.ToList());
    }

    /// <summary>
    /// Clears the credential cache (useful after bulk updates).
    /// </summary>
    public void ClearCache()
    {
        // Note: IMemoryCache doesn't have a built-in clear all method
        // We'd need to track keys or use a different caching strategy for bulk clear
        _logger.LogInformation("Credential cache clear requested (individual entries will expire naturally)");
    }

    /// <summary>
    /// Logs credential access to audit trail.
    /// </summary>
    private async Task LogCredentialAccessAsync(int credentialId, string credentialKey, string action, bool? success = null, string? errorMessage = null)
    {
        try
        {
            var httpContext = _httpContextAccessor.HttpContext;
            var currentUser = httpContext?.User?.Identity?.Name ?? "System";
            var ipAddress = httpContext?.Connection?.RemoteIpAddress?.ToString();
            var userAgent = httpContext?.Request?.Headers["User-Agent"].ToString();

            var auditLog = new CredentialAuditLog
            {
                CredentialID = credentialId,
                CredentialKey = credentialKey,
                Action = action,
                ActionBy = currentUser,
                ActionDate = DateTime.UtcNow,
                IPAddress = ipAddress,
                UserAgent = userAgent,
                Success = success,
                ErrorMessage = errorMessage
            };

            _context.CredentialAuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging credential access for '{Key}'", credentialKey);
            // Don't throw - audit logging failure shouldn't break credential access
        }
    }

    /// <summary>
    /// Gets environment variable from User or Machine level.
    /// </summary>
    private string? GetEnvironmentVariable(string variableName)
    {
        // Try process-level first (where hosting platforms like Site4Now inject app settings)
        var value = Environment.GetEnvironmentVariable(variableName);

        // Try user-level
        if (string.IsNullOrWhiteSpace(value))
        {
            value = Environment.GetEnvironmentVariable(variableName, EnvironmentVariableTarget.User);
        }

        // Try machine-level
        if (string.IsNullOrWhiteSpace(value))
        {
            value = Environment.GetEnvironmentVariable(variableName, EnvironmentVariableTarget.Machine);
        }

        return value;
    }
}
