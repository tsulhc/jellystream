namespace Jellyfin.Plugin.Jellystream.Models;

/// <summary>
/// Normalized AceStream channel exposed to Jellyfin Live TV.
/// </summary>
public sealed record JellystreamChannel(
    string Id,
    string Name,
    string ContentId,
    string? Group,
    string? LogoUrl,
    string? TvgId);
