# Quick start

## Installation

You can download the latest version from the [NuGet](https://www.nuget.org/packages/OpenCompote.SGA) repo.

```bash
dotnet add package OpenCompote.SGA
```

## Reading an SGA archive

> For background on the file format, see the [schema documentation](../schema/index.md).

### Open and inspect an archive

`SgaArchiveFile.Open` detects the archive version. Use `SgaMode.Read` when you only need to inspect or extract the archive.

```csharp
using OpenCompote.SGA;

using SgaArchive archive = SgaArchiveFile.Open("example.sga", SgaMode.Read);

Console.WriteLine($"Archive name: {archive.ArchiveName}");
Console.WriteLine($"Version: {archive.Version}");
Console.WriteLine($"Drive count: {archive.Drives.Count}");
```

### Browse drives, folders, and files

The entries at the root of a drive are available through `SgaDrive.Contents`. Recursively inspect folders to list the full archive structure:

```csharp
using System;
using System.Collections.Generic;
using OpenCompote.SGA;

void PrintEntries(IEnumerable<SgaEntry> entries, int indent)
{
    string prefix = new string(' ', indent * 2);

    foreach (SgaEntry entry in entries)
    {
        if (entry is SgaFolder folder)
        {
            Console.WriteLine($"{prefix}Folder: {folder.Name}");
            PrintEntries(folder.Contents, indent + 1);
        }
        else if (entry is SgaFile file)
        {
            Console.WriteLine($"{prefix}File: {file.Name} (size: {file.Size})");
        }
    }
}

using SgaArchive archive = SgaArchiveFile.Open("example.sga", SgaMode.Read);

foreach (SgaDrive drive in archive.Drives)
{
    Console.WriteLine($"Drive: {drive.Name} (alias: {drive.Alias})");
    PrintEntries(drive.Contents, 1);
}
```

### Find and read a file

Use `GetDrive` to select a drive by its name or alias, then `GetEntry` with a path relative to that drive. `GetEntry` returns `null` when the path does not exist. This example reads a UTF-8 text file; use the stream directly for binary files.

```csharp
using System;
using System.IO;
using System.Text;
using OpenCompote.SGA;

using SgaArchive archive = SgaArchiveFile.Open("example.sga", SgaMode.Read);

SgaDrive drive = archive.GetDrive("DATA")
    ?? throw new InvalidOperationException("Drive not found.");

if (drive.GetEntry(@"config\settings.txt") is not SgaFile file)
    throw new FileNotFoundException("File not found in the archive.");

using Stream fileStream = file.Open();
using var reader = new StreamReader(fileStream, Encoding.UTF8);
Console.WriteLine(reader.ReadToEnd());
```

### Extract an entire archive

To extract every drive and its contents, use the convenience method:

```csharp
using OpenCompote.SGA;

SgaArchiveFile.ExtractToDirectory("example.sga", "./exported");
```

By default, extraction does not overwrite existing files. Pass `overwrite: true` to replace them.

## Updating an existing archive

Open an archive in `SgaMode.Write` to modify it. Changes are written when the archive is disposed. The file stream is writable; call `SetLength(0)` before writing when replacing the whole file so that shorter content does not leave old bytes at the end.

```csharp
using System;
using System.IO;
using System.Text;
using OpenCompote.SGA;

using SgaArchive archive = SgaArchiveFile.Open("example.sga", SgaMode.Write);

SgaDrive drive = archive.GetDrive("DATA")
    ?? throw new InvalidOperationException("Drive not found.");

if (drive.GetEntry(@"config\settings.txt") is not SgaFile file)
    throw new FileNotFoundException("File not found in the archive.");

byte[] updatedContent = Encoding.UTF8.GetBytes("Updated content");
using (Stream writeStream = file.Open())
{
    writeStream.SetLength(0);
    writeStream.Write(updatedContent);
}
```

## Creating an SGA archive

Create a new archive, add a drive and file, then write the file contents through its stream:

```csharp
using System.IO;
using System.Text;
using OpenCompote.SGA;

using SgaArchive archive = SgaArchiveFile.Create(
    "new-archive.sga",
    SgaVersion.V2,
    overwrite: true);

SgaDrive drive = archive.AddDrive("GAME", "GameDrive");
SgaFile file = drive.AddFile("hello.txt", StorageType.Uncompress);

byte[] content = Encoding.UTF8.GetBytes("Hello from OpenCompote!");
using (Stream writeStream = file.Open())
{
    writeStream.Write(content);
}
```

The archive is finalized when it is disposed.

## Creating and extracting from directories

`CreateFromDirectory` creates one drive for each immediate subdirectory of the source directory. Files placed directly in the source directory are not imported by this method.

```csharp
using OpenCompote.SGA;

SgaArchiveFile.CreateFromDirectory(
    "./source",
    "destination.sga",
    SgaVersion.V2,
    StorageType.StreamCompress,
    overwrite: true);
```

For more control, create an archive and import a single directory as a drive with `SgaArchive.ImportDriveFromDirectory`.
