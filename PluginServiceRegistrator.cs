using JellyfinTrending.Repository;
using JellyfinTrending.Tasks;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace JellyfinTrending;

public class PluginServiceRegistrator : IPluginServiceRegistrator
{
    public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
    {
        serviceCollection.AddSingleton<TrendingRepository>();
        serviceCollection.AddSingleton<IScheduledTask, RefreshTrendingTask>();
        serviceCollection.AddHostedService<WebInjectionService>();
    }
}
