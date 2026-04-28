using System.Text;
using Jellyfin.Plugin.Jellystream.Configuration;
using Jellyfin.Plugin.Jellystream.Models;
using Jellyfin.Plugin.Jellystream.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Jellystream.Controllers;

/// <summary>
/// Jellystream API endpoints used by Jellyfin Live TV and the admin dashboard.
/// </summary>
[ApiController]
[Route("Jellystream")]
public sealed class JellystreamController : ControllerBase
{
    private readonly IAceStreamClient _aceStreamClient;
    private readonly IChannelProvider _channelProvider;
    private readonly IStreamSessionManager _streamSessionManager;
    private readonly ILogger<JellystreamController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="JellystreamController"/> class.
    /// </summary>
    public JellystreamController(
        IAceStreamClient aceStreamClient,
        IChannelProvider channelProvider,
        IStreamSessionManager streamSessionManager,
        ILogger<JellystreamController> logger)
    {
        _aceStreamClient = aceStreamClient;
        _channelProvider = channelProvider;
        _streamSessionManager = streamSessionManager;
        _logger = logger;
    }

    /// <summary>
    /// Gets AceStream health.
    /// </summary>
    [HttpGet("Health")]
    [ProducesResponseType(typeof(AceStreamHealth), 200)]
    public async Task<ActionResult<AceStreamHealth>> GetHealth(CancellationToken cancellationToken)
    {
        return Ok(await _aceStreamClient.CheckHealthAsync(cancellationToken).ConfigureAwait(false));
    }

    /// <summary>
    /// Gets normalized channels.
    /// </summary>
    [HttpGet("Channels")]
    [ProducesResponseType(typeof(IReadOnlyList<JellystreamChannel>), 200)]
    public async Task<ActionResult<IReadOnlyList<JellystreamChannel>>> GetChannels(CancellationToken cancellationToken)
    {
        return Ok(await _channelProvider.GetChannelsAsync(cancellationToken).ConfigureAwait(false));
    }

    /// <summary>
    /// Gets the generated M3U playlist consumed by Jellyfin Live TV.
    /// </summary>
    [HttpGet("Playlist.m3u")]
    [Produces("audio/x-mpegurl")]
    public async Task<IActionResult> GetPlaylist(CancellationToken cancellationToken)
    {
        var channels = await _channelProvider.GetChannelsAsync(cancellationToken).ConfigureAwait(false);
        var builder = new StringBuilder("#EXTM3U\n");

        foreach (var channel in channels)
        {
            var streamUrl = BuildAbsoluteUrl($"Jellystream/Stream/{Uri.EscapeDataString(channel.Id)}");
            builder.Append("#EXTINF:-1");
            AppendAttribute(builder, "tvg-id", channel.TvgId);
            AppendAttribute(builder, "tvg-name", channel.Name);
            AppendAttribute(builder, "tvg-logo", channel.LogoUrl);
            AppendAttribute(builder, "group-title", channel.Group);
            builder.Append(',').Append(channel.Name).Append('\n');
            builder.Append(streamUrl).Append('\n');
        }

        return Content(builder.ToString(), "audio/x-mpegurl", Encoding.UTF8);
    }

    /// <summary>
    /// Opens a channel stream for Jellyfin playback.
    /// </summary>
    [HttpGet("Stream/{channelId}")]
    public async Task<IActionResult> StreamChannel(string channelId, CancellationToken cancellationToken)
    {
        var channels = await _channelProvider.GetChannelsAsync(cancellationToken).ConfigureAwait(false);
        var channel = channels.FirstOrDefault(candidate => string.Equals(candidate.Id, channelId, StringComparison.OrdinalIgnoreCase));
        if (channel is null)
        {
            return NotFound("Unknown Jellystream channel.");
        }

        return await StreamContentId(channel.ContentId, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Opens a raw AceStream content id for diagnostics.
    /// </summary>
    [HttpGet("StreamByContentId/{contentId}")]
    public async Task<IActionResult> StreamContentId(string contentId, CancellationToken cancellationToken)
    {
        var configuration = GetConfiguration();
        try
        {
            if (!configuration.ProxyStreams)
            {
                var playback = _aceStreamClient.ResolvePlayback(contentId);
                return Redirect(playback.PlaybackUri.ToString());
            }

            var result = await _streamSessionManager.OpenAsync(contentId, cancellationToken).ConfigureAwait(false);
            return File(result.Stream, result.ContentType, enableRangeProcessing: false);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(429, ex.Message);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
        {
            _logger.LogWarning(ex, "Failed to open AceStream content id {ContentId}", contentId);
            return StatusCode(504, "AceStream stream did not become available in time.");
        }
    }

    private string BuildAbsoluteUrl(string path)
    {
        var request = HttpContext.Request;
        var basePath = request.PathBase.HasValue ? request.PathBase.Value!.TrimEnd('/') : string.Empty;
        return $"{request.Scheme}://{request.Host}{basePath}/{path.TrimStart('/')}";
    }

    private static void AppendAttribute(StringBuilder builder, string name, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        builder.Append(' ').Append(name).Append("=\"").Append(value.Replace("\"", string.Empty, StringComparison.Ordinal)).Append('"');
    }

    private static PluginConfiguration GetConfiguration()
    {
        return Plugin.Instance?.Configuration ?? new PluginConfiguration();
    }
}
