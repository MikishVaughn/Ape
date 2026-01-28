using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace Ape.Models
{
    public class UserProfiles
    {
        [Key]
        [ForeignKey("User")] // Links this profile to a specific IdentityUser
        public required string UserId { get; set; }
        public string? FirstName { get; set; }
        public string? MiddleName { get; set; }
        public string? LastName { get; set; }
        public DateTime? Birthday { get; set; }
        public DateTime? Anniversary { get; set; }
        public string? AddressLine1 { get; set; }
        public string? AddressLine2 { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? ZipCode { get; set; }
       
        public string? IsMinor { get; set; }
        public string? AgeVerified { get; set; }

        public DateTime? ParentalConsentDate { get; set; }

        public string? HomePhoneNumber { get; set; }
        public DateTime? LastLogin { get; set; }
        public DateTime? LastActivity { get; set; }

        // Soft Delete Fields
        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Deactivated Date")]
        [DataType(DataType.DateTime)]
        public DateTime? DeactivatedDate { get; set; }

        [StringLength(100)]
        [Display(Name = "Deactivated By")]
        public string? DeactivatedBy { get; set; }

        [StringLength(500)]
        [Display(Name = "Deactivation Reason")]
        public string? DeactivationReason { get; set; }       

        public required IdentityUser User { get; set; }
    }
}