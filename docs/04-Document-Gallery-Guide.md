# Ape Framework - Document Library & Photo Gallery Guide

## Introduction

The Ape Framework includes two content management systems: a Document Library for PDF files and a Photo Gallery for images. Both support hierarchical organization with access control.

---

## Document Library

### Overview

The Document Library stores PDF files in a hierarchical folder structure. Key features:

- **Nested folders** - Unlimited folder depth
- **Access control** - Member or Admin visibility per folder
- **In-browser viewing** - View PDFs without downloading
- **File management** - Upload, rename, move, delete
- **Sortable** - Custom ordering for folders and files

### Folder Structure

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

### Access Levels

| Level | Who Can See | Use For |
|-------|-------------|---------|
| Member | All logged-in users | Public documentation, guides, policies |
| Admin | Administrators only | Internal documents, sensitive files |

**Note:** Access level is set per folder. All files in a folder inherit that folder's access level.

### For Members (Viewing Documents)

1. Navigate to **Documents** from the menu
2. Browse folders to find documents
3. Click a document to view it in-browser
4. Click **Download** to save a local copy

### For Administrators and Managers (Managing Documents)

Both Admin and Manager roles have full document management capabilities.

#### Creating Folders

1. Navigate to the parent location (or root)
2. Click **New Folder**
3. Enter:
   - **Folder Name** - Descriptive name
   - **Access Level** - Member or Admin
4. Click **Create**

#### Uploading Files

1. Navigate to the target folder
2. Click **Upload**
3. Select one or more PDF files
4. Click **Upload**

Files appear in the folder with upload date and uploader name.

#### Renaming Items

1. Click the item's menu (⋮ or right-click)
2. Select **Rename**
3. Enter the new name
4. Click **Save**

#### Moving Items

1. Click the item's menu
2. Select **Move**
3. Choose the destination folder from the tree
4. Click **Move**

**Note:** You cannot move a folder into itself or its descendants.

#### Deleting Items

1. Click the item's menu
2. Select **Delete**
3. Confirm the deletion

**Warning:** Deleting a folder removes all contents (subfolders and files).

#### Changing Access Level

1. Click the folder's menu
2. Select **Change Access**
3. Select the new level
4. Click **Save**

#### Reordering

Drag and drop items to change their display order. The order is saved automatically.

### File Storage

- **Location:** `/ProtectedFiles/` directory
- **Naming:** Original filenames preserved
- **Deduplication:** If the same file is linked multiple times, physical deletion only occurs when all links are removed

---

## Photo Gallery

### Overview

The Photo Gallery stores images in a category-based hierarchy. Key features:

- **Nested categories** - Organize images in subcategories
- **Automatic thumbnails** - Generated on upload for fast browsing
- **Image optimization** - Automatic quality optimization
- **Batch upload** - Upload multiple images at once
- **Descriptions** - Add descriptions to categories and images

### Category Structure

```
Gallery/
├── Events (Member)
│   ├── 2024 Annual Meeting/
│   │   ├── image1.jpg
│   │   └── image2.jpg
│   └── Holiday Party/
└── Internal (Admin)
    └── Team Photos/
```

### Access Levels

| Level | Who Can See | Use For |
|-------|-------------|---------|
| Member | All logged-in users | Public photos, event galleries |
| Admin | Administrators only | Internal images, drafts |

### For Members (Viewing Gallery)

1. Navigate to **Gallery** from the menu
2. Browse categories
3. Click thumbnails to view full-size images
4. Navigate with breadcrumbs

### For Administrators and Managers (Managing Gallery)

Both Admin and Manager roles have full gallery management capabilities.

#### Creating Categories

1. Navigate to the parent category (or root)
2. Click **New Category**
3. Enter:
   - **Category Name**
   - **Description** (optional)
   - **Access Level**
4. Click **Create**

#### Uploading Images

**Single Upload:**
1. Navigate to the target category
2. Click **Upload**
3. Select an image file
4. Click **Upload**

**Batch Upload:**
1. Navigate to the target category
2. Click **Upload**
3. Select multiple image files (Ctrl+click or Shift+click)
4. Click **Upload**

Supported formats: JPG, JPEG, PNG, GIF, WebP, BMP

#### Image Processing

On upload, the system automatically:
1. Optimizes the image quality
2. Generates a thumbnail (filename_thumb.ext)
3. Records metadata (uploader, date)

#### Managing Images

**Rename:**
1. Click the image menu
2. Select **Rename**
3. Enter new name (without extension)
4. Click **Save**

**Add/Edit Description:**
1. Click the image menu
2. Select **Edit Description**
3. Enter description text
4. Click **Save**

**Move:**
1. Click the image menu
2. Select **Move**
3. Choose destination category
4. Click **Move**

**Delete:**
1. Click the image menu
2. Select **Delete**
3. Confirm deletion

#### Managing Categories

**Rename Category:**
1. Click category menu
2. Select **Rename**
3. Enter new name
4. Click **Save**

**Edit Description:**
1. Click category menu
2. Select **Edit Description**
3. Enter description
4. Click **Save**

**Move Category:**
1. Click category menu
2. Select **Move**
3. Choose destination
4. Click **Move**

**Change Access Level:**
1. Click category menu
2. Select **Change Access**
3. Select new level
4. Click **Save**

**Delete Category:**
1. Click category menu
2. Select **Delete**
3. Confirm (removes all contents)

#### Reordering

Drag and drop categories and images to customize display order.

### File Storage

- **Location:** `/wwwroot/Galleries/` directory
- **Thumbnails:** Same location with `_thumb` suffix
- **Public access:** Images served as static files

---

## Links Directory

### Overview

The Links Directory provides a simple way to organize and display external links.

### Structure

```
Links/
├── Resources (Public)
│   ├── Documentation → https://docs.example.com
│   └── Support → https://support.example.com
└── Admin Tools (Admin Only)
    └── Server Panel → https://admin.example.com
```

### For Members

1. Navigate to **More Links**
2. Browse categories
3. Click links to open in new tab

### For Administrators and Managers

#### Managing Categories

1. Navigate to **More Links** → **Manage Categories**
2. **Add Category:**
   - Enter category name
   - Set sort order
   - Check "Admin Only" if needed
   - Click **Add**
3. **Edit/Delete:** Use the action buttons

#### Managing Links

1. Select a category
2. Click **Manage Links**
3. **Add Link:**
   - Enter link name (display text)
   - Enter full URL (include https://)
   - Set sort order
   - Click **Add**
4. **Edit/Delete:** Use the action buttons

---

## Best Practices

### Organization

1. **Plan your structure** - Think about categories before creating
2. **Use descriptive names** - Clear names help users find content
3. **Consistent access levels** - Group similar content together
4. **Don't over-nest** - 2-3 levels deep is usually sufficient

### Content Management

1. **Meaningful filenames** - Rename files to be descriptive
2. **Add descriptions** - Help users understand content
3. **Regular cleanup** - Remove outdated content
4. **Backup regularly** - Backup uploaded files

### Security

1. **Appropriate access levels** - Don't make sensitive content public
2. **Review before publishing** - Check content before making available
3. **Monitor uploads** - Watch for inappropriate content

---

## Storage Locations Summary

| Content Type | Storage Location | Access Method |
|--------------|------------------|---------------|
| PDF Documents | `/ProtectedFiles/` | Controller-mediated |
| Gallery Images | `/wwwroot/Galleries/` | Direct static file |
| Thumbnails | `/wwwroot/Galleries/` | Direct static file |

### Backup Recommendations

For full backup, include:
1. Database (all metadata and settings)
2. `/ProtectedFiles/` directory (documents)
3. `/wwwroot/Galleries/` directory (images)

---

## Troubleshooting

### Documents

**"File not found" error:**
- Check the file exists in `/ProtectedFiles/`
- Verify database entry exists

**Can't upload files:**
- Check folder write permissions
- Verify file is a valid PDF
- Check file size limits

**Can't view PDF in browser:**
- Ensure browser supports PDF viewing
- Try downloading instead

### Gallery

**Thumbnails not generating:**
- Check ImageSharp package is installed
- Verify write permissions on Galleries folder
- Check for errors in logs

**Images not displaying:**
- Verify files exist in `/wwwroot/Galleries/`
- Check file permissions
- Clear browser cache

**Upload fails:**
- Check file size limits
- Verify image format is supported
- Check folder permissions

### Links

**Links not opening:**
- Verify URL includes protocol (https://)
- Check URL is valid
- Test URL manually

---

**Version:** 1.0.0
**Framework:** Ape Framework
**Site:** https://Illustrate.net
