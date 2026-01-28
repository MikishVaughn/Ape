using Ape.Models;
using Ape.Models.ViewModels;
using Ape.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ape.Controllers
{
    [Authorize(Roles = "Admin,Member")]
    public class GalleryController(
        IGalleryManagementService galleryService,
        ILogger<GalleryController> logger) : Controller
    {
        private readonly IGalleryManagementService _galleryService = galleryService;
        private readonly ILogger<GalleryController> _logger = logger;

        // ============================================================
        // Browse View
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Browse(int? categoryId, int page = 1, int pageSize = 24)
        {
            // Clamp pagination values
            if (page < 1) page = 1;
            if (pageSize < 12) pageSize = 12;
            if (pageSize > 48) pageSize = 48;

            var userAccessLevel = GetUserAccessLevel();
            var canManage = User.IsInRole("Admin");

            var viewModel = await _galleryService.BuildBrowseViewModelAsync(
                categoryId, userAccessLevel, canManage, page, pageSize);

            ViewData["Title"] = viewModel.CurrentCategoryId.HasValue
                ? $"Gallery - {viewModel.CurrentCategoryName}"
                : "Image Gallery";

            return View(viewModel);
        }

        // ============================================================
        // AJAX Endpoints
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> GetCategoryTree(int? selectedCategoryId)
        {
            var userAccessLevel = GetUserAccessLevel();
            var tree = await _galleryService.GetCategoryTreeAsync(userAccessLevel, selectedCategoryId);
            return Json(tree);
        }

        [HttpGet]
        public async Task<IActionResult> GetCategories(int? parentCategoryId)
        {
            var userAccessLevel = GetUserAccessLevel();
            var categories = await _galleryService.GetCategoriesAsync(parentCategoryId, userAccessLevel);
            return Json(categories);
        }

        // ============================================================
        // Category Management
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateCategory([FromBody] CreateGalleryCategoryModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Name))
            {
                return Json(new { success = false, message = "Category name is required." });
            }

            var result = await _galleryService.CreateCategoryAsync(model);
            return Json(new { success = result.Success, message = result.Message, categoryId = result.CategoryId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RenameCategory(int categoryId, string newName)
        {
            var result = await _galleryService.RenameCategoryAsync(categoryId, newName);
            return Json(new { success = result.Success, message = result.Message });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateCategoryAccessLevel(int categoryId, DocumentAccessLevel accessLevel)
        {
            var result = await _galleryService.UpdateCategoryAccessLevelAsync(categoryId, accessLevel);
            return Json(new { success = result.Success, message = result.Message });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateCategoryDescription(int categoryId, string? description)
        {
            var result = await _galleryService.UpdateCategoryDescriptionAsync(categoryId, description);
            return Json(new { success = result.Success, message = result.Message });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> MoveCategory(int categoryId, int? targetCategoryId)
        {
            var result = await _galleryService.MoveCategoryAsync(categoryId, targetCategoryId);
            return Json(new { success = result.Success, message = result.Message });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteCategory(int categoryId, bool deleteContents = false)
        {
            var result = await _galleryService.DeleteCategoryAsync(categoryId, deleteContents);
            return Json(new { success = result.Success, message = result.Message });
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CategoryHasContents(int categoryId)
        {
            var hasContents = await _galleryService.CategoryHasContentsAsync(categoryId);
            return Json(new { hasContents });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateCategorySortOrder(int[] categoryIds, int[] sortOrders)
        {
            var result = await _galleryService.UpdateCategorySortOrdersAsync(categoryIds, sortOrders);
            return Json(new { success = result.Success, message = result.Message });
        }

        // ============================================================
        // Image Management
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UploadImages(int categoryId, IList<IFormFile> files, string? description)
        {
            var userName = User.Identity?.Name ?? "Unknown";
            var result = await _galleryService.UploadImagesAsync(categoryId, files, description, userName);

            if (Request.Headers.XRequestedWith == "XMLHttpRequest")
            {
                return Json(new { success = result.Success, message = result.Message, uploadedCount = result.UploadedCount, failedCount = result.FailedCount });
            }

            if (result.Success)
                TempData["SuccessMessage"] = result.Message;
            else
                TempData["ErrorMessage"] = result.Message;

            return RedirectToAction(nameof(Browse), new { categoryId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UploadSingleImage(int categoryId, IFormFile file, string? description)
        {
            var userName = User.Identity?.Name ?? "Unknown";
            var result = await _galleryService.UploadSingleImageAsync(categoryId, file, description, userName);
            return Json(new { success = result.Success, message = result.Message, imageId = result.ImageId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RenameImage(int imageId, string newName)
        {
            var result = await _galleryService.RenameImageAsync(imageId, newName);
            return Json(new { success = result.Success, message = result.Message });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateImageDescription(int imageId, string? description)
        {
            var result = await _galleryService.UpdateImageDescriptionAsync(imageId, description);
            return Json(new { success = result.Success, message = result.Message });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> MoveImage(int imageId, int targetCategoryId)
        {
            var result = await _galleryService.MoveImageAsync(imageId, targetCategoryId);
            return Json(new { success = result.Success, message = result.Message });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> MoveImages(int[] imageIds, int targetCategoryId)
        {
            var result = await _galleryService.MoveImagesAsync(imageIds, targetCategoryId);
            return Json(new { success = result.Success, message = result.Message });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteImage(int imageId)
        {
            var result = await _galleryService.DeleteImageAsync(imageId);
            return Json(new { success = result.Success, message = result.Message });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateImageSortOrder(int[] imageIds, int[] sortOrders)
        {
            var result = await _galleryService.UpdateImageSortOrdersAsync(imageIds, sortOrders);
            return Json(new { success = result.Success, message = result.Message });
        }

        // ============================================================
        // Form-Based Fallbacks
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateCategoryForm(string categoryName, int? parentCategoryId, DocumentAccessLevel accessLevel, string? description)
        {
            var model = new CreateGalleryCategoryModel
            {
                Name = categoryName,
                ParentCategoryId = parentCategoryId,
                AccessLevel = accessLevel,
                Description = description
            };

            var result = await _galleryService.CreateCategoryAsync(model);

            if (result.Success)
                TempData["SuccessMessage"] = result.Message;
            else
                TempData["ErrorMessage"] = result.Message;

            return RedirectToAction(nameof(Browse), new { categoryId = parentCategoryId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteCategoryForm(int categoryId, int? parentCategoryId, bool deleteContents = false)
        {
            var result = await _galleryService.DeleteCategoryAsync(categoryId, deleteContents);

            if (result.Success)
                TempData["SuccessMessage"] = result.Message;
            else
                TempData["ErrorMessage"] = result.Message;

            return RedirectToAction(nameof(Browse), new { categoryId = parentCategoryId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteImageForm(int imageId, int categoryId)
        {
            var result = await _galleryService.DeleteImageAsync(imageId);

            if (result.Success)
                TempData["SuccessMessage"] = result.Message;
            else
                TempData["ErrorMessage"] = result.Message;

            return RedirectToAction(nameof(Browse), new { categoryId });
        }

        // ============================================================
        // Helpers
        // ============================================================

        private DocumentAccessLevel GetUserAccessLevel()
        {
            var roles = User.Claims
                .Where(c => c.Type == System.Security.Claims.ClaimTypes.Role)
                .Select(c => c.Value);

            return _galleryService.GetUserAccessLevel(roles);
        }
    }
}
