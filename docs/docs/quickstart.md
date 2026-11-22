# Quick start

This page gets you started with the most common task: opening an SGA archive and reading a file. For more complete examples, see the guides linked at the end.

## Install

Install the package from [NuGet](https://www.nuget.org/packages/OpenCompote.SGA):

```bash
dotnet add package OpenCompote.SGA
```

> **Supported versions:** SGA V2 is currently implemented. Other values in `SgaVersion` are not yet supported for reading or writing.

## Open an archive and read a file

Open an archive in `SgaMode.Read` when you only need to inspect or read its contents. The archive detects its format version when it opens.

```csharp
using System;
using System.IO;
using System.Text;
using OpenCompote.SGA;

using SgaArchive archive = SgaArchiveFile.Open("example.sga", SgaMode.Read);

Console.WriteLine($"SGA version: {archive.Version}");

// Gets settings.txt inside the config folder in the DATA drive.
if (archive.GetEntry(@"DATA:\config\settings.txt") is not SgaFile file)
    throw new FileNotFoundException("File not found in the archive.");

using Stream fileStream = file.Open();
using var reader = new StreamReader(fileStream, Encoding.UTF8);
Console.WriteLine(reader.ReadToEnd());
```

`GetEntry` takes the absolute path of an archive entry and returns `null` if it cannot find the entry. This sample assumes that the archived file is UTF-8 text. For binary data, read from `file.Open()` as a stream instead. See [Reading archives](reading.md) for browsing entries, paths, and more reading examples.

## Extract an archive

You can quickly extract an entire archive and its contents to a directory using `SgaArchiveFile.ExtractToDirectory` extension method:

```csharp
using OpenCompote.SGA;

SgaArchiveFile.ExtractToDirectory("example.sga", "./exported");
```

Extraction does not overwrite existing files by default. Pass `overwrite: true` to replace them. See [Importing and extracting](directory-workflows.md) for directory imports and additional extraction details.

## Next steps

- [Reading archives](reading.md) — browse drives and folders, find entries by path, and read file streams.
- [Creating and editing archives](editing.md) — create an archive, add entries, or update an existing file.
- [Importing and extracting](directory-workflows.md) — create archives from directories and extract their contents.
- [Names and paths](naming.md) — path formats, lookup behavior, and naming restrictions.
- [SGA format schemas](../schema/index.md) — learn about the archive format itself.

For all public types and members, see the [API reference](../api/OpenCompote.SGA.yml).
