namespace OpenCompote.SGA;

public class SgaFolder: SgaEntry
{
    public readonly List<SgaEntry> Contents;

    internal uint StartFolder {get;}
    internal uint EndFolder {get;}
    internal uint StartFile {get;}
    internal uint EndFile {get;}

    public SgaFolder(string name)
    {
        Name = name;
    }

    internal SgaFolder(string name, uint startFolder, uint endFolder,  uint startFile, uint endFile)
    {
        Contents = new List<SgaEntry>();
        Name = name;
        StartFolder = startFolder;
        EndFolder = endFolder;
        StartFile = startFile;
        EndFile = endFile;
    }

    public void ExtractToDirectory(string destination, bool overwrite = false)
    {
        throw new NotImplementedException();
    }

    public override void Delete()
    {
        throw new NotImplementedException();
    }

    public SgaFolder AddFolder(string name)
    {
        throw new NotImplementedException();
    }

    public SgaFile AddFile(string name, StorageType type)
    {
        throw new NotImplementedException();
    }
}
