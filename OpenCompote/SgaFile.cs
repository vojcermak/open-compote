namespace OpenCompote.SGA;

public class SgaFile: SgaEntry
{
    public StorageType StorageType {get; set;}
    internal SgaFile (string name, StorageType type, uint dataOffset, uint compressSize, uint size)
    {
        Name = name;
        StorageType = type;
    }
}
