namespace Jellyfin.Plugin.Jellystream.Models;

/// <summary>
/// AceStream service health result.
/// </summary>
public sealed record AceStreamHealth(bool IsOnline, string BaseUrl, string? Error);
