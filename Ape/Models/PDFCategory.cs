using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ape.Models
{
    /// <summary>
    /// Access levels for document folders
    /// </summary>
    public enum DocumentAccessLevel
    {
        Member = 0,   // Visible to all members
        Admin = 1     // Visible to admins only
    }

    public class PDFCategory
    {
        [Key]
        public int CategoryID { get; set; }

        [Required]
        public required string CategoryName { get; set; }

        [Required]
        public int SortOrder { get; set; }

        /// <summary>
        /// Parent folder ID for hierarchical structure. Null = root level folder.
        /// </summary>
        public int? ParentCategoryID { get; set; }

        /// <summary>
        /// Access level for the folder.
        /// </summary>
        public DocumentAccessLevel AccessLevel { get; set; } = DocumentAccessLevel.Member;

        // Navigation property to parent folder
        [ForeignKey("ParentCategoryID")]
        public virtual PDFCategory? ParentCategory { get; set; }

        // Navigation property to child folders
        public virtual ICollection<PDFCategory> ChildCategories { get; set; } = [];

        // Navigation property to the CategoryFiles
        public virtual ICollection<CategoryFile> CategoryFiles { get; set; } = [];

        public PDFCategory()
        {
            CategoryFiles = [];
            ChildCategories = [];
        }
    }
}
