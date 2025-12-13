using System.Runtime.InteropServices;

namespace OpenCompote.SGA;

public class SgaDrive
{
    internal int RootFolderIndex {get; private set;} 
    public string Alias {get; private set;}
    public string Name  {get; private set;}
    public SgaArchive Archive {get;}
    public SgaFolder? RootFolder {get; internal set;}

    internal uint StartFolder {get;}
    internal uint EndFolder {get;}
    internal uint StartFile {get;}
    internal uint EndFile {get;}

    public SgaDrive(string alias, string name, SgaArchive archive)
    {
        Alias = alias;
        Name = name;
        Archive = archive;
        RootFolder = new SgaFolder(name);
    }

    public SgaDrive(string alias, string name, SgaArchive archive, SgaFolder rootFolder)
    {
        Alias = alias;
        Name = name;
        Archive = archive;
        RootFolder = rootFolder;
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
    }

    public void Delete()
    {
        throw new NotImplementedException();
    }
}
