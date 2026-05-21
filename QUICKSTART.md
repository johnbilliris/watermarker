# Image Watermarker - Quick Start Guide

## Quick Test Setup

To quickly test the Image Watermarker, follow these steps:

### 1. Create Test Folders

Create the following folder structure:
```
C:\temp\
├── test-images\      (source images go here)
├── watermarked\      (output folder - created automatically)
└── watermark.png     (your watermark/logo file)
```

### 2. Add Sample Images

Place some test images (JPEG, PNG, BMP, etc.) in `C:\temp\test-images\`

### 3. Create or Find a Watermark

- Create a PNG file with your logo/watermark
- Save it as `C:\temp\watermark.png`
- Recommended: Use transparent PNG for best results
- Keep it reasonably sized (e.g., 200x200 pixels)

### 4. Run the Application

#### Option A: Interactive Mode
```bash
cd C:\code\watermarker
dotnet run
```

Then enter:
- Source folder: `C:\temp\test-images`
- Watermark image: `C:\temp\watermark.png`
- Output folder: `C:\temp\watermarked`
- Output image quality: `100` (or press Enter for default)
- Opacity: `0.7` (or press Enter for default)
- Margin: `20` (or press Enter for default)
- Position: Select from 1-9 or enter name like `TopRight` (or press Enter for default BottomRight)
- Output width (px): press Enter to keep source width, or type a number (e.g. `1920`)
- Output height (px): press Enter to keep source height, or type a number (e.g. `1080`)
- Preserve orientation: when both width and height are provided, you'll be asked `y/N` to keep the source's portrait/landscape orientation

#### Option B: Command Line Mode
```bash
cd C:\code\watermarker
dotnet run -- "C:\temp\test-images" "C:\temp\watermark.png" "C:\temp\watermarked"
```

#### Option C: With Custom Settings
```bash
cd C:\code\watermarker
dotnet run -- "C:\temp\test-images" "C:\temp\watermark.png" "C:\temp\watermarked" --outputimagequality=85 --opacity=0.5 --margin=30 --position=TopRight
```

#### Option D: Resize Output Images
```bash
# Resize all output images to 1920x1080
dotnet run -- "C:\temp\test-images" "C:\temp\watermark.png" "C:\temp\watermarked" --width=1920 --height=1080

# Resize while preserving each image's orientation:
#   - portrait sources will be 1080 x 1920
#   - landscape sources will be 1920 x 1080
dotnet run -- "C:\temp\test-images" "C:\temp\watermark.png" "C:\temp\watermarked" --width=1920 --height=1080 --preserve-orientation

# Resize by width only - height is scaled proportionally
dotnet run -- "C:\temp\test-images" "C:\temp\watermark.png" "C:\temp\watermarked" --width=1280
```

### 5. Check Results

After processing:
- Check `C:\temp\watermarked\` for the watermarked images
- Compare original vs. watermarked images
- Verify image quality and watermark placement

## Creating an Executable

To create a standalone executable:

```bash
cd C:\code\watermarker

# For Windows x64
dotnet publish -c Release -r win-x64 --self-contained

# The executable will be in: bin\Release\net8.0\win-x64\publish\ImageWatermarker.exe
```

You can then run the executable directly:
```bash
ImageWatermarker.exe "C:\Photos" "C:\logo.png" "C:\Output"
ImageWatermarker.exe "C:\Photos" "C:\logo.png" "C:\Output" --outputimagequality=90 --opacity=0.6 --margin=25 --position=CenterRight
```

## Tips for Best Results

1. **Watermark Design**: 
   - Use PNG with transparency
   - Keep size reasonable (under 500x500 pixels)
   - Use high contrast colors for visibility

2. **Image Quality**:
   - Source images maintain full quality
   - JPEG images saved at configurable quality (default 100%)
   - Original resolution and DPI preserved
   - All EXIF metadata preserved (camera settings, GPS, etc.)

3. **Watermark Positions**:
   - **Corners**: TopLeft, TopRight, BottomLeft, BottomRight
   - **Centers**: TopCenter, CenterCenter, BottomCenter
   - **Sides**: CenterLeft, CenterRight
   - Use position names or numbers (1-9) in interactive mode

4. **Resizing & Orientation**:
   - Use `--width` and/or `--height` to set output dimensions; omit both to keep the source size
   - Supplying only one dimension scales the other proportionally to preserve aspect ratio
   - Add `--preserve-orientation` (with both `--width` and `--height`) to keep the source's portrait/landscape orientation. The larger of the two values is mapped to the long edge of the source image and the smaller to the short edge.

5. **Performance**:
   - Processing speed depends on image size and count
   - Large images (>10MP) may take longer
   - Progress is shown in real-time

6. **Batch Processing**:
   - No limit on number of files
   - Skips unsupported formats automatically
   - Failed files reported at end

## Example Output

```
=== Image Watermarker ===
Analyzing source folder...
Source folder: C:\temp\test-images
Found 5 images (12.3 MB)
File types: .jpg (3), .png (2)
Watermark: C:\temp\watermark.png
Output folder: C:\temp\watermarked
Quality: 100%, Opacity: 70%, Margin: 20px, Position: BottomRight

Proceed with watermarking? (y/N): y

Starting watermark process...
[1/5] Processed: IMG_001.jpg
[2/5] Processed: IMG_002.jpg
[3/5] Processed: photo.png
[4/5] Processed: landscape.jpg
[5/5] Processed: portrait.png

=== Processing Complete ===
Duration: 00:00:15
Total files: 5
Processed successfully: 5
Failed: 0
Success rate: 100.0%

Watermarked images saved to: C:\temp\watermarked
```