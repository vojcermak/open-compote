using System.IO.Compression;
using OpenCompote.SGA.CustomStreams;

namespace OpenCompote.SGA;

public class SgaFile: SgaEntry
{
    private readonly bool _isInStream = false;
    private readonly uint _DataOffset;
    private Stream? _fileContents;
    private bool _isOpen;
    public StorageType StorageType {get; set;}
    public uint CompressedSize {get; private set;}
    public uint Size {get; private set;}

    internal SgaFile(string name, StorageType type, SgaDrive drive, SgaFolder parent)
    {   
        Drive = drive;
        _Name = name;
        StorageType = type;
        Parent = parent;
    }

    internal SgaFile(string name, StorageType type, uint dataOffset, uint compressedSize, uint size, SgaDrive drive, SgaFolder parent)
    {
        _DataOffset = dataOffset;
        _Name = name;
        StorageType = type;
        CompressedSize = compressedSize;
        Size = size;
        Drive = drive;
        Parent = parent;
        _isInStream = true;
    }

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
            return new ReadSubStream(Drive!.Archive!._archiveStream, _DataOffset, CompressedSize);
            
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

    private Stream OpenReadOnly()
    {
        ReadSubStream compressed = new ReadSubStream(Drive!.Archive!._archiveStream, _DataOffset, CompressedSize);

        if(StorageType == StorageType.Uncompress)
            return compressed;
        else
            return new ZLibStream(compressed, CompressionMode.Decompress);
    }

    private Stream OpenReadWrite()
    {
        if (_isOpen)
            throw new IOException("Files cannot be opened multiple times in Update/Create mode.");

        if (_isInStream && _fileContents == null)
        {
            _fileContents = new MemoryStream();
            var tempStream = OpenReadOnly();
            tempStream.CopyTo(_fileContents);
        }
        else if(StorageType != StorageType.Uncompress && _fileContents != null)
        {
            var zLibStream = new ZLibStream(_fileContents, CompressionMode.Decompress, true);
            _fileContents = new MemoryStream();
            zLibStream.CopyTo(_fileContents); 
        }

        _fileContents ??= new MemoryStream();
        _fileContents.Position = 0;
        _isOpen = true;

        return new WrapperStream(_fileContents, () =>
        {
            Size = (uint)_fileContents.Length;

            if(StorageType != StorageType.Uncompress)
            {   
                _fileContents.Position = 0;
                var compressedStream = new MemoryStream();
                var zLibStream = new ZLibStream(compressedStream, CompressionMode.Compress, true);
                _fileContents.CopyTo(zLibStream);
                _fileContents = compressedStream;
                zLibStream.Dispose();
            }

            CompressedSize = (uint)_fileContents.Length;
            _fileContents.Position = 0;
            _isOpen = false;
        });
    }

    // ------------------------ Extending functions ------------------------
    // List of future ideas.  
    /// <summary>
    /// NOT IMPLEMENTED! DO NOT USE
    /// </summary>
    public void ExtractToFile(string destination, bool overwrite = false)
    {
        throw new NotImplementedException();
    }
}
