using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ape.Models
{
    public class GalleryCategory
    {
        [Key]
        public int CategoryID { get; set; }

        [Required]
        public required string CategoryName { get; set; }

        [Required]
        public int SortOrder { get; set; }

        /// <summary>
        /// Parent category ID for hierarchical structure. Null = root level category.
        /// </summary>
        public int? ParentCategoryID { get; set; }

        /// <summary>
        /// Access level for the category. Reuses DocumentAccessLevel from Document Library.
        /// </summary>
        public DocumentAccessLevel AccessLevel { get; set; } = DocumentAccessLevel.Member;

        /// <summary>
        /// Optional description for the category
        /// </summary>
        [MaxLength(500)]
        public string? Description { get; set; }

        // Navigation property to parent category
        [ForeignKey("ParentCategoryID")]
        public virtual GalleryCategory? ParentCategory { get; set; }

        // Navigation property to child categories
        public virtual ICollection<GalleryCategory> ChildCategories { get; set; } = [];

        // Navigation property to gallery images
        public virtual ICollection<GalleryImage> GalleryImages { get; set; } = [];

        public GalleryCategory()
        {
            ChildCategories = [];
            GalleryImages = [];
        }
    }
}
