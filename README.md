# Ape Framework

A production-ready ASP.NET Core MVC framework with comprehensive authentication, role-based access control, document management, and administrative tools. This framework provides a solid foundation for building secure, enterprise-grade web applications.

## Live Demo

**[https://Illustrate.net](https://Illustrate.net)**

## Features

### Authentication & Authorization
- **ASP.NET Core Identity** with full user management
- **Role-Based Access Control** (Admin, Member roles pre-configured)
- **Two-Factor Authentication (2FA)** with authenticator app support
- **Email Verification** for new accounts
- **Password Reset** via email
- **Recovery Codes** for 2FA backup
- **User Profile Management** with extended fields (address, phone, dates)
- **Activity Tracking** - monitors user presence and last activity

### Email System
- **Dual-Provider Architecture**: Azure Communication Services (primary) with SMTP fallback
- **Automatic Failover**: Tracks consecutive failures and gracefully falls back to SMTP
- **Email Logging**: All sent emails logged with status, provider used, and error details
- **Attachment Support**: Send emails with file attachments via both providers
- **Admin Dashboard**: View, search, and filter email logs; clear old entries

### Contact Form
- **Configurable Recipients**: Admin-editable list of email recipients
- **Anti-Spam Protection**: Triple-layer protection (honeypot fields, JavaScript token, timing validation)
- **HTML Email Templates**: Professional formatting for contact submissions
- **Spam Attempt Logging**: Records blocked attempts with IP addresses

### Document Library
- **Hierarchical Organization**: Nested folders with unlimited depth
- **Access Control**: Member-level and Admin-level document categories
- **PDF Management**: Upload, view, download, rename, move, and delete PDF files
- **Folder Management**: Create, rename, move, delete, and reorder folders
- **Breadcrumb Navigation**: Easy navigation through folder hierarchy
- **Sortable Content**: Drag-and-drop reordering support

### Photo Gallery
- **Category System**: Nested categories with unlimited depth
- **Access Control**: Member-level and Admin-level gallery categories
- **Image Management**: Upload, rename, move, delete images
- **Automatic Thumbnails**: Generated on upload for optimized display
- **Batch Upload**: Upload multiple images at once
- **Image Optimization**: Using SixLabors.ImageSharp

### Links Directory
- **Category Organization**: Group links into categories
- **Access Control**: Public and Admin-only link categories
- **CRUD Operations**: Full management of categories and links
- **Sortable Display**: Customizable display order

### System Credentials Management
- **Secure Storage**: AES-256 encryption for all credentials
- **Categorized Organization**: Database, API, Email, Billing, Site categories
- **Full Audit Trail**: Every access logged (view, create, update, delete, test)
- **Built-in Testing**: Test database connections and API keys directly
- **Environment Variable Fallback**: Supports legacy configurations

### Administrative Tools
- **Active Users Dashboard**: Real-time view of user activity status
- **Email Test Console**: Test Azure and SMTP email configurations
- **Email Log Viewer**: Search, filter, and manage sent email records
- **System Settings**: Key-value configuration storage

### UI/UX
- **Responsive Layout**: Mobile-friendly design
- **Partial Header/Footer**: Modular layout with sticky footer
- **Menu System**: Role-aware navigation
- **Bootstrap Styling**: Clean, professional appearance

## Technology Stack

- **Framework**: ASP.NET Core 10.0 (MVC + Razor Pages)
- **Database**: SQL Server with Entity Framework Core
- **Authentication**: ASP.NET Core Identity
- **Email**: Azure Communication Services + SMTP
- **Image Processing**: SixLabors.ImageSharp
- **Frontend**: Bootstrap, jQuery

## Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download) or later
- SQL Server (LocalDB for development, full SQL Server for production)
- Azure Communication Services account (optional, for Azure email)
- SMTP server access (for fallback/primary email)

## Installation

### 1. Clone the Repository

```bash
git clone https://github.com/MikishVaughn/Ape.git
cd Ape
```

### 2. Set Environment Variables

#### Development (Windows)
Set these in your system environment variables or use `launchSettings.json`:

```
MASTER_CREDENTIAL_KEY_ILLUSTRATE=YourSecure32CharacterOrLongerKey!
```

#### Development (User Secrets - Recommended)
```bash
cd Ape
dotnet user-secrets set "MASTER_CREDENTIAL_KEY_ILLUSTRATE" "YourSecure32CharacterOrLongerKey!"
```

#### Production
Set these at the IIS Application Pool level or in your hosting environment:

```
DB_SERVER_ILLUSTRATE=your-database-server
DB_NAME_ILLUSTRATE=your-database-name
DB_USER_ILLUSTRATE=your-database-user
DB_PASSWORD_ILLUSTRATE=your-database-password
MASTER_CREDENTIAL_KEY_ILLUSTRATE=YourSecure32CharacterOrLongerKey!
```

> **Important**: The master credential key must be at least 32 characters long and should be kept secure. This key encrypts all stored credentials.

### 3. Configure the Database Connection

#### Development
The default connection string in `appsettings.json` uses LocalDB:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=aspnet-Ape;Trusted_Connection=True;MultipleActiveResultSets=true"
  }
}
```

#### Production
Set the environment variables listed above. The application will construct the connection string automatically.

### 4. Run Database Migrations

```bash
cd Ape
dotnet ef database update
```

Or let the application auto-migrate on first run (configured by default).

### 5. Build and Run

```bash
dotnet build
dotnet run
```

Navigate to `https://localhost:5001` or the URL shown in the console.

### 6. Initial Admin Account

On first run, if no admin users exist, the system creates a default admin account:
- **Email**: admin@admin.com
- **Password**: Admin123!

> **Important**: Change this password immediately after first login!

You can also specify custom admin credentials in `appsettings.json`:
```json
{
  "AdminEmail": "your-admin@example.com",
  "AdminPassword": "YourSecurePassword123!"
}
```

## Configuration

### Email Configuration

After logging in as Admin, navigate to **System Credentials** to configure email settings:

#### Azure Communication Services (Primary)
| Credential Key | Description |
|----------------|-------------|
| `AZURE_COMMUNICATION_CONNECTION_STRING` | Your Azure Communication Services connection string |
| `AZURE_EMAIL_FROM` | The verified sender email address |

#### SMTP (Fallback)
| Credential Key | Description |
|----------------|-------------|
| `SMTP_SERVER` | SMTP server address (e.g., smtp.gmail.com) |
| `SMTP_PORT` | SMTP port (typically 587 for TLS) |
| `SMTP_USERNAME` | SMTP authentication username |
| `SMTP_PASSWORD` | SMTP authentication password |
| `SMTP_SSL` | Enable SSL/TLS (true/false) |

#### Site Settings
| Credential Key | Description |
|----------------|-------------|
| `SITE_NAME` | Your site name (used in email templates) |

### Contact Form Recipients

1. Navigate to **Admin > Contact Form Settings**
2. Enter email addresses separated by commas
3. Save changes

## Usage Guide

### For Administrators

#### Managing Users
- User management is handled through ASP.NET Core Identity
- Users can be assigned to Admin or Member roles
- View active users in the **Active Users** dashboard

#### Document Library
1. Navigate to **Documents** (Admin view)
2. Create categories using **New Folder**
3. Set **Access Level** (Member or Admin)
4. Upload PDFs to categories
5. Organize with drag-and-drop reordering

#### Photo Gallery
1. Navigate to **Gallery** (Admin view)
2. Create categories for organization
3. Set **Access Level** as needed
4. Upload images (thumbnails generated automatically)
5. Add descriptions and reorder as needed

#### Links Management
1. Navigate to **More Links** (Admin view)
2. Create categories for grouping
3. Add links with names and URLs
4. Set visibility (public or admin-only)

#### System Credentials
1. Navigate to **System Credentials**
2. Add new credentials with appropriate category
3. Values are encrypted before storage
4. Use **Test** buttons to verify configurations
5. View audit history for security tracking

### For Members

- **Documents**: Browse and download documents in Member-accessible categories
- **Gallery**: View images in Member-accessible categories
- **Links**: Access public link categories
- **Profile**: Update personal information, enable 2FA

## Project Structure

```
Ape/
├── Areas/
│   └── Identity/Pages/Account/    # Identity Razor Pages (login, register, 2FA)
├── Controllers/
│   ├── InfoController.cs          # Public pages, contact form
│   ├── DocumentController.cs      # Document library management
│   ├── GalleryController.cs       # Photo gallery management
│   ├── LinksController.cs         # Links directory
│   ├── SystemCredentialsController.cs  # Credential management
│   ├── EmailLogController.cs      # Email log viewer
│   ├── EmailTestController.cs     # Email testing tools
│   ├── ContactFormSettingsController.cs  # Contact recipients
│   └── ActiveUsersController.cs   # User activity dashboard
├── Data/
│   ├── ApplicationDbContext.cs    # EF Core context
│   └── Migrations/                # Database migrations
├── Models/
│   ├── PDFCategory.cs             # Document categories
│   ├── CategoryFile.cs            # Document files
│   ├── GalleryCategory.cs         # Gallery categories
│   ├── GalleryImage.cs            # Gallery images
│   ├── LinkCategory.cs            # Link categories
│   ├── CategoryLink.cs            # Links
│   ├── SystemCredential.cs        # Encrypted credentials
│   ├── CredentialAuditLog.cs      # Credential access log
│   ├── EmailLog.cs                # Email sending log
│   ├── SystemSetting.cs           # System settings
│   ├── UserProfiles.cs            # Extended user data
│   └── ViewModels/                # View-specific models
├── Services/
│   ├── CredentialEncryptionService.cs   # AES-256 encryption
│   ├── SecureConfigurationService.cs    # Credential retrieval
│   ├── EnhancedEmailService.cs          # Email sending (Azure + SMTP)
│   ├── DocumentManagementService.cs     # Document operations
│   ├── GalleryManagementService.cs      # Gallery operations
│   ├── ImageOptimizationService.cs      # Image processing
│   └── SystemSettingsService.cs         # Settings management
├── Middleware/
│   └── ActivityTrackingMiddleware.cs    # User activity tracking
├── Views/                         # Razor views
├── wwwroot/
│   ├── css/                       # Stylesheets
│   ├── Galleries/                 # Uploaded gallery images
│   └── Images/                    # Site images
├── ProtectedFiles/                # Uploaded PDF documents
├── Program.cs                     # Application startup
└── appsettings.json              # Configuration
```

## Database Schema

### Identity Tables
- `AspNetUsers` - User accounts
- `AspNetRoles` - Roles (Admin, Member)
- `AspNetUserRoles` - User-role assignments
- `AspNetUserClaims`, `AspNetRoleClaims` - Claims
- `AspNetUserLogins`, `AspNetUserTokens` - External logins, tokens

### Application Tables
- `UserProfiles` - Extended user information
- `SystemCredentials` - Encrypted credentials
- `CredentialAuditLogs` - Credential access history
- `EmailLogs` - Email sending history
- `SystemSettings` - Key-value settings
- `PDFCategories` - Document folders
- `CategoryFiles` - Document files
- `GalleryCategories` - Image categories
- `GalleryImages` - Gallery images
- `LinkCategories` - Link groups
- `CategoryLinks` - Individual links

## Security Considerations

### Encryption
- All credentials stored with AES-256 encryption
- Master key stored in environment variables (not in code)
- IV randomly generated for each encryption

### Authentication
- Email confirmation required for new accounts
- Two-factor authentication available
- Account lockout after failed attempts
- Secure password requirements enforced

### Authorization
- Role-based access control throughout
- Admin-only areas protected
- Document/Gallery access levels enforced

### Audit Trail
- All credential access logged
- Email sending logged
- User activity tracked

### Data Protection
- CSRF protection on all forms
- Input validation and sanitization
- Parameterized database queries (EF Core)

## Deployment

### IIS Deployment

1. Publish the application:
   ```bash
   dotnet publish -c Release -o ./publish
   ```

2. Create an IIS website pointing to the publish folder

3. Set Application Pool to "No Managed Code"

4. Configure environment variables at the Application Pool level:
   - Open IIS Manager
   - Select the Application Pool
   - Click "Advanced Settings"
   - Under "Environment Variables" add your keys

5. Ensure the application has write access to:
   - `/ProtectedFiles/` (documents)
   - `/wwwroot/Galleries/` (images)

### Azure App Service

1. Create an Azure App Service

2. Configure Application Settings:
   - `DB_SERVER_ILLUSTRATE`
   - `DB_NAME_ILLUSTRATE`
   - `DB_USER_ILLUSTRATE`
   - `DB_PASSWORD_ILLUSTRATE`
   - `MASTER_CREDENTIAL_KEY_ILLUSTRATE`

3. Deploy via Visual Studio, GitHub Actions, or Azure CLI

## Migration Commands

```bash
# Add a new migration
dotnet ef migrations add MigrationName

# Update database
dotnet ef database update

# Remove last migration (if not applied)
dotnet ef migrations remove

# Generate SQL script
dotnet ef migrations script
```

## Troubleshooting

### Email Not Sending
1. Check **Email Test** page to verify configuration
2. Review **Email Logs** for error messages
3. Verify credentials in **System Credentials**
4. Ensure firewall allows SMTP port (typically 587)

### Database Connection Failed
1. Verify environment variables are set correctly
2. Test connection in **System Credentials** page
3. Check SQL Server is running and accessible
4. Verify firewall rules for SQL Server port (1433)

### 2FA Not Working
1. Ensure server time is synchronized (TOTP is time-based)
2. Have user regenerate authenticator setup
3. Use recovery codes if authenticator is lost

### Files Not Uploading
1. Check folder permissions on `/ProtectedFiles/` and `/wwwroot/Galleries/`
2. Verify file size limits in web.config/IIS settings
3. Ensure correct file types (PDF for documents, images for gallery)

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

- [ASP.NET Core](https://docs.microsoft.com/aspnet/core)
- [Entity Framework Core](https://docs.microsoft.com/ef/core)
- [Azure Communication Services](https://azure.microsoft.com/services/communication-services)
- [SixLabors.ImageSharp](https://sixlabors.com/products/imagesharp)
- [Bootstrap](https://getbootstrap.com)

---

**Version**: 1.0.0
**Last Updated**: January 2026
