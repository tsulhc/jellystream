using Jellyfin.Plugin.Jellystream.Models;

namespace Jellyfin.Plugin.Jellystream.Services;

/// <summary>
/// Provides configured AceStream channels.
/// </summary>
public interface IChannelProvider
{
    /// <summary>
    /// Gets configured channels.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Configured channels.</returns>
    Task<IReadOnlyList<JellystreamChannel>> GetChannelsAsync(CancellationToken cancellationToken);
}
