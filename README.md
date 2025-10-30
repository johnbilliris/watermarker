# Image Watermarker

A C# console application that adds watermarks to all images in a folder while preserving the original image quality and resolution. The watermark is placed in the bottom-right corner of each image, and the processed images are saved to a separate output folder with the same filenames.

## Features

- **High-Quality Processing**: Preserves original image resolution and quality with configurable output quality
- **Complete Metadata Preservation**: Maintains all EXIF data, GPS info, camera settings, and image metadata
- **File Properties Preservation**: Maintains original creation date, modification date, and file attributes
- **Batch Processing**: Processes entire folders of images automatically with progress tracking
- **Multiple Formats**: Supports JPEG, PNG, BMP, GIF, TIFF formats
- **Flexible Positioning**: 9 position options - corners, centers, and edges with configurable margin
- **Opacity Control**: Adjustable watermark transparency (0.0 to 1.0)
- **Quality Control**: Configurable output image quality (1-100) for JPEG files
- **Progress Tracking**: Real-time progress reporting during batch processing
- **Error Handling**: Robust error handling with detailed reporting and graceful fallbacks
- **Command Line & Interactive**: Supports both command-line and interactive modes

## Requirements

- .NET 8.0 or later
- Windows, macOS, or Linux
- System.Drawing.Common package (included in project)

## Installation & Build

1. Clone or download the source code
2. Open terminal/command prompt in the project directory
3. Build the project:
   ```bash
   dotnet build
   ```
4. Run the application:
   ```bash
   dotnet run
   ```

## Usage

### Interactive Mode

Run the application without arguments to use interactive mode:

```bash
dotnet run
```

You'll be prompted to enter:
- Source folder path (folder containing images to watermark)
- Watermark image path (your logo/watermark file)
- Output folder path (where watermarked images will be saved)
- Output image quality (optional, 1-100 for JPEG files, default: 95)
- Opacity (optional, 0.0 to 1.0, default: 0.7)
- Margin (optional, pixels from edges, default: 20)
- Position (optional, 9 available positions, default: BottomRight)

### Command Line Mode

```bash
dotnet run -- <source_folder> <watermark_image> <output_folder> [options]
```

**Parameters:**
- `source_folder`: Path to folder containing images to watermark
- `watermark_image`: Path to watermark/logo image file
- `output_folder`: Path to folder where watermarked images will be saved

**Options:**
- `--outputimagequality=<value>`: Output image quality (1-100, default: 95)
- `--opacity=<value>`: Watermark opacity (0.0 to 1.0, default: 0.7)
- `--margin=<pixels>`: Margin from edges (default: 20)
- `--position=<position>`: Watermark position (default: BottomRight)

**Available Positions:**
TopLeft, TopCenter, TopRight, CenterLeft, CenterCenter, CenterRight, BottomLeft, BottomCenter, BottomRight

**Examples:**

```bash
# Basic usage
dotnet run -- "C:\Photos" "C:\watermark.png" "C:\Output"

# With custom settings and top-right position
dotnet run -- "C:\Photos" "C:\logo.png" "C:\Output" --outputimagequality=85 --opacity=0.5 --margin=30 --position=TopRight

# Using quotes for paths with spaces
dotnet run -- "C:\My Photos" "C:\Company Logo.png" "C:\Watermarked Images"
```

## Supported Image Formats

- JPEG (.jpg, .jpeg)
- PNG (.png)
- BMP (.bmp)
- GIF (.gif)
- TIFF (.tiff, .tif)

## How It Works

1. **Image Quality Preservation**: Uses high-quality rendering settings and maintains original pixel format and resolution
2. **Watermark Sizing**: Automatically scales watermark to maximum 25% of image width (landscape) or 33% (portrait) while maintaining aspect ratio
3. **Flexible Positioning**: Places watermark in any of 9 positions (corners, centers, edges) with specified margin
4. **Opacity**: Applies specified transparency to watermark using color matrix transformation
5. **Format Handling**: Saves images in their original format with configurable quality settings (default 95% for JPEG)
6. **Metadata Preservation**: Copies all EXIF data, GPS information, camera settings, and other image metadata
7. **File Properties Preservation**: Copies creation time, modification time, last access time, and file attributes from source to output file

## Technical Details

### Key Classes

- **`WatermarkProcessor`**: Core image processing logic
- **`FileSystemManager`**: Handles batch processing and file operations
- **`Program`**: Main application entry point and user interface

### Quality Preservation Features

- Maintains original image resolution (DPI)
- Preserves pixel format and color depth
- Uses high-quality interpolation and smoothing
- Configurable output quality for JPEG files (1-100, default: 95)
- Optimized encoding parameters for each format
- No unnecessary compression or quality loss
- Preserves all EXIF metadata (camera settings, GPS, timestamps)
- Preserves file timestamps (creation, modification, access dates)
- Maintains original file attributes while ensuring output file is writable

### Error Handling

The application includes comprehensive error handling for:
- Missing or invalid files/folders
- Unsupported image formats
- File access permissions
- Processing errors
- Invalid configuration parameters

### Progress Reporting

Real-time progress updates including:
- Current file being processed
- Percentage completion
- Files processed vs. total
- Processing speed and time estimates
- Success/failure statistics

## Example Output

```
=== Image Watermarker ===
Adds watermarks to all images in a folder while preserving quality and resolution.

Analyzing source folder...
Source folder: C:\Photos
Found 150 images (45.2 MB)
File types: .jpg (120), .png (25), .bmp (5)
Watermark: C:\logo.png
Output folder: C:\Watermarked
Quality: 95%, Opacity: 70%, Margin: 20px, Position: BottomRight

Proceed with watermarking? (y/N): y

Starting watermark process...
Progress: 100.0% (150/150) - IMG_2547.jpg

=== Processing Complete ===
Duration: 00:02:15
Total files: 150
Processed successfully: 148
Failed: 2
Success rate: 98.7%

Watermarked images saved to: C:\Watermarked
```

## Best Practices

1. **Watermark Design**: Use PNG format with transparency for best results
2. **Size Considerations**: Keep watermark files reasonably sized (under 1MB recommended)
3. **Output Location**: Ensure output folder is not inside source folder to prevent conflicts
4. **Backup**: Always keep backups of original images
5. **Testing**: Test with a small batch first to verify settings
6. **Performance**: For large batches, ensure sufficient disk space and memory

## Troubleshooting

### Common Issues

1. **"Access Denied" errors**: Run with administrator privileges or check folder permissions
2. **"Format not supported"**: Ensure watermark image is in a supported format
3. **Out of memory**: Process smaller batches or use smaller watermark images
4. **Slow performance**: Close other applications or process fewer files at once

### Error Messages

- `Source folder not found`: Check the source folder path exists and is accessible
- `Watermark image not found`: Verify watermark file path and file exists
- `Output folder cannot be inside source folder`: Choose a different output location
- `No encoder found for format`: File may be corrupted or in an unsupported format

## License

This project is provided as-is for educational and commercial use. Feel free to modify and distribute according to your needs.

## Contributing

To contribute improvements:
1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Test thoroughly
5. Submit a pull request

## Version History

- **v1.0**: Initial release with core watermarking functionality
  - High-quality image processing
  - Batch folder processing
  - Command line and interactive modes
  - Comprehensive error handling and progress reporting