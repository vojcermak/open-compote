using System.Runtime.InteropServices;

namespace OpenCompote.SGA;

public class SgaDrive
{
    public string Alias {get; set;}
    public string Name  {get; set;}
    public SgaArchive? Archive {get; private set;}
    public SgaFolder RootFolder {get; internal set;}

    public SgaDrive(string alias, string name, SgaArchive archive)
    {
        Alias = alias;
        Name = name;
        Archive = archive;
        RootFolder = new SgaFolder(name, this, null);
    }

    public void Delete()
    {
        if(Archive == null)
            return;

        if(Archive.Mode == SgaMode.Read)
            throw new NotSupportedException("Writing is not supported in this mode.");

        Archive.ThrowIfDisposed();

        Archive._drives.Remove(this);
        RootFolder.Delete();
        Archive = null;
    }

    public SgaEntry GetEntry(string entryName)
    {
        throw new NotImplementedException();
    }
}
