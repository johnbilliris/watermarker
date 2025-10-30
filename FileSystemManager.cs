namespace ImageWatermarker
{
    /// <summary>
    /// Handles file system operations for batch watermarking
    /// </summary>
    public class FileSystemManager
    {
        private readonly WatermarkProcessor _watermarkProcessor;
        
        public FileSystemManager()
        {
            _watermarkProcessor = new WatermarkProcessor();
        }
        
        /// <summary>
        /// Event raised when processing progress is updated
        /// </summary>
        public event Action<int, int, string>? ProgressUpdated;

        /// <summary>
        /// Event raised when an error occurs during processing
        /// </summary>
        public event Action<string, Exception>? ErrorOccurred;
        
        /// <summary>
        /// Processes all images in the source folder and saves watermarked versions to the output folder
        /// </summary>
        /// <param name="sourceFolderPath">Path to the folder containing source images</param>
        /// <param name="watermarkImagePath">Path to the watermark image</param>
        /// <param name="outputFolderPath">Path to the folder where watermarked images will be saved</param>
        /// <param name="outputImageQuality">Quality of the output image (1-100) for lossy formats like JPEG</param>
        /// <param name="opacity">Watermark opacity (0.0 to 1.0)</param>
        /// <param name="margin">Margin from the edges in pixels</param>
        /// <param name="position">Position where the watermark will be placed</param>
        /// <returns>ProcessingResult with statistics</returns>
        public ProcessingResult ProcessFolder(string sourceFolderPath, string watermarkImagePath,
            string outputFolderPath, long outputImageQuality,
            float opacity = 0.7f, int margin = 20, WatermarkPosition position = WatermarkPosition.BottomRight)
        {
            ValidateInputs(sourceFolderPath, watermarkImagePath, outputFolderPath);
            
            // Ensure output directory exists
            Directory.CreateDirectory(outputFolderPath);
            
            // Get all supported image files from the source folder
            var imageFiles = GetImageFiles(sourceFolderPath);
            
            var result = new ProcessingResult
            {
                TotalFiles = imageFiles.Count,
                StartTime = DateTime.Now
            };
            
            Console.WriteLine($"Found {imageFiles.Count} image files to process...");
            
            for (int i = 0; i < imageFiles.Count; i++)
            {
                var sourceFile = imageFiles[i];
                var fileName = Path.GetFileName(sourceFile);
                var outputFile = Path.Combine(outputFolderPath, fileName);
                
                try
                {
                    // Report progress
                    ProgressUpdated?.Invoke(i + 1, imageFiles.Count, fileName);
                    
                    // Process the image
                    _watermarkProcessor.AddWatermark(sourceFile, watermarkImagePath, outputFile, outputImageQuality, opacity, margin, position);
                    
                    result.ProcessedFiles++;
                    Console.WriteLine($"[{i + 1}/{imageFiles.Count}] Processed: {fileName}");
                }
                catch (Exception ex)
                {
                    result.FailedFiles++;
                    result.Errors.Add($"{fileName}: {ex.Message}");
                    
                    // Report error
                    ErrorOccurred?.Invoke(fileName, ex);
                    Console.WriteLine($"[{i + 1}/{imageFiles.Count}] Failed: {fileName} - {ex.Message}");
                }
            }
            
            result.EndTime = DateTime.Now;
            return result;
        }
        
        /// <summary>
        /// Gets all supported image files from the specified directory
        /// </summary>
        private List<string> GetImageFiles(string directoryPath)
        {
            var imageFiles = new List<string>();
            var supportedExtensions = WatermarkProcessor.GetSupportedExtensions();
            
            try
            {
                var allFiles = Directory.GetFiles(directoryPath, "*.*", SearchOption.TopDirectoryOnly);
                
                foreach (var file in allFiles)
                {
                    if (WatermarkProcessor.IsSupportedImageFile(file))
                    {
                        imageFiles.Add(file);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error reading directory '{directoryPath}': {ex.Message}", ex);
            }
            
            return imageFiles.OrderBy(f => Path.GetFileName(f)).ToList();
        }
        
        /// <summary>
        /// Validates input parameters
        /// </summary>
        private void ValidateInputs(string sourceFolderPath, string watermarkImagePath, string outputFolderPath)
        {
            if (string.IsNullOrWhiteSpace(sourceFolderPath))
                throw new ArgumentException("Source folder path cannot be empty", nameof(sourceFolderPath));
                
            if (string.IsNullOrWhiteSpace(watermarkImagePath))
                throw new ArgumentException("Watermark image path cannot be empty", nameof(watermarkImagePath));
                
            if (string.IsNullOrWhiteSpace(outputFolderPath))
                throw new ArgumentException("Output folder path cannot be empty", nameof(outputFolderPath));
                
            if (!Directory.Exists(sourceFolderPath))
                throw new DirectoryNotFoundException($"Source folder not found: {sourceFolderPath}");
                
            if (!File.Exists(watermarkImagePath))
                throw new FileNotFoundException($"Watermark image not found: {watermarkImagePath}");
                
            if (!WatermarkProcessor.IsSupportedImageFile(watermarkImagePath))
                throw new ArgumentException($"Watermark file is not a supported image format: {watermarkImagePath}");
            
            // Check if output folder is inside source folder (to prevent infinite loops)
            var fullSourcePath = Path.GetFullPath(sourceFolderPath);
            var fullOutputPath = Path.GetFullPath(outputFolderPath);
            
            if (fullOutputPath.StartsWith(fullSourcePath, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Output folder cannot be inside the source folder");
        }
        
        /// <summary>
        /// Gets information about the images in a folder
        /// </summary>
        public FolderInfo GetFolderInfo(string folderPath)
        {
            if (!Directory.Exists(folderPath))
                throw new DirectoryNotFoundException($"Folder not found: {folderPath}");
                
            var imageFiles = GetImageFiles(folderPath);
            var totalSize = imageFiles.Sum(file => new FileInfo(file).Length);
            
            return new FolderInfo
            {
                FolderPath = folderPath,
                TotalImages = imageFiles.Count,
                TotalSizeBytes = totalSize,
                SupportedExtensions = imageFiles.GroupBy(f => Path.GetExtension(f).ToLowerInvariant())
                    .ToDictionary(g => g.Key, g => g.Count())
            };
        }
    }
    
    /// <summary>
    /// Contains the results of a folder processing operation
    /// </summary>
    public class ProcessingResult
    {
        public int TotalFiles { get; set; }
        public int ProcessedFiles { get; set; }
        public int FailedFiles { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration => EndTime - StartTime;
        public double SuccessRate => TotalFiles > 0 ? (double)ProcessedFiles / TotalFiles * 100 : 0;
    }
    
    /// <summary>
    /// Contains information about images in a folder
    /// </summary>
    public class FolderInfo
    {
        public string FolderPath { get; set; } = string.Empty;
        public int TotalImages { get; set; }
        public long TotalSizeBytes { get; set; }
        public Dictionary<string, int> SupportedExtensions { get; set; } = new Dictionary<string, int>();
        
        public string TotalSizeFormatted => FormatBytes(TotalSizeBytes);
        
        private string FormatBytes(long bytes)
        {
            const int unit = 1024;
            if (bytes < unit) return $"{bytes} B";
            int exp = (int)(Math.Log(bytes) / Math.Log(unit));
            return $"{bytes / Math.Pow(unit, exp):F1} {("KMGTPE")[exp - 1]}B";
        }
    }
}