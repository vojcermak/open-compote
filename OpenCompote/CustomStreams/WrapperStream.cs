namespace OpenCompote.SGA.CustomStreams;

/// <summary>
/// Stream is used as a wrapper around SgaArchive file stream. Is used to override dispose to close the stream for user, but not delete the
/// memory stream in the library.
/// </summary>
internal sealed class WrapperStream : Stream
{
    private readonly Stream _baseStream;
    private readonly bool _closeBaseStream;
    private bool _isDisposed;
    private readonly Action? _onDispose;

    public override bool CanRead => !_isDisposed && _baseStream.CanRead;

    public override bool CanSeek => !_isDisposed && _baseStream.CanSeek;

    public override bool CanWrite => !_isDisposed && _baseStream.CanWrite;

    public override long Length
    {
        get
        {
            ThrowIfDisposed();
            return _baseStream.Length;
        }
    }

    public override long Position 
    { 
        get
        {
            ThrowIfDisposed();
            return _baseStream.Position;
        }
        set
        {
            ThrowIfDisposed();
            ThrowIfCantSeek();
            _baseStream.Position = value;
        } 
    }

    public WrapperStream(Stream baseStream, Action? onDispose, bool closeStream = false)
    {
        _baseStream = baseStream;
        _closeBaseStream = closeStream;
        _isDisposed = false;
        _onDispose = onDispose;
    }

    public override void Flush()
    {
        ThrowIfDisposed();
        ThrowIfCantWrite();

        _baseStream.Flush();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        ThrowIfDisposed();
        ThrowIfCantRead();

        return _baseStream.Read(buffer, offset, count);
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        ThrowIfDisposed();
        ThrowIfCantSeek();

        return _baseStream.Seek(offset, origin);
    }

    public override void SetLength(long value)
    {
        ThrowIfDisposed();
        ThrowIfCantSeek();
        ThrowIfCantWrite();

        _baseStream.SetLength(value);
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        ThrowIfDisposed();
        ThrowIfCantWrite();

        _baseStream.Write(buffer, offset, count);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && !_isDisposed)
        {
            _onDispose?.Invoke();

            if (_closeBaseStream)
                _baseStream.Dispose();

            _isDisposed = true;
        }
        base.Dispose(disposing);
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
    }
    private void ThrowIfCantSeek()
    {
        if (!CanSeek)
            throw new NotSupportedException("This stream does not support seeking.");
    }
    private void ThrowIfCantRead()
    {
        if (!CanRead)
            throw new NotSupportedException("This stream does not support reading.");
    }

    private void ThrowIfCantWrite()
    {
        if (!CanWrite)
            throw new NotSupportedException("This stream does not support writing.");
    }
}