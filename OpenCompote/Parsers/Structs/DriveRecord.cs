namespace OpenCompote.SGA.Parsers.Structs;

internal readonly struct DriveRecord(
  string driveName,
  string driveAlias,
  ushort firstFolder,
  ushort lastFolder,
  ushort firstFile,
  ushort lastFile,
  ushort rootFolder)
{
    public string DriveName { get; } = driveName;
    public string DriveAlias { get; } = driveAlias;
    public ushort FirstFolder { get; } = firstFolder;
    public ushort LastFolder { get; } = lastFolder;
    public ushort FirstFile { get; } = firstFile;
    public ushort LastFile { get; } = lastFile;
    public ushort RootFolder { get; } = rootFolder;
}
