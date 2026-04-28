namespace Jellyfin.Plugin.Jellystream.Models;

/// <summary>
/// Open AceStream response ready to be returned to Jellyfin.
/// </summary>
public sealed record StreamOpenResult(Stream Stream, string ContentType);
