using Jellyfin.Plugin.Jellystream.Models;

namespace Jellyfin.Plugin.Jellystream.Services;

/// <summary>
/// Talks to the configured AceStream engine or bridge.
/// </summary>
public interface IAceStreamClient
{
    /// <summary>
    /// Checks whether the AceStream API is reachable.
    /// </summary>
    Task<AceStreamHealth> CheckHealthAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Resolves a content id to a playback URL.
    /// </summary>
    AceStreamPlayback ResolvePlayback(string contentId);

    /// <summary>
    /// Opens a playback stream from AceStream.
    /// </summary>
    Task<StreamOpenResult> OpenStreamAsync(string contentId, CancellationToken cancellationToken);
}
