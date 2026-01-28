using Ape.Data;
using Ape.Models;
using Ape.Models.ViewModels;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using IOFile = System.IO.File;

namespace Ape.Services
{
    // Internal record for folder tree building
    internal record FolderData(int CategoryID, string CategoryName, int? ParentCategoryID, DocumentAccessLevel AccessLevel);

    /// <summary>
    /// Implementation of document management operations
    /// </summary>
    public class DocumentManagementService(
        ApplicationDbContext context,
        IWebHostEnvironment environment,
        ILogger<DocumentManagementService> logger) : IDocumentManagementService
    {
        private readonly ApplicationDbContext _context = context;
        private readonly ILogger<DocumentManagementService> _logger = logger;
        private readonly string _protectedFilesBasePath = Path.Combine(environment.ContentRootPath, "ProtectedFiles");

        // ============================================================
        // Folder Operations
        // ============================================================

        public async Task<List<FolderViewModel>> GetFoldersAsync(int? parentFolderId, DocumentAccessLevel userAccessLevel)
        {
            var folders = await _context.PDFCategories
                .Where(c => c.ParentCategoryID == parentFolderId && c.AccessLevel <= userAccessLevel)
                .OrderBy(c => c.AccessLevel)
                .ThenBy(c => c.CategoryName)
                .Select(c => new FolderViewModel
                {
                    FolderId = c.CategoryID,
                    Name = c.CategoryName,
                    ParentFolderId = c.ParentCategoryID,
                    SortOrder = c.SortOrder,
                    AccessLevel = c.AccessLevel,
                    HasChildren = c.ChildCategories.Any() || c.CategoryFiles.Any(),
                    FileCount = c.CategoryFiles.Count
                })
                .ToListAsync();

            return folders;
        }

        public async Task<FolderViewModel?> GetFolderAsync(int folderId, DocumentAccessLevel userAccessLevel)
        {
            var folder = await _context.PDFCategories
                .Where(c => c.CategoryID == folderId && c.AccessLevel <= userAccessLevel)
                .Select(c => new FolderViewModel
                {
                    FolderId = c.CategoryID,
                    Name = c.CategoryName,
                    ParentFolderId = c.ParentCategoryID,
                    SortOrder = c.SortOrder,
                    AccessLevel = c.AccessLevel,
                    HasChildren = c.ChildCategories.Any() || c.CategoryFiles.Any(),
                    FileCount = c.CategoryFiles.Count
                })
                .FirstOrDefaultAsync();

            return folder;
        }

        public async Task<List<FolderTreeNode>> GetFolderTreeAsync(DocumentAccessLevel userAccessLevel, int? selectedFolderId = null)
        {
            // Get all folders the user can access
            var allFolders = await _context.PDFCategories
                .Where(c => c.AccessLevel <= userAccessLevel)
                .OrderBy(c => c.AccessLevel)
                .ThenBy(c => c.CategoryName)
                .Select(c => new FolderData(c.CategoryID, c.CategoryName, c.ParentCategoryID, c.AccessLevel))
                .ToListAsync();

            // Build the tree structure recursively
            var rootNodes = allFolders
                .Where(f => f.ParentCategoryID == null)
                .Select(f => BuildTreeNode(f, allFolders, selectedFolderId))
                .ToList();

            return rootNodes;
        }

        private static FolderTreeNode BuildTreeNode(
            FolderData folder,
            List<FolderData> allFolders,
            int? selectedFolderId)
        {
            var node = new FolderTreeNode
            {
                FolderId = folder.CategoryID,
                Name = folder.CategoryName,
                ParentFolderId = folder.ParentCategoryID,
                AccessLevel = folder.AccessLevel,
                IsSelected = folder.CategoryID == selectedFolderId,
                Children = []
            };

            // Find and add children recursively
            foreach (var child in allFolders.Where(f => f.ParentCategoryID == folder.CategoryID))
            {
                var childNode = BuildTreeNode(child, allFolders, selectedFolderId);
                node.Children.Add(childNode);

                // Expand parent nodes that contain the selected folder
                if (childNode.IsSelected || childNode.IsExpanded)
                {
                    node.IsExpanded = true;
                }
            }

            return node;
        }

        public async Task<List<BreadcrumbItem>> GetBreadcrumbsAsync(int? folderId)
        {
            var breadcrumbs = new List<BreadcrumbItem>
            {
                new() { FolderId = null, Name = "Document Library", IsCurrent = folderId == null }
            };

            if (folderId == null) return breadcrumbs;

            var path = new List<BreadcrumbItem>();
            var currentId = folderId;

            while (currentId.HasValue)
            {
                var folder = await _context.PDFCategories
                    .Where(c => c.CategoryID == currentId.Value)
                    .Select(c => new { c.CategoryID, c.CategoryName, c.ParentCategoryID })
                    .FirstOrDefaultAsync();

                if (folder == null) break;

                path.Insert(0, new BreadcrumbItem
                {
                    FolderId = folder.CategoryID,
                    Name = folder.CategoryName,
                    IsCurrent = folder.CategoryID == folderId
                });

                currentId = folder.ParentCategoryID;
            }

            breadcrumbs.AddRange(path);
            return breadcrumbs;
        }

        public async Task<FolderOperationResult> CreateFolderAsync(CreateFolderModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Name))
            {
                return FolderOperationResult.Failed("Folder name is required.");
            }

            // Validate parent folder exists if specified
            if (model.ParentFolderId.HasValue)
            {
                var parentExists = await _context.PDFCategories
                    .AnyAsync(c => c.CategoryID == model.ParentFolderId.Value);
                if (!parentExists)
                {
                    return FolderOperationResult.Failed("Parent folder not found.");
                }
            }

            // Get next sort order
            var maxSortOrder = await _context.PDFCategories
                .Where(c => c.ParentCategoryID == model.ParentFolderId)
                .MaxAsync(c => (int?)c.SortOrder) ?? 0;

            var newFolder = new PDFCategory
            {
                CategoryName = model.Name.Trim(),
                ParentCategoryID = model.ParentFolderId,
                AccessLevel = model.AccessLevel,
                SortOrder = maxSortOrder + 1
            };

            _context.PDFCategories.Add(newFolder);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created folder '{FolderName}' (ID: {FolderId}) with access level {AccessLevel}",
                newFolder.CategoryName, newFolder.CategoryID, newFolder.AccessLevel);

            return FolderOperationResult.Succeeded(newFolder.CategoryID, $"Folder '{newFolder.CategoryName}' created successfully.");
        }

        public async Task<FolderOperationResult> RenameFolderAsync(int folderId, string newName)
        {
            if (string.IsNullOrWhiteSpace(newName))
            {
                return FolderOperationResult.Failed("Folder name is required.");
            }

            var folder = await _context.PDFCategories.FindAsync(folderId);
            if (folder == null)
            {
                return FolderOperationResult.Failed("Folder not found.");
            }

            var oldName = folder.CategoryName;
            folder.CategoryName = newName.Trim();
            await _context.SaveChangesAsync();

            _logger.LogInformation("Renamed folder from '{OldName}' to '{NewName}' (ID: {FolderId})",
                oldName, newName, folderId);

            return FolderOperationResult.Succeeded(folderId, $"Folder renamed to '{newName}'.");
        }

        public async Task<FolderOperationResult> UpdateFolderAccessLevelAsync(int folderId, DocumentAccessLevel accessLevel)
        {
            var folder = await _context.PDFCategories.FindAsync(folderId);
            if (folder == null)
            {
                return FolderOperationResult.Failed("Folder not found.");
            }

            folder.AccessLevel = accessLevel;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated folder '{FolderName}' (ID: {FolderId}) access level to {AccessLevel}",
                folder.CategoryName, folderId, accessLevel);

            return FolderOperationResult.Succeeded(folderId, $"Folder access level updated to {accessLevel}.");
        }

        public async Task<FolderOperationResult> MoveFolderAsync(int folderId, int? newParentFolderId)
        {
            var folder = await _context.PDFCategories.FindAsync(folderId);
            if (folder == null)
            {
                return FolderOperationResult.Failed("Folder not found.");
            }

            // Prevent moving folder into itself or its descendants
            if (newParentFolderId.HasValue)
            {
                if (newParentFolderId.Value == folderId)
                {
                    return FolderOperationResult.Failed("Cannot move a folder into itself.");
                }

                // Check if new parent is a descendant of this folder
                if (await IsDescendantOfAsync(newParentFolderId.Value, folderId))
                {
                    return FolderOperationResult.Failed("Cannot move a folder into one of its subfolders.");
                }

                var parentExists = await _context.PDFCategories.AnyAsync(c => c.CategoryID == newParentFolderId.Value);
                if (!parentExists)
                {
                    return FolderOperationResult.Failed("Target folder not found.");
                }
            }

            // Get next sort order in target location
            var maxSortOrder = await _context.PDFCategories
                .Where(c => c.ParentCategoryID == newParentFolderId)
                .MaxAsync(c => (int?)c.SortOrder) ?? 0;

            folder.ParentCategoryID = newParentFolderId;
            folder.SortOrder = maxSortOrder + 1;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Moved folder '{FolderName}' (ID: {FolderId}) to parent {ParentId}",
                folder.CategoryName, folderId, newParentFolderId);

            return FolderOperationResult.Succeeded(folderId, $"Folder '{folder.CategoryName}' moved successfully.");
        }

        private async Task<bool> IsDescendantOfAsync(int folderId, int potentialAncestorId)
        {
            var currentId = folderId;
            var visited = new HashSet<int>();

            while (true)
            {
                if (visited.Contains(currentId)) break; // Prevent infinite loops
                visited.Add(currentId);

                var folder = await _context.PDFCategories
                    .Where(c => c.CategoryID == currentId)
                    .Select(c => new { c.ParentCategoryID })
                    .FirstOrDefaultAsync();

                if (folder?.ParentCategoryID == null) break;
                if (folder.ParentCategoryID == potentialAncestorId) return true;

                currentId = folder.ParentCategoryID.Value;
            }

            return false;
        }

        public async Task<FolderOperationResult> DeleteFolderAsync(int folderId, bool deleteContents = false)
        {
            var folder = await _context.PDFCategories
                .Include(c => c.CategoryFiles)
                .Include(c => c.ChildCategories)
                .FirstOrDefaultAsync(c => c.CategoryID == folderId);

            if (folder == null)
            {
                return FolderOperationResult.Failed("Folder not found.");
            }

            // Check for contents
            var hasChildren = folder.ChildCategories.Any();
            var hasFiles = folder.CategoryFiles.Any();

            if ((hasChildren || hasFiles) && !deleteContents)
            {
                return FolderOperationResult.Failed(
                    "Folder is not empty. Move or delete contents first, or confirm deletion of all contents.");
            }

            if (deleteContents)
            {
                // Recursively delete subfolders
                foreach (var childFolder in folder.ChildCategories.ToList())
                {
                    var result = await DeleteFolderAsync(childFolder.CategoryID, true);
                    if (!result.Success)
                    {
                        return result;
                    }
                }

                // Delete files in this folder
                foreach (var file in folder.CategoryFiles.ToList())
                {
                    await DeletePhysicalFileIfNotLinkedAsync(file.FileName, folder.CategoryID);
                    _context.CategoryFiles.Remove(file);
                }
            }

            _context.PDFCategories.Remove(folder);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted folder '{FolderName}' (ID: {FolderId})", folder.CategoryName, folderId);

            return FolderOperationResult.Succeeded(folderId, $"Folder '{folder.CategoryName}' deleted successfully.");
        }

        public async Task<bool> FolderHasContentsAsync(int folderId)
        {
            return await _context.PDFCategories.AnyAsync(c => c.ParentCategoryID == folderId)
                || await _context.CategoryFiles.AnyAsync(f => f.CategoryID == folderId);
        }

        public async Task<FolderOperationResult> UpdateFolderSortOrdersAsync(int[] folderIds, int[] sortOrders)
        {
            if (folderIds.Length != sortOrders.Length)
            {
                return FolderOperationResult.Failed("Invalid sort order data.");
            }

            for (int i = 0; i < folderIds.Length; i++)
            {
                var folder = await _context.PDFCategories.FindAsync(folderIds[i]);
                if (folder != null)
                {
                    folder.SortOrder = sortOrders[i];
                }
            }

            await _context.SaveChangesAsync();
            return FolderOperationResult.Succeeded(0, "Folder order updated successfully.");
        }

        // ============================================================
        // File Operations
        // ============================================================

        public async Task<List<FileViewModel>> GetFilesAsync(int? folderId, DocumentAccessLevel userAccessLevel)
        {
            if (folderId == null) return [];

            // Verify user can access the folder
            var folder = await _context.PDFCategories.FindAsync(folderId);
            if (folder == null || folder.AccessLevel > userAccessLevel) return [];

            var files = await _context.CategoryFiles
                .Where(f => f.CategoryID == folderId)
                .OrderBy(f => f.SortOrder)
                .ThenBy(f => f.FileName)
                .Select(f => new FileViewModel
                {
                    FileId = f.FileID,
                    FileName = f.FileName,
                    Description = f.Description,
                    SortOrder = f.SortOrder,
                    FolderId = f.CategoryID,
                    FolderName = f.PDFCategory != null ? f.PDFCategory.CategoryName : string.Empty,
                    UploadedDate = f.UploadedDate,
                    UploadedBy = f.UploadedBy
                })
                .ToListAsync();

            // Add file sizes from physical files
            foreach (var file in files)
            {
                var filePath = Path.Combine(_protectedFilesBasePath, file.FileName);
                if (IOFile.Exists(filePath))
                {
                    file.FileSizeBytes = new FileInfo(filePath).Length;
                }
            }

            return files;
        }

        public async Task<FileViewModel?> GetFileAsync(int fileId, DocumentAccessLevel userAccessLevel)
        {
            var file = await _context.CategoryFiles
                .Include(f => f.PDFCategory)
                .Where(f => f.FileID == fileId && f.PDFCategory != null && f.PDFCategory.AccessLevel <= userAccessLevel)
                .Select(f => new FileViewModel
                {
                    FileId = f.FileID,
                    FileName = f.FileName,
                    Description = f.Description,
                    SortOrder = f.SortOrder,
                    FolderId = f.CategoryID,
                    FolderName = f.PDFCategory != null ? f.PDFCategory.CategoryName : string.Empty,
                    UploadedDate = f.UploadedDate,
                    UploadedBy = f.UploadedBy
                })
                .FirstOrDefaultAsync();

            if (file != null)
            {
                var filePath = Path.Combine(_protectedFilesBasePath, file.FileName);
                if (IOFile.Exists(filePath))
                {
                    file.FileSizeBytes = new FileInfo(filePath).Length;
                }
            }

            return file;
        }

        public async Task<FileOperationResult> UploadFileAsync(int folderId, IFormFile file, string? description, string uploadedBy)
        {
            var folder = await _context.PDFCategories.FindAsync(folderId);
            if (folder == null)
            {
                return FileOperationResult.Failed("Folder not found.");
            }

            if (file == null || file.Length == 0)
            {
                return FileOperationResult.Failed("No file selected or file is empty.");
            }

            if (!file.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                return FileOperationResult.Failed("Only PDF files are allowed.");
            }

            // Sanitize filename
            var originalFileName = Path.GetFileName(file.FileName);
            var sanitizedFileName = Path.GetInvalidFileNameChars()
                .Aggregate(originalFileName, (current, c) => current.Replace(c.ToString(), "_"));

            var filePath = Path.Combine(_protectedFilesBasePath, sanitizedFileName);

            try
            {
                if (!Directory.Exists(_protectedFilesBasePath))
                {
                    Directory.CreateDirectory(_protectedFilesBasePath);
                }

                using var fileStream = new FileStream(filePath, FileMode.Create);
                await file.CopyToAsync(fileStream);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving physical file {FileName}", sanitizedFileName);
                return FileOperationResult.Failed("Error saving file to server.");
            }

            // Get next sort order
            var maxSortOrder = await _context.CategoryFiles
                .Where(f => f.CategoryID == folderId)
                .MaxAsync(f => (int?)f.SortOrder) ?? 0;

            // Check if file already exists in this folder
            var existingFile = await _context.CategoryFiles
                .FirstOrDefaultAsync(f => f.CategoryID == folderId && f.FileName == sanitizedFileName);

            if (existingFile != null)
            {
                existingFile.Description = description;
                existingFile.UploadedDate = DateTime.UtcNow;
                existingFile.UploadedBy = uploadedBy;
                await _context.SaveChangesAsync();

                return FileOperationResult.Succeeded(existingFile.FileID, sanitizedFileName,
                    $"File '{sanitizedFileName}' updated in '{folder.CategoryName}'.");
            }

            var newFile = new CategoryFile
            {
                CategoryID = folderId,
                FileName = sanitizedFileName,
                Description = description,
                SortOrder = maxSortOrder + 1,
                UploadedDate = DateTime.UtcNow,
                UploadedBy = uploadedBy,
                PDFCategory = folder
            };

            _context.CategoryFiles.Add(newFile);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Uploaded file '{FileName}' to folder '{FolderName}' by {User}",
                sanitizedFileName, folder.CategoryName, uploadedBy);

            return FileOperationResult.Succeeded(newFile.FileID, sanitizedFileName,
                $"File '{sanitizedFileName}' uploaded to '{folder.CategoryName}'.");
        }

        public async Task<FileOperationResult> RenameFileAsync(int fileId, string newFileName)
        {
            if (string.IsNullOrWhiteSpace(newFileName))
            {
                return FileOperationResult.Failed("File name is required.");
            }

            if (!newFileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                return FileOperationResult.Failed("File name must end with .pdf.");
            }

            var file = await _context.CategoryFiles.FindAsync(fileId);
            if (file == null)
            {
                return FileOperationResult.Failed("File not found.");
            }

            var oldFileName = file.FileName;
            if (oldFileName.Equals(newFileName, StringComparison.OrdinalIgnoreCase))
            {
                return FileOperationResult.Succeeded(fileId, newFileName, "No changes made.");
            }

            var oldFilePath = Path.Combine(_protectedFilesBasePath, oldFileName);
            var newFilePath = Path.Combine(_protectedFilesBasePath, newFileName);

            if (IOFile.Exists(newFilePath))
            {
                return FileOperationResult.Failed($"A file named '{newFileName}' already exists.");
            }

            if (IOFile.Exists(oldFilePath))
            {
                try
                {
                    IOFile.Move(oldFilePath, newFilePath);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error renaming physical file from {Old} to {New}", oldFileName, newFileName);
                    return FileOperationResult.Failed("Error renaming physical file.");
                }
            }

            file.FileName = newFileName;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Renamed file from '{OldName}' to '{NewName}' (ID: {FileId})",
                oldFileName, newFileName, fileId);

            return FileOperationResult.Succeeded(fileId, newFileName, $"File renamed to '{newFileName}'.");
        }

        public async Task<FileOperationResult> UpdateFileDescriptionAsync(int fileId, string? description)
        {
            var file = await _context.CategoryFiles.FindAsync(fileId);
            if (file == null)
            {
                return FileOperationResult.Failed("File not found.");
            }

            file.Description = description;
            await _context.SaveChangesAsync();

            return FileOperationResult.Succeeded(fileId, file.FileName, "File description updated.");
        }

        public async Task<FileOperationResult> MoveFileAsync(int fileId, int targetFolderId)
        {
            var file = await _context.CategoryFiles.Include(f => f.PDFCategory).FirstOrDefaultAsync(f => f.FileID == fileId);
            if (file == null)
            {
                return FileOperationResult.Failed("File not found.");
            }

            var targetFolder = await _context.PDFCategories.FindAsync(targetFolderId);
            if (targetFolder == null)
            {
                return FileOperationResult.Failed("Target folder not found.");
            }

            if (file.CategoryID == targetFolderId)
            {
                return FileOperationResult.Succeeded(fileId, file.FileName, "File is already in the target folder.");
            }

            // Get next sort order in target folder
            var maxSortOrder = await _context.CategoryFiles
                .Where(f => f.CategoryID == targetFolderId)
                .MaxAsync(f => (int?)f.SortOrder) ?? 0;

            var oldFolderName = file.PDFCategory?.CategoryName ?? "Unknown";
            file.CategoryID = targetFolderId;
            file.SortOrder = maxSortOrder + 1;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Moved file '{FileName}' from '{OldFolder}' to '{NewFolder}'",
                file.FileName, oldFolderName, targetFolder.CategoryName);

            return FileOperationResult.Succeeded(fileId, file.FileName,
                $"File '{file.FileName}' moved to '{targetFolder.CategoryName}'.");
        }

        public async Task<FileOperationResult> MoveFilesAsync(int[] fileIds, int targetFolderId)
        {
            if (fileIds == null || fileIds.Length == 0)
            {
                return FileOperationResult.Failed("No files selected.");
            }

            var targetFolder = await _context.PDFCategories.FindAsync(targetFolderId);
            if (targetFolder == null)
            {
                return FileOperationResult.Failed("Target folder not found.");
            }

            var files = await _context.CategoryFiles
                .Include(f => f.PDFCategory)
                .Where(f => fileIds.Contains(f.FileID))
                .ToListAsync();

            if (files.Count == 0)
            {
                return FileOperationResult.Failed("No valid files found.");
            }

            // Get next sort order in target folder
            var maxSortOrder = await _context.CategoryFiles
                .Where(f => f.CategoryID == targetFolderId)
                .MaxAsync(f => (int?)f.SortOrder) ?? 0;

            int movedCount = 0;
            foreach (var file in files)
            {
                if (file.CategoryID != targetFolderId)
                {
                    file.CategoryID = targetFolderId;
                    file.SortOrder = ++maxSortOrder;
                    movedCount++;
                }
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Moved {Count} files to folder '{FolderName}'",
                movedCount, targetFolder.CategoryName);

            return FileOperationResult.Succeeded(0, null,
                $"{movedCount} file(s) moved to '{targetFolder.CategoryName}'.");
        }

        public async Task<FileOperationResult> DeleteFileAsync(int fileId)
        {
            var file = await _context.CategoryFiles.Include(f => f.PDFCategory).FirstOrDefaultAsync(f => f.FileID == fileId);
            if (file == null)
            {
                return FileOperationResult.Failed("File not found.");
            }

            var fileName = file.FileName;
            var folderId = file.CategoryID;

            _context.CategoryFiles.Remove(file);
            await _context.SaveChangesAsync();

            await DeletePhysicalFileIfNotLinkedAsync(fileName, folderId);

            _logger.LogInformation("Deleted file '{FileName}' (ID: {FileId})", fileName, fileId);

            return FileOperationResult.Succeeded(fileId, fileName, $"File '{fileName}' deleted successfully.");
        }

        public async Task<FileOperationResult> UpdateFileSortOrdersAsync(int[] fileIds, int[] sortOrders)
        {
            if (fileIds.Length != sortOrders.Length)
            {
                return FileOperationResult.Failed("Invalid sort order data.");
            }

            for (int i = 0; i < fileIds.Length; i++)
            {
                var file = await _context.CategoryFiles.FindAsync(fileIds[i]);
                if (file != null)
                {
                    file.SortOrder = sortOrders[i];
                }
            }

            await _context.SaveChangesAsync();
            return FileOperationResult.Succeeded(0, null, "File order updated successfully.");
        }

        public async Task<string?> GetFilePathAsync(int fileId, DocumentAccessLevel userAccessLevel)
        {
            var file = await _context.CategoryFiles
                .Include(f => f.PDFCategory)
                .FirstOrDefaultAsync(f => f.FileID == fileId && f.PDFCategory != null && f.PDFCategory.AccessLevel <= userAccessLevel);

            if (file == null) return null;

            var filePath = Path.Combine(_protectedFilesBasePath, file.FileName);
            return IOFile.Exists(filePath) ? filePath : null;
        }

        private async Task DeletePhysicalFileIfNotLinkedAsync(string fileName, int excludeCategoryId)
        {
            // Check if file is used in any other category
            var isLinkedElsewhere = await _context.CategoryFiles
                .AnyAsync(cf => cf.FileName == fileName && cf.CategoryID != excludeCategoryId);

            if (!isLinkedElsewhere)
            {
                var filePath = Path.Combine(_protectedFilesBasePath, fileName);
                if (IOFile.Exists(filePath))
                {
                    try
                    {
                        IOFile.Delete(filePath);
                        _logger.LogInformation("Physical file deleted: {FileName}", filePath);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error deleting physical file {FileName}", filePath);
                    }
                }
            }
            else
            {
                _logger.LogInformation("Physical file NOT deleted (still linked elsewhere): {FileName}", fileName);
            }
        }

        // ============================================================
        // Access Control
        // ============================================================

        public async Task<bool> UserCanAccessFolderAsync(int folderId, DocumentAccessLevel userAccessLevel)
        {
            var folder = await _context.PDFCategories.FindAsync(folderId);
            return folder != null && folder.AccessLevel <= userAccessLevel;
        }

        public async Task<bool> UserCanAccessFileAsync(int fileId, DocumentAccessLevel userAccessLevel)
        {
            var file = await _context.CategoryFiles
                .Include(f => f.PDFCategory)
                .FirstOrDefaultAsync(f => f.FileID == fileId);

            return file?.PDFCategory != null && file.PDFCategory.AccessLevel <= userAccessLevel;
        }

        public DocumentAccessLevel GetUserAccessLevel(IEnumerable<string> userRoles)
        {
            var roles = userRoles.ToList();

            if (roles.Contains("Admin"))
                return DocumentAccessLevel.Admin;

            // Default to Member level
            return DocumentAccessLevel.Member;
        }

        // ============================================================
        // Explorer View Model
        // ============================================================

        public async Task<DocumentExplorerViewModel> BuildExplorerViewModelAsync(
            int? folderId,
            DocumentAccessLevel userAccessLevel,
            bool canManageFolders,
            bool canManageFiles)
        {
            var viewModel = new DocumentExplorerViewModel
            {
                CurrentFolderId = folderId,
                UserAccessLevel = userAccessLevel,
                CanManageFolders = canManageFolders,
                CanManageFiles = canManageFiles
            };

            // Get current folder info
            if (folderId.HasValue)
            {
                var folder = await GetFolderAsync(folderId.Value, userAccessLevel);
                if (folder != null)
                {
                    viewModel.CurrentFolderName = folder.Name;
                    viewModel.CurrentFolderAccessLevel = folder.AccessLevel;
                }
                else
                {
                    // Folder not found or user can't access it
                    viewModel.CurrentFolderId = null;
                }
            }

            // Build breadcrumbs
            viewModel.Breadcrumbs = await GetBreadcrumbsAsync(viewModel.CurrentFolderId);

            // Get subfolders
            viewModel.Folders = await GetFoldersAsync(viewModel.CurrentFolderId, userAccessLevel);

            // Get files (only if in a folder, not at root)
            if (viewModel.CurrentFolderId.HasValue)
            {
                viewModel.Files = await GetFilesAsync(viewModel.CurrentFolderId, userAccessLevel);
            }

            // Build folder tree for sidebar
            viewModel.FolderTree = await GetFolderTreeAsync(userAccessLevel, viewModel.CurrentFolderId);

            return viewModel;
        }
    }
}
