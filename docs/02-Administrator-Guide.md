# Ape Framework - Administrator Guide

## Introduction

This guide covers all administrative features available to users with the **Admin** or **Manager** role. Both roles have access to system configuration, user management, and content management tools.

**Key Difference:** The Manager role has the same access as Admin, except Managers **cannot access System Credentials**. This allows you to delegate administrative tasks while keeping sensitive configuration secure.

---

## Admin Menu Overview

After logging in as an Admin or Manager, you'll see additional menu options:

| Menu Item | Purpose | Admin | Manager |
|-----------|---------|:-----:|:-------:|
| System Credentials | Manage encrypted site credentials | ✓ | ✗ |
| Email Test | Test email configuration | ✓ | ✓ |
| Email Logs | View sent email history | ✓ | ✓ |
| Active Users | Monitor user activity | ✓ | ✓ |
| Contact Settings | Configure contact form recipients | ✓ | ✓ |
| Documents | Manage PDF document library | ✓ | ✓ |
| Gallery | Manage photo gallery | ✓ | ✓ |
| More Links | Manage link categories | ✓ | ✓ |

---

## System Credentials Management

The System Credentials page allows you to securely store and manage sensitive configuration values like API keys, database connections, and email credentials.

### Security Features

- **AES-256 Encryption:** All values encrypted before database storage
- **Audit Trail:** Every access (view, create, edit, delete) is logged
- **Categorization:** Organize credentials by type (Database, API, Email, etc.)
- **Built-in Testing:** Test database connections and API keys directly

### Adding a Credential

1. Navigate to **System Credentials**
2. Click **Add New Credential**
3. Fill in the fields:
   - **Key:** Unique identifier (e.g., `SMTP_PASSWORD`)
   - **Name:** Friendly name (e.g., "SMTP Password")
   - **Category:** Select appropriate category
   - **Value:** The secret value (will be encrypted)
4. Click **Save**

### Viewing Credentials

- Credentials are displayed with masked values by default
- Click **Show** to reveal the decrypted value (this action is logged)
- Use **Test** buttons where available to verify credentials work

### Audit History

1. Click **Audit History** on any credential
2. View all access events including:
   - Who accessed it
   - What action was taken
   - When it occurred
   - IP address and user agent

---

## Email System Administration

### Email Test Page

The Email Test page helps you verify your email configuration:

1. Navigate to **Email Test**
2. Enter a recipient email address
3. Click **Test Azure Email** or **Test SMTP Email**
4. Check results for success or error messages

### Email Logs

View all sent emails and their status:

1. Navigate to **Email Logs**
2. Use filters to search by:
   - Date range
   - Recipient
   - Status (success/failed)
   - Email type
3. View details including error messages for failed emails
4. Clear old logs using the cleanup options

### Email Failover System

The framework uses a dual-provider system:
- **Primary:** Azure Communication Services
- **Fallback:** SMTP

If Azure fails 3 consecutive times, the system automatically skips Azure for 2 minutes and uses SMTP directly. This prevents delays when Azure is unavailable.

---

## Contact Form Settings

Configure who receives messages from the public contact form:

1. Navigate to **Contact Form Settings**
2. Enter email addresses separated by commas
3. Click **Save**

Example: `admin@example.com, support@example.com`

### Anti-Spam Protection

The contact form includes three layers of protection:
1. **Honeypot fields:** Hidden fields that bots fill out
2. **JavaScript token:** Required token that bots can't generate
3. **Timing validation:** Form must be open for 3+ seconds

Spam attempts are logged with IP addresses in the email logs.

---

## User Activity Monitoring

### Active Users Dashboard

Monitor who's currently using your site:

1. Navigate to **Active Users**
2. View users grouped by status:
   - **Active Now:** Activity within last 5 minutes
   - **Active Recently:** Activity within last 30 minutes
   - **Idle:** Activity within last 12 hours
   - **Session Expired:** No activity for 12+ hours

### User Information Displayed

- User email
- Last activity timestamp
- Activity status indicator

---

## Document Library Administration

As an admin, you have full control over the document library.

### Access Levels

- **Member:** Documents visible to all authenticated users
- **Admin:** Documents visible only to administrators

### Folder Management

**Create Folder:**
1. Navigate to the parent location
2. Click **New Folder**
3. Enter folder name
4. Select access level
5. Click **Create**

**Rename Folder:**
1. Click the folder's menu (⋮)
2. Select **Rename**
3. Enter new name
4. Click **Save**

**Move Folder:**
1. Click the folder's menu (⋮)
2. Select **Move**
3. Choose destination folder
4. Click **Move**

**Delete Folder:**
1. Click the folder's menu (⋮)
2. Select **Delete**
3. Confirm deletion (removes all contents)

**Change Access Level:**
1. Click the folder's menu (⋮)
2. Select **Change Access**
3. Choose new level
4. Click **Save**

### File Management

**Upload Files:**
1. Navigate to the target folder
2. Click **Upload**
3. Select PDF file(s)
4. Click **Upload**

**Rename File:**
1. Click the file's menu (⋮)
2. Select **Rename**
3. Enter new name
4. Click **Save**

**Move File:**
1. Click the file's menu (⋮)
2. Select **Move**
3. Choose destination folder
4. Click **Move**

**Delete File:**
1. Click the file's menu (⋮)
2. Select **Delete**
3. Confirm deletion

### Reordering

Drag and drop folders and files to reorder them. The sort order is saved automatically.

---

## Photo Gallery Administration

### Category Management

Works similarly to document folders:

- Create categories and subcategories
- Set access levels (Member/Admin)
- Add descriptions to categories
- Rename, move, and delete categories

### Image Management

**Upload Images:**
1. Navigate to target category
2. Click **Upload**
3. Select image file(s) - supports JPG, PNG, GIF, WebP
4. Images are optimized and thumbnails are generated automatically

**Edit Image:**
1. Click the image's menu
2. Options: Rename, Move, Delete, Edit Description

**Batch Upload:**
Select multiple images at once for bulk upload.

### Storage Location

- Images stored in: `/wwwroot/Galleries/`
- Thumbnails: `filename_thumb.extension`

---

## Links Management

### Category Management

1. Navigate to **More Links** → **Manage Categories**
2. Add categories with:
   - Category name
   - Sort order
   - Admin-only visibility option

### Link Management

1. Select a category
2. Click **Manage Links**
3. Add links with:
   - Link name (display text)
   - URL (full URL including https://)
   - Sort order

### Visibility

- **Admin-only categories:** Only visible to administrators
- **Public categories:** Visible to all members

---

## Security Best Practices

### Credential Management

1. **Rotate credentials regularly** - Update passwords and API keys periodically
2. **Use strong master key** - At least 32 characters with mixed characters
3. **Review audit logs** - Check for unauthorized access attempts
4. **Limit admin accounts** - Only grant admin access when necessary

### User Management

1. **Require email confirmation** - Enabled by default
2. **Encourage 2FA** - Recommend users enable two-factor authentication
3. **Monitor active users** - Watch for suspicious activity patterns

### Content Security

1. **Use appropriate access levels** - Don't make admin documents public
2. **Review uploaded content** - Check documents and images before publishing
3. **Regular backups** - Back up the database and uploaded files

---

## Maintenance Tasks

### Regular Maintenance

- **Clear old email logs** - Remove logs older than needed
- **Review credential audit logs** - Check for suspicious access
- **Update credentials** - Rotate API keys and passwords
- **Database backup** - Regular backups of the database
- **File backup** - Backup `/ProtectedFiles/` and `/wwwroot/Galleries/`

### Troubleshooting

**Users can't receive emails:**
1. Check Email Test page
2. Review Email Logs for errors
3. Verify credentials in System Credentials

**Files not uploading:**
1. Check folder permissions
2. Verify file types (PDF for documents, images for gallery)
3. Check file size limits

**Users locked out:**
1. Check Active Users for their status
2. Review their account in Identity management
3. Reset their password if needed

---

**Version:** 1.0.0
**Framework:** Ape Framework
**Site:** https://Illustrate.net
