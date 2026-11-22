# Reading archives

This guide covers opening an SGA archive, browsing its entries, finding entries by path, and reading file contents.

> **Supported versions:** SGA V2 is currently implemented. See the [schema documentation](../schema/index.md) for format details.

## Open an archive

Use `SgaArchiveFile.Open` with `SgaMode.Read` when the archive is only being inspected or read. The archive version is detected automatically.

```csharp
using OpenCompote.SGA;

using SgaArchive archive = SgaArchiveFile.Open("example.sga", SgaMode.Read);

Console.WriteLine($"Archive: {archive.ArchiveName}");
Console.WriteLine($"Version: {archive.Version}");
Console.WriteLine($"Drives: {archive.Drives.Count}");
```

`SgaArchive` implements `IDisposable`, so you can use a `using` statement to close the archive and its underlying file when finished. Do not use the archive or its entries after it has been disposed. Using the archive or its entries after the archive has been disposed throws `ObjectDisposedException`.

## Understand the archive structure

An archive contains drives. A drive contains files and folders directly. Folders can contain more folders and files. `Drives` and `Contents` are enumerable collections, so iterate over them rather than accessing entries by numeric index.

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
            Console.WriteLine($"{prefix}File: {file.Name} ({file.Size} bytes)");
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

`SgaFile.Size` is the uncompressed size in bytes. Use `SgaFile.CompressedSize` for the stored size.

## Find an entry by path

To get an entry from the archive by path, use the `GetEntry` method. It is available on the [archive object](./naming.md#archive-level), which uses absolute paths, and on [drives](./naming.md#drive-level) and [folders](./naming.md#folder-level), which use paths relative to the current object. The result is an `SgaEntry`, which is the base type for `SgaFile` and `SgaFolder`. The method returns `null` when no entry is found.

```csharp
using System;
using OpenCompote.SGA;

using SgaArchive archive = SgaArchiveFile.Open("example.sga", SgaMode.Read);

if (archive.GetEntry(@"DATA:\config\settings.txt") is SgaFile file)
{
    Console.WriteLine($"Found {file.Path}, {file.Size} bytes");
}
```

See [Names and paths](naming.md) for more detailed documentation about path rules.

The `SgaArchive.GetEntry` method finds files and folders. To find an `SgaDrive` by name or alias, use `SgaArchive.GetDrive` method.

## Read a file

If you want to access the contents of a file inside the archive, call `SgaFile.Open()` method. This method returns a stream containing the uncompressed file contents. 

Here is an example that gets a file by its path inside the SGA archive, opens it, and writes its contents to the console as text.

```csharp
using System;
using System.IO;
using System.Text;
using OpenCompote.SGA;

using SgaArchive archive = SgaArchiveFile.Open("example.sga", SgaMode.Read);

if (archive.GetEntry(@"DATA:\config\settings.txt") is not SgaFile file)
    throw new FileNotFoundException("File not found in the archive.");

using Stream fileStream = file.Open();
using var reader = new StreamReader(fileStream, Encoding.UTF8);
string text = reader.ReadToEnd();
Console.WriteLine(text);
```

If you want to extract the entire sga file into normal operating system file, prefer `SgaArchiveFile.ExtractToDirectory` or `SgaArchive.ExtractToDirectory`. See [Importing and extracting](directory-workflows.md) for more details.
