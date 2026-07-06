using System.IO.Compression;
using System.IO.Hashing;
using OpenCompote.SGA.CustomStreams;

namespace OpenCompote.SGA;

/// <summary>
/// Represents a file within a SGA archive.
/// </summary>
public class SgaFile: SgaEntry
{
    private readonly bool _isInStream = false;
    private readonly uint _dataOffset;
    private Stream? _fileContents;
    private bool _isOpen;
    private StorageType _storageType;
    private DateTimeOffset? _modified;
    private uint? _crc;

    /// <summary>
    /// Gets or sets the file's storage type.
    /// </summary>
    /// <exception cref="ObjectDisposedException">The archive for this file has been disposed.</exception>
    /// <exception cref="InvalidOperationException">The archive is opened in read-only mode or the file is currently open."</exception>
    public StorageType StorageType
    {
        get
        {
            ThrowIfDeleted();
            return _storageType;
        }
        set
        {
            ThrowIfDeleted();
            if(Drive!.Archive!.Mode == SgaMode.Read)
                throw new InvalidOperationException("Writing is not supported.");
            if(_isOpen)
                throw new InvalidOperationException("Cannot change Storage type when file is open.");
            if(_storageType == value)
                return;

            SetupFileContents();

            if(value == StorageType.Uncompress)
            {
                _fileContents = DecompressFileContents();
            }
            else if(_storageType == StorageType.Uncompress)
            {
                _fileContents = CompressFileContents();
            }

            CompressedSize = (uint)_fileContents!.Length;
            _storageType = value;
        }
    }

    /// <summary>
    /// Gets the compressed size in bytes, of the file in the archive.
    /// </summary>
    public uint CompressedSize {get; private set;}

    /// <summary>
    /// Gets the uncompressed size in bytes, of the file in the archive.
    /// </summary>
    public uint Size {get; private set;}
     
    /// <summary>
    /// Gets or sets the last write time of the file in the archive. When setting this property, the DateTime will be converted to the 32-bit unix timestamp format.
    /// This property could be <see langword="null"/> when opening SGA V2 archive without file metadata present. 
    /// Modified is automatically set to the current date and time when new file is created or changed file content stream is closed.  
    /// </summary>
    /// <exception cref="ObjectDisposedException">The archive for this file has been disposed.</exception>
    /// <exception cref="InvalidOperationException">The archive is opened in read-only mode.</exception>
    public DateTimeOffset? Modified {
        get
        {
            ThrowIfDeleted();
            return _modified;
        }
        set
        {
            ThrowIfDeleted();
            if(Drive!.Archive!.Mode == SgaMode.Read)
                throw new InvalidOperationException("Writing is not supported.");
            _modified = value;
        }
    }

    /// <summary>
    /// Get the CRC(Cyclic redundant check) of the opened file. Only loaded when present in the archive. If the crc in newly open archive this property will be <see langword="null"/>.
    /// CRC is automatically calculated for sga versions which are using CRCs. CRCs are not automatically check when file is opened, but you can check the your self by opening the file.
    /// </summary>
    /// <exception cref="ObjectDisposedException">The archive for this file has been disposed.</exception>
    public uint? Crc
    {
        get
        {
            ThrowIfDeleted();
            return _crc;
        }
    }

    internal SgaFile(string name, StorageType type, SgaDrive drive, SgaFolder parent)
    {   
        Drive = drive;
        _name = name;
        _storageType = type;
        Parent = parent;
        _modified = drive!.Archive!._timeProvider.GetLocalNow();
        _crc = 0;
    }

    internal SgaFile(
        string name, 
        StorageType type,
        uint dataOffset,
        uint compressedSize,
        uint size,
        DateTimeOffset? modified,
        uint? crc,
        SgaDrive drive,
        SgaFolder parent
    )
    {
        _dataOffset = dataOffset;
        _name = name;
        _storageType = type;
        CompressedSize = compressedSize;
        Size = size;
        Drive = drive;
        Parent = parent;
        _modified = modified;
        _crc = crc;
        _isInStream = true;
    }

    /// <summary>
    /// Opens the file and gets the file contents.
    /// </summary>
    /// <returns>A stream with the file contents.</returns>
    /// <exception cref="ObjectDisposedException">The SGA archive for this file has been disposed, or this file is deleted.</exception>
    /// <exception cref="IOException">The entry is currently open for writing.</exception>
    /// <exception cref="InvalidOperationException">Archive <see cref="SgaMode"/> value is invalid.</exception>
    public Stream Open()
    {   
        ThrowIfDeleted();

        switch (Drive!.Archive!.Mode)
        {
            case SgaMode.Read:
                return OpenReadOnly();
            case SgaMode.Create:
            case SgaMode.Write:
                return OpenReadWrite();
            default:
                throw new InvalidOperationException($"Invalid Mode value: {Drive.Archive.Mode}");
        }
    }

    internal Stream GetResultStream()
    {
        if(_isInStream && _fileContents == null)
            return new ReadSubStream(Drive!.Archive!._archiveStream, _dataOffset, CompressedSize);
            
        return _fileContents!;    
    }

    internal override void Delete(bool subDelete)
    {
        ThrowIfDeleted();
        
        if(Drive!.Archive!.Mode == SgaMode.Read)
            throw new NotSupportedException("Deleting is not supported in this mode.");
        
        if(!subDelete)
            Parent!._contents.Remove(this);

        Parent = null;
        Drive = null;
        
        _fileContents?.Dispose();
    }

    private void SetupFileContents()
    {
        if(_fileContents == null)
        {
            _fileContents = new MemoryStream();

            if (_isInStream)
            {
                var tempStream = new ReadSubStream(Drive!.Archive!._archiveStream, _dataOffset, CompressedSize);;
                tempStream.CopyTo(_fileContents);
                _fileContents.Position = 0;
            }

        }
    }

    private MemoryStream CompressFileContents()
    {
        if(_fileContents == null)
            ArgumentNullException.ThrowIfNull(_fileContents);
        
        _fileContents.Position = 0;
        var compressedStream = new MemoryStream();
        var zLibStream = new ZLibStream(compressedStream, CompressionMode.Compress, true);
        _fileContents.CopyTo(zLibStream);
        
        // Force ZLIB stream to write contents to the compressedStream. 
        // If the zlibStream wouldn`t be closed, bytes would be missing from the end of the compressed stream.
        zLibStream.Dispose();
        compressedStream.Position = 0; // Reset position because CopyTo moves it.
        return compressedStream;
    }

    private MemoryStream DecompressFileContents()
    {
        if(_fileContents == null)
            ArgumentNullException.ThrowIfNull(_fileContents);

        var zLibStream = new ZLibStream(_fileContents, CompressionMode.Decompress, true);
        var decompressedStream = new MemoryStream();
        zLibStream.CopyTo(decompressedStream);
        decompressedStream.Position = 0; // Reset position because CopyTo moves it.
        return decompressedStream;
    }

    private uint CalculateCrc()
    {
        if(_fileContents == null)
            ArgumentNullException.ThrowIfNull(_fileContents);

        _fileContents.Position = 0;

        var crc = new Crc32();
        crc.Append(_fileContents);
        
        return crc.GetCurrentHashAsUInt32();
    }

    private Stream OpenReadOnly()
    {
        ReadSubStream compressed = new ReadSubStream(Drive!.Archive!._archiveStream, _dataOffset, CompressedSize);

        if(StorageType == StorageType.Uncompress)
            return compressed;
        else
            return new ZLibStream(compressed, CompressionMode.Decompress);
    }

    private Stream OpenReadWrite()
    {
        if (_isOpen)
            throw new IOException("Files cannot be opened multiple times in Update/Create mode.");

        SetupFileContents();
        
        if(StorageType != StorageType.Uncompress)
            _fileContents = DecompressFileContents();

        _fileContents ??= new MemoryStream();
        _isOpen = true;

        return new WrapperStream(_fileContents, () =>
        {
            Size = (uint)_fileContents.Length;
            _crc = CalculateCrc();

            if(StorageType != StorageType.Uncompress)
                _fileContents = CompressFileContents();

            CompressedSize = (uint)_fileContents.Length;
            _isOpen = false;
            _modified = Drive!.Archive!._timeProvider.GetLocalNow();
        });
    }

    // ------------------------ Extending functions ------------------------
    // List of future ideas.  
    /// <summary>
    /// NOT IMPLEMENTED! DO NOT USE
    /// </summary>
    /// <exclude />
    internal void ExtractToFile(string destination, bool overwrite = false)
    {
        throw new NotImplementedException();
    }
}
