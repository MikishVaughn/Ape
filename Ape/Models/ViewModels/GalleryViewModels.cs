using Ape.Models;

namespace Ape.Models.ViewModels
{
    /// <summary>
    /// Represents a category (folder) in the gallery browser
    /// </summary>
    public class GalleryCategoryViewModel
    {
        public int CategoryId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int? ParentCategoryId { get; set; }
        public int SortOrder { get; set; }
        public DocumentAccessLevel AccessLevel { get; set; }
        public bool HasChildren { get; set; }
        public int ImageCount { get; set; }
        public string? Description { get; set; }
    }

    /// <summary>
    /// Represents an image in the gallery browser
    /// </summary>
    public class GalleryImageViewModel
    {
        public int ImageId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string? OriginalFileName { get; set; }
        public string? Description { get; set; }
        public int SortOrder { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public DateTime? UploadedDate { get; set; }
        public string? UploadedBy { get; set; }

        /// <summary>
        /// Thumbnail file name computed from FileName with _thumb suffix
        /// </summary>
        public string ThumbnailFileName
        {
            get
            {
                if (string.IsNullOrEmpty(FileName)) return string.Empty;
                var ext = Path.GetExtension(FileName);
                var nameWithoutExt = Path.GetFileNameWithoutExtension(FileName);
                return $"{nameWithoutExt}_thumb{ext}";
            }
        }

        /// <summary>
        /// Public URL to the full-size image
        /// </summary>
        public string ImageUrl => $"/galleries/{FileName}";

        /// <summary>
        /// Public URL to the thumbnail image
        /// </summary>
        public string ThumbnailUrl => $"/galleries/{ThumbnailFileName}";
    }

    /// <summary>
    /// Breadcrumb navigation item for gallery
    /// </summary>
    public class GalleryBreadcrumbItem
    {
        public int? CategoryId { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsCurrent { get; set; }
    }

    /// <summary>
    /// Node in the category tree sidebar
    /// </summary>
    public class GalleryCategoryTreeNode
    {
        public int CategoryId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int? ParentCategoryId { get; set; }
        public DocumentAccessLevel AccessLevel { get; set; }
        public bool IsExpanded { get; set; }
        public bool IsSelected { get; set; }
        public List<GalleryCategoryTreeNode> Children { get; set; } = [];
    }

    /// <summary>
    /// Main view model for the gallery Browse action
    /// </summary>
    public class GalleryBrowseViewModel
    {
        public int? CurrentCategoryId { get; set; }
        public string CurrentCategoryName { get; set; } = "Image Gallery";
        public DocumentAccessLevel? CurrentCategoryAccessLevel { get; set; }
        public string? CurrentCategoryDescription { get; set; }
        public List<GalleryBreadcrumbItem> Breadcrumbs { get; set; } = [];
        public List<GalleryCategoryViewModel> Categories { get; set; } = [];
        public List<GalleryImageViewModel> Images { get; set; } = [];
        public List<GalleryCategoryTreeNode> CategoryTree { get; set; } = [];
        public bool CanManage { get; set; }
        public DocumentAccessLevel UserAccessLevel { get; set; }

        // Pagination
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 24;
        public int TotalPages { get; set; }
        public int TotalImages { get; set; }
    }

    /// <summary>
    /// Result of a gallery category operation
    /// </summary>
    public class GalleryCategoryOperationResult
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public int? CategoryId { get; set; }

        public static GalleryCategoryOperationResult Succeeded(int categoryId, string? message = null)
            => new() { Success = true, CategoryId = categoryId, Message = message };

        public static GalleryCategoryOperationResult Failed(string message)
            => new() { Success = false, Message = message };
    }

    /// <summary>
    /// Result of a gallery image operation
    /// </summary>
    public class GalleryImageOperationResult
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public int? ImageId { get; set; }
        public int UploadedCount { get; set; }
        public int FailedCount { get; set; }

        public static GalleryImageOperationResult Succeeded(int imageId, string? message = null)
            => new() { Success = true, ImageId = imageId, Message = message };

        public static GalleryImageOperationResult UploadResult(int uploaded, int failed, string? message = null)
            => new() { Success = failed == 0, UploadedCount = uploaded, FailedCount = failed, Message = message };

        public static GalleryImageOperationResult Failed(string message)
            => new() { Success = false, Message = message };
    }

    /// <summary>
    /// Model for creating a new gallery category
    /// </summary>
    public class CreateGalleryCategoryModel
    {
        public string Name { get; set; } = string.Empty;
        public int? ParentCategoryId { get; set; }
        public DocumentAccessLevel AccessLevel { get; set; } = DocumentAccessLevel.Member;
        public string? Description { get; set; }
    }
}
