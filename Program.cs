using ImageWatermarker;

namespace ImageWatermarker
{
    /// <summary>
    /// Main program class for the Image Watermarker application
    /// </summary>
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("=== Image Watermarker ===");
            Console.WriteLine("Adds watermarks to all images in a folder while preserving quality and resolution.\n");
            
            // Check for help flag first
            if (args.Length > 0 && (args[0] == "--help" || args[0] == "-h" || args[0] == "/?" || args[0] == "help"))
            {
                ShowUsage();
                return;
            }
            
            try
            {
                var config = args.Length >= 3 ? ParseCommandLineArgs(args) : GetUserInput();
                
                if (config == null)
                {
                    ShowUsage();
                    return;
                }
                
                await Task.Run(() => ProcessImages(config));
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\nError: {ex.Message}");
                Console.ResetColor();
                
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Details: {ex.InnerException.Message}");
                }
                
                Environment.Exit(1);
            }
        }
        
        /// <summary>
        /// Processes images using the provided configuration
        /// </summary>
        static void ProcessImages(WatermarkConfig config)
        {
            var fileManager = new FileSystemManager();
            
            // Subscribe to events for progress reporting
            fileManager.ProgressUpdated += (current, total, fileName) =>
            {
                var percentage = (double)current / total * 100;
                Console.Write($"\rProgress: {percentage:F1}% ({current}/{total}) - {fileName}".PadRight(80));
            };
            
            fileManager.ErrorOccurred += (fileName, exception) =>
            {
                Console.WriteLine($"\nError processing {fileName}: {exception.Message}");
            };
            
            // Show folder information
            Console.WriteLine("Analyzing source folder...");
            var folderInfo = fileManager.GetFolderInfo(config.SourceFolder);
            
            Console.WriteLine($"Source folder: {folderInfo.FolderPath}");
            Console.WriteLine($"Found {folderInfo.TotalImages} images ({folderInfo.TotalSizeFormatted})");
            
            if (folderInfo.SupportedExtensions.Any())
            {
                Console.WriteLine("File types: " + string.Join(", ", 
                    folderInfo.SupportedExtensions.Select(kv => $"{kv.Key} ({kv.Value})")));
            }
            
            Console.WriteLine($"Watermark: {config.WatermarkImage}");
            Console.WriteLine($"Output folder: {config.OutputFolder}");
            if (WatermarkProcessor.IsNoneWatermark(config.WatermarkImage))
            {
                Console.WriteLine($"Quality: {config.OutputImageQuality}% (watermarking disabled)");
            }
            else
            {
                Console.WriteLine($"Quality: {config.OutputImageQuality}%, Opacity: {config.Opacity:P0}, Margin: {config.Margin}px, Position: {config.Position}");
            }

            if (config.Width.HasValue || config.Height.HasValue)
            {
                var w = config.Width.HasValue ? config.Width.Value.ToString() : "auto";
                var h = config.Height.HasValue ? config.Height.Value.ToString() : "auto";
                Console.WriteLine($"Resize: {w} x {h}{(config.PreserveOrientation ? " (preserve orientation)" : string.Empty)}");
            }
            
            if (folderInfo.TotalImages == 0)
            {
                Console.WriteLine("\nNo supported image files found in the source folder.");
                return;
            }
            
            Console.Write("\nProceed with watermarking? (y/N): ");
            var response = Console.ReadLine()?.Trim().ToLowerInvariant();
            
            if (response != "y" && response != "yes")
            {
                Console.WriteLine("Operation cancelled.");
                return;
            }
            
            Console.WriteLine("\nStarting watermark process...");
            
             var result = fileManager.ProcessFolder(
                config.SourceFolder,
                config.WatermarkImage,
                config.OutputFolder,
                config.OutputImageQuality,
                config.Opacity,
                config.Margin,
                config.Position,
                config.Width,
                config.Height,
                config.PreserveOrientation
            );
            
            // Show results
            Console.WriteLine($"\n\n=== Processing Complete ===");
            Console.WriteLine($"Duration: {result.Duration:hh\\:mm\\:ss}");
            Console.WriteLine($"Total files: {result.TotalFiles}");
            Console.WriteLine($"Processed successfully: {result.ProcessedFiles}");
            Console.WriteLine($"Failed: {result.FailedFiles}");
            Console.WriteLine($"Success rate: {result.SuccessRate:F1}%");
            
            if (result.Errors.Any())
            {
                Console.WriteLine("\nErrors encountered:");
                foreach (var error in result.Errors.Take(10)) // Show max 10 errors
                {
                    Console.WriteLine($"  • {error}");
                }
                
                if (result.Errors.Count > 10)
                {
                    Console.WriteLine($"  ... and {result.Errors.Count - 10} more errors");
                }
            }
            
            if (result.ProcessedFiles > 0)
            {
                Console.WriteLine($"\nWatermarked images saved to: {config.OutputFolder}");
            }
        }
        
        /// <summary>
        /// Gets configuration from user input
        /// </summary>
        static WatermarkConfig? GetUserInput()
        {
            try
            {
                Console.Write("Enter source folder path: ");
                var sourceFolder = Console.ReadLine()?.Trim().Trim('"');
                
                if (string.IsNullOrEmpty(sourceFolder))
                {
                    Console.WriteLine("Source folder is required.");
                    return null;
                }
                
                Console.Write("Enter watermark image path: ");
                var watermarkImage = Console.ReadLine()?.Trim().Trim('"');
                
                if (string.IsNullOrEmpty(watermarkImage))
                {
                    Console.WriteLine("Watermark image is required.");
                    return null;
                }
                
                Console.Write("Enter output folder path: ");
                var outputFolder = Console.ReadLine()?.Trim().Trim('"');

                if (string.IsNullOrEmpty(outputFolder))
                {
                    Console.WriteLine("Output folder is required.");
                    return null;
                }
                
                // Optional parameters
                Console.Write("Enter output image quality (1-100, default 100): ");
                var qualityInput = Console.ReadLine()?.Trim();
                var outputImageQuality = 100L;

                if (!string.IsNullOrEmpty(qualityInput) && long.TryParse(qualityInput, out var parsedQuality))
                {
                    outputImageQuality = Math.Clamp(parsedQuality, 1L, 100L);
                }
                
                Console.Write("Enter watermark opacity (0.0-1.0, default 0.7): ");
                var opacityInput = Console.ReadLine()?.Trim();
                var opacity = 0.7f;
                
                if (!string.IsNullOrEmpty(opacityInput) && float.TryParse(opacityInput, out var parsedOpacity))
                {
                    opacity = Math.Clamp(parsedOpacity, 0.0f, 1.0f);
                }
                
                Console.Write("Enter margin in pixels (default 20): ");
                var marginInput = Console.ReadLine()?.Trim();
                var margin = 20;
                
                if (!string.IsNullOrEmpty(marginInput) && int.TryParse(marginInput, out var parsedMargin))
                {
                    margin = Math.Max(0, parsedMargin);
                }
                
                // Position input
                Console.WriteLine("\nAvailable watermark positions:");
                var positions = WatermarkProcessor.GetAvailablePositions();
                for (int i = 0; i < positions.Length; i++)
                {
                    Console.WriteLine($"  {i + 1}. {positions[i]}");
                }
                
                Console.Write("Enter watermark position (1-9 or name, default BottomRight): ");
                var positionInput = Console.ReadLine()?.Trim();
                var position = WatermarkPosition.BottomRight;
                
                if (!string.IsNullOrEmpty(positionInput))
                {
                    if (int.TryParse(positionInput, out var positionIndex) && 
                        positionIndex >= 1 && positionIndex <= positions.Length)
                    {
                        position = positions[positionIndex - 1];
                    }
                    else
                    {
                        try
                        {
                            position = WatermarkProcessor.ParsePosition(positionInput);
                        }
                        catch (ArgumentException)
                        {
                            Console.WriteLine($"Invalid position '{positionInput}', using default BottomRight");
                        }
                    }
                }

                // Optional resize parameters
                Console.Write("Enter output width in pixels (optional, press Enter to keep source width): ");
                var widthInput = Console.ReadLine()?.Trim();
                int? width = null;
                if (!string.IsNullOrEmpty(widthInput) && int.TryParse(widthInput, out var parsedWidth) && parsedWidth > 0)
                {
                    width = parsedWidth;
                }

                Console.Write("Enter output height in pixels (optional, press Enter to keep source height): ");
                var heightInput = Console.ReadLine()?.Trim();
                int? height = null;
                if (!string.IsNullOrEmpty(heightInput) && int.TryParse(heightInput, out var parsedHeight) && parsedHeight > 0)
                {
                    height = parsedHeight;
                }

                bool preserveOrientation = false;
                if (width.HasValue && height.HasValue)
                {
                    Console.Write("Preserve source orientation when resizing? (y/N): ");
                    var poInput = Console.ReadLine()?.Trim().ToLowerInvariant();
                    preserveOrientation = poInput == "y" || poInput == "yes";
                }

                return new WatermarkConfig
                {
                    SourceFolder = sourceFolder,
                    WatermarkImage = watermarkImage,
                    OutputFolder = outputFolder,
                    OutputImageQuality = outputImageQuality,
                    Opacity = opacity,
                    Margin = margin,
                    Position = position,
                    Width = width,
                    Height = height,
                    PreserveOrientation = preserveOrientation
                };
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error reading user input", ex);
            }
        }
        
        /// <summary>
        /// Parses command line arguments
        /// </summary>
        static WatermarkConfig? ParseCommandLineArgs(string[] args)
        {
            if (args.Length < 3)
                return null;
                
            var config = new WatermarkConfig
            {
                SourceFolder = args[0].Trim('"'),
                WatermarkImage = args[1].Trim('"'),
                OutputFolder = args[2].Trim('"')
            };
            
            // Parse optional parameters
            for (int i = 3; i < args.Length; i++)
            {
                var arg = args[i];

                if (arg.StartsWith("--opacity=") && float.TryParse(arg[10..], out var opacity))
                {
                    config.Opacity = Math.Clamp(opacity, 0.0f, 1.0f);
                }
                else if (arg.StartsWith("--margin=") && int.TryParse(arg[9..], out var margin))
                {
                    config.Margin = Math.Max(0, margin);
                }
                else if (arg.StartsWith("--outputimagequality=") && long.TryParse(arg[21..], out var quality))
                {
                    config.OutputImageQuality = Math.Clamp(quality, 1L, 100L);
                }
                else if (arg.StartsWith("--position="))
                {
                    try
                    {
                        config.Position = WatermarkProcessor.ParsePosition(arg[11..]);
                    }
                    catch (ArgumentException)
                    {
                        Console.WriteLine($"Invalid position '{arg[11..]}', using default BottomRight");
                    }
                }
                else if (arg.StartsWith("--width=") && int.TryParse(arg[8..], out var width))
                {
                    config.Width = Math.Max(1, width);
                }
                else if (arg.StartsWith("--height=") && int.TryParse(arg[9..], out var height))
                {
                    config.Height = Math.Max(1, height);
                }
                else if (arg == "--preserve-orientation" ||
                         (arg.StartsWith("--preserve-orientation=") && bool.TryParse(arg[23..], out _)))
                {
                    if (arg == "--preserve-orientation")
                        config.PreserveOrientation = true;
                    else if (bool.TryParse(arg[23..], out var po))
                        config.PreserveOrientation = po;
                }
            }
            
            return config;
        }
        
        /// <summary>
        /// Shows usage information
        /// </summary>
        static void ShowUsage()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("  Interactive mode: ImageWatermarker.exe");
            Console.WriteLine("  Command line:     ImageWatermarker.exe <source_folder> <watermark_image> <output_folder> [options]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  --outputimagequality=<value>  Output image quality (1-100, default: 100)");
            Console.WriteLine("  --opacity=<value>             Watermark opacity (0.0 to 1.0, default: 0.7)");
            Console.WriteLine("  --margin=<pixels>             Margin from edges (default: 20)");
            Console.WriteLine("  --position=<position>         Watermark position (default: BottomRight)");
            Console.WriteLine("  --width=<pixels>              Target output image width (default: source width)");
            Console.WriteLine("  --height=<pixels>             Target output image height (default: source height)");
            Console.WriteLine("  --preserve-orientation        Preserve source orientation when resizing (used with --width and --height)");
            Console.WriteLine();
            Console.WriteLine("Available positions:");
            Console.WriteLine("  TopLeft, TopCenter, TopRight, CenterLeft, CenterCenter,");
            Console.WriteLine("  CenterRight, BottomLeft, BottomCenter, BottomRight");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  ImageWatermarker.exe \"C:\\Photos\" \"C:\\watermark.png\" \"C:\\Output\"");
            Console.WriteLine("  ImageWatermarker.exe \"C:\\Photos\" \"C:\\logo.png\" \"C:\\Output\" --outputimagequality=80 --opacity=0.5 --margin=30 --position=TopRight");
            Console.WriteLine("  ImageWatermarker.exe \"C:\\Photos\" \"C:\\logo.png\" \"C:\\Output\" --width=1920 --height=1080 --preserve-orientation");
            Console.WriteLine();
            Console.WriteLine("Supported formats: " + string.Join(", ", WatermarkProcessor.GetSupportedExtensions()));
        }
    }
    
    /// <summary>
    /// Configuration for watermarking operation
    /// </summary>
    public class WatermarkConfig
    {
        public string SourceFolder { get; set; } = string.Empty;
        public string WatermarkImage { get; set; } = string.Empty;
        public string OutputFolder { get; set; } = string.Empty;
        public long OutputImageQuality { get; set; } = 100L;
        public float Opacity { get; set; } = 0.7f;
        public int Margin { get; set; } = 20;
        public WatermarkPosition Position { get; set; } = WatermarkPosition.BottomRight;
        public int? Width { get; set; }
        public int? Height { get; set; }
        public bool PreserveOrientation { get; set; }
    }
}