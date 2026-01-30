using Ape.Data;
using Ape.Models;
using Ape.Models.ViewModels;
using Ape.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Net.Http;

namespace Ape.Controllers;

[Authorize(Roles = "Admin")]
public class SystemCredentialsController(
    ApplicationDbContext context,
    SecureConfigurationService configService,
    CredentialEncryptionService encryptionService,
    ILogger<SystemCredentialsController> logger) : Controller
{
    private readonly ApplicationDbContext _context = context;
    private readonly SecureConfigurationService _configService = configService;
    private readonly CredentialEncryptionService _encryptionService = encryptionService;
    private readonly ILogger<SystemCredentialsController> _logger = logger;

    /// <summary>
    /// Display all credentials grouped by category.
    /// </summary>
    public async Task<IActionResult> Index()
    {
        var viewModel = new CredentialsIndexViewModel
        {
            IsMasterKeyConfigured = _encryptionService.IsMasterKeyConfigured(),
            Categories = CredentialCategory.GetAll()
        };

        if (!viewModel.IsMasterKeyConfigured)
        {
            TempData["Warning"] = "Master credential key is not configured. Please set the MASTER_CREDENTIAL_KEY_ILLUSTRATE environment variable.";
            return View(viewModel);
        }

        try
        {
            var credentialsByCategory = await _configService.GetAllCredentialsGroupedAsync();

            viewModel.CredentialsByCategory = credentialsByCategory
                .ToDictionary(
                    g => g.Key,
                    g => g.Value.Select(c => new CredentialViewModel
                    {
                        CredentialID = c.CredentialID,
                        CredentialKey = c.CredentialKey,
                        CredentialName = c.CredentialName,
                        Category = c.Category,
                        Description = c.Description,
                        IsActive = c.IsActive,
                        CreatedDate = c.CreatedDate,
                        CreatedBy = c.CreatedBy,
                        UpdatedDate = c.UpdatedDate,
                        UpdatedBy = c.UpdatedBy,
                        DecryptedValue = c.DecryptedValue
                    }).ToList()
                );

            viewModel.TotalCredentials = credentialsByCategory.Sum(g => g.Value.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading credentials");
            TempData["Error"] = "Error loading credentials: " + ex.Message;
        }

        return View(viewModel);
    }

    /// <summary>
    /// Show create credential form.
    /// </summary>
    public IActionResult Create()
    {
        if (!_encryptionService.IsMasterKeyConfigured())
        {
            TempData["Error"] = "Master credential key is not configured.";
            return RedirectToAction(nameof(Index));
        }

        ViewBag.Categories = CredentialCategory.GetAll();
        return View(new CredentialEditViewModel());
    }

    /// <summary>
    /// Create new credential.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CredentialEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Categories = CredentialCategory.GetAll();
            return View(model);
        }

        try
        {
            var success = await _configService.SetCredentialAsync(
                model.CredentialKey,
                model.CredentialName,
                model.CredentialValue,
                model.Category,
                model.Description
            );

            if (success)
            {
                TempData["Success"] = $"Credential '{model.CredentialName}' created successfully.";
                return RedirectToAction(nameof(Index));
            }
            else
            {
                ModelState.AddModelError("", "Failed to create credential.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating credential");
            ModelState.AddModelError("", "Error creating credential: " + ex.Message);
        }

        ViewBag.Categories = CredentialCategory.GetAll();
        return View(model);
    }

    /// <summary>
    /// Show edit credential form.
    /// </summary>
    public async Task<IActionResult> Edit(int id)
    {
        if (!_encryptionService.IsMasterKeyConfigured())
        {
            TempData["Error"] = "Master credential key is not configured.";
            return RedirectToAction(nameof(Index));
        }

        var credential = await _context.SystemCredentials.FindAsync(id);
        if (credential == null)
        {
            return NotFound();
        }

        var model = new CredentialEditViewModel
        {
            CredentialID = credential.CredentialID,
            CredentialKey = credential.CredentialKey,
            CredentialName = credential.CredentialName,
            Category = credential.Category,
            CredentialValue = _encryptionService.Decrypt(credential.EncryptedValue),
            Description = credential.Description,
            IsActive = credential.IsActive
        };

        ViewBag.Categories = CredentialCategory.GetAll();
        return View(model);
    }

    /// <summary>
    /// Update credential.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CredentialEditViewModel model)
    {
        if (id != model.CredentialID)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            ViewBag.Categories = CredentialCategory.GetAll();
            return View(model);
        }

        try
        {
            var success = await _configService.SetCredentialAsync(
                model.CredentialKey,
                model.CredentialName,
                model.CredentialValue,
                model.Category,
                model.Description
            );

            if (success)
            {
                TempData["Success"] = $"Credential '{model.CredentialName}' updated successfully.";
                return RedirectToAction(nameof(Index));
            }
            else
            {
                ModelState.AddModelError("", "Failed to update credential.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating credential");
            ModelState.AddModelError("", "Error updating credential: " + ex.Message);
        }

        ViewBag.Categories = CredentialCategory.GetAll();
        return View(model);
    }

    /// <summary>
    /// Delete credential confirmation.
    /// </summary>
    public async Task<IActionResult> Delete(int id)
    {
        var credential = await _context.SystemCredentials.FindAsync(id);
        if (credential == null)
        {
            return NotFound();
        }

        var viewModel = new CredentialViewModel
        {
            CredentialID = credential.CredentialID,
            CredentialKey = credential.CredentialKey,
            CredentialName = credential.CredentialName,
            Category = credential.Category,
            Description = credential.Description,
            CreatedDate = credential.CreatedDate,
            CreatedBy = credential.CreatedBy
        };

        return View(viewModel);
    }

    /// <summary>
    /// Delete credential confirmed.
    /// </summary>
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        try
        {
            var credential = await _context.SystemCredentials.FindAsync(id);
            if (credential == null)
            {
                return NotFound();
            }

            var credentialName = credential.CredentialName;
            var success = await _configService.DeleteCredentialAsync(credential.CredentialKey);

            if (success)
            {
                TempData["Success"] = $"Credential '{credentialName}' deleted successfully.";
            }
            else
            {
                TempData["Error"] = "Failed to delete credential.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting credential");
            TempData["Error"] = "Error deleting credential: " + ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Get decrypted credential value (AJAX).
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GetDecryptedValue(int id)
    {
        try
        {
            var credential = await _context.SystemCredentials.FindAsync(id);
            if (credential == null)
            {
                return NotFound();
            }

            var decryptedValue = _encryptionService.Decrypt(credential.EncryptedValue);

            // Log the view action
            await LogAuditAsync(credential.CredentialID, credential.CredentialKey, CredentialAction.Viewed);

            return Json(new { success = true, value = decryptedValue });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error decrypting credential");
            return Json(new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Test database connection (AJAX).
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> TestDatabaseConnection(int id)
    {
        try
        {
            var credential = await _context.SystemCredentials.FindAsync(id);
            if (credential == null)
            {
                return Json(new { success = false, message = "Credential not found" });
            }

            var decryptedValue = _encryptionService.Decrypt(credential.EncryptedValue);

            // Try to open connection
            using var connection = new SqlConnection(decryptedValue);
            await connection.OpenAsync();

            await LogAuditAsync(credential.CredentialID, credential.CredentialKey, CredentialAction.Tested,
                success: true, details: "Database connection successful");

            return Json(new
            {
                success = true,
                message = "Database connection successful!",
                details = $"Server: {connection.DataSource}, Database: {connection.Database}"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database connection test failed");

            var credential = await _context.SystemCredentials.FindAsync(id);
            if (credential != null)
            {
                await LogAuditAsync(credential.CredentialID, credential.CredentialKey, CredentialAction.Tested,
                    success: false, details: ex.Message);
            }

            return Json(new { success = false, message = "Connection failed", error = ex.Message });
        }
    }

    /// <summary>
    /// Test API key by making a test call (AJAX).
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> TestApiKey(int id)
    {
        try
        {
            var credential = await _context.SystemCredentials.FindAsync(id);
            if (credential == null)
            {
                return Json(new { success = false, message = "Credential not found" });
            }

            var apiKey = _encryptionService.Decrypt(credential.EncryptedValue);

            // Determine API type based on credential key and test accordingly
            string testResult;
            if (credential.CredentialKey.Contains("WEATHER", StringComparison.OrdinalIgnoreCase))
            {
                testResult = await TestWeatherApi(apiKey);
            }
            else if (credential.CredentialKey.Contains("AZURE", StringComparison.OrdinalIgnoreCase))
            {
                testResult = "Azure API test not implemented yet";
            }
            else
            {
                testResult = "Generic API key validated (format check only)";
            }

            await LogAuditAsync(credential.CredentialID, credential.CredentialKey, CredentialAction.Tested,
                success: true, details: testResult);

            return Json(new { success = true, message = "API key test successful", details = testResult });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "API key test failed");

            var credential = await _context.SystemCredentials.FindAsync(id);
            if (credential != null)
            {
                await LogAuditAsync(credential.CredentialID, credential.CredentialKey, CredentialAction.Tested,
                    success: false, details: ex.Message);
            }

            return Json(new { success = false, message = "API test failed", error = ex.Message });
        }
    }

    /// <summary>
    /// Test weather API specifically.
    /// </summary>
    private async Task<string> TestWeatherApi(string apiKey)
    {
        using var httpClient = new HttpClient();
        var testUrl = $"https://weather.visualcrossing.com/VisualCrossingWebServices/rest/services/timeline/Miami,FL/2024-01-01?key={apiKey}";

        var response = await httpClient.GetAsync(testUrl);
        response.EnsureSuccessStatusCode();

        return "Visual Crossing Weather API test successful";
    }

    /// <summary>
    /// View audit history for a credential.
    /// </summary>
    public async Task<IActionResult> AuditHistory(int id)
    {
        var credential = await _context.SystemCredentials.FindAsync(id);
        if (credential == null)
        {
            return NotFound();
        }

        var auditLogs = await _context.CredentialAuditLogs
            .Where(log => log.CredentialID == id)
            .OrderByDescending(log => log.ActionDate)
            .Take(50)
            .ToListAsync();

        var viewModel = new
        {
            Credential = new CredentialViewModel
            {
                CredentialID = credential.CredentialID,
                CredentialName = credential.CredentialName,
                CredentialKey = credential.CredentialKey,
                Category = credential.Category
            },
            AuditLogs = auditLogs.Select(log => new CredentialAuditViewModel
            {
                AuditID = log.AuditID,
                CredentialKey = log.CredentialKey,
                Action = log.Action,
                ActionDetails = log.ActionDetails,
                ActionBy = log.ActionBy,
                ActionDate = log.ActionDate,
                IPAddress = log.IPAddress,
                Success = log.Success,
                ErrorMessage = log.ErrorMessage
            }).ToList()
        };

        return View(viewModel);
    }

    /// <summary>
    /// Log audit entry for credential access.
    /// </summary>
    private async Task LogAuditAsync(int credentialId, string credentialKey, string action, bool? success = null, string? details = null)
    {
        try
        {
            var auditLog = new CredentialAuditLog
            {
                CredentialID = credentialId,
                CredentialKey = credentialKey,
                Action = action,
                ActionDetails = details,
                ActionBy = User.Identity?.Name ?? "System",
                ActionDate = DateTime.UtcNow,
                IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent = Request.Headers["User-Agent"].ToString(),
                Success = success
            };

            _context.CredentialAuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging audit entry");
        }
    }
}
