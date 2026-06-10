using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JellyfinTrending.Repository;
using MediaBrowser.Model.Tasks;

namespace JellyfinTrending.Tasks;

public class RefreshTrendingTask : IScheduledTask
{
    private readonly TrendingRepository _repository;

    public RefreshTrendingTask(TrendingRepository repository)
    {
        _repository = repository;
    }

    public string Name        => "Refresh Trending Cache";
    public string Key         => "TrendingRefresh";
    public string Description => "Recalculates trending content across all users and time ranges.";
    public string Category    => "Trending";

    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
    {
        yield return new TaskTriggerInfo
        {
            Type          = TaskTriggerInfoType.IntervalTrigger,
            IntervalTicks = TimeSpan.FromHours(6).Ticks
        };
    }

    public Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        progress.Report(0);
        _repository.RefreshCache();
        progress.Report(100);
        return Task.CompletedTask;
    }
}
