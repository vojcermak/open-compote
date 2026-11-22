# Importing and extracting directories

If you want to quickly import or extract entire files folders or drives you can use build in convenience functions.

## Extract an archive

If you want to extract entire archive to a specified folder you can use `SgaArchiveFile.ExtractToDirectory` method:

```csharp
using OpenCompote.SGA;

SgaArchiveFile.ExtractToDirectory(
    "example.sga",
    "./exported");
```

By default, extraction fails if a destination file already exists. Pass `overwrite: true` to replace existing files:

```csharp
SgaArchiveFile.ExtractToDirectory(
    "example.sga",
    "./exported",
    overwrite: true);
```

The archive's drive names are used as directory names. You can also open an archive yourself and call the `ExtractToDirectory` on it, or extract an individual drive or folder.

## Create an archive from a directory tree

If you want to quickly package some directory to an archive you can use the `SgaArchiveFile.CreateFromDirectory` method. `SgaArchiveFile.CreateFromDirectory` treats each immediate subdirectory of the source directory as a sga drive. The subdirectory name becomes the drive's name and alias. Its contents are imported recursively. Files placed directly in the source directory are not included.

For example, with this source layout:

```text
source/
  DATA/
    config/
      settings.txt
  SOUND/
    music/
      theme.wav
```

the archive contains `DATA` and `SOUND` drives.

```csharp
using OpenCompote.SGA;

SgaArchiveFile.CreateFromDirectory(
    "./source",
    "destination.sga",
    SgaVersion.V2,
    StorageType.StreamCompress);
```

The `defaultStorageType` defines the default `StorageType` that will be used for imported files. The destination archive is not overwritten unless you pass `overwrite: true`.

## Import a single directory as a drive

If you already have an archive open for writing use `ImportDriveFromDirectory` to add one directory tree as a drive:

```csharp
using OpenCompote.SGA;

using SgaArchive archive = SgaArchiveFile.Create(
    "destination.sga",
    SgaVersion.V2);

SgaDrive drive = archive.ImportDriveFromDirectory(
    "./source/DATA",
    StorageType.StreamCompress);
```

The imported directory's name is used as both the drive name and alias.

## Import into an existing drive or folder

Use `ImportFromDirectory` to add a directory and its contents as a folder inside a drive or another folder. Use `ImportFromFile` to add an individual file. For example:

```csharp
using OpenCompote.SGA;

using SgaArchive archive = SgaArchiveFile.Create(
    "destination.sga",
    SgaVersion.V2);

SgaDrive drive = archive.AddDrive("DATA", "DATA");
drive.ImportFromDirectory("./source/config", StorageType.Uncompress);
drive.ImportFromFile("./source/readme.txt", StorageType.Uncompress);
```
