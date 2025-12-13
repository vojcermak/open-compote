using System.Data;
using System.Dynamic;
using System.IO.Compression;

namespace OpenCompote.SGA;

public class SgaFile: SgaEntry
{
    private uint _DataOffset;
    public StorageType StorageType {get; set;}
    public uint CompressedSize {get; private set;}
    public uint Size {get; private set;}

    internal SgaFile (string name, StorageType type, uint dataOffset, uint compressedSize, uint size)
    {
        _DataOffset = dataOffset;
        Name = name;
        StorageType = type;
        CompressedSize = compressedSize;
        Size = size;
    }

    public void Open()
    {
        long currentPosition = Drive.Archive._archiveStream.Position;
        Drive.Archive._archiveStream.Position = _DataOffset;
        byte[] buffer = new byte[CompressedSize];
        Drive.Archive._archiveStream.Read(buffer, 0, (int)CompressedSize);
        using var compressed = new MemoryStream(buffer);
        using var outputFile = File.Create("./dump/" + Name);

        if(StorageType == StorageType.Uncompress)
        {
            compressed.CopyTo(outputFile);
        }
        else
        {
            using var deflate = new ZLibStream(compressed, CompressionMode.Decompress);
            deflate.CopyTo(outputFile);
        }
        
        Drive.Archive._archiveStream.Position = currentPosition;
    }

    public void ExtractToFile(string destination, bool overwrite = false)
    {
        throw new NotImplementedException();
    }

    public override void Delete()
    {
        throw new NotImplementedException();
    }
}
