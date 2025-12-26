using System.Data;
using System.Dynamic;
using System.IO.Compression;

namespace OpenCompote.SGA;

public class SgaFile: SgaEntry
{
    private uint _DataOffset;
    private Stream? _fileContents;
    private bool _isInStream = false;
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

        if (_isInStream)
        {
            long currentPosition = Drive!.Archive!._archiveStream.Position;
            Drive.Archive._archiveStream.Position = _DataOffset;
            
            byte[] buffer = new byte[CompressedSize];
            Drive.Archive._archiveStream.ReadExactly(buffer);
            using var compressed = new MemoryStream(buffer);
            
            Drive.Archive._archiveStream.Position = currentPosition;

            if(StorageType == StorageType.Uncompress)
                return compressed;
            else
                return new ZLibStream(compressed, CompressionMode.Decompress);   
        }

        _fileContents ??= new MemoryStream();
        return _fileContents;
    }

    public void ExtractToFile(string destination, bool overwrite = false)
    {
        throw new NotImplementedException();
    }

    public override void Delete()
    {
        throw new NotImplementedException();
    }

    private void ThrowIfDeleted()
    {
        ObjectDisposedException.ThrowIf(Drive == null || Drive.Archive == null,this);
        Drive.Archive.ThrowIfDisposed();
    }
}
