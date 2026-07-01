using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Querying;
using Microsoft.Extensions.Logging;

namespace JellyfinTrending.Repository;

public class TrendingRepository
{
    private readonly ILibraryManager _libraryManager;
    private readonly IUserManager _userManager;
    private readonly IUserDataManager _userDataManager;
    private readonly ILogger<TrendingRepository> _logger;

    private readonly Dictionary<(string, int), CachedResult> _cache = new();
    private readonly object _lock = new();

    public TrendingRepository(
        ILibraryManager libraryManager,
        IUserManager userManager,
        IUserDataManager userDataManager,
        ILogger<TrendingRepository> logger)
    {
        _libraryManager = libraryManager;
        _userManager = userManager;
        _userDataManager = userDataManager;
        _logger = logger;
    }

    public List<TrendingItem> GetTrending(string? type = null, int days = 7, int limit = 20)
    {
        var key = (type ?? "all", days);
        lock (_lock)
        {
            if (_cache.TryGetValue(key, out var cached))
                return cached.Items.Take(limit).ToList();
        }

        _logger.LogInformation("Trending: cache miss for ({Type}, {Days}d), computing now", type ?? "all", days);
        var result = Compute(type, days);
        lock (_lock) { _cache[key] = new CachedResult(result); }
        return result.Take(limit).ToList();
    }

    public void RefreshCache()
    {
        _logger.LogInformation("Trending: refreshing cache...");
        var combinations = new[]
        {
            ((string?)null, 7),   ((string?)null, 30),   ((string?)null, 365),
            ("Movie",  7),        ("Movie",  30),        ("Movie",  365),
            ("Series", 7),        ("Series", 30),        ("Series", 365),
        };
        foreach (var (type, days) in combinations)
        {
            var result = Compute(type, days);
            lock (_lock) { _cache[(type ?? "all", days)] = new CachedResult(result); }
        }
        _logger.LogInformation("Trending: cache refreshed");
    }

    private List<TrendingItem> Compute(string? type, int days)
    {
        var cutoff = days > 0 ? DateTime.UtcNow.AddDays(-days) : DateTime.MinValue;
        var users = _userManager.GetUsers().ToList();
        var results = new List<TrendingItem>();

        // --- Movies ---
        if (type == null || type.Equals("Movie", StringComparison.OrdinalIgnoreCase))
        {
            var movies = _libraryManager.GetItemList(new InternalItemsQuery
            {
                IncludeItemTypes = new[] { BaseItemKind.Movie },
                IsVirtualItem = false,
                Recursive = true,
            });

            foreach (var movie in movies)
            {
                var viewers = users.Count(user =>
                {
                    var data = _userDataManager.GetUserData(user, movie);
                    if (data == null || data.PlayCount == 0) return false;
                    if (days <= 0) return true;
                    return data.LastPlayedDate.HasValue && data.LastPlayedDate.Value >= cutoff;
                });
                if (viewers > 0)
                    results.Add(new TrendingItem
                    {
                        ItemId = movie.Id.ToString("N"),
                        ItemName = movie.Name ?? string.Empty,
                        ItemType = "Movie",
                        UniqueViewers = viewers
                    });
            }
        }

        // --- Series: aggregate via Episodes ---
        if (type == null || type.Equals("Series", StringComparison.OrdinalIgnoreCase))
        {
            // Get all episodes, then group by SeriesId
            var episodes = _libraryManager.GetItemList(new InternalItemsQuery
            {
                IncludeItemTypes = new[] { BaseItemKind.Episode },
                IsVirtualItem = false,
                Recursive = true,
            });

            // For each user, find which series they watched (any episode) within the window
            // Result: seriesId -> set of userIds
            var seriesViewers = new Dictionary<Guid, HashSet<Guid>>();

            foreach (var episode in episodes.OfType<Episode>())
            {
                var seriesId = episode.SeriesId;
                if (seriesId == Guid.Empty) continue;

                foreach (var user in users)
                {
                    var data = _userDataManager.GetUserData(user, episode);
                    if (data == null || data.PlayCount == 0) continue;
                    if (days > 0 && (!data.LastPlayedDate.HasValue || data.LastPlayedDate.Value < cutoff))
                        continue;

                    if (!seriesViewers.TryGetValue(seriesId, out var viewers))
                        seriesViewers[seriesId] = viewers = new HashSet<Guid>();
                    viewers.Add(user.Id);
                }
            }

            // Resolve series names
            foreach (var (seriesId, viewers) in seriesViewers)
            {
                var series = _libraryManager.GetItemById(seriesId);
                if (series == null) continue;

                results.Add(new TrendingItem
                {
                    ItemId = seriesId.ToString("N"),
                    ItemName = series.Name ?? string.Empty,
                    ItemType = "Series",
                    UniqueViewers = viewers.Count
                });
            }
        }

        return results
            .OrderByDescending(t => t.UniqueViewers)
            .Take(100)
            .ToList();
    }

    private record CachedResult(List<TrendingItem> Items);
}

public class TrendingItem
{
    public string ItemId        { get; set; } = string.Empty;
    public string ItemName      { get; set; } = string.Empty;
    public string ItemType      { get; set; } = string.Empty;
    public int    UniqueViewers { get; set; }
}
