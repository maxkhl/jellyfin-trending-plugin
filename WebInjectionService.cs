using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace JellyfinTrending;

/// <summary>
/// Injects the Trending nav script into the Jellyfin web UI's index.html on startup.
/// All failures are swallowed — this must never prevent the server from starting.
/// </summary>
public class WebInjectionService : IHostedService
{
    private const string Marker = "<!-- JellyfinTrending nav -->";
    private const string ScriptTag = Marker + "<script defer src=\"/Trending/ClientScript\"></script>";

    private readonly IServerApplicationPaths _paths;
    private readonly ILogger<WebInjectionService> _logger;

    /// <summary>Initializes a new instance of <see cref="WebInjectionService"/>.</summary>
    public WebInjectionService(IServerApplicationPaths paths, ILogger<WebInjectionService> logger)
    {
        _paths = paths;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            var indexPath = Path.Combine(_paths.WebPath, "index.html");
            if (!File.Exists(indexPath))
            {
                _logger.LogInformation("Trending: index.html not found at {Path}; skipping nav injection.", indexPath);
                return Task.CompletedTask;
            }

            var html = File.ReadAllText(indexPath);
            if (html.Contains(Marker, StringComparison.Ordinal))
                return Task.CompletedTask; // already injected

            var idx = html.LastIndexOf("</body>", StringComparison.OrdinalIgnoreCase);
            if (idx < 0)
            {
                _logger.LogWarning("Trending: no </body> tag in index.html; skipping nav injection.");
                return Task.CompletedTask;
            }

            html = html.Insert(idx, ScriptTag);
            File.WriteAllText(indexPath, html);
            _logger.LogInformation("Trending: injected nav link into {Path}.", indexPath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Trending: could not inject nav link (web dir may be read-only). The page is still available at /Trending/Page.");
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
