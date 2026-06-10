using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace JellyfinTrending;

/// <summary>
/// Adds the Trending nav script to the Jellyfin web UI. Prefers the File
/// Transformation plugin (in-memory, works with a read-only web dir); falls
/// back to patching index.html on disk. Never throws — UI integration must
/// never prevent the server from starting.
/// </summary>
public class WebInjectionService : IHostedService
{
    private const string PluginGuid = "3a7b9c2d-4e5f-6a7b-8c9d-0e1f2a3b4c5d";
    private const string ScriptTag = "<script defer src=\"/Trending/ClientScript\"></script>";

    private static ILogger<WebInjectionService>? _staticLogger;

    private readonly IServerApplicationPaths _paths;
    private readonly ILogger<WebInjectionService> _logger;

    /// <summary>Initializes a new instance of <see cref="WebInjectionService"/>.</summary>
    public WebInjectionService(IServerApplicationPaths paths, ILogger<WebInjectionService> logger)
    {
        _paths = paths;
        _logger = logger;
        _staticLogger = logger;
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (TryRegisterFileTransformation())
        {
            _logger.LogInformation("Trending: registered nav injection via File Transformation plugin.");
            return Task.CompletedTask;
        }

        _logger.LogInformation("Trending: File Transformation plugin not found; trying on-disk injection.");
        TryInjectOnDisk();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// Callback invoked by the File Transformation plugin for each index.html request.
    /// Returns the HTML with our script tag injected. Must never return null/empty,
    /// or it would blank the web UI.
    /// </summary>
    public static string TransformIndexHtml(object payload)
    {
        var contents = ExtractContents(payload);
        try
        {
            if (string.IsNullOrEmpty(contents)) return contents ?? string.Empty;
            if (contents.Contains("/Trending/ClientScript", StringComparison.Ordinal)) return contents;

            var idx = contents.LastIndexOf("</body>", StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return contents;
            return contents.Insert(idx, ScriptTag);
        }
        catch (Exception ex)
        {
            _staticLogger?.LogWarning(ex, "Trending: index.html transform failed; serving original.");
            return contents ?? string.Empty;
        }
    }

    private bool TryRegisterFileTransformation()
    {
        try
        {
            var assembly = AssemblyLoadContext.All
                .SelectMany(c => c.Assemblies)
                .FirstOrDefault(a => a.FullName?.Contains(".FileTransformation", StringComparison.Ordinal) ?? false);

            var pluginInterface = assembly?.GetType("Jellyfin.Plugin.FileTransformation.PluginInterface");
            var register = pluginInterface?.GetMethod("RegisterTransformation", BindingFlags.Public | BindingFlags.Static);
            if (register == null) return false;

            var payload = new
            {
                id = PluginGuid,
                fileNamePattern = "^index\\.html$",
                callbackAssembly = typeof(WebInjectionService).Assembly.FullName,
                callbackClass = typeof(WebInjectionService).FullName,
                callbackMethod = nameof(TransformIndexHtml),
            };

            register.Invoke(null, new object?[] { payload });
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Trending: File Transformation registration failed; falling back to on-disk injection.");
            return false;
        }
    }

    private void TryInjectOnDisk()
    {
        try
        {
            var indexPath = Path.Combine(_paths.WebPath, "index.html");
            if (!File.Exists(indexPath))
            {
                _logger.LogInformation("Trending: index.html not found at {Path}; skipping.", indexPath);
                return;
            }

            var html = File.ReadAllText(indexPath);
            if (html.Contains("/Trending/ClientScript", StringComparison.Ordinal)) return;

            var idx = html.LastIndexOf("</body>", StringComparison.OrdinalIgnoreCase);
            if (idx < 0)
            {
                _logger.LogWarning("Trending: no </body> tag in index.html; skipping.");
                return;
            }

            File.WriteAllText(indexPath, html.Insert(idx, ScriptTag));
            _logger.LogInformation("Trending: injected nav link into {Path}.", indexPath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Trending: could not inject nav link (web dir is read-only and the File Transformation plugin "
                + "is not installed). Install it for sidebar integration. The page is still at /Trending/Page.");
        }
    }

    private static string? ExtractContents(object? payload)
    {
        if (payload == null) return null;
        if (payload is string s) return s;

        // POCO / anonymous object with a `contents` property.
        var prop = payload.GetType().GetProperty("contents") ?? payload.GetType().GetProperty("Contents");
        if (prop != null) return prop.GetValue(payload)?.ToString();

        // JObject / dictionary-like with a string indexer (e.g. Newtonsoft).
        foreach (var pi in payload.GetType().GetProperties())
        {
            var indexParams = pi.GetIndexParameters();
            if (indexParams.Length == 1 &&
                (indexParams[0].ParameterType == typeof(string) || indexParams[0].ParameterType == typeof(object)))
            {
                try
                {
                    var value = pi.GetValue(payload, new object[] { "contents" });
                    if (value != null) return value.ToString();
                }
                catch
                {
                    // try next indexer
                }
            }
        }

        return null;
    }
}
