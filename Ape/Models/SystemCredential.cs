using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ape.Models;

/// <summary>
/// Represents a securely stored system credential with encryption.
/// </summary>
public class SystemCredential
{
    [Key]
    public int CredentialID { get; set; }

    [Required]
    [MaxLength(100)]
    public string CredentialKey { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string CredentialName { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Category { get; set; } = string.Empty;

    [Required]
    public byte[] EncryptedValue { get; set; } = Array.Empty<byte>();

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    [MaxLength(100)]
    public string? CreatedBy { get; set; }

    public DateTime? UpdatedDate { get; set; }

    [MaxLength(100)]
    public string? UpdatedBy { get; set; }

    // Navigation property
    public virtual ICollection<CredentialAuditLog> AuditLogs { get; set; } = new List<CredentialAuditLog>();

    // Not mapped to database - used for display/edit in UI
    [NotMapped]
    public string? DecryptedValue { get; set; }
}

/// <summary>
/// Audit log entry for credential access and modifications.
/// </summary>
public class CredentialAuditLog
{
    [Key]
    public int AuditID { get; set; }

    [Required]
    public int CredentialID { get; set; }

    [Required]
    [MaxLength(100)]
    public string CredentialKey { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Action { get; set; } = string.Empty;

    public string? ActionDetails { get; set; }

    [MaxLength(100)]
    public string? ActionBy { get; set; }

    public DateTime ActionDate { get; set; } = DateTime.UtcNow;

    [MaxLength(50)]
    public string? IPAddress { get; set; }

    [MaxLength(500)]
    public string? UserAgent { get; set; }

    public bool? Success { get; set; }

    public string? ErrorMessage { get; set; }

    // Navigation property
    [ForeignKey("CredentialID")]
    public virtual SystemCredential? Credential { get; set; }
}

/// <summary>
/// Categories for organizing credentials.
/// </summary>
public static class CredentialCategory
{
    public const string Database = "Database";
    public const string API = "API";
    public const string Email = "Email";
    public const string Billing = "Billing";
    public const string Site = "Site";

    public static List<string> GetAll()
    {
        return new List<string> { Database, API, Email, Billing, Site };
    }
}

/// <summary>
/// Audit action types for credential logging.
/// </summary>
public static class CredentialAction
{
    public const string Created = "Created";
    public const string Updated = "Updated";
    public const string Viewed = "Viewed";
    public const string Deleted = "Deleted";
    public const string Tested = "Tested";
}
