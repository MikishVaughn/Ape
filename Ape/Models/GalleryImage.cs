using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ape.Models
{
    public class GalleryImage
    {
        [Key]
        public int ImageID { get; set; }

        [ForeignKey("GalleryCategory")]
        public int CategoryID { get; set; }

        [Required]
        public required string FileName { get; set; }

        /// <summary>
        /// The original file name as uploaded by the user
        /// </summary>
        [MaxLength(500)]
        public string? OriginalFileName { get; set; }

        public int SortOrder { get; set; }

        /// <summary>
        /// Optional description for the image
        /// </summary>
        [MaxLength(500)]
        public string? Description { get; set; }

        /// <summary>
        /// When the image was uploaded
        /// </summary>
        public DateTime? UploadedDate { get; set; }

        /// <summary>
        /// Who uploaded the image
        /// </summary>
        [MaxLength(256)]
        public string? UploadedBy { get; set; }

        /// <summary>
        /// Thumbnail file name derived from FileName with _thumb suffix
        /// </summary>
        [NotMapped]
        public string ThumbnailFileName
        {
            get
            {
                if (string.IsNullOrEmpty(FileName)) return string.Empty;
                var ext = Path.GetExtension(FileName);
                var nameWithoutExt = Path.GetFileNameWithoutExtension(FileName);
                return $"{nameWithoutExt}_thumb{ext}";
            }
        }

        public virtual GalleryCategory? GalleryCategory { get; set; }
    }
}
