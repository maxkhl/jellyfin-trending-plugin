# Jellyfin Trending Plugin

Shows trending content across all users based on real server-side playback data.
Tracks plays and exposes a user-facing trending page at `/Trending/Page`.

- **Target Jellyfin ABI:** 10.11.11.0
- **Framework:** .NET 9

## Install via repository (recommended)

1. In Jellyfin, go to **Dashboard → Plugins → Repositories**.
2. Click **+** and add this repository:
   - **Name:** `Trending`
   - **URL:**
     ```
     https://raw.githubusercontent.com/maxkhl/jellyfin-trending-plugin/main/manifest.json
     ```
3. Go to **Catalog**, find **Trending** under *General*, and install it.
4. Restart Jellyfin.

Users can then access trending at `http://your-jellyfin:8096/Trending/Page`.

## How releases work

Every push to `main` triggers [the build workflow](.github/workflows/build.yml), which:

1. Builds the plugin (`dotnet publish`) with version `1.0.0.<run-number>`.
2. Packages `JellyfinTrending.dll` + `meta.json` into a zip.
3. Creates a GitHub **Release** with the zip attached.
4. Regenerates `manifest.json` (MD5 checksum + release download URL) and commits
   it back to `main`.

Jellyfin reads `manifest.json` from the repository URL above, so installs and
updates flow automatically once a build completes.

## Build locally

```bash
./build-and-install.sh
```

This runs `dotnet publish -c Release` and, if a Jellyfin data directory is
detected, copies the plugin into place. See the script for manual install paths.
