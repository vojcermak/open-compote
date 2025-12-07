namespace OpenCompote.SGA;

public class SgaFolder: SgaEntry
{
    public readonly List<SgaEntry> Contents;

    internal uint StartFolder {get;}
    internal uint EndFolder {get;}
    internal uint StartFile {get;}
    internal uint EndFile {get;}

    internal SgaFolder(string name, uint startFolder, uint endFolder,  uint startFile, uint endFile)
    {
        Contents = new List<SgaEntry>();
        Name = name;
        StartFolder = startFolder;
        EndFolder = endFolder;
        StartFile = startFile;
        EndFile = endFile;
    }
}
