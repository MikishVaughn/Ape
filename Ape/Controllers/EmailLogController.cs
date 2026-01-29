using Ape.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ape.Controllers
{
    [Authorize(Roles = "Admin,Manager")]
    public class EmailLogController : Controller
    {
        private readonly ILogger<EmailLogController> _logger;
        private readonly ApplicationDbContext _context;

        public EmailLogController(ILogger<EmailLogController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index(int page = 1, int pageSize = 50, string? search = null, string? serverFilter = null)
        {
            var query = _context.EmailLogs.AsQueryable();

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(e =>
                    e.ToEmail.Contains(search) ||
                    e.Subject.Contains(search) ||
                    e.Details!.Contains(search) ||
                    e.SentBy!.Contains(search));
            }

            // Apply server filter
            if (!string.IsNullOrWhiteSpace(serverFilter) && serverFilter != "All")
            {
                query = query.Where(e => e.EmailServer == serverFilter);
            }

            // Get total count for pagination
            var totalCount = await query.CountAsync();

            // Apply pagination and ordering
            var emailLogs = await query
                .OrderByDescending(e => e.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Calculate pagination info
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            // Get server statistics
            var serverStats = await _context.EmailLogs
                .GroupBy(e => e.EmailServer)
                .Select(g => new { Server = g.Key, Count = g.Count() })
                .ToListAsync();

            ViewBag.EmailLogs = emailLogs;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalCount = totalCount;
            ViewBag.Search = search;
            ViewBag.ServerFilter = serverFilter;
            ViewBag.ServerStats = serverStats;

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ClearOldLogs(int daysToKeep = 30)
        {
            try
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-daysToKeep);
                var oldLogs = await _context.EmailLogs
                    .Where(e => e.Timestamp < cutoffDate)
                    .ToListAsync();

                if (oldLogs.Any())
                {
                    _context.EmailLogs.RemoveRange(oldLogs);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = $"Cleared {oldLogs.Count} email logs older than {daysToKeep} days.";
                }
                else
                {
                    TempData["InfoMessage"] = $"No email logs older than {daysToKeep} days found.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing old email logs");
                TempData["ErrorMessage"] = $"Error clearing old logs: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> ClearLogsByDate(string? cutoffDate)
        {
            try
            {
                _logger.LogInformation("ClearLogsByDate called with cutoffDate: '{CutoffDate}'", cutoffDate);

                if (string.IsNullOrWhiteSpace(cutoffDate))
                {
                    TempData["ErrorMessage"] = "No date was provided for cleanup.";
                    return RedirectToAction(nameof(Index));
                }

                if (!DateTime.TryParse(cutoffDate, out var parsedDate))
                {
                    TempData["ErrorMessage"] = $"Invalid date format: '{cutoffDate}'.";
                    return RedirectToAction(nameof(Index));
                }

                var cutoffDateUtc = DateTime.SpecifyKind(parsedDate, DateTimeKind.Utc);

                var oldLogs = await _context.EmailLogs
                    .Where(e => e.Timestamp < cutoffDateUtc)
                    .ToListAsync();

                if (oldLogs.Count > 0)
                {
                    var oldestLog = oldLogs.Min(e => e.Timestamp);

                    _context.EmailLogs.RemoveRange(oldLogs);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = $"Deleted {oldLogs.Count} email logs from {oldestLog:MM/dd/yyyy} to {parsedDate:MM/dd/yyyy}.";
                    _logger.LogInformation("Cleared {Count} email logs older than {Date}", oldLogs.Count, parsedDate);
                }
                else
                {
                    TempData["InfoMessage"] = $"No email logs found before {parsedDate:MM/dd/yyyy}.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing email logs by date");
                TempData["ErrorMessage"] = $"Error clearing logs: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> GetOldestLogDate()
        {
            try
            {
                var oldestLog = await _context.EmailLogs
                    .OrderBy(e => e.Timestamp)
                    .FirstOrDefaultAsync();

                if (oldestLog != null)
                {
                    return Json(new { oldestDate = oldestLog.Timestamp.ToString("yyyy-MM-dd") });
                }

                return Json(new { oldestDate = "" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching oldest log date");
                return Json(new { error = ex.Message });
            }
        }
    }
}
