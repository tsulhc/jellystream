using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.Jellystream.Configuration;

/// <summary>
/// Stores Jellystream settings persisted by Jellyfin.
/// </summary>
public sealed class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>
    /// Gets or sets the AceStream API base URL.
    /// </summary>
    public string AceStreamApiBaseUrl { get; set; } = "http://127.0.0.1:6878";

    /// <summary>
    /// Gets or sets the AceStream playback base URL visible to Jellyfin.
    /// </summary>
    public string AceStreamPlaybackBaseUrl { get; set; } = "http://127.0.0.1:6878";

    /// <summary>
    /// Gets or sets the URL template used to create playback URLs.
    /// </summary>
    public string PlaybackUrlTemplate { get; set; } = "/ace/getstream?id={contentId}";

    /// <summary>
    /// Gets or sets how generated M3U entries should point to streams.
    /// </summary>
    public PlaylistStreamMode PlaylistStreamMode { get; set; } = PlaylistStreamMode.Direct;

    /// <summary>
    /// Gets or sets the upstream user-agent used when Jellystream talks to AceStream.
    /// </summary>
    public string UpstreamUserAgent { get; set; } = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36";

    /// <summary>
    /// Gets or sets a value indicating whether Jellystream should proxy playback through Jellyfin.
    /// </summary>
    public bool ProxyStreams { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether Fire TV compatibility behavior is enabled.
    /// </summary>
    public bool FireTvCompatibilityMode { get; set; } = true;

    /// <summary>
    /// Gets or sets the preferred stream output.
    /// </summary>
    public StreamOutputPreference StreamOutputPreference { get; set; } = StreamOutputPreference.Auto;

    /// <summary>
    /// Gets or sets the number of seconds to wait while AceStream prepares playback.
    /// </summary>
    public int PrebufferTimeoutSeconds { get; set; } = 15;

    /// <summary>
    /// Gets or sets the keepalive window after last stream access.
    /// </summary>
    public int KeepAliveMinutes { get; set; } = 2;

    /// <summary>
    /// Gets or sets the maximum concurrent stream sessions.
    /// </summary>
    public int MaxConcurrentStreams { get; set; } = 2;

    /// <summary>
    /// Gets or sets static playlist text for initial MVP testing.
    /// </summary>
    public string InlineM3U { get; set; } = "#EXTM3U";

    /// <summary>
    /// Gets or sets local M3U playlist file paths readable by the Jellyfin server.
    /// </summary>
    public string[] LocalPlaylistFiles { get; set; } = [];

    /// <summary>
    /// Gets or sets allowed remote playlist hostnames.
    /// </summary>
    public string[] AllowedPlaylistHosts { get; set; } = [];

    /// <summary>
    /// Gets or sets remote playlist URLs.
    /// </summary>
    public string[] PlaylistSources { get; set; } = [];
}

/// <summary>
/// Desired output container type.
/// </summary>
public enum StreamOutputPreference
{
    /// <summary>
    /// Let the AceStream bridge and Jellyfin choose the best route.
    /// </summary>
    Auto,

    /// <summary>
    /// Prefer HLS output for Fire TV stability.
    /// </summary>
    Hls,

    /// <summary>
    /// Prefer MPEG-TS over HTTP.
    /// </summary>
    MpegTs
}

/// <summary>
/// Defines how Jellystream writes stream URLs in generated M3U playlists.
/// </summary>
public enum PlaylistStreamMode
{
    /// <summary>
    /// Write AceStream playback URLs directly in the generated M3U playlist.
    /// </summary>
    Direct,

    /// <summary>
    /// Write Jellystream proxy URLs in the generated M3U playlist.
    /// </summary>
    Proxy,

    /// <summary>
    /// Write Jellystream URLs that redirect to AceStream playback URLs.
    /// </summary>
    Redirect
}
