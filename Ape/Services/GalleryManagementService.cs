using Ape.Data;
using Ape.Models;
using Ape.Models.ViewModels;
using Microsoft.EntityFrameworkCore;
using IOFile = System.IO.File;

namespace Ape.Services
{
    // Internal record for category tree building
    internal record GalleryCategoryData(int CategoryID, string CategoryName, int? ParentCategoryID, DocumentAccessLevel AccessLevel);

    public class GalleryManagementService(
        ApplicationDbContext context,
        IWebHostEnvironment environment,
        IImageOptimizationService imageOptimization,
        ILogger<GalleryManagementService> logger) : IGalleryManagementService
    {
        private readonly ApplicationDbContext _context = context;
        private readonly IImageOptimizationService _imageOptimization = imageOptimization;
        private readonly ILogger<GalleryManagementService> _logger = logger;
        private readonly string _galleriesBasePath = Path.Combine(environment.WebRootPath, "galleries");

        // ============================================================
        // Category Operations
        // ============================================================

        public async Task<List<GalleryCategoryViewModel>> GetCategoriesAsync(int? parentCategoryId, DocumentAccessLevel userAccessLevel)
        {
            var categories = await _context.GalleryCategories
                .Where(c => c.ParentCategoryID == parentCategoryId && c.AccessLevel <= userAccessLevel)
                .OrderBy(c => c.SortOrder)
                .ThenBy(c => c.CategoryName)
                .Select(c => new GalleryCategoryViewModel
                {
                    CategoryId = c.CategoryID,
                    Name = c.CategoryName,
                    ParentCategoryId = c.ParentCategoryID,
                    SortOrder = c.SortOrder,
                    AccessLevel = c.AccessLevel,
                    HasChildren = c.ChildCategories.Any() || c.GalleryImages.Any(),
                    ImageCount = c.GalleryImages.Count,
                    Description = c.Description
                })
                .ToListAsync();

            return categories;
        }

        public async Task<GalleryCategoryViewModel?> GetCategoryAsync(int categoryId, DocumentAccessLevel userAccessLevel)
        {
            var category = await _context.GalleryCategories
                .Where(c => c.CategoryID == categoryId && c.AccessLevel <= userAccessLevel)
                .Select(c => new GalleryCategoryViewModel
                {
                    CategoryId = c.CategoryID,
                    Name = c.CategoryName,
                    ParentCategoryId = c.ParentCategoryID,
                    SortOrder = c.SortOrder,
                    AccessLevel = c.AccessLevel,
                    HasChildren = c.ChildCategories.Any() || c.GalleryImages.Any(),
                    ImageCount = c.GalleryImages.Count,
                    Description = c.Description
                })
                .FirstOrDefaultAsync();

            return category;
        }

        public async Task<List<GalleryCategoryTreeNode>> GetCategoryTreeAsync(DocumentAccessLevel userAccessLevel, int? selectedCategoryId = null)
        {
            var allCategories = await _context.GalleryCategories
                .Where(c => c.AccessLevel <= userAccessLevel)
                .OrderBy(c => c.SortOrder)
                .ThenBy(c => c.CategoryName)
                .Select(c => new GalleryCategoryData(c.CategoryID, c.CategoryName, c.ParentCategoryID, c.AccessLevel))
                .ToListAsync();

            var rootNodes = allCategories
                .Where(c => c.ParentCategoryID == null)
                .Select(c => BuildTreeNode(c, allCategories, selectedCategoryId))
                .ToList();

            return rootNodes;
        }

        private static GalleryCategoryTreeNode BuildTreeNode(
            GalleryCategoryData category,
            List<GalleryCategoryData> allCategories,
            int? selectedCategoryId)
        {
            var node = new GalleryCategoryTreeNode
            {
                CategoryId = category.CategoryID,
                Name = category.CategoryName,
                ParentCategoryId = category.ParentCategoryID,
                AccessLevel = category.AccessLevel,
                IsSelected = category.CategoryID == selectedCategoryId,
                Children = []
            };

            foreach (var child in allCategories.Where(c => c.ParentCategoryID == category.CategoryID))
            {
                var childNode = BuildTreeNode(child, allCategories, selectedCategoryId);
                node.Children.Add(childNode);

                if (childNode.IsSelected || childNode.IsExpanded)
                {
                    node.IsExpanded = true;
                }
            }

            return node;
        }

        public async Task<List<GalleryBreadcrumbItem>> GetBreadcrumbsAsync(int? categoryId)
        {
            var breadcrumbs = new List<GalleryBreadcrumbItem>
            {
                new() { CategoryId = null, Name = "Image Gallery", IsCurrent = categoryId == null }
            };

            if (categoryId == null) return breadcrumbs;

            var path = new List<GalleryBreadcrumbItem>();
            var currentId = categoryId;

            while (currentId.HasValue)
            {
                var category = await _context.GalleryCategories
                    .Where(c => c.CategoryID == currentId.Value)
                    .Select(c => new { c.CategoryID, c.CategoryName, c.ParentCategoryID })
                    .FirstOrDefaultAsync();

                if (category == null) break;

                path.Insert(0, new GalleryBreadcrumbItem
                {
                    CategoryId = category.CategoryID,
                    Name = category.CategoryName,
                    IsCurrent = category.CategoryID == categoryId
                });

                currentId = category.ParentCategoryID;
            }

            breadcrumbs.AddRange(path);
            return breadcrumbs;
        }

        public async Task<GalleryCategoryOperationResult> CreateCategoryAsync(CreateGalleryCategoryModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Name))
            {
                return GalleryCategoryOperationResult.Failed("Category name is required.");
            }

            if (model.ParentCategoryId.HasValue)
            {
                var parentExists = await _context.GalleryCategories
                    .AnyAsync(c => c.CategoryID == model.ParentCategoryId.Value);
                if (!parentExists)
                {
                    return GalleryCategoryOperationResult.Failed("Parent category not found.");
                }
            }

            var maxSortOrder = await _context.GalleryCategories
                .Where(c => c.ParentCategoryID == model.ParentCategoryId)
                .MaxAsync(c => (int?)c.SortOrder) ?? 0;

            var newCategory = new GalleryCategory
            {
                CategoryName = model.Name.Trim(),
                ParentCategoryID = model.ParentCategoryId,
                AccessLevel = model.AccessLevel,
                Description = model.Description?.Trim(),
                SortOrder = maxSortOrder + 1
            };

            _context.GalleryCategories.Add(newCategory);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created gallery category '{CategoryName}' (ID: {CategoryId}) with access level {AccessLevel}",
                newCategory.CategoryName, newCategory.CategoryID, newCategory.AccessLevel);

            return GalleryCategoryOperationResult.Succeeded(newCategory.CategoryID, $"Category '{newCategory.CategoryName}' created successfully.");
        }

        public async Task<GalleryCategoryOperationResult> RenameCategoryAsync(int categoryId, string newName)
        {
            if (string.IsNullOrWhiteSpace(newName))
            {
                return GalleryCategoryOperationResult.Failed("Category name is required.");
            }

            var category = await _context.GalleryCategories.FindAsync(categoryId);
            if (category == null)
            {
                return GalleryCategoryOperationResult.Failed("Category not found.");
            }

            var oldName = category.CategoryName;
            category.CategoryName = newName.Trim();
            await _context.SaveChangesAsync();

            _logger.LogInformation("Renamed gallery category from '{OldName}' to '{NewName}' (ID: {CategoryId})",
                oldName, newName, categoryId);

            return GalleryCategoryOperationResult.Succeeded(categoryId, $"Category renamed to '{newName}'.");
        }

        public async Task<GalleryCategoryOperationResult> UpdateCategoryAccessLevelAsync(int categoryId, DocumentAccessLevel accessLevel)
        {
            var category = await _context.GalleryCategories.FindAsync(categoryId);
            if (category == null)
            {
                return GalleryCategoryOperationResult.Failed("Category not found.");
            }

            category.AccessLevel = accessLevel;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated gallery category '{CategoryName}' (ID: {CategoryId}) access level to {AccessLevel}",
                category.CategoryName, categoryId, accessLevel);

            return GalleryCategoryOperationResult.Succeeded(categoryId, $"Category access level updated to {accessLevel}.");
        }

        public async Task<GalleryCategoryOperationResult> UpdateCategoryDescriptionAsync(int categoryId, string? description)
        {
            var category = await _context.GalleryCategories.FindAsync(categoryId);
            if (category == null)
            {
                return GalleryCategoryOperationResult.Failed("Category not found.");
            }

            category.Description = description?.Trim();
            await _context.SaveChangesAsync();

            return GalleryCategoryOperationResult.Succeeded(categoryId, "Category description updated.");
        }

        public async Task<GalleryCategoryOperationResult> MoveCategoryAsync(int categoryId, int? newParentCategoryId)
        {
            var category = await _context.GalleryCategories.FindAsync(categoryId);
            if (category == null)
            {
                return GalleryCategoryOperationResult.Failed("Category not found.");
            }

            if (newParentCategoryId.HasValue)
            {
                if (newParentCategoryId.Value == categoryId)
                {
                    return GalleryCategoryOperationResult.Failed("Cannot move a category into itself.");
                }

                if (await IsDescendantOfAsync(newParentCategoryId.Value, categoryId))
                {
                    return GalleryCategoryOperationResult.Failed("Cannot move a category into one of its subcategories.");
                }

                var parentExists = await _context.GalleryCategories.AnyAsync(c => c.CategoryID == newParentCategoryId.Value);
                if (!parentExists)
                {
                    return GalleryCategoryOperationResult.Failed("Target category not found.");
                }
            }

            var maxSortOrder = await _context.GalleryCategories
                .Where(c => c.ParentCategoryID == newParentCategoryId)
                .MaxAsync(c => (int?)c.SortOrder) ?? 0;

            category.ParentCategoryID = newParentCategoryId;
            category.SortOrder = maxSortOrder + 1;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Moved gallery category '{CategoryName}' (ID: {CategoryId}) to parent {ParentId}",
                category.CategoryName, categoryId, newParentCategoryId);

            return GalleryCategoryOperationResult.Succeeded(categoryId, $"Category '{category.CategoryName}' moved successfully.");
        }

        private async Task<bool> IsDescendantOfAsync(int categoryId, int potentialAncestorId)
        {
            var currentId = categoryId;
            var visited = new HashSet<int>();

            while (true)
            {
                if (visited.Contains(currentId)) break;
                visited.Add(currentId);

                var category = await _context.GalleryCategories
                    .Where(c => c.CategoryID == currentId)
                    .Select(c => new { c.ParentCategoryID })
                    .FirstOrDefaultAsync();

                if (category?.ParentCategoryID == null) break;
                if (category.ParentCategoryID == potentialAncestorId) return true;

                currentId = category.ParentCategoryID.Value;
            }

            return false;
        }

        public async Task<GalleryCategoryOperationResult> DeleteCategoryAsync(int categoryId, bool deleteContents = false)
        {
            var category = await _context.GalleryCategories
                .Include(c => c.GalleryImages)
                .Include(c => c.ChildCategories)
                .FirstOrDefaultAsync(c => c.CategoryID == categoryId);

            if (category == null)
            {
                return GalleryCategoryOperationResult.Failed("Category not found.");
            }

            var hasChildren = category.ChildCategories.Any();
            var hasImages = category.GalleryImages.Any();

            if ((hasChildren || hasImages) && !deleteContents)
            {
                return GalleryCategoryOperationResult.Failed(
                    "Category is not empty. Move or delete contents first, or confirm deletion of all contents.");
            }

            if (deleteContents)
            {
                foreach (var childCategory in category.ChildCategories.ToList())
                {
                    var result = await DeleteCategoryAsync(childCategory.CategoryID, true);
                    if (!result.Success)
                    {
                        return result;
                    }
                }

                foreach (var image in category.GalleryImages.ToList())
                {
                    DeletePhysicalImage(image.FileName);
                    _context.GalleryImages.Remove(image);
                }
            }

            _context.GalleryCategories.Remove(category);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted gallery category '{CategoryName}' (ID: {CategoryId})", category.CategoryName, categoryId);

            return GalleryCategoryOperationResult.Succeeded(categoryId, $"Category '{category.CategoryName}' deleted successfully.");
        }

        public async Task<bool> CategoryHasContentsAsync(int categoryId)
        {
            return await _context.GalleryCategories.AnyAsync(c => c.ParentCategoryID == categoryId)
                || await _context.GalleryImages.AnyAsync(i => i.CategoryID == categoryId);
        }

        public async Task<GalleryCategoryOperationResult> UpdateCategorySortOrdersAsync(int[] categoryIds, int[] sortOrders)
        {
            if (categoryIds.Length != sortOrders.Length)
            {
                return GalleryCategoryOperationResult.Failed("Invalid sort order data.");
            }

            for (int i = 0; i < categoryIds.Length; i++)
            {
                var category = await _context.GalleryCategories.FindAsync(categoryIds[i]);
                if (category != null)
                {
                    category.SortOrder = sortOrders[i];
                }
            }

            await _context.SaveChangesAsync();
            return GalleryCategoryOperationResult.Succeeded(0, "Category order updated successfully.");
        }

        // ============================================================
        // Image Operations
        // ============================================================

        public async Task<(List<GalleryImageViewModel> Images, int TotalCount)> GetImagesAsync(
            int? categoryId, DocumentAccessLevel userAccessLevel, int page = 1, int pageSize = 24)
        {
            if (categoryId == null) return ([], 0);

            var category = await _context.GalleryCategories.FindAsync(categoryId);
            if (category == null || category.AccessLevel > userAccessLevel) return ([], 0);

            var query = _context.GalleryImages
                .Where(i => i.CategoryID == categoryId)
                .OrderBy(i => i.SortOrder)
                .ThenBy(i => i.OriginalFileName);

            var totalCount = await query.CountAsync();

            var images = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(i => new GalleryImageViewModel
                {
                    ImageId = i.ImageID,
                    FileName = i.FileName,
                    OriginalFileName = i.OriginalFileName,
                    Description = i.Description,
                    SortOrder = i.SortOrder,
                    CategoryId = i.CategoryID,
                    CategoryName = i.GalleryCategory != null ? i.GalleryCategory.CategoryName : string.Empty,
                    UploadedDate = i.UploadedDate,
                    UploadedBy = i.UploadedBy
                })
                .ToListAsync();

            return (images, totalCount);
        }

        public async Task<GalleryImageViewModel?> GetImageAsync(int imageId, DocumentAccessLevel userAccessLevel)
        {
            var image = await _context.GalleryImages
                .Include(i => i.GalleryCategory)
                .Where(i => i.ImageID == imageId && i.GalleryCategory != null && i.GalleryCategory.AccessLevel <= userAccessLevel)
                .Select(i => new GalleryImageViewModel
                {
                    ImageId = i.ImageID,
                    FileName = i.FileName,
                    OriginalFileName = i.OriginalFileName,
                    Description = i.Description,
                    SortOrder = i.SortOrder,
                    CategoryId = i.CategoryID,
                    CategoryName = i.GalleryCategory != null ? i.GalleryCategory.CategoryName : string.Empty,
                    UploadedDate = i.UploadedDate,
                    UploadedBy = i.UploadedBy
                })
                .FirstOrDefaultAsync();

            return image;
        }

        public async Task<GalleryImageOperationResult> UploadImagesAsync(int categoryId, IList<IFormFile> files, string? description, string uploadedBy)
        {
            var category = await _context.GalleryCategories.FindAsync(categoryId);
            if (category == null)
            {
                return GalleryImageOperationResult.Failed("Category not found.");
            }

            if (files == null || files.Count == 0)
            {
                return GalleryImageOperationResult.Failed("No files selected.");
            }

            // Ensure galleries directory exists
            if (!Directory.Exists(_galleriesBasePath))
            {
                Directory.CreateDirectory(_galleriesBasePath);
            }

            var maxSortOrder = await _context.GalleryImages
                .Where(i => i.CategoryID == categoryId)
                .MaxAsync(i => (int?)i.SortOrder) ?? 0;

            int uploadedCount = 0;
            int failedCount = 0;

            foreach (var file in files)
            {
                try
                {
                    if (!_imageOptimization.IsValidImageFormat(file))
                    {
                        _logger.LogWarning("Skipped invalid image format: {FileName}", file.FileName);
                        failedCount++;
                        continue;
                    }

                    // Generate unique filename with GUID
                    var guid = Guid.NewGuid().ToString("N");
                    var fullFileName = $"{guid}.jpg";
                    var thumbFileName = $"{guid}_thumb.jpg";

                    // Optimize full-size image (1200px max)
                    using var fullStream = file.OpenReadStream();
                    var optimizedBytes = await _imageOptimization.OptimizeImageAsync(
                        fullStream, _imageOptimization.GetRecommendedMaxWidth(ImageType.Gallery));

                    var fullPath = Path.Combine(_galleriesBasePath, fullFileName);
                    await IOFile.WriteAllBytesAsync(fullPath, optimizedBytes);

                    // Generate thumbnail (400px max)
                    using var thumbStream = new MemoryStream(optimizedBytes);
                    var thumbnailBytes = await _imageOptimization.GenerateThumbnailAsync(
                        thumbStream, _imageOptimization.GetRecommendedMaxWidth(ImageType.Thumbnail));

                    var thumbPath = Path.Combine(_galleriesBasePath, thumbFileName);
                    await IOFile.WriteAllBytesAsync(thumbPath, thumbnailBytes);

                    // Create DB record
                    var newImage = new GalleryImage
                    {
                        CategoryID = categoryId,
                        FileName = fullFileName,
                        OriginalFileName = Path.GetFileName(file.FileName),
                        Description = description?.Trim(),
                        SortOrder = ++maxSortOrder,
                        UploadedDate = DateTime.UtcNow,
                        UploadedBy = uploadedBy
                    };

                    _context.GalleryImages.Add(newImage);
                    uploadedCount++;

                    _logger.LogInformation("Uploaded gallery image '{OriginalName}' as '{FileName}' to category '{CategoryName}'",
                        file.FileName, fullFileName, category.CategoryName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error uploading image '{FileName}'", file.FileName);
                    failedCount++;
                }
            }

            await _context.SaveChangesAsync();

            var message = uploadedCount > 0
                ? $"{uploadedCount} image(s) uploaded to '{category.CategoryName}'."
                : "No images were uploaded.";

            if (failedCount > 0)
            {
                message += $" {failedCount} file(s) failed.";
            }

            return GalleryImageOperationResult.UploadResult(uploadedCount, failedCount, message);
        }

        public async Task<GalleryImageOperationResult> UploadSingleImageAsync(int categoryId, IFormFile file, string? description, string uploadedBy)
        {
            var category = await _context.GalleryCategories.FindAsync(categoryId);
            if (category == null)
            {
                return GalleryImageOperationResult.Failed("Category not found.");
            }

            if (file == null || file.Length == 0)
            {
                return GalleryImageOperationResult.Failed("No file provided.");
            }

            if (!_imageOptimization.IsValidImageFormat(file))
            {
                return GalleryImageOperationResult.Failed($"Invalid image format: {file.FileName}");
            }

            // Ensure galleries directory exists
            if (!Directory.Exists(_galleriesBasePath))
            {
                Directory.CreateDirectory(_galleriesBasePath);
            }

            var maxSortOrder = await _context.GalleryImages
                .Where(i => i.CategoryID == categoryId)
                .MaxAsync(i => (int?)i.SortOrder) ?? 0;

            // Generate unique filename with GUID
            var guid = Guid.NewGuid().ToString("N");
            var fullFileName = $"{guid}.jpg";
            var thumbFileName = $"{guid}_thumb.jpg";

            // Optimize/verify full-size image (client already resized to ~1920x1080)
            using var fullStream = file.OpenReadStream();
            var optimizedBytes = await _imageOptimization.OptimizeImageAsync(
                fullStream, _imageOptimization.GetRecommendedMaxWidth(ImageType.Gallery));

            var fullPath = Path.Combine(_galleriesBasePath, fullFileName);
            await IOFile.WriteAllBytesAsync(fullPath, optimizedBytes);

            // Generate thumbnail (400px max)
            using var thumbStream = new MemoryStream(optimizedBytes);
            var thumbnailBytes = await _imageOptimization.GenerateThumbnailAsync(
                thumbStream, _imageOptimization.GetRecommendedMaxWidth(ImageType.Thumbnail));

            var thumbPath = Path.Combine(_galleriesBasePath, thumbFileName);
            await IOFile.WriteAllBytesAsync(thumbPath, thumbnailBytes);

            // Create DB record and save immediately (per-file commit)
            var newImage = new GalleryImage
            {
                CategoryID = categoryId,
                FileName = fullFileName,
                OriginalFileName = Path.GetFileName(file.FileName),
                Description = description?.Trim(),
                SortOrder = maxSortOrder + 1,
                UploadedDate = DateTime.UtcNow,
                UploadedBy = uploadedBy
            };

            _context.GalleryImages.Add(newImage);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Uploaded gallery image '{OriginalName}' as '{FileName}' to category '{CategoryName}'",
                file.FileName, fullFileName, category.CategoryName);

            return GalleryImageOperationResult.Succeeded(newImage.ImageID, $"Image '{file.FileName}' uploaded successfully.");
        }

        public async Task<GalleryImageOperationResult> RenameImageAsync(int imageId, string newOriginalName)
        {
            if (string.IsNullOrWhiteSpace(newOriginalName))
            {
                return GalleryImageOperationResult.Failed("Image name is required.");
            }

            var image = await _context.GalleryImages.FindAsync(imageId);
            if (image == null)
            {
                return GalleryImageOperationResult.Failed("Image not found.");
            }

            image.OriginalFileName = newOriginalName.Trim();
            await _context.SaveChangesAsync();

            _logger.LogInformation("Renamed gallery image display name to '{NewName}' (ID: {ImageId})",
                newOriginalName, imageId);

            return GalleryImageOperationResult.Succeeded(imageId, $"Image renamed to '{newOriginalName}'.");
        }

        public async Task<GalleryImageOperationResult> UpdateImageDescriptionAsync(int imageId, string? description)
        {
            var image = await _context.GalleryImages.FindAsync(imageId);
            if (image == null)
            {
                return GalleryImageOperationResult.Failed("Image not found.");
            }

            image.Description = description?.Trim();
            await _context.SaveChangesAsync();

            return GalleryImageOperationResult.Succeeded(imageId, "Image description updated.");
        }

        public async Task<GalleryImageOperationResult> MoveImageAsync(int imageId, int targetCategoryId)
        {
            var image = await _context.GalleryImages.Include(i => i.GalleryCategory).FirstOrDefaultAsync(i => i.ImageID == imageId);
            if (image == null)
            {
                return GalleryImageOperationResult.Failed("Image not found.");
            }

            var targetCategory = await _context.GalleryCategories.FindAsync(targetCategoryId);
            if (targetCategory == null)
            {
                return GalleryImageOperationResult.Failed("Target category not found.");
            }

            if (image.CategoryID == targetCategoryId)
            {
                return GalleryImageOperationResult.Succeeded(imageId, "Image is already in the target category.");
            }

            var maxSortOrder = await _context.GalleryImages
                .Where(i => i.CategoryID == targetCategoryId)
                .MaxAsync(i => (int?)i.SortOrder) ?? 0;

            var oldCategoryName = image.GalleryCategory?.CategoryName ?? "Unknown";
            image.CategoryID = targetCategoryId;
            image.SortOrder = maxSortOrder + 1;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Moved gallery image '{FileName}' from '{OldCategory}' to '{NewCategory}'",
                image.OriginalFileName, oldCategoryName, targetCategory.CategoryName);

            return GalleryImageOperationResult.Succeeded(imageId, $"Image moved to '{targetCategory.CategoryName}'.");
        }

        public async Task<GalleryImageOperationResult> MoveImagesAsync(int[] imageIds, int targetCategoryId)
        {
            if (imageIds == null || imageIds.Length == 0)
            {
                return GalleryImageOperationResult.Failed("No images selected.");
            }

            var targetCategory = await _context.GalleryCategories.FindAsync(targetCategoryId);
            if (targetCategory == null)
            {
                return GalleryImageOperationResult.Failed("Target category not found.");
            }

            var images = await _context.GalleryImages
                .Where(i => imageIds.Contains(i.ImageID))
                .ToListAsync();

            if (images.Count == 0)
            {
                return GalleryImageOperationResult.Failed("No valid images found.");
            }

            var maxSortOrder = await _context.GalleryImages
                .Where(i => i.CategoryID == targetCategoryId)
                .MaxAsync(i => (int?)i.SortOrder) ?? 0;

            int movedCount = 0;
            foreach (var image in images)
            {
                if (image.CategoryID != targetCategoryId)
                {
                    image.CategoryID = targetCategoryId;
                    image.SortOrder = ++maxSortOrder;
                    movedCount++;
                }
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Moved {Count} gallery images to category '{CategoryName}'",
                movedCount, targetCategory.CategoryName);

            return GalleryImageOperationResult.UploadResult(movedCount, 0,
                $"{movedCount} image(s) moved to '{targetCategory.CategoryName}'.");
        }

        public async Task<GalleryImageOperationResult> DeleteImageAsync(int imageId)
        {
            var image = await _context.GalleryImages.FindAsync(imageId);
            if (image == null)
            {
                return GalleryImageOperationResult.Failed("Image not found.");
            }

            var fileName = image.FileName;
            var originalName = image.OriginalFileName;

            _context.GalleryImages.Remove(image);
            await _context.SaveChangesAsync();

            DeletePhysicalImage(fileName);

            _logger.LogInformation("Deleted gallery image '{OriginalName}' ({FileName}) (ID: {ImageId})",
                originalName, fileName, imageId);

            return GalleryImageOperationResult.Succeeded(imageId, $"Image '{originalName}' deleted successfully.");
        }

        public async Task<GalleryImageOperationResult> DeleteImagesAsync(int[] imageIds)
        {
            if (imageIds == null || imageIds.Length == 0)
            {
                return GalleryImageOperationResult.Failed("No images specified.");
            }

            var images = await _context.GalleryImages
                .Where(i => imageIds.Contains(i.ImageID))
                .ToListAsync();

            if (!images.Any())
            {
                return GalleryImageOperationResult.Failed("No images found.");
            }

            int deletedCount = 0;
            foreach (var image in images)
            {
                var fileName = image.FileName;
                _context.GalleryImages.Remove(image);
                DeletePhysicalImage(fileName);
                deletedCount++;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Bulk deleted {Count} gallery images", deletedCount);

            return GalleryImageOperationResult.UploadResult(deletedCount, 0,
                $"{deletedCount} image(s) deleted successfully.");
        }

        public async Task<GalleryImageOperationResult> UpdateImageSortOrdersAsync(int[] imageIds, int[] sortOrders)
        {
            if (imageIds.Length != sortOrders.Length)
            {
                return GalleryImageOperationResult.Failed("Invalid sort order data.");
            }

            for (int i = 0; i < imageIds.Length; i++)
            {
                var image = await _context.GalleryImages.FindAsync(imageIds[i]);
                if (image != null)
                {
                    image.SortOrder = sortOrders[i];
                }
            }

            await _context.SaveChangesAsync();
            return GalleryImageOperationResult.Succeeded(0, "Image order updated successfully.");
        }

        // ============================================================
        // Access Control
        // ============================================================

        public async Task<bool> UserCanAccessCategoryAsync(int categoryId, DocumentAccessLevel userAccessLevel)
        {
            var category = await _context.GalleryCategories.FindAsync(categoryId);
            return category != null && category.AccessLevel <= userAccessLevel;
        }

        public DocumentAccessLevel GetUserAccessLevel(IEnumerable<string> userRoles)
        {
            var roles = userRoles.ToList();

            if (roles.Contains("Admin"))
                return DocumentAccessLevel.Admin;

            return DocumentAccessLevel.Member;
        }

        // ============================================================
        // Browse View Model
        // ============================================================

        public async Task<GalleryBrowseViewModel> BuildBrowseViewModelAsync(
            int? categoryId,
            DocumentAccessLevel userAccessLevel,
            bool canManage,
            int page = 1,
            int pageSize = 24)
        {
            var viewModel = new GalleryBrowseViewModel
            {
                CurrentCategoryId = categoryId,
                UserAccessLevel = userAccessLevel,
                CanManage = canManage,
                CurrentPage = page,
                PageSize = pageSize
            };

            // Get current category info
            if (categoryId.HasValue)
            {
                var category = await GetCategoryAsync(categoryId.Value, userAccessLevel);
                if (category != null)
                {
                    viewModel.CurrentCategoryName = category.Name;
                    viewModel.CurrentCategoryAccessLevel = category.AccessLevel;
                    viewModel.CurrentCategoryDescription = category.Description;
                }
                else
                {
                    viewModel.CurrentCategoryId = null;
                }
            }

            // Build breadcrumbs
            viewModel.Breadcrumbs = await GetBreadcrumbsAsync(viewModel.CurrentCategoryId);

            // Get subcategories
            viewModel.Categories = await GetCategoriesAsync(viewModel.CurrentCategoryId, userAccessLevel);

            // Get paginated images (only if in a category, not at root)
            if (viewModel.CurrentCategoryId.HasValue)
            {
                var (images, totalCount) = await GetImagesAsync(
                    viewModel.CurrentCategoryId, userAccessLevel, page, pageSize);

                viewModel.Images = images;
                viewModel.TotalImages = totalCount;
                viewModel.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            }

            // Build category tree for sidebar
            viewModel.CategoryTree = await GetCategoryTreeAsync(userAccessLevel, viewModel.CurrentCategoryId);

            return viewModel;
        }

        // ============================================================
        // Private Helpers
        // ============================================================

        private void DeletePhysicalImage(string fileName)
        {
            // Delete full-size image
            var fullPath = Path.Combine(_galleriesBasePath, fileName);
            if (IOFile.Exists(fullPath))
            {
                try
                {
                    IOFile.Delete(fullPath);
                    _logger.LogInformation("Deleted gallery image file: {FilePath}", fullPath);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error deleting gallery image file: {FilePath}", fullPath);
                }
            }

            // Delete thumbnail
            var ext = Path.GetExtension(fileName);
            var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
            var thumbFileName = $"{nameWithoutExt}_thumb{ext}";
            var thumbPath = Path.Combine(_galleriesBasePath, thumbFileName);
            if (IOFile.Exists(thumbPath))
            {
                try
                {
                    IOFile.Delete(thumbPath);
                    _logger.LogInformation("Deleted gallery thumbnail file: {FilePath}", thumbPath);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error deleting gallery thumbnail file: {FilePath}", thumbPath);
                }
            }
        }
    }
}
