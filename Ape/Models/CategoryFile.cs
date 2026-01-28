using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ape.Models
{
    public class CategoryFile
    {
        [Key]
        public int FileID { get; set; }

        [ForeignKey("PDFCategory")]
        public int CategoryID { get; set; }

        [Required]
        public required string FileName { get; set; }

        public int SortOrder { get; set; }

        /// <summary>
        /// Optional description for the file
        /// </summary>
        [MaxLength(500)]
        public string? Description { get; set; }

        /// <summary>
        /// When the file was uploaded
        /// </summary>
        public DateTime? UploadedDate { get; set; }

        /// <summary>
        /// Who uploaded the file
        /// </summary>
        [MaxLength(256)]
        public string? UploadedBy { get; set; }

        public virtual PDFCategory? PDFCategory { get; set; }
    }
}
