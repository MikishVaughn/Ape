using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace Ape.Services
{
    public class ImageOptimizationService(ILogger<ImageOptimizationService> logger) : IImageOptimizationService
    {
        private readonly ILogger<ImageOptimizationService> _logger = logger;

        private static readonly string[] SupportedMimeTypes =
        [
            "image/jpeg", "image/jpg", "image/png", "image/gif",
            "image/bmp", "image/webp"
        ];

        private static readonly string[] SupportedExtensions =
        [
            ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp"
        ];

        public async Task<byte[]> OptimizeImageAsync(Stream inputStream, int maxWidth, int? maxHeight = null, int quality = 85)
        {
            try
            {
                using var image = await Image.LoadAsync(inputStream);

                _logger.LogDebug("Original image: {Width}x{Height}", image.Width, image.Height);

                var (newWidth, newHeight) = CalculateOptimalDimensions(image.Width, image.Height, maxWidth, maxHeight);

                // Only resize if the image is larger than target
                if (newWidth < image.Width || newHeight < image.Height)
                {
                    image.Mutate(x => x.Resize(newWidth, newHeight));
                    _logger.LogDebug("Resized image to: {Width}x{Height}", newWidth, newHeight);
                }

                using var outputStream = new MemoryStream();
                var encoder = new JpegEncoder { Quality = quality };
                await image.SaveAsJpegAsync(outputStream, encoder);

                var optimizedBytes = outputStream.ToArray();
                _logger.LogInformation("Image optimized: {OriginalSize} KB -> {OptimizedSize} KB",
                    inputStream.Length / 1024.0, optimizedBytes.Length / 1024.0);

                return optimizedBytes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to optimize image");
                throw new InvalidOperationException("Failed to optimize image", ex);
            }
        }

        public async Task<byte[]> OptimizeImageAsync(IFormFile imageFile, int maxWidth, int? maxHeight = null, int quality = 85)
        {
            if (!IsValidImageFormat(imageFile))
            {
                throw new ArgumentException("Invalid image format", nameof(imageFile));
            }

            using var inputStream = imageFile.OpenReadStream();
            return await OptimizeImageAsync(inputStream, maxWidth, maxHeight, quality);
        }

        public async Task<byte[]> GenerateThumbnailAsync(Stream inputStream, int maxWidth = 400, int quality = 85)
        {
            return await OptimizeImageAsync(inputStream, maxWidth, quality: quality);
        }

        public int GetRecommendedMaxWidth(ImageType imageType)
        {
            return (int)imageType;
        }

        public bool IsValidImageFormat(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return false;

            if (!SupportedMimeTypes.Contains(file.ContentType?.ToLowerInvariant()))
                return false;

            var extension = Path.GetExtension(file.FileName)?.ToLowerInvariant();
            if (string.IsNullOrEmpty(extension) || !SupportedExtensions.Contains(extension))
                return false;

            return true;
        }

        private static (int width, int height) CalculateOptimalDimensions(int originalWidth, int originalHeight, int maxWidth, int? maxHeight = null)
        {
            if (originalWidth <= maxWidth && (maxHeight == null || originalHeight <= maxHeight))
            {
                return (originalWidth, originalHeight);
            }

            double aspectRatio = (double)originalHeight / originalWidth;

            int newWidth = Math.Min(originalWidth, maxWidth);
            int newHeight = (int)(newWidth * aspectRatio);

            if (maxHeight.HasValue && newHeight > maxHeight.Value)
            {
                newHeight = maxHeight.Value;
                newWidth = (int)(newHeight / aspectRatio);
            }

            return (newWidth, newHeight);
        }
    }
}
