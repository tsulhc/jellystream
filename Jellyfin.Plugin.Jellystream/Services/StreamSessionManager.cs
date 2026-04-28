using Jellyfin.Plugin.Jellystream.Configuration;
using Jellyfin.Plugin.Jellystream.Models;

namespace Jellyfin.Plugin.Jellystream.Services;

/// <summary>
/// Enforces basic concurrency limits around AceStream playback.
/// </summary>
public sealed class StreamSessionManager : IStreamSessionManager
{
    private readonly IAceStreamClient _aceStreamClient;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private int _activeStreams;

    /// <summary>
    /// Initializes a new instance of the <see cref="StreamSessionManager"/> class.
    /// </summary>
    public StreamSessionManager(IAceStreamClient aceStreamClient)
    {
        _aceStreamClient = aceStreamClient;
    }

    /// <inheritdoc />
    public async Task<StreamOpenResult> OpenAsync(string contentId, CancellationToken cancellationToken)
    {
        var configuration = GetConfiguration();
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_activeStreams >= Math.Max(1, configuration.MaxConcurrentStreams))
            {
                throw new InvalidOperationException("Jellystream concurrent stream limit reached.");
            }

            _activeStreams++;
        }
        finally
        {
            _gate.Release();
        }

        try
        {
            var result = await _aceStreamClient.OpenStreamAsync(contentId, cancellationToken).ConfigureAwait(false);
            return result with { Stream = new CountingStream(result.Stream, ReleaseAsync) };
        }
        catch
        {
            await ReleaseAsync().ConfigureAwait(false);
            throw;
        }
    }

    private static PluginConfiguration GetConfiguration()
    {
        return Plugin.Instance?.Configuration ?? new PluginConfiguration();
    }

    private async ValueTask ReleaseAsync()
    {
        await _gate.WaitAsync().ConfigureAwait(false);
        try
        {
            _activeStreams = Math.Max(0, _activeStreams - 1);
        }
        finally
        {
            _gate.Release();
        }
    }
}
