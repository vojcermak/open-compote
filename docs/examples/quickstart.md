# Quick start

## Installation

You can download latest version from the [NuGet](https://www.nuget.org/packages/OpenCompote.SGA) repo.

```bash
dotnet add package OpenCompote.SGA
```

## Examples

### Opening the archive

The simplest way to open an existing SGA archive is with `SgaArchiveFile.Open(...)`.

```csharp
using OpenCompote.SGA;

using SgaArchive archive = SgaArchiveFile.Open("example.sga", SgaMode.Read);

Console.WriteLine($"Archive name: {archive.ArchiveName}");
Console.WriteLine($"Version: {archive.Version}");
Console.WriteLine($"Drive count: {archive.Drives.Count}");
```

### Read archive file contents

Once an archive is open, you can inspect drives, folders, and files. The archive uses `SgaDrive` objects and a tree of `SgaFolder` / `SgaFile` entries.

```csharp
using OpenCompote.SGA;
using System.Text;

using SgaArchive archive = SgaArchiveFile.Open("example.sga", SgaMode.Read);

foreach (SgaDrive drive in archive.Drives)
{
    Console.WriteLine($"Drive: {drive.Name} (alias: {drive.Alias})");

    void PrintFolder(SgaFolder folder, int indent)
    {
        string prefix = new string(' ', indent * 2);
        Console.WriteLine($"{prefix}Folder: {folder.Name}");

        foreach (SgaEntry entry in folder.Contents)
        {
            if (entry is SgaFolder subfolder)
            {
                PrintFolder(subfolder, indent + 1);
            }
            else if (entry is SgaFile file)
            {
                Console.WriteLine($"{prefix}  File: {file.Name} (size: {file.Size})");
            }
        }
    }

    PrintFolder(drive.RootFolder, 0);
}
```

To read the contents of a file, open the `SgaFile` entry and read its stream.

```csharp
using OpenCompote.SGA;
using System.Text;

using SgaArchive archive = SgaArchiveFile.Open("example.sga", SgaMode.Read);

SgaDrive drive = archive.Drives[0];
SgaFolder root = drive.RootFolder;

SgaFile file = (SgaFile)root.Contents[0];

using Stream fileStream = file.Open();
using var reader = new StreamReader(fileStream, Encoding.UTF8);
string contents = reader.ReadToEnd();
Console.WriteLine(contents);
```

### Write archive file contents

Create a new archive or open an existing one in `SgaMode.Create` or `SgaMode.Write` to add and update entries.

```csharp
using OpenCompote.SGA;
using System.Text;

using SgaArchive archive = SgaArchiveFile.Create("new-archive.sga", SgaVersion.V2, overwrite: true);

SgaDrive drive = archive.AddDrive("GAME", "GameDrive");
SgaFolder root = drive.RootFolder;
SgaFolder textures = root.AddFolder("textures");
SgaFile file = textures.AddFile("hello.txt", StorageType.Uncompress);

byte[] contentBytes = Encoding.UTF8.GetBytes("Hello from OpenCompote!");
using (Stream writeStream = file.Open())
{
    writeStream.Write(contentBytes, 0, contentBytes.Length);
}

Console.WriteLine($"Created file: {file.Path} size={file.Size}");
```

If you want to update an existing archive, open it in `SgaMode.Write`, modify entries, then dispose the archive to persist changes.

```csharp
using OpenCompote.SGA;
using System.Text;

using SgaArchive archive = SgaArchiveFile.Open("example.sga", SgaMode.Write);

SgaDrive drive = archive.GetDrive("GAME") ?? archive.Drives[0];
SgaFolder root = drive.RootFolder;
SgaFile file = (SgaFile)root.Contents[0];

using Stream writeStream = file.Open();
byte[] newBytes = Encoding.UTF8.GetBytes("Updated content");
writeStream.Write(newBytes, 0, newBytes.Length);
```

> When the archive is disposed, pending changes are written back to the archive file automatically.
