using System.Runtime.InteropServices;

namespace OpenCompote.SGA;

public class SgaDrive
{
    internal int RootFolderIndex {get; private set;} 
    public string Alias {get; private set;}
    public string Name  {get; private set;}
    public SgaArchive Archive {get;}
    public SgaFolder RootFolder {get;}

    public SgaDrive(string alias, string name, SgaArchive archive)
    {
        Alias = alias;
        Name = name;
        Archive = archive;
    }

    internal SgaDrive(string alias, string name, SgaArchive archive, int rootFolderIndex)
    {
        Alias = alias;
        Name = name;
        Archive = archive;
        RootFolderIndex = rootFolderIndex;
    }
}
