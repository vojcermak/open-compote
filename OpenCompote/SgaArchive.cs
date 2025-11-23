using System.Collections.ObjectModel;

namespace OpenCompote.SGA;

public class SgaArchive
{
    public int Version {get;}
    public int Mode {get;}
    public int ArchiveName {get; set;}
    public ReadOnlyCollection<SgaDrive> Drives;
    public int BlockSize {get; set;}

    private Stream ArchiveStream;

    public SgaArchive(Stream stream, int version, int mode){
        throw new NotImplementedException();
    }
}
