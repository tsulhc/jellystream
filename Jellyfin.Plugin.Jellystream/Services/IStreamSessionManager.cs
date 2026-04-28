using Jellyfin.Plugin.Jellystream.Models;

namespace Jellyfin.Plugin.Jellystream.Services;

/// <summary>
/// Manages access to live AceStream sessions.
/// </summary>
public interface IStreamSessionManager
{
    /// <summary>
    /// Opens a stream while enforcing plugin concurrency limits.
    /// </summary>
    Task<StreamOpenResult> OpenAsync(string contentId, CancellationToken cancellationToken);
}
