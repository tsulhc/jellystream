using System.Globalization;
using System.Text.RegularExpressions;
using Jellyfin.Plugin.Jellystream.Configuration;
using Jellyfin.Plugin.Jellystream.Models;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Jellystream.Services;

/// <summary>
/// Minimal AceStream HTTP client.
/// </summary>
public sealed partial class AceStreamClient : IAceStreamClient
{
    private static readonly Regex ContentIdRegex = CreateContentIdRegex();
    private readonly HttpClient _httpClient;
    private readonly ILogger<AceStreamClient> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AceStreamClient"/> class.
    /// </summary>
    public AceStreamClient(HttpClient httpClient, ILogger<AceStreamClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<AceStreamHealth> CheckHealthAsync(CancellationToken cancellationToken)
    {
        var configuration = GetConfiguration();
        var baseUrl = NormalizeBaseUrl(configuration.AceStreamApiBaseUrl);

        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(5));
            using var request = new HttpRequestMessage(HttpMethod.Get, baseUrl);
            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token).ConfigureAwait(false);
            return new AceStreamHealth(response.IsSuccessStatusCode, baseUrl.ToString(), response.IsSuccessStatusCode ? null : response.StatusCode.ToString());
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
        {
            _logger.LogDebug(ex, "AceStream healthcheck failed");
            return new AceStreamHealth(false, baseUrl.ToString(), ex.Message);
        }
    }

    /// <inheritdoc />
    public AceStreamPlayback ResolvePlayback(string contentId)
    {
        if (!IsValidContentId(contentId))
        {
            throw new ArgumentException("Invalid AceStream content id.", nameof(contentId));
        }

        var configuration = GetConfiguration();
        var baseUrl = NormalizeBaseUrl(configuration.AceStreamPlaybackBaseUrl);
        var template = string.IsNullOrWhiteSpace(configuration.PlaybackUrlTemplate)
            ? "/ace/getstream?id={contentId}"
            : configuration.PlaybackUrlTemplate;
        var path = template.Replace("{contentId}", Uri.EscapeDataString(contentId.ToLower(CultureInfo.InvariantCulture)), StringComparison.Ordinal);
        var playbackUri = new Uri(baseUrl, path);
        return new AceStreamPlayback(contentId.ToLower(CultureInfo.InvariantCulture), playbackUri);
    }

    /// <inheritdoc />
    public async Task<StreamOpenResult> OpenStreamAsync(string contentId, CancellationToken cancellationToken)
    {
        var playback = ResolvePlayback(contentId);
        var configuration = GetConfiguration();
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(Math.Max(5, configuration.PrebufferTimeoutSeconds)));

        var request = new HttpRequestMessage(HttpMethod.Get, playback.PlaybackUri);
        request.Headers.UserAgent.ParseAdd(string.IsNullOrWhiteSpace(configuration.UpstreamUserAgent) ? "Jellystream/0.1.2" : configuration.UpstreamUserAgent);

        var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            response.Dispose();
            throw new HttpRequestException($"AceStream returned {(int)response.StatusCode} {response.ReasonPhrase}");
        }

        var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        var contentType = response.Content.Headers.ContentType?.ToString() ?? GuessContentType(configuration.StreamOutputPreference);
        return new StreamOpenResult(new HttpResponseMessageStream(response, stream), contentType);
    }

    private static PluginConfiguration GetConfiguration()
    {
        return Plugin.Instance?.Configuration ?? new PluginConfiguration();
    }

    private static bool IsValidContentId(string contentId)
    {
        return ContentIdRegex.IsMatch(contentId);
    }

    private static Uri NormalizeBaseUrl(string value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return new Uri("http://127.0.0.1:6878/");
        }

        return uri.AbsoluteUri.EndsWith("/", StringComparison.Ordinal) ? uri : new Uri(uri.AbsoluteUri + "/");
    }

    private static string GuessContentType(StreamOutputPreference preference)
    {
        return preference == StreamOutputPreference.Hls ? "application/vnd.apple.mpegurl" : "video/mp2t";
    }

    [GeneratedRegex("^[a-fA-F0-9]{40}$", RegexOptions.Compiled)]
    private static partial Regex CreateContentIdRegex();
}
