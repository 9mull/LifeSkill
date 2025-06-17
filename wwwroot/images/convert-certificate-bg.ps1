# Check if ImageMagick is installed
if (-not (Get-Command magick -ErrorAction SilentlyContinue)) {
    Write-Host "ImageMagick is not installed. Please install it first."
    exit 1
}
 
# Convert SVG to PNG
magick convert certificate-bg.svg certificate-bg.png 