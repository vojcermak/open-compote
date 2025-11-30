using System.Runtime.InteropServices;

namespace OpenCompote.SGA;

public class SgaDrive
{
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
}
