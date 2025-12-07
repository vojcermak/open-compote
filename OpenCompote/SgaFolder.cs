namespace OpenCompote.SGA;

public class SgaFolder: SgaEntry
{
    public readonly List<SgaEntry> Contents;

    internal SgaFolder(string name)
    {
        Name = name;
        Contents = new List<SgaEntry>();
    }
}
