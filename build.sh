#!/bin/bash

# Build script for Jellyfin.Plugin.Kinopoisk

set -e

VERSION=${1:-"1.0.0.0"}
CONFIGURATION=${2:-"Release"}
OUTPUT_DIR="dist"

echo "Building Jellyfin.Plugin.Kinopoisk version $VERSION..."

# Clean previous builds
rm -rf "$OUTPUT_DIR"
mkdir -p "$OUTPUT_DIR"

# Restore and build
dotnet restore
dotnet build -c "$CONFIGURATION" /p:Version="$VERSION"

# Get the output path
BUILD_OUTPUT="Jellyfin.Plugin.Kinopoisk/bin/$CONFIGURATION/net8.0"

# Create plugin folder structure
PLUGIN_DIR="$OUTPUT_DIR/КиноПоиск_$VERSION"
mkdir -p "$PLUGIN_DIR"

# Copy files
cp "$BUILD_OUTPUT/Jellyfin.Plugin.Kinopoisk.dll" "$PLUGIN_DIR/"

# Create meta.json
cat > "$PLUGIN_DIR/meta.json" << EOF
{
  "guid": "a0ad3c8a-0e15-4c2f-8f5a-9c7b6d4e3f2a",
  "name": "КиноПоиск",
  "description": "Метаданные фильмов и сериалов с сайта КиноПоиск",
  "overview": "Загружает метаданные с kinopoisk.ru",
  "owner": "Your Name",
  "category": "Metadata",
  "version": "$VERSION",
  "targetAbi": "10.9.0.0",
  "status": "Active"
}
EOF

# Create ZIP archive
cd "$OUTPUT_DIR"
zip -r "Jellyfin.Plugin.Kinopoisk_$VERSION.zip" "КиноПоиск_$VERSION"
cd ..

# Calculate checksum
CHECKSUM=$(sha256sum "$OUTPUT_DIR/Jellyfin.Plugin.Kinopoisk_$VERSION.zip" | cut -d' ' -f1)
echo "Checksum: $CHECKSUM"

# Update manifest with checksum
sed -i "s/\"checksum\": \"\"/\"checksum\": \"$CHECKSUM\"/" manifest.json

echo ""
echo "Build complete!"
echo "Plugin ZIP: $OUTPUT_DIR/Jellyfin.Plugin.Kinopoisk_$VERSION.zip"
echo "Plugin folder: $OUTPUT_DIR/КиноПоиск_$VERSION"
echo ""
echo "To install manually, copy the folder to your Jellyfin plugins directory."
