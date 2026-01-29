using Ape.Models;
using Ape.Models.ViewModels;
using Ape.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ape.Controllers
{
    /// <summary>
    /// Controller for document library - browsing and managing categorized PDF files
    /// </summary>
    [Authorize(Roles = "Admin,Member")]
    public class DocumentController(
        IDocumentManagementService documentService,
        ILogger<DocumentController> logger) : Controller
    {
        private readonly IDocumentManagementService _documentService = documentService;
        private readonly ILogger<DocumentController> _logger = logger;

        // ============================================================
        // Explorer Views
        // ============================================================

        /// <summary>
        /// Main document explorer view
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Browse(int? folderId)
        {
            var userAccessLevel = GetUserAccessLevel();
            var canManage = User.IsInRole("Admin") || User.IsInRole("Manager");

            var viewModel = await _documentService.BuildExplorerViewModelAsync(
                folderId, userAccessLevel, canManage, canManage);

            ViewData["Title"] = viewModel.CurrentFolderId.HasValue
                ? $"Documents - {viewModel.CurrentFolderName}"
                : "Document Library";

            return View(viewModel);
        }

        /// <summary>
        /// AJAX endpoint to get folder tree for sidebar
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetFolderTree(int? selectedFolderId)
        {
            var userAccessLevel = GetUserAccessLevel();
            var tree = await _documentService.GetFolderTreeAsync(userAccessLevel, selectedFolderId);
            return Json(tree);
        }

        /// <summary>
        /// AJAX endpoint to get folders at a specific level
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetFolders(int? parentFolderId)
        {
            var userAccessLevel = GetUserAccessLevel();
            var folders = await _documentService.GetFoldersAsync(parentFolderId, userAccessLevel);
            return Json(folders);
        }

        /// <summary>
        /// AJAX endpoint to get files in a folder
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetFiles(int folderId)
        {
            var userAccessLevel = GetUserAccessLevel();
            var files = await _documentService.GetFilesAsync(folderId, userAccessLevel);
            return Json(files);
        }

        // ============================================================
        // Folder Management
        // ============================================================

        /// <summary>
        /// Create a new folder
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> CreateFolder([FromBody] CreateFolderModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Name))
            {
                return Json(new { success = false, message = "Folder name is required." });
            }

            var result = await _documentService.CreateFolderAsync(model);
            return Json(new { success = result.Success, message = result.Message, folderId = result.FolderId });
        }

        /// <summary>
        /// Rename a folder
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> RenameFolder(int folderId, string newName)
        {
            var result = await _documentService.RenameFolderAsync(folderId, newName);
            return Json(new { success = result.Success, message = result.Message });
        }

        /// <summary>
        /// Update folder access level
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> UpdateFolderAccessLevel(int folderId, DocumentAccessLevel accessLevel)
        {
            var result = await _documentService.UpdateFolderAccessLevelAsync(folderId, accessLevel);
            return Json(new { success = result.Success, message = result.Message });
        }

        /// <summary>
        /// Move a folder to a new location
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> MoveFolder(int folderId, int? targetFolderId)
        {
            var result = await _documentService.MoveFolderAsync(folderId, targetFolderId);
            return Json(new { success = result.Success, message = result.Message });
        }

        /// <summary>
        /// Delete a folder
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> DeleteFolder(int folderId, bool deleteContents = false)
        {
            var result = await _documentService.DeleteFolderAsync(folderId, deleteContents);
            return Json(new { success = result.Success, message = result.Message });
        }

        /// <summary>
        /// Check if folder has contents (for delete confirmation)
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> FolderHasContents(int folderId)
        {
            var hasContents = await _documentService.FolderHasContentsAsync(folderId);
            return Json(new { hasContents });
        }

        /// <summary>
        /// Update folder sort orders after drag-drop reordering
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> UpdateFolderSortOrder(int[] folderIds, int[] sortOrders)
        {
            var result = await _documentService.UpdateFolderSortOrdersAsync(folderIds, sortOrders);
            return Json(new { success = result.Success, message = result.Message });
        }

        // ============================================================
        // File Management
        // ============================================================

        /// <summary>
        /// Upload a file to a folder
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> UploadFile(int folderId, IFormFile file, string? description)
        {
            var userName = User.Identity?.Name ?? "Unknown";
            var result = await _documentService.UploadFileAsync(folderId, file, description, userName);

            if (Request.Headers.XRequestedWith == "XMLHttpRequest")
            {
                return Json(new { success = result.Success, message = result.Message, fileId = result.FileId });
            }

            if (result.Success)
                TempData["SuccessMessage"] = result.Message;
            else
                TempData["ErrorMessage"] = result.Message;

            return RedirectToAction(nameof(Browse), new { folderId });
        }

        /// <summary>
        /// Rename a file
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> RenameFile(int fileId, string newFileName)
        {
            var result = await _documentService.RenameFileAsync(fileId, newFileName);
            return Json(new { success = result.Success, message = result.Message });
        }

        /// <summary>
        /// Update file description
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> UpdateFileDescription(int fileId, string? description)
        {
            var result = await _documentService.UpdateFileDescriptionAsync(fileId, description);
            return Json(new { success = result.Success, message = result.Message });
        }

        /// <summary>
        /// Move a file to a different folder
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> MoveFile(int fileId, int targetFolderId)
        {
            var result = await _documentService.MoveFileAsync(fileId, targetFolderId);
            return Json(new { success = result.Success, message = result.Message });
        }

        /// <summary>
        /// Move multiple files to a different folder
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> MoveFiles(int[] fileIds, int targetFolderId)
        {
            var result = await _documentService.MoveFilesAsync(fileIds, targetFolderId);
            return Json(new { success = result.Success, message = result.Message });
        }

        /// <summary>
        /// Delete a file
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> DeleteFile(int fileId)
        {
            var result = await _documentService.DeleteFileAsync(fileId);
            return Json(new { success = result.Success, message = result.Message });
        }

        /// <summary>
        /// Update file sort orders after drag-drop reordering
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> UpdateFileSortOrder(int[] fileIds, int[] sortOrders)
        {
            var result = await _documentService.UpdateFileSortOrdersAsync(fileIds, sortOrders);
            return Json(new { success = result.Success, message = result.Message });
        }

        /// <summary>
        /// Download a file
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Download(int fileId)
        {
            var userAccessLevel = GetUserAccessLevel();
            var filePath = await _documentService.GetFilePathAsync(fileId, userAccessLevel);

            if (filePath == null)
            {
                TempData["ErrorMessage"] = "File not found or access denied.";
                return RedirectToAction(nameof(Browse));
            }

            var fileName = Path.GetFileName(filePath);
            var contentType = "application/pdf";

            return PhysicalFile(filePath, contentType, fileName);
        }

        /// <summary>
        /// Serve raw PDF inline (used by PDF.js viewer)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> View(int fileId)
        {
            var userAccessLevel = GetUserAccessLevel();
            var filePath = await _documentService.GetFilePathAsync(fileId, userAccessLevel);

            if (filePath == null)
            {
                TempData["ErrorMessage"] = "File not found or access denied.";
                return RedirectToAction(nameof(Browse));
            }

            var contentType = "application/pdf";
            return PhysicalFile(filePath, contentType);
        }

        /// <summary>
        /// View a PDF using the dedicated PDF viewer page
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ViewPdf(int fileId)
        {
            var userAccessLevel = GetUserAccessLevel();
            var filePath = await _documentService.GetFilePathAsync(fileId, userAccessLevel);

            if (filePath == null)
            {
                TempData["ErrorMessage"] = "File not found or access denied.";
                return RedirectToAction(nameof(Browse));
            }

            ViewData["FileId"] = fileId;
            ViewData["FileName"] = Path.GetFileName(filePath);
            return View();
        }

        // ============================================================
        // Form-based endpoints (for non-AJAX operations)
        // ============================================================

        /// <summary>
        /// Form-based folder creation (redirects back to browse)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> CreateFolderForm(string folderName, int? parentFolderId, DocumentAccessLevel accessLevel)
        {
            var model = new CreateFolderModel
            {
                Name = folderName,
                ParentFolderId = parentFolderId,
                AccessLevel = accessLevel
            };

            var result = await _documentService.CreateFolderAsync(model);

            if (result.Success)
                TempData["SuccessMessage"] = result.Message;
            else
                TempData["ErrorMessage"] = result.Message;

            return RedirectToAction(nameof(Browse), new { folderId = parentFolderId });
        }

        /// <summary>
        /// Form-based folder deletion (redirects back to browse)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> DeleteFolderForm(int folderId, int? parentFolderId, bool deleteContents = false)
        {
            var result = await _documentService.DeleteFolderAsync(folderId, deleteContents);

            if (result.Success)
                TempData["SuccessMessage"] = result.Message;
            else
                TempData["ErrorMessage"] = result.Message;

            return RedirectToAction(nameof(Browse), new { folderId = parentFolderId });
        }

        /// <summary>
        /// Form-based file deletion (redirects back to browse)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> DeleteFileForm(int fileId, int folderId)
        {
            var result = await _documentService.DeleteFileAsync(fileId);

            if (result.Success)
                TempData["SuccessMessage"] = result.Message;
            else
                TempData["ErrorMessage"] = result.Message;

            return RedirectToAction(nameof(Browse), new { folderId });
        }

        // ============================================================
        // Helper Methods
        // ============================================================

        private DocumentAccessLevel GetUserAccessLevel()
        {
            var roles = User.Claims
                .Where(c => c.Type == System.Security.Claims.ClaimTypes.Role)
                .Select(c => c.Value);

            return _documentService.GetUserAccessLevel(roles);
        }
    }
}
