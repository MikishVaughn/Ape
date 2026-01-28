using Ape.Models;
using Ape.Models.ViewModels;
using Microsoft.AspNetCore.Http;

namespace Ape.Services
{
    /// <summary>
    /// Service interface for document management operations
    /// </summary>
    public interface IDocumentManagementService
    {
        // ============================================================
        // Folder Operations
        // ============================================================

        /// <summary>
        /// Get all folders accessible by the user's role at a specific level
        /// </summary>
        Task<List<FolderViewModel>> GetFoldersAsync(int? parentFolderId, DocumentAccessLevel userAccessLevel);

        /// <summary>
        /// Get a single folder by ID
        /// </summary>
        Task<FolderViewModel?> GetFolderAsync(int folderId, DocumentAccessLevel userAccessLevel);

        /// <summary>
        /// Get the full folder tree for sidebar navigation
        /// </summary>
        Task<List<FolderTreeNode>> GetFolderTreeAsync(DocumentAccessLevel userAccessLevel, int? selectedFolderId = null);

        /// <summary>
        /// Get breadcrumb navigation path to a folder
        /// </summary>
        Task<List<BreadcrumbItem>> GetBreadcrumbsAsync(int? folderId);

        /// <summary>
        /// Create a new folder
        /// </summary>
        Task<FolderOperationResult> CreateFolderAsync(CreateFolderModel model);

        /// <summary>
        /// Rename a folder
        /// </summary>
        Task<FolderOperationResult> RenameFolderAsync(int folderId, string newName);

        /// <summary>
        /// Update folder access level
        /// </summary>
        Task<FolderOperationResult> UpdateFolderAccessLevelAsync(int folderId, DocumentAccessLevel accessLevel);

        /// <summary>
        /// Move a folder to a new parent
        /// </summary>
        Task<FolderOperationResult> MoveFolderAsync(int folderId, int? newParentFolderId);

        /// <summary>
        /// Delete a folder and optionally its contents
        /// </summary>
        Task<FolderOperationResult> DeleteFolderAsync(int folderId, bool deleteContents = false);

        /// <summary>
        /// Check if a folder has children (subfolders or files)
        /// </summary>
        Task<bool> FolderHasContentsAsync(int folderId);

        /// <summary>
        /// Update folder sort orders (for drag-and-drop reordering)
        /// </summary>
        Task<FolderOperationResult> UpdateFolderSortOrdersAsync(int[] folderIds, int[] sortOrders);

        // ============================================================
        // File Operations
        // ============================================================

        /// <summary>
        /// Get all files in a folder
        /// </summary>
        Task<List<FileViewModel>> GetFilesAsync(int? folderId, DocumentAccessLevel userAccessLevel);

        /// <summary>
        /// Get a single file by ID
        /// </summary>
        Task<FileViewModel?> GetFileAsync(int fileId, DocumentAccessLevel userAccessLevel);

        /// <summary>
        /// Upload a file to a folder
        /// </summary>
        Task<FileOperationResult> UploadFileAsync(int folderId, IFormFile file, string? description, string uploadedBy);

        /// <summary>
        /// Rename a file
        /// </summary>
        Task<FileOperationResult> RenameFileAsync(int fileId, string newFileName);

        /// <summary>
        /// Update file description
        /// </summary>
        Task<FileOperationResult> UpdateFileDescriptionAsync(int fileId, string? description);

        /// <summary>
        /// Move a file to a different folder
        /// </summary>
        Task<FileOperationResult> MoveFileAsync(int fileId, int targetFolderId);

        /// <summary>
        /// Move multiple files to a different folder
        /// </summary>
        Task<FileOperationResult> MoveFilesAsync(int[] fileIds, int targetFolderId);

        /// <summary>
        /// Delete a file
        /// </summary>
        Task<FileOperationResult> DeleteFileAsync(int fileId);

        /// <summary>
        /// Update file sort orders (for drag-and-drop reordering)
        /// </summary>
        Task<FileOperationResult> UpdateFileSortOrdersAsync(int[] fileIds, int[] sortOrders);

        /// <summary>
        /// Get the physical file path for download
        /// </summary>
        Task<string?> GetFilePathAsync(int fileId, DocumentAccessLevel userAccessLevel);

        // ============================================================
        // Access Control
        // ============================================================

        /// <summary>
        /// Check if user has access to a specific folder
        /// </summary>
        Task<bool> UserCanAccessFolderAsync(int folderId, DocumentAccessLevel userAccessLevel);

        /// <summary>
        /// Check if user has access to a specific file
        /// </summary>
        Task<bool> UserCanAccessFileAsync(int fileId, DocumentAccessLevel userAccessLevel);

        /// <summary>
        /// Convert user role string to DocumentAccessLevel
        /// </summary>
        DocumentAccessLevel GetUserAccessLevel(IEnumerable<string> userRoles);

        // ============================================================
        // Explorer View Model
        // ============================================================

        /// <summary>
        /// Build the complete view model for the explorer view
        /// </summary>
        Task<DocumentExplorerViewModel> BuildExplorerViewModelAsync(
            int? folderId,
            DocumentAccessLevel userAccessLevel,
            bool canManageFolders,
            bool canManageFiles);
    }
}
