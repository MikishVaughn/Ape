# Ape Framework - Photo Gallery Guide

## Introduction

The Photo Gallery provides a full-featured image management system with hierarchical categories, automatic thumbnail generation, and role-based access control. This guide covers all aspects of using and managing the gallery.

---

## Features Overview

- **Hierarchical Categories** - Organize images in nested categories with unlimited depth
- **Access Control** - Member-level and Admin-level category visibility
- **Automatic Thumbnails** - Generated on upload for fast browsing
- **Image Optimization** - Automatic quality optimization using ImageSharp
- **Batch Upload** - Upload multiple images at once
- **Public Access** - Optional public gallery viewing without login
- **Responsive Design** - Mobile-friendly grid layout with pagination

---

## Accessing the Gallery

### Public Users (if enabled)
- Navigate to the gallery URL directly
- View public categories and images
- No management capabilities

### Members
1. Log in to your account
2. Navigate to **Welcome** > **Image Gallery**
3. Browse categories and view images
4. Download images (if permitted)

### Administrators and Managers
1. Log in with Admin or Manager role
2. Navigate to **Welcome** > **Image Gallery**
3. Full management controls are displayed
4. Create, edit, move, and delete categories and images

---

## Browsing the Gallery

### Navigation Elements

**Category Tree (Sidebar)**
- Shows all accessible categories in a tree structure
- Click a category to view its contents
- Expand/collapse nested categories
- Current category is highlighted

**Breadcrumb Navigation**
- Shows your current location in the hierarchy
- Click any breadcrumb to jump to that level
- "Gallery" returns to the root

**Categories Grid**
- Displays subcategories as folder cards
- Shows image count for each category
- Lock icon indicates Admin-only categories
- Click to enter a category

**Images Grid**
- Displays images as thumbnail cards
- Pagination controls for large galleries
- Click thumbnail to view full-size image

### Pagination

The gallery displays 24 images per page by default. Use the pagination controls at the bottom to navigate:
- Page numbers for direct access
- Previous/Next buttons
- Page size can be adjusted (12, 24, 36, 48)

---

## Category Management

*Requires Admin or Manager role*

### Creating Categories

1. Navigate to the desired parent location (or root for top-level)
2. Click **+ New Category**
3. Fill in the form:
   - **Category Name** - Descriptive name (required)
   - **Description** - Optional description shown in the header
   - **Access Level** - Member (all users) or Admin (restricted)
4. Click **Create**

### Editing Categories

1. Find the category in the grid
2. Click **Edit** button
3. Modify:
   - **Name** - Update the category name
   - **Description** - Add or change description
   - **Access Level** - Change visibility
4. Click **Save Changes**

### Moving Categories

1. Click **Move** on the category card
2. Select the destination from the category tree
3. Choose "Gallery Root" to make it a top-level category
4. Click **Move**

**Note:** You cannot move a category into itself or its descendants.

### Deleting Categories

1. Click **Delete** on the category card
2. Confirm the deletion

**Warning:** Deleting a category removes ALL contents including:
- All subcategories (recursively)
- All images in the category and subcategories

---

## Image Management

*Requires Admin or Manager role*

### Uploading Images

**Single Upload:**
1. Navigate to the target category
2. Click **+ Upload Images**
3. Click **Choose Files** or drag and drop
4. Select an image file
5. Click **Upload**

**Batch Upload:**
1. Navigate to the target category
2. Click **+ Upload Images**
3. Select multiple files (Ctrl+click or Shift+click)
4. All selected images upload sequentially
5. Progress shown for each file

**Supported Formats:**
- JPEG / JPG
- PNG
- GIF
- WebP
- BMP

**File Size:** Maximum size depends on server configuration (typically 10-50MB)

### Image Processing

On upload, the system automatically:

1. **Validates** the file type and size
2. **Optimizes** image quality (reduces file size while maintaining visual quality)
3. **Generates thumbnail** (smaller version for grid display)
4. **Records metadata** (uploader, date, original filename)

Thumbnails are named: `originalname_thumb.extension`

### Renaming Images

1. Click the **Edit** button on the image card
2. Enter the new name (without extension)
3. The original file extension is preserved automatically
4. Click **Save**

**Example:**
- Original: `vacation-photo.jpg`
- Enter: `beach-sunset`
- Result: `beach-sunset.jpg`

### Adding/Editing Descriptions

1. Click **Edit** on the image card
2. Enter or modify the description text
3. Click **Save**

Descriptions appear below the image when viewing.

### Moving Images

1. Click **Move** on the image card
2. Select the destination category from the tree
3. Click **Move**

### Deleting Images

1. Click **Delete** on the image card
2. Confirm the deletion

This permanently removes:
- The full-size image file
- The thumbnail file
- The database record

### Bulk Operations

**Select Multiple Images:**
1. Click the checkbox on image cards to select
2. Selected count shows in the toolbar
3. Use bulk action buttons:
   - **Move Selected** - Move all to another category
   - **Delete Selected** - Remove all selected images

**Select All:**
- Use "Select All" checkbox in the toolbar
- Applies to current page only

---

## Access Levels

### Member Level (Default)
- Visible to all authenticated users
- Appropriate for general photos, public events

### Admin Level
- Visible only to Admin and Manager roles
- Use for internal images, drafts, sensitive content
- Indicated by lock icon and "Admin Only" badge

### Changing Access Level

1. Edit the category
2. Select new Access Level
3. Save changes

**Note:** Access level applies to the category and all images within it. Subcategories can have different access levels.

---

## Viewing Images

### Thumbnail Grid
- Click any thumbnail to view the full-size image
- Image opens in a lightbox/modal view
- Navigate between images with arrows
- Close with X or click outside

### Image Details
When viewing an image, you can see:
- Full-size image
- Image name
- Description (if set)
- Upload date and uploader

### Downloading
- Right-click the full-size image to save
- Or use the Download button if available

---

## Storage and Files

### Storage Location
```
/wwwroot/Galleries/
├── image1.jpg
├── image1_thumb.jpg
├── image2.png
├── image2_thumb.png
└── ...
```

### File Naming
- Original filename is preserved in the database
- Physical files may be renamed to avoid conflicts
- Thumbnails use `_thumb` suffix

### Backup Recommendations
To backup the gallery:
1. **Database** - Contains category structure and image metadata
2. **Files** - `/wwwroot/Galleries/` directory with all images

Both are required for a complete restore.

---

## Best Practices

### Organization
1. **Plan your structure** - Design categories before uploading
2. **Use descriptive names** - Clear category and image names
3. **Appropriate depth** - 2-3 levels is usually sufficient
4. **Consistent access levels** - Group similar content together

### Image Quality
1. **Optimize before upload** - Pre-optimize very large images
2. **Appropriate resolution** - Web images don't need print resolution
3. **Consistent format** - JPEG for photos, PNG for graphics

### Content Management
1. **Add descriptions** - Help users understand context
2. **Regular cleanup** - Remove outdated images
3. **Review access levels** - Ensure sensitive content is protected

---

## Troubleshooting

### Images Not Uploading

**Check file type:**
- Ensure file is a supported image format
- Verify file extension matches content

**Check file size:**
- Server may have upload limits
- Try a smaller image

**Check permissions:**
- Verify `/wwwroot/Galleries/` is writable
- Check application pool identity permissions

### Thumbnails Not Generating

**Check ImageSharp:**
- Verify SixLabors.ImageSharp package is installed
- Check for errors in application logs

**Check disk space:**
- Ensure sufficient space for thumbnails
- Each thumbnail adds ~10-50KB

### Images Not Displaying

**Check file exists:**
- Verify files in `/wwwroot/Galleries/`
- Check for file permission issues

**Check database:**
- Verify image record exists
- Check filename matches physical file

**Clear cache:**
- Browser may cache broken images
- Try hard refresh (Ctrl+F5)

### Access Denied Errors

**Check authentication:**
- Ensure you're logged in
- Verify your role has access

**Check category access level:**
- Admin-only categories require Admin or Manager role
- Check parent category access levels

---

## API Endpoints

For developers integrating with the gallery:

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/Gallery/Browse` | GET | Browse gallery with optional categoryId |
| `/Gallery/GetCategoryTree` | GET | Get category tree JSON |
| `/Gallery/CreateCategory` | POST | Create new category |
| `/Gallery/RenameCategory` | POST | Rename a category |
| `/Gallery/DeleteCategory` | POST | Delete category and contents |
| `/Gallery/UploadImages` | POST | Upload images (multipart) |
| `/Gallery/RenameImage` | POST | Rename an image |
| `/Gallery/MoveImage` | POST | Move image to category |
| `/Gallery/DeleteImage` | POST | Delete an image |

All POST endpoints require:
- Authentication (Admin or Manager role)
- Anti-forgery token

---

**Version:** 1.0.0
**Framework:** Ape Framework
**Site:** https://Illustrate.net
