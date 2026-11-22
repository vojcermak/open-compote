# Creating and editing archives

This guide shows how to create a new archive and how to replace a file in an existing archive.

> **Supported versions:** SGA V2 is currently implemented for reading and writing. Other values in `SgaVersion` are not yet supported.

## Create an archive

You can create an archive with the `SgaArchiveFile.Create` method. This method creates an empty archive without any drives or other contents. At the archive level, you can set `SgaArchive.ArchiveName` or add SGA drives. An SGA drive is a top-level collection of folders and files and works similarly to drives in Windows.

You can create a drive using the `SgaArchive.AddDrive()` method, which takes the drive alias first and its name as the second argument. Its entries are accessed directly through the `SgaDrive.Contents` attribute.

To add other folders and files to the drive, use the `AddFolder` and `AddFile` methods. These methods can be called on either a drive or a folder to create entries at that level.

```csharp
using System.IO;
using System.Text;
using OpenCompote.SGA;

using SgaArchive archive = SgaArchiveFile.Create(
    "new-archive.sga",
    SgaVersion.V2);

SgaDrive drive = archive.AddDrive("DATA", "GameDrive");
SgaFolder config = drive.AddFolder("config");
SgaFile file = config.AddFile("settings.txt", StorageType.Uncompress);

byte[] content = Encoding.UTF8.GetBytes("setting=value");
using (Stream writeStream = file.Open())
{
    writeStream.Write(content);
}
```

The changes of the archive are written to the file on disk only when the `SgaArchive` object is disposed. Dispose each file stream before disposing the archive.

## Replace a file in an existing archive

To update an existing archive, open it in `SgaMode.Write`. Find or create the file that you want to change, open it with `SgaFile.Open`, and write the changes to the returned stream.

> In `SgaMode.Write` or `SgaMode.Create` modes, `SgaFile.Open()` returns a readable and writable stream containing decompressed file data. When the parent archive is opened in read-only mode, the stream is read-only.

```csharp
using System;
using System.IO;
using System.Text;
using OpenCompote.SGA;

using SgaArchive archive = SgaArchiveFile.Open("example.sga", SgaMode.Write);

if (archive.GetEntry(@"DATA:\config\settings.txt") is not SgaFile file)
    throw new FileNotFoundException("File not found in the archive.");

byte[] updatedContent = Encoding.UTF8.GetBytes("setting=updated");
using (Stream writeStream = file.Open())
{
    writeStream.SetLength(0);
    writeStream.Write(updatedContent);
}
```
Dispose the file stream to finish the update. File metadata such as `SgaFile.Size`, `SgaFile.CompressedSize`, `SgaFile.Modified` and `SgaFile.Crc` are updated when the stream is disposed. Changes to the archive are persisted in the archive when the archive itself is disposed.

> **Do not open the same file for writing more than once at the same time.** In `SgaMode.Create` or `SgaMode.Write`, a second call to `SgaFile.Open()` while the first writable stream is still open throws `IOException`. Dispose the first stream before opening the file again. Read-only streams do not have this restriction.

## File properties

`SgaFile` exposes properties that describe the entry and its contents:

- `Name` gets or sets the file name. It is inherited from `SgaEntry` and must follow [name restrictions](naming.md#filefolder-naming-restrictions).
- `Path` gets the fully qualified path of the entry.
- `Drive` gets the drive that contains the file.
- `Parent` gets the containing folder, or `null` when the file is directly inside a drive.
- `StorageType` gets or sets how the file is stored. It can be changed in create/write mode, but not while the file stream is open.
- `Size` gets the uncompressed size of the file in bytes.
- `CompressedSize` gets the stored size in bytes.
- `Modified` gets or sets the file's last-write time as a `DateTimeOffset`. Existing SGA V2 files may have no modification metadata, in which case it can be `null`. A newly created or changed file is updated when its stream is disposed.
- `Crc` gets the CRC-32 checksum value of the uncompressed file contents. It can be `null` when the archive format or metadata has no value to expose. Opening a file does not automatically validate the file-level CRC.

The `Size`, `CompressedSize`, `Modified`, and `Crc` values for edited files are automatically updated when the writable sga file stream is disposed. Reading properties again after disposing the stream shows the updated values.

For existing entries, changing `StorageType` in create/write mode converts the file between uncompressed and ZLib-compressed storage. Changing `StorageType` while the file contents are open for editing results in `IOException`.

## Choose a storage type

When adding a new SGA file, you can choose whether the file is compressed using the `StorageType` property. `StorageType` supports these options:

- [Uncompress](xref:OpenCompote.SGA.StorageType.Uncompress) - stores the file without compression.
- [StreamCompress](xref:OpenCompote.SGA.StorageType.StreamCompress) - stores the file using ZLib compression.
- [BufferCompress](xref:OpenCompote.SGA.StorageType.BufferCompress) - stores the file using ZLib compression.

As you can see both `StreamCompress` and `BufferCompress` store the file contents using ZLib compression. These options correspond to storage types used by the SGA format. The parser and writer treat these options identically, but games may treat the files differently.

To rename an entry, set its `Name` property:

```csharp
file.Name = "new-settings.txt";
folder.Name = "configuration";
```

Names are validated and case-insensitive within a drive or folder. See [Names and paths](naming.md) for the naming rules.

## Delete entries

You can also delete existing entries by calling `Delete()` on a file, folder, or drive while the archive is in create or write mode. Deleting a folder or drive also deletes all of its files and subfolders.

```csharp
// deleting file
if (archive.GetEntry(@"DATA:\config\obsolete.txt") is SgaEntry obsoleteFile)
    obsoleteFile.Delete();

// deleting folder
if (archive.GetEntry(@"DATA:\old-content") is SgaEntry oldFolder)
    oldFolder.Delete();

// deleting drive
if (archive.GetDrive("DATA") is SgaDrive drive)
    drive.Delete();
```

The entry is removed from its parent collection when `Delete()` succeeds. An entry that has already been deleted or the parent archive is disposed should not be used again.

Do not delete a file or folder while an editable file stream is open. A file opened for writing cannot be deleted until its stream is disposed. A folder cannot be deleted while a file inside it is open for writing.
