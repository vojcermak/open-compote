using System.IO.Compression;
using OpenCompote.SGA.CustomStreams;

namespace OpenCompote.SGA;

public class SgaFile: SgaEntry
{
    private readonly uint _DataOffset;
    private Stream? _fileContents;
    private readonly bool _isInStream = false;
    public StorageType StorageType {get; set;}
    public uint CompressedSize {get; private set;}
    public uint Size {get; private set;}

    internal SgaFile(string name, StorageType type, SgaDrive drive, SgaFolder parent)
    {   
        Drive = drive;
        Name = name;
        StorageType = type;
        Parent = parent;
    }

    internal SgaFile(string name, StorageType type, uint dataOffset, uint compressedSize, uint size)
    {
        _DataOffset = dataOffset;
        Name = name;
        StorageType = type;
        CompressedSize = compressedSize;
        Size = size;
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

    public override void Delete()
    {
        ThrowIfDeleted();
        
        if(Drive!.Archive!.Mode == SgaMode.Read)
            throw new NotSupportedException("Deleting is not supported in this mode.");

        Parent!._contents.Remove(this);
        Parent = null;
        Drive = null;
        
        _fileContents?.Dispose();
    }

    public void ExtractToFile(string destination, bool overwrite = false)
    {
        throw new NotImplementedException();
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
        if (_isInStream && _fileContents == null)
        {
            _fileContents = new MemoryStream();
            var tempStream = OpenReadOnly();
            tempStream.CopyTo(_fileContents);
            _fileContents.Position = 0;
        }

        _fileContents ??= new MemoryStream();
        return _fileContents;
    }

    private void ThrowIfDeleted()
    {
        ObjectDisposedException.ThrowIf(Drive == null || Drive.Archive == null,this);
        Drive.Archive.ThrowIfDisposed();
    }
}
