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