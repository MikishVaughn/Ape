using Ape.Models;

namespace Ape.Models.ViewModels
{
    /// <summary>
    /// Represents a folder in the document explorer
    /// </summary>
    public class FolderViewModel
    {
        public int FolderId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int? ParentFolderId { get; set; }
        public int SortOrder { get; set; }
        public DocumentAccessLevel AccessLevel { get; set; }
        public bool HasChildren { get; set; }
        public int FileCount { get; set; }
    }

    /// <summary>
    /// Represents a file in the document explorer
    /// </summary>
    public class FileViewModel
    {
        public int FileId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int SortOrder { get; set; }
        public int FolderId { get; set; }
        public string FolderName { get; set; } = string.Empty;
        public DateTime? UploadedDate { get; set; }
        public string? UploadedBy { get; set; }
        public long? FileSizeBytes { get; set; }

        /// <summary>
        /// Display-friendly file size (e.g., "1.5 MB")
        /// </summary>
        public string FileSizeDisplay => FileSizeBytes.HasValue
            ? FileSizeBytes.Value switch
            {
                < 1024 => $"{FileSizeBytes.Value} B",
                < 1024 * 1024 => $"{FileSizeBytes.Value / 1024.0:F1} KB",
                _ => $"{FileSizeBytes.Value / (1024.0 * 1024.0):F1} MB"
            }
            : "Unknown";
    }

    /// <summary>
    /// Breadcrumb navigation item
    /// </summary>
    public class BreadcrumbItem
    {
        public int? FolderId { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsCurrent { get; set; }
    }

    /// <summary>
    /// Node in the folder tree sidebar
    /// </summary>
    public class FolderTreeNode
    {
        public int FolderId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int? ParentFolderId { get; set; }
        public DocumentAccessLevel AccessLevel { get; set; }
        public bool IsExpanded { get; set; }
        public bool IsSelected { get; set; }
        public List<FolderTreeNode> Children { get; set; } = [];
    }

    /// <summary>
    /// Main view model for the document explorer Browse action
    /// </summary>
    public class DocumentExplorerViewModel
    {
        public int? CurrentFolderId { get; set; }
        public string CurrentFolderName { get; set; } = "Document Library";
        public DocumentAccessLevel? CurrentFolderAccessLevel { get; set; }
        public List<BreadcrumbItem> Breadcrumbs { get; set; } = [];
        public List<FolderViewModel> Folders { get; set; } = [];
        public List<FileViewModel> Files { get; set; } = [];
        public List<FolderTreeNode> FolderTree { get; set; } = [];
        public bool CanManageFolders { get; set; }
        public bool CanManageFiles { get; set; }
        public DocumentAccessLevel UserAccessLevel { get; set; }
    }

    /// <summary>
    /// Result of a folder operation
    /// </summary>
    public class FolderOperationResult
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public int? FolderId { get; set; }

        public static FolderOperationResult Succeeded(int folderId, string? message = null)
            => new() { Success = true, FolderId = folderId, Message = message };

        public static FolderOperationResult Failed(string message)
            => new() { Success = false, Message = message };
    }

    /// <summary>
    /// Result of a file operation
    /// </summary>
    public class FileOperationResult
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public int? FileId { get; set; }
        public string? FileName { get; set; }

        public static FileOperationResult Succeeded(int fileId, string? fileName = null, string? message = null)
            => new() { Success = true, FileId = fileId, FileName = fileName, Message = message };

        public static FileOperationResult Failed(string message)
            => new() { Success = false, Message = message };
    }

    /// <summary>
    /// Model for creating a new folder
    /// </summary>
    public class CreateFolderModel
    {
        public string Name { get; set; } = string.Empty;
        public int? ParentFolderId { get; set; }
        public DocumentAccessLevel AccessLevel { get; set; } = DocumentAccessLevel.Member;
    }

    /// <summary>
    /// Model for renaming a folder
    /// </summary>
    public class RenameFolderModel
    {
        public int FolderId { get; set; }
        public string NewName { get; set; } = string.Empty;
    }

    /// <summary>
    /// Model for moving a folder or file
    /// </summary>
    public class MoveItemModel
    {
        public int ItemId { get; set; }
        public int? TargetFolderId { get; set; }
        public bool IsFolder { get; set; }
    }

    /// <summary>
    /// Model for updating sort order via drag-and-drop
    /// </summary>
    public class UpdateSortOrderModel
    {
        public int[] ItemIds { get; set; } = [];
        public int[] SortOrders { get; set; } = [];
        public bool IsFolders { get; set; }
    }
}
