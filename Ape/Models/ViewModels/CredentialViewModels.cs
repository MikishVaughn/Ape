using System.ComponentModel.DataAnnotations;

namespace Ape.Models.ViewModels;

/// <summary>
/// View model for displaying credentials in the admin interface.
/// </summary>
public class CredentialViewModel
{
    public int CredentialID { get; set; }
    public string CredentialKey { get; set; } = string.Empty;
    public string CredentialName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedDate { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedDate { get; set; }
    public string? UpdatedBy { get; set; }

    // Decrypted value (masked by default in UI)
    public string? DecryptedValue { get; set; }

    // For display in UI
    public string MaskedValue => new string('●', 20);
}

/// <summary>
/// View model for creating or editing credentials.
/// </summary>
public class CredentialEditViewModel
{
    public int? CredentialID { get; set; }

    [Required(ErrorMessage = "Credential key is required")]
    [MaxLength(100)]
    [Display(Name = "Credential Key")]
    public string CredentialKey { get; set; } = string.Empty;

    [Required(ErrorMessage = "Credential name is required")]
    [MaxLength(200)]
    [Display(Name = "Credential Name")]
    public string CredentialName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Category is required")]
    [Display(Name = "Category")]
    public string Category { get; set; } = string.Empty;

    [Required(ErrorMessage = "Credential value is required")]
    [Display(Name = "Credential Value")]
    public string CredentialValue { get; set; } = string.Empty;

    [Display(Name = "Description")]
    public string? Description { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// View model for the credentials index page with grouped credentials.
/// </summary>
public class CredentialsIndexViewModel
{
    public Dictionary<string, List<CredentialViewModel>> CredentialsByCategory { get; set; }
        = new Dictionary<string, List<CredentialViewModel>>();

    public List<string> Categories { get; set; } = new List<string>();

    public bool IsMasterKeyConfigured { get; set; }

    public int TotalCredentials { get; set; }
}

/// <summary>
/// View model for test connection results.
/// </summary>
public class TestConnectionResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? ErrorDetails { get; set; }
    public DateTime TestedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// View model for audit log entries.
/// </summary>
public class CredentialAuditViewModel
{
    public int AuditID { get; set; }
    public string CredentialKey { get; set; } = string.Empty;
    public string CredentialName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? ActionDetails { get; set; }
    public string? ActionBy { get; set; }
    public DateTime ActionDate { get; set; }
    public string? IPAddress { get; set; }
    public bool? Success { get; set; }
    public string? ErrorMessage { get; set; }
}
