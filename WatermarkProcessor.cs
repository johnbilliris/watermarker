using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace ImageWatermarker
{
    /// <summary>
    /// Represents the position where the watermark will be placed on the image
    /// </summary>
    public enum WatermarkPosition
    {
        TopLeft,
        TopCenter,
        TopRight,
        CenterLeft,
        CenterCenter,
        CenterRight,
        BottomLeft,
        BottomCenter,
        BottomRight
    }

    /// <summary>
    /// Provides functionality to add watermarks to images while preserving quality and resolution
    /// </summary>
    public class WatermarkProcessor
    {
        /// <summary>
        /// Adds a watermark to an image and saves it to the specified output path
        /// </summary>
        /// <param name="sourceImagePath">Path to the source image</param>
        /// <param name="watermarkImagePath">Path to the watermark image</param>
        /// <param name="outputImagePath">Path where the watermarked image will be saved</param>
        /// <param name="outputImageQuality">Quality of the output image (1-100) for lossy formats like JPEG</param>
        /// <param name="opacity">Watermark opacity (0.0 to 1.0)</param>
        /// <param name="margin">Margin from the edges in pixels</param>
        /// <param name="position">Position where the watermark will be placed</param>
        public void AddWatermark(string sourceImagePath, string watermarkImagePath, string outputImagePath,
            long outputImageQuality = 100L, float opacity = 0.7f, int margin = 20, WatermarkPosition position = WatermarkPosition.BottomRight,
            int? targetWidth = null, int? targetHeight = null, bool preserveOrientation = false)
        {
            if (!File.Exists(sourceImagePath))
                throw new FileNotFoundException($"Source image not found: {sourceImagePath}");

            bool skipWatermark = IsNoneWatermark(watermarkImagePath);

            if (!skipWatermark && !File.Exists(watermarkImagePath))
                throw new FileNotFoundException($"Watermark image not found: {watermarkImagePath}");

            using var sourceImage = Image.FromFile(sourceImagePath);
            using var watermarkImage = skipWatermark ? null : Image.FromFile(watermarkImagePath);

            var (outWidth, outHeight) = ResolveOutputDimensions(
                sourceImage.Width, sourceImage.Height, targetWidth, targetHeight, preserveOrientation);

            // Create a new bitmap with the resolved output dimensions and source pixel format
            using var outputImage = new Bitmap(outWidth, outHeight, sourceImage.PixelFormat);

            // Set the resolution to match the source image
            outputImage.SetResolution(sourceImage.HorizontalResolution, sourceImage.VerticalResolution);
            
            using var graphics = Graphics.FromImage(outputImage);
            
            // Set high-quality rendering settings to preserve image quality
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.SmoothingMode = SmoothingMode.HighQuality;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            graphics.CompositingQuality = CompositingQuality.HighQuality;

            // Draw the original image (scaled to output dimensions)
            graphics.DrawImage(sourceImage, 0, 0, outWidth, outHeight);

            if (!skipWatermark && watermarkImage != null)
            {
                // Calculate watermark size and position based on output dimensions
                bool isLandscape = outWidth >= outHeight;

                var watermarkWidth = isLandscape ? Math.Min(watermarkImage.Width, outWidth / 4)    // Max 25% of image width
                                                  : Math.Min(watermarkImage.Width, outWidth / 3);  // Max 33% of image width
                var watermarkHeight = (int)(watermarkImage.Height * ((float)watermarkWidth / watermarkImage.Width));

                // Calculate position based on the specified position parameter
                var (x, y) = CalculateWatermarkPosition(outWidth, outHeight,
                    watermarkWidth, watermarkHeight, margin, position);

                // Create color matrix for opacity
                var colorMatrix = new ColorMatrix
                {
                    Matrix33 = opacity // Set the alpha (opacity) value
                };

                var imageAttributes = new ImageAttributes();
                imageAttributes.SetColorMatrix(colorMatrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);

                // Draw the watermark with opacity
                graphics.DrawImage(watermarkImage,
                    new Rectangle(x, y, watermarkWidth, watermarkHeight),
                    0, 0, watermarkImage.Width, watermarkImage.Height,
                    GraphicsUnit.Pixel, imageAttributes);
            }

            // Copy all metadata (EXIF, etc.) from source image to output image
            PreserveImageMetadata(sourceImage, outputImage);

            // Save the image with the same format and quality as the original
            SaveImageWithQuality(outputImage, outputImagePath, sourceImage, outputImageQuality);
            
            // Preserve file properties from source file
            PreserveFileProperties(sourceImagePath, outputImagePath);
        }
        
        /// <summary>
        /// Resolves the final output dimensions based on target width/height and preserve-orientation flag.
        /// </summary>
        private static (int width, int height) ResolveOutputDimensions(
            int sourceWidth, int sourceHeight, int? targetWidth, int? targetHeight, bool preserveOrientation)
        {
            // No resize requested
            if (!targetWidth.HasValue && !targetHeight.HasValue)
                return (sourceWidth, sourceHeight);

            // Only one supplied: scale proportionally to preserve aspect ratio
            if (targetWidth.HasValue && !targetHeight.HasValue)
            {
                int w = Math.Max(1, targetWidth.Value);
                int h = Math.Max(1, (int)Math.Round(sourceHeight * ((double)w / sourceWidth)));
                return (w, h);
            }
            if (targetHeight.HasValue && !targetWidth.HasValue)
            {
                int h = Math.Max(1, targetHeight.Value);
                int w = Math.Max(1, (int)Math.Round(sourceWidth * ((double)h / sourceHeight)));
                return (w, h);
            }

            // Both supplied
            int reqW = Math.Max(1, targetWidth!.Value);
            int reqH = Math.Max(1, targetHeight!.Value);

            if (!preserveOrientation)
                return (reqW, reqH);

            int larger = Math.Max(reqW, reqH);
            int smaller = Math.Min(reqW, reqH);

            // Source portrait: height >= width -> height should be the larger dimension
            // Source landscape: width >= height -> width should be the larger dimension
            bool sourceIsPortrait = sourceHeight > sourceWidth;
            return sourceIsPortrait ? (smaller, larger) : (larger, smaller);
        }

        /// <summary>
        /// Saves the image maintaining the original format and quality settings
        /// </summary>
        private void SaveImageWithQuality(Image image, string outputPath, Image originalImage, long outputImageQuality)
        {
            // Get the image format from the original image
            var format = originalImage.RawFormat;
            
            // For JPEG images, use high quality settings
            if (IsJpegFormat(format))
            {
                var jpegEncoder = GetEncoder(ImageFormat.Jpeg);
                var encoderParameters = new EncoderParameters(1);
                encoderParameters.Param[0] = new EncoderParameter(Encoder.Quality, outputImageQuality);                
                image.Save(outputPath, jpegEncoder, encoderParameters);
            }
            else
            {
                // For other formats (PNG, BMP, etc.), save directly
                image.Save(outputPath, format);
            }
        }
        
        /// <summary>
        /// Gets the image encoder for the specified format
        /// </summary>
        private ImageCodecInfo GetEncoder(ImageFormat format)
        {
            var codecs = ImageCodecInfo.GetImageDecoders();
            return codecs.FirstOrDefault(codec => codec.FormatID == format.Guid)
                ?? throw new NotSupportedException($"No encoder found for format: {format}");
        }
        
        /// <summary>
        /// Checks if the image format is JPEG
        /// </summary>
        private bool IsJpegFormat(ImageFormat format)
        {
            return format.Equals(ImageFormat.Jpeg) || format.Equals(ImageFormat.Exif);
        }
        
        /// <summary>
        /// Gets the supported image extensions
        /// </summary>
        public static string[] GetSupportedExtensions()
        {
            return new[] { ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".tiff", ".tif" };
        }

        /// <summary>
        /// Checks if a file is a supported image format
        /// </summary>
        public static bool IsSupportedImageFile(string filePath)
        {
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            return GetSupportedExtensions().Contains(extension);
        }

        /// <summary>
        /// Returns true when the watermark path is the literal value "None" (case-insensitive),
        /// indicating that no watermark should be applied.
        /// </summary>
        public static bool IsNoneWatermark(string? watermarkImagePath)
        {
            return !string.IsNullOrWhiteSpace(watermarkImagePath)
                && string.Equals(watermarkImagePath.Trim(), "None", StringComparison.OrdinalIgnoreCase);
        }


        /// <summary>
        /// Copies metadata from source image to output image
        /// </summary>
        /// <param name="sourceImage"></param>
        /// <param name="outputImage"></param>
        private void PreserveImageMetadata(Image sourceImage, Image outputImage)
        {
            // Copy all metadata (EXIF, etc.) from source image to output image
            // This preserves camera settings, GPS data, timestamps, etc.
            foreach (PropertyItem propertyItem in sourceImage.PropertyItems)
            {
                // Direct copy of property item to output image
                // SetPropertyItem will handle creating new items as needed
                outputImage.SetPropertyItem(propertyItem);
            }
        }

        /// <summary>
        /// Preserves file properties (creation time, last write time, and attributes) from source to output file
        /// </summary>
        private void PreserveFileProperties(string sourceFilePath, string outputFilePath)
        {
            try
            {
                var sourceFileInfo = new FileInfo(sourceFilePath);
                var outputFileInfo = new FileInfo(outputFilePath);
                
                if (sourceFileInfo.Exists && outputFileInfo.Exists)
                {
                    // Preserve creation time
                    outputFileInfo.CreationTime = sourceFileInfo.CreationTime;
                    outputFileInfo.CreationTimeUtc = sourceFileInfo.CreationTimeUtc;
                    
                    // Preserve last write time (modified time)
                    outputFileInfo.LastWriteTime = sourceFileInfo.LastWriteTime;
                    outputFileInfo.LastWriteTimeUtc = sourceFileInfo.LastWriteTimeUtc;
                    
                    // Preserve last access time
                    outputFileInfo.LastAccessTime = sourceFileInfo.LastAccessTime;
                    outputFileInfo.LastAccessTimeUtc = sourceFileInfo.LastAccessTimeUtc;
                    
                    // Preserve file attributes (but keep the file writable)
                    var attributes = sourceFileInfo.Attributes;
                    // Remove ReadOnly attribute if present to ensure we can modify the output file
                    attributes &= ~FileAttributes.ReadOnly;
                    outputFileInfo.Attributes = attributes;
                }
            }
            catch (Exception ex)
            {
                // Log the error but don't fail the entire watermarking process
                Console.WriteLine($"Warning: Could not preserve file properties: {ex.Message}");
            }
        }

        /// <summary>
        /// Calculates the watermark position based on the specified position and margin
        /// </summary>
        private (int x, int y) CalculateWatermarkPosition(int imageWidth, int imageHeight, 
            int watermarkWidth, int watermarkHeight, int margin, WatermarkPosition position)
        {
            int x, y;

            switch (position)
            {
                case WatermarkPosition.TopLeft:
                    x = margin;
                    y = margin;
                    break;

                case WatermarkPosition.TopCenter:
                    x = (imageWidth - watermarkWidth) / 2;
                    y = margin;
                    break;

                case WatermarkPosition.TopRight:
                    x = imageWidth - watermarkWidth - margin;
                    y = margin;
                    break;

                case WatermarkPosition.CenterLeft:
                    x = margin;
                    y = (imageHeight - watermarkHeight) / 2;
                    break;

                case WatermarkPosition.CenterCenter:
                    x = (imageWidth - watermarkWidth) / 2;
                    y = (imageHeight - watermarkHeight) / 2;
                    break;

                case WatermarkPosition.CenterRight:
                    x = imageWidth - watermarkWidth - margin;
                    y = (imageHeight - watermarkHeight) / 2;
                    break;

                case WatermarkPosition.BottomLeft:
                    x = margin;
                    y = imageHeight - watermarkHeight - margin;
                    break;

                case WatermarkPosition.BottomCenter:
                    x = (imageWidth - watermarkWidth) / 2;
                    y = imageHeight - watermarkHeight - margin;
                    break;

                case WatermarkPosition.BottomRight:
                default:
                    x = imageWidth - watermarkWidth - margin;
                    y = imageHeight - watermarkHeight - margin;
                    break;
            }

            return (x, y);
        }

        /// <summary>
        /// Gets all available watermark positions
        /// </summary>
        public static WatermarkPosition[] GetAvailablePositions()
        {
            return Enum.GetValues<WatermarkPosition>();
        }

        /// <summary>
        /// Converts a string to WatermarkPosition enum
        /// </summary>
        public static WatermarkPosition ParsePosition(string position)
        {
            if (Enum.TryParse<WatermarkPosition>(position, true, out var result))
            {
                return result;
            }
            
            // Try alternative parsing for user-friendly names
            return position.ToLowerInvariant().Replace(" ", "").Replace("-", "") switch
            {
                "topleft" => WatermarkPosition.TopLeft,
                "topcenter" or "topcentre" => WatermarkPosition.TopCenter,
                "topright" => WatermarkPosition.TopRight,
                "centerleft" or "centreleft" => WatermarkPosition.CenterLeft,
                "centercenter" or "centercentre" or "centrecentre" or "center" or "centre" => WatermarkPosition.CenterCenter,
                "centerright" or "centreright" => WatermarkPosition.CenterRight,
                "bottomleft" => WatermarkPosition.BottomLeft,
                "bottomcenter" or "bottomcentre" => WatermarkPosition.BottomCenter,
                "bottomright" => WatermarkPosition.BottomRight,
                _ => throw new ArgumentException($"Invalid watermark position: {position}")
            };
        }
    }
}