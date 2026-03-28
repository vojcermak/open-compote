namespace OpenCompote.SGA.Parsers.Structs;

internal readonly struct FolderRecord(
  uint nameOffset,
  ushort firstFolder,
  ushort lastFolder,
  ushort firstFile,
  ushort lastFile)
{
    public uint NameOffset { get; } = nameOffset;
    public ushort FirstFolder { get; } = firstFolder;
    public ushort LastFolder { get; } = lastFolder;
    public ushort FirstFile { get; } = firstFile;
    public ushort LastFile { get; } = lastFile;
}

internal class FolderWriterRecord(SgaFolder folder)
{
    public SgaFolder Folder { get; set; } = folder;
    public ushort FirstFolder {get; set;}
    public ushort LastFolder {get; set;}
    public ushort FirstFile {get; set;}
    public ushort LastFile {get; set;}
}