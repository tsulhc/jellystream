using Jellyfin.Plugin.Jellystream.Configuration;
using Jellyfin.Plugin.Jellystream.Models;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Jellystream.Services;

/// <summary>
/// Loads channels from inline and remote playlists.
/// </summary>
public sealed class ChannelProvider : IChannelProvider
{
    private readonly IM3UParser _parser;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ChannelProvider> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChannelProvider"/> class.
    /// </summary>
    public ChannelProvider(IM3UParser parser, IHttpClientFactory httpClientFactory, ILogger<ChannelProvider> logger)
    {
        _parser = parser;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<JellystreamChannel>> GetChannelsAsync(CancellationToken cancellationToken)
    {
        var configuration = GetConfiguration();
        var channels = new List<JellystreamChannel>();

        channels.AddRange(_parser.Parse(configuration.InlineM3U));

        foreach (var source in configuration.PlaylistSources.Where(static source => !string.IsNullOrWhiteSpace(source)))
        {
            if (!IsAllowedRemoteSource(source, configuration))
            {
                _logger.LogWarning("Skipping remote playlist source with non-allowed host: {Source}", source);
                continue;
            }

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, source);
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeout.CancelAfter(TimeSpan.FromSeconds(20));

                var client = _httpClientFactory.CreateClient(nameof(ChannelProvider));
                using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                var playlist = await response.Content.ReadAsStringAsync(timeout.Token).ConfigureAwait(false);
                channels.AddRange(_parser.Parse(playlist));
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
            {
                _logger.LogWarning(ex, "Failed to load remote playlist source: {Source}", source);
            }
        }

        return channels
            .GroupBy(static channel => channel.ContentId, StringComparer.OrdinalIgnoreCase)
            .Select(static group => group.First())
            .OrderBy(static channel => channel.Group, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static channel => channel.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static PluginConfiguration GetConfiguration()
    {
        return Plugin.Instance?.Configuration ?? new PluginConfiguration();
    }

    private static bool IsAllowedRemoteSource(string source, PluginConfiguration configuration)
    {
        if (!Uri.TryCreate(source, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return false;
        }

        if (configuration.AllowedPlaylistHosts.Length == 0)
        {
            return false;
        }

        return configuration.AllowedPlaylistHosts.Any(host => string.Equals(host, uri.Host, StringComparison.OrdinalIgnoreCase));
    }
}
