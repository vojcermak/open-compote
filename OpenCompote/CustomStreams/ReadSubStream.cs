namespace OpenCompote.SGA.CustomStreams;

/// <summary>
/// Stream is used as a wrapper around another stream. This stream is used to represent read only file in SGA archive.
/// Only reading is supported.
/// </summary>
internal sealed class ReadSubStream : Stream
{
    private readonly long _startOffset;
    private readonly long _maxLength;
    private long _position;
    private readonly Stream _superStream;
    private bool _isDisposed;
    private bool _canRead;

    public override bool CanRead => _superStream.CanRead && _canRead;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length
    {
        get
        {
            ThrowIfDisposed();
            
            return _maxLength;
        }
    }

    public override long Position
    {
        get
        {
            ThrowIfDisposed();
            
            return _position;
        }
        set
        {
            throw new NotSupportedException("This stream does not support seeking.");
        }
    }
    
    public ReadSubStream(Stream superStream, long startPosition, long maxLength)
    {
        _superStream = superStream;
        _startOffset = startPosition;
        _maxLength = maxLength;
        _position = 0;
        _canRead = true;
        _isDisposed = false;
    }

    public override void Flush()
    {
        throw new NotImplementedException("This stream does not support writing.");
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        ThrowIfDisposed();
        if (!CanRead)
            throw new NotSupportedException("This stream does not support reading");
        
        if(_superStream.Position != (_startOffset + _position))
            _superStream.Position = _startOffset + _position;
        if (_startOffset + _position + count > _startOffset + _maxLength)
                count = (int)(_maxLength - _position) ;

        int readCount = _superStream.Read(buffer, offset, count);
        _position += readCount;
        return readCount;
    }

    public override int Read(Span<byte> buffer)
    {
        int count = buffer.Length;

        ThrowIfDisposed();
        if (!CanRead)
            throw new NotSupportedException("This stream does not support reading");
        
        if(_superStream.Position != (_startOffset + _position))
            _superStream.Position = _startOffset + _position;
        if (_startOffset + _position + count > _startOffset + _maxLength)
                count = (int)(_maxLength - _position) ;

        int readCount = _superStream.Read(buffer[..count]);
        _position += readCount;
        return readCount;
    }

    public override int ReadByte()
    {
        ThrowIfDisposed();
        if (!CanRead)
            throw new NotSupportedException("This stream does not support reading");

        byte b = default;
        return Read(new Span<byte>(ref b)) == 1 ? b : -1;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException("This stream does not support seeking.");
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException("This stream does not support seeking.");
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotImplementedException("This stream does not support writing.");
    }

    protected override void Dispose(bool disposing)
    {
        if(disposing && !_isDisposed)
        {
            _canRead = false;
            _isDisposed = true;
        }
        base.Dispose(disposing);
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
    }
}
