namespace Jellyfin.Plugin.Jellystream.Models;

/// <summary>
/// Resolved playback information for an AceStream content id.
/// </summary>
public sealed record AceStreamPlayback(string ContentId, Uri PlaybackUri);
