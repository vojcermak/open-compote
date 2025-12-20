using System.Runtime.InteropServices;

namespace OpenCompote.SGA;

public class SgaDrive
{
    internal int RootFolderIndex {get; private set;} 
    internal uint StartFolder {get;}
    internal uint EndFolder {get;}
    internal uint StartFile {get;}
    internal uint EndFile {get;}

    public string Alias {get; set;}
    public string Name  {get; set;}
    public SgaArchive? Archive {get; private set;}
    public SgaFolder RootFolder {get; internal set;}

    public SgaDrive(string alias, string name, SgaArchive archive)
    {
        Alias = alias;
        Name = name;
        Archive = archive;
        RootFolder = new SgaFolder(name, this);
    }

    internal SgaDrive(string alias, string name, SgaArchive archive, int rootFolderIndex, uint startFolder, uint endFolder,  uint startFile, uint endFile)
    {
        Alias = alias;
        Name = name;
        Archive = archive;
        RootFolderIndex = rootFolderIndex;

        StartFolder = startFolder;
        EndFolder = endFolder;
        StartFile = startFile;
        EndFile = endFile;
        RootFolder = new SgaFolder(name, this);
    }

    public void Delete()
    {
        if(Archive == null)
            return;

        if(Archive.Mode == SgaMode.Read)
            throw new NotSupportedException("Writing is not supported in this mode.");

        Archive.ThrowIfDisposed();

        Archive._drives.Remove(this);
        Archive = null;
    }

    public SgaEntry GetEntry(string entryName)
    {
        throw new NotImplementedException();
    }
}
