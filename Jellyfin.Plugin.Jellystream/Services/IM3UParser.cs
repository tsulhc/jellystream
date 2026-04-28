using Jellyfin.Plugin.Jellystream.Models;

namespace Jellyfin.Plugin.Jellystream.Services;

/// <summary>
/// Parses M3U playlists into Jellystream channels.
/// </summary>
public interface IM3UParser
{
    /// <summary>
    /// Parses playlist text.
    /// </summary>
    /// <param name="playlist">M3U playlist text.</param>
    /// <returns>Normalized channels.</returns>
    IReadOnlyList<JellystreamChannel> Parse(string playlist);
}
