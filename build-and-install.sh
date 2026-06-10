#!/usr/bin/env bash
# Build and install JellyfinTrending plugin
# Run this on any machine with .NET 8 SDK and internet access
# Then copy the output to maxmedia

set -e

PLUGIN_DIR="${1:-/var/lib/jellyfin/plugins/JellyfinTrending}"
OUTPUT_DIR="./bin/plugin"

echo "==> Building JellyfinTrending..."
dotnet restore
dotnet publish -c Release -o "$OUTPUT_DIR" --no-self-contained

echo ""
echo "==> Build complete. Files in: $OUTPUT_DIR"
echo ""

# If running directly on maxmedia, install automatically
if [ -d "/var/lib/jellyfin" ] || [ -d "/config/jellyfin" ]; then
    # Try to detect Jellyfin data dir
    if [ -d "/var/lib/jellyfin/plugins" ]; then
        JELLYFIN_PLUGINS="/var/lib/jellyfin/plugins"
    elif [ -d "/config/jellyfin/plugins" ]; then
        JELLYFIN_PLUGINS="/config/jellyfin/plugins"
    fi

    if [ -n "$JELLYFIN_PLUGINS" ]; then
        TARGET="$JELLYFIN_PLUGINS/JellyfinTrending"
        echo "==> Installing to $TARGET ..."
        mkdir -p "$TARGET"
        cp "$OUTPUT_DIR/JellyfinTrending.dll" "$TARGET/"
        # Copy meta.json
        cp meta.json "$TARGET/" 2>/dev/null || true
        echo "==> Done! Restart Jellyfin to load the plugin."
        echo ""
        echo "Users can access trending at:"
        echo "  http://your-jellyfin:8096/Trending/Page"
        exit 0
    fi
fi

echo "==> Manual install: copy these files to your Jellyfin plugins folder:"
echo "    $OUTPUT_DIR/JellyfinTrending.dll"
echo ""
echo "    Typical plugin path on Jellyfin (Docker):"
echo "      /config/plugins/JellyfinTrending/"
echo "    Or bare-metal:"
echo "      /var/lib/jellyfin/plugins/JellyfinTrending/"
echo ""
echo "Then restart Jellyfin. Users access trending at:"
echo "  http://maxmedia:8096/Trending/Page"
