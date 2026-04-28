namespace Jellyfin.Plugin.Jellystream.Services;

/// <summary>
/// Stream wrapper that invokes a release callback exactly once when disposed.
/// </summary>
public sealed class CountingStream : Stream
{
    private readonly Stream _inner;
    private readonly Func<ValueTask> _release;
    private int _released;

    /// <summary>
    /// Initializes a new instance of the <see cref="CountingStream"/> class.
    /// </summary>
    public CountingStream(Stream inner, Func<ValueTask> release)
    {
        _inner = inner;
        _release = release;
    }

    /// <inheritdoc />
    public override bool CanRead => _inner.CanRead;

    /// <inheritdoc />
    public override bool CanSeek => false;

    /// <inheritdoc />
    public override bool CanWrite => false;

    /// <inheritdoc />
    public override long Length => throw new NotSupportedException();

    /// <inheritdoc />
    public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

    /// <inheritdoc />
    public override void Flush() => _inner.Flush();

    /// <inheritdoc />
    public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);

    /// <inheritdoc />
    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) => _inner.ReadAsync(buffer, cancellationToken);

    /// <inheritdoc />
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    /// <inheritdoc />
    public override void SetLength(long value) => throw new NotSupportedException();

    /// <inheritdoc />
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _inner.Dispose();
            ReleaseOnceAsync().AsTask().GetAwaiter().GetResult();
        }

        base.Dispose(disposing);
    }

    /// <inheritdoc />
    public override async ValueTask DisposeAsync()
    {
        await _inner.DisposeAsync().ConfigureAwait(false);
        await ReleaseOnceAsync().ConfigureAwait(false);
        await base.DisposeAsync().ConfigureAwait(false);
    }

    private ValueTask ReleaseOnceAsync()
    {
        return Interlocked.Exchange(ref _released, 1) == 0 ? _release() : ValueTask.CompletedTask;
    }
}
