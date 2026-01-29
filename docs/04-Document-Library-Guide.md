# Ape Framework - Document Library Guide

## Introduction

The Document Library provides secure storage and management for PDF files with hierarchical organization and role-based access control. This guide covers all aspects of using and managing the document library.

**Note:** For image management, see the separate **Photo Gallery Guide**.

---

## Features Overview

- **Hierarchical Folders** - Organize documents in nested folders with unlimited depth
- **Access Control** - Member-level and Admin-level folder visibility
- **In-Browser Viewing** - View PDFs directly without downloading
- **Secure Storage** - Documents stored outside web root for security
- **File Management** - Upload, rename, move, delete documents
- **Sortable Content** - Custom ordering for folders and files

---

## Accessing the Document Library

### Members
1. Log in to your account
2. Navigate to **Members** > **Document Library**
3. Browse folders to find documents
4. Click a document to view or download

### Administrators and Managers
1. Log in with Admin or Manager role
2. Navigate to **Members** > **Document Library**
3. Full management controls are displayed
4. Create, edit, move, and delete folders and files

---

## Browsing Documents

### Navigation Elements

**Folder Tree (Sidebar)**
- Shows all accessible folders in a tree structure
- Click a folder to view its contents
- Expand/collapse nested folders
- Current folder is highlighted

**Breadcrumb Navigation**
- Shows your current location in the hierarchy
- Click any breadcrumb to jump to that level
- "Documents" returns to the root

**Folders Grid**
- Displays subfolders as folder cards
- Shows file count for each folder
- Lock icon indicates Admin-only folders
- Click to enter a folder

**Files List**
- Displays PDF documents in the current folder
- Shows filename, upload date, and uploader
- Click to view or download

---

## Folder Structure Example

```
Documents/
├── Public Documents (Member)
│   ├── Getting Started/
│   │   ├── Welcome.pdf
│   │   └── Quick Start Guide.pdf
│   └── Policies/
│       └── Terms of Service.pdf
└── Admin Documents (Admin)
    ├── Internal Procedures/
    └── Financial Reports/
```

---

## Access Levels

### Member Level (Default)
- Visible to all authenticated users
- Appropriate for public documentation, guides, policies

### Admin Level
- Visible only to Admin and Manager roles
- Use for internal documents, sensitive files
- Indicated by lock icon

### Setting Access Levels

Access level is set per folder. All files in a folder inherit that folder's access level.

To change a folder's access level:
1. Click the folder's menu
2. Select **Change Access**
3. Select the new level
4. Click **Save**

---

## Folder Management

*Requires Admin or Manager role*

### Creating Folders

1. Navigate to the desired parent location (or root for top-level)
2. Click **New Folder**
3. Fill in the form:
   - **Folder Name** - Descriptive name (required)
   - **Access Level** - Member (all users) or Admin (restricted)
4. Click **Create**

### Renaming Folders

1. Click the folder's menu (⋮)
2. Select **Rename**
3. Enter the new name
4. Click **Save**

### Moving Folders

1. Click the folder's menu
2. Select **Move**
3. Choose the destination folder from the tree
4. Click **Move**

**Note:** You cannot move a folder into itself or its descendants.

### Deleting Folders

1. Click the folder's menu
2. Select **Delete**
3. Confirm the deletion

**Warning:** Deleting a folder removes ALL contents including:
- All subfolders (recursively)
- All files in the folder and subfolders

### Reordering Folders

Drag and drop folders to change their display order. The order is saved automatically.

---

## File Management

*Requires Admin or Manager role*

### Uploading Files

1. Navigate to the target folder
2. Click **Upload**
3. Select one or more PDF files
4. Click **Upload**

Files appear in the folder with:
- Original filename
- Upload date
- Uploader name

**Supported Format:** PDF only

**File Size:** Maximum size depends on server configuration

### Renaming Files

1. Click the file's menu (⋮)
2. Select **Rename**
3. Enter the new name
4. Click **Save**

### Moving Files

1. Click the file's menu
2. Select **Move**
3. Choose the destination folder from the tree
4. Click **Move**

### Deleting Files

1. Click the file's menu
2. Select **Delete**
3. Confirm the deletion

This permanently removes the file from storage.

### Reordering Files

Drag and drop files to change their display order within the folder.

---

## Viewing Documents

### In-Browser Viewing
- Click any document to open it in the browser's PDF viewer
- Use browser controls to navigate, zoom, and search
- Works in all modern browsers

### Downloading
- Click the **Download** button to save a local copy
- Or use the browser's PDF viewer download option

---

## Storage and Security

### Storage Location
```
/ProtectedFiles/
├── document1.pdf
├── document2.pdf
└── ...
```

Documents are stored **outside the web root** (`/ProtectedFiles/`), which means:
- Files cannot be accessed directly via URL
- All access goes through the controller
- Access control is enforced on every request

### Security Features
- Files served through controller with authentication check
- Access level verified before serving content
- Original filenames preserved in database
- Physical files may use different names for security

### Backup Recommendations
To backup the document library:
1. **Database** - Contains folder structure and file metadata
2. **Files** - `/ProtectedFiles/` directory with all PDFs

Both are required for a complete restore.

---

## Best Practices

### Organization
1. **Plan your structure** - Design folders before uploading
2. **Use descriptive names** - Clear folder and file names
3. **Appropriate depth** - 2-3 levels is usually sufficient
4. **Consistent access levels** - Group similar content together

### Content Management
1. **Meaningful filenames** - Rename files to be descriptive
2. **Regular cleanup** - Remove outdated documents
3. **Version management** - Consider naming conventions for versions

### Security
1. **Appropriate access levels** - Don't make sensitive content public
2. **Review before publishing** - Check content before uploading
3. **Audit periodically** - Review who has access to what

---

## Troubleshooting

### "File not found" Error
- Check the file exists in `/ProtectedFiles/`
- Verify database entry exists and filename matches
- Check file permissions

### Can't Upload Files
- Check folder write permissions on `/ProtectedFiles/`
- Verify file is a valid PDF
- Check file size limits in server configuration
- Ensure you have Admin or Manager role

### Can't View PDF in Browser
- Ensure browser supports PDF viewing
- Try downloading instead
- Check browser extensions that may block PDFs
- Clear browser cache

### Access Denied
- Verify you're logged in
- Check your role has access (Admin/Manager for admin folders)
- Check the folder's access level setting

### Files Not Appearing After Upload
- Refresh the page
- Check for error messages during upload
- Verify the file was uploaded to the correct folder
- Check application logs for errors

---

## Related Guides

- **Photo Gallery Guide** - For managing images
- **Administrator Guide** - Overview of all admin features
- **Security Guide** - Security features and best practices

---

**Version:** 1.0.0
**Framework:** Ape Framework
**Site:** https://Illustrate.net
