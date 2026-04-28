using Jellyfin.Plugin.Jellystream.Services;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.Jellystream;

/// <summary>
/// Registers Jellystream services with Jellyfin's dependency injection container.
/// </summary>
public sealed class PluginServiceRegistrator : IPluginServiceRegistrator
{
    /// <inheritdoc />
    public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
    {
        serviceCollection.AddHttpClient<IAceStreamClient, AceStreamClient>();
        serviceCollection.AddSingleton<IM3UParser, M3UParser>();
        serviceCollection.AddSingleton<IChannelProvider, ChannelProvider>();
        serviceCollection.AddSingleton<IStreamSessionManager, StreamSessionManager>();
    }
}
