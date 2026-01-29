using Ape.Models;
using Ape.Models.ViewModels;

namespace Ape.Services
{
    public interface IGalleryManagementService
    {
        // ============================================================
        // Category Operations
        // ============================================================

        Task<List<GalleryCategoryViewModel>> GetCategoriesAsync(int? parentCategoryId, DocumentAccessLevel userAccessLevel);
        Task<GalleryCategoryViewModel?> GetCategoryAsync(int categoryId, DocumentAccessLevel userAccessLevel);
        Task<List<GalleryCategoryTreeNode>> GetCategoryTreeAsync(DocumentAccessLevel userAccessLevel, int? selectedCategoryId = null);
        Task<List<GalleryBreadcrumbItem>> GetBreadcrumbsAsync(int? categoryId);
        Task<GalleryCategoryOperationResult> CreateCategoryAsync(CreateGalleryCategoryModel model);
        Task<GalleryCategoryOperationResult> RenameCategoryAsync(int categoryId, string newName);
        Task<GalleryCategoryOperationResult> UpdateCategoryAccessLevelAsync(int categoryId, DocumentAccessLevel accessLevel);
        Task<GalleryCategoryOperationResult> UpdateCategoryDescriptionAsync(int categoryId, string? description);
        Task<GalleryCategoryOperationResult> MoveCategoryAsync(int categoryId, int? newParentCategoryId);
        Task<GalleryCategoryOperationResult> DeleteCategoryAsync(int categoryId, bool deleteContents = false);
        Task<bool> CategoryHasContentsAsync(int categoryId);
        Task<GalleryCategoryOperationResult> UpdateCategorySortOrdersAsync(int[] categoryIds, int[] sortOrders);

        // ============================================================
        // Image Operations
        // ============================================================

        Task<(List<GalleryImageViewModel> Images, int TotalCount)> GetImagesAsync(int? categoryId, DocumentAccessLevel userAccessLevel, int page = 1, int pageSize = 24);
        Task<GalleryImageViewModel?> GetImageAsync(int imageId, DocumentAccessLevel userAccessLevel);
        Task<GalleryImageOperationResult> UploadImagesAsync(int categoryId, IList<IFormFile> files, string? description, string uploadedBy);
        Task<GalleryImageOperationResult> UploadSingleImageAsync(int categoryId, IFormFile file, string? description, string uploadedBy);
        Task<GalleryImageOperationResult> RenameImageAsync(int imageId, string newOriginalName);
        Task<GalleryImageOperationResult> UpdateImageDescriptionAsync(int imageId, string? description);
        Task<GalleryImageOperationResult> MoveImageAsync(int imageId, int targetCategoryId);
        Task<GalleryImageOperationResult> MoveImagesAsync(int[] imageIds, int targetCategoryId);
        Task<GalleryImageOperationResult> DeleteImageAsync(int imageId);
        Task<GalleryImageOperationResult> DeleteImagesAsync(int[] imageIds);
        Task<GalleryImageOperationResult> UpdateImageSortOrdersAsync(int[] imageIds, int[] sortOrders);

        // ============================================================
        // Access Control
        // ============================================================

        Task<bool> UserCanAccessCategoryAsync(int categoryId, DocumentAccessLevel userAccessLevel);
        DocumentAccessLevel GetUserAccessLevel(IEnumerable<string> userRoles);

        // ============================================================
        // Browse View Model
        // ============================================================

        Task<GalleryBrowseViewModel> BuildBrowseViewModelAsync(
            int? categoryId,
            DocumentAccessLevel userAccessLevel,
            bool canManage,
            int page = 1,
            int pageSize = 24);
    }
}
