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

## Rebuild for a new Jellyfin version (manual)

When Jellyfin releases a new version, the existing plugin DLL may fail at runtime
with a `MissingMethodException` because it was compiled against the older Jellyfin
API. To rebuild against the new API **without touching code or your local machine**:

1. Go to the repo's **Actions** tab → **Build & Publish Plugin** → **Run workflow**.
2. Fill in **`jellyfin_version`** with the new version (e.g. `10.11.12`).
   Leave **`target_abi`** blank to auto-set it to `<jellyfin_version>.0`.
3. Run it. The workflow overrides the `Jellyfin.Controller` / `Jellyfin.Model`
   NuGet versions (via `-p:JellyfinVersion=…`, wired through
   [`JellyfinTrending.csproj`](JellyfinTrending.csproj)), recompiles, releases,
   and updates `manifest.json`.

Outcomes:

- **Build succeeds** → the new Jellyfin API was source-compatible with this
  plugin. The new version deploys and should run without changes.
- **Build fails** → an API this plugin uses actually changed. Nothing broken is
  shipped; the CI log points at the code that needs updating.

> The rebuild needs the `Jellyfin.Controller` NuGet package for the target
> version to exist on nuget.org. It usually publishes with the Jellyfin release
> but can lag a day or two — a failure at the **Restore** step ("package not
> found") means it isn't available yet; wait and re-run.

Leaving both inputs blank (or the automatic on-push build) uses the project's
default Jellyfin version, so normal releases are unaffected.

## Build locally

```bash
./build-and-install.sh
```

This runs `dotnet publish -c Release` and, if a Jellyfin data directory is
detected, copies the plugin into place. See the script for manual install paths.
