namespace Ape.Services
{
    public interface IImageOptimizationService
    {
        /// <summary>
        /// Optimizes an image by resizing and compressing it
        /// </summary>
        Task<byte[]> OptimizeImageAsync(Stream inputStream, int maxWidth, int? maxHeight = null, int quality = 85);

        /// <summary>
        /// Optimizes an image from IFormFile
        /// </summary>
        Task<byte[]> OptimizeImageAsync(IFormFile imageFile, int maxWidth, int? maxHeight = null, int quality = 85);

        /// <summary>
        /// Generates a thumbnail from an image stream
        /// </summary>
        Task<byte[]> GenerateThumbnailAsync(Stream inputStream, int maxWidth = 400, int quality = 85);

        /// <summary>
        /// Gets the recommended maximum width for different image types
        /// </summary>
        int GetRecommendedMaxWidth(ImageType imageType);

        /// <summary>
        /// Validates if a file is a supported image format
        /// </summary>
        bool IsValidImageFormat(IFormFile file);
    }

    public enum ImageType
    {
        Gallery = 1920,
        Thumbnail = 400
    }
}
