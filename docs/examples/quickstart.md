# Quick start

## Installation

You can download the latest version from the [NuGet](https://www.nuget.org/packages/OpenCompote.SGA) repo.

```bash
dotnet add package OpenCompote.SGA
```

## Reading from SGA archive

> Before you start, I recommend reading up on how SGA files work in our [schema documentation](../schema/index.md). 

### Opening the archive

The way how to open an existing SGA archive is to use the `SgaArchiveFile.Open(...)` function. When you want to open the archive for reading only you can use 

```csharp
using OpenCompote.SGA;

using SgaArchive archive = SgaArchiveFile.Open("example.sga", SgaMode.Read);

Console.WriteLine($"Archive name: {archive.ArchiveName}");
Console.WriteLine($"Version: {archive.Version}");
Console.WriteLine($"Drive count: {archive.Drives.Count}");
```

### Read archive contents

Once an archive is open, you can inspect drives, folders, and files. The archive uses `SgaDrive` objects and a tree of `SgaFolder` / `SgaFile` entries.

```csharp
using OpenCompote.SGA;
using System.Text;

// Open the archive
using SgaArchive archive = SgaArchiveFile.Open("example.sga", SgaMode.Read);

// prints entire folder structure
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

To read the contents of a specific file inside the opened archive, you can open the `SgaFile` entry and read its content stream.

```csharp
using OpenCompote.SGA;
using System.Text;

// Opens archive
using SgaArchive archive = SgaArchiveFile.Open("example.sga", SgaMode.Read);

SgaDrive drive = archive.Drives[0];
SgaFolder root = drive.RootFolder;

// Find the specific file
SgaFile file = (SgaFile)root.Contents[0];

// Open the file and write its contents to the console.
using Stream fileStream = file.Open();
using var reader = new StreamReader(fileStream, Encoding.UTF8);
string contents = reader.ReadToEnd();
Console.WriteLine(contents);
```

## Writing to the SGA archive

If you want to update an existing archive, open it in `SgaMode.Write` mode, modify entries, then dispose the archive to write the changes back to the file.

```csharp
using OpenCompote.SGA;
using System.Text;

// Open existing archive for writing.
using SgaArchive archive = SgaArchiveFile.Open("example.sga", SgaMode.Write);

// Find the folder we want to change
SgaDrive drive = archive.Drives[0];
SgaFolder root = drive.RootFolder;
SgaFile file = (SgaFile)root.Contents[0];

// Open the file and update its content.
using Stream writeStream = file.Open();
byte[] newBytes = Encoding.UTF8.GetBytes("Updated content");
writeStream.Write(newBytes, 0, newBytes.Length);
```

## Create new SGA archives

To create a new archive you need to call the `SgaArchiveFile.Create(...)` function. After that you can continue editing the same way as if you open the archive for editing.

```csharp
using OpenCompote.SGA;
using System.Text;

// Creates new SGA V2 archive
using SgaArchive archive = SgaArchiveFile.Create("new-archive.sga", SgaVersion.V2, overwrite: true);

// Create new drive and file in the new archive
SgaDrive drive = archive.AddDrive("GAME", "GameDrive");
SgaFolder root = drive.RootFolder;
SgaFile file = root.AddFile("hello.txt", StorageType.Uncompress);

// writes new file contents
byte[] contentBytes = Encoding.UTF8.GetBytes("Hello from OpenCompote!");
using (Stream writeStream = file.Open())
{
    writeStream.Write(contentBytes, 0, contentBytes.Length);
}
```

> When the archive is disposed, pending changes are written back to the archive file automatically.
