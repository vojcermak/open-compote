// See https://aka.ms/new-console-template for more information
using System.IO.Compression;
using OpenCompote.SGA;

/*
- Pack archive
- Unpack archive
- List
- Get specific file/folder/drive
- Add specific file/folder/drive
- remove specific file/folder/drive
*/

if (args.Length == 0)
{
    Console.WriteLine("Usage: dotnet run -- <path-to-sga-file>");
    return;
}

string sgaPath = args[0];

using(var newSga = new SgaArchive(new FileStream("output.sga", FileMode.Create, FileAccess.ReadWrite), SgaMode.Create, SgaVersion.V2))
{
    newSga.ArchiveName = "MyNewArchive";
    var dataDrive = newSga.AddDrive("data", "gameData");
    var attrDrive = newSga.AddDrive("attribs", "gameAttributes");

    dataDrive.RootFolder.Name = ""; 
    var subfolder = dataDrive.RootFolder.AddFolder("subfolder");
    subfolder.AddFolder("subsubFolder");
    subfolder.AddFolder("folder2");
    var file1 = subfolder.AddFile("file1.txt", StorageType.Uncompress);
    using var fileStream = file1.Open();

    using(var fileWriter = new StreamWriter(fileStream, leaveOpen: true))
    {
        fileWriter.Write("Hello world!");
    }

    Console.WriteLine(fileStream.Position);
    fileStream.Position = 0;
    Console.WriteLine(fileStream.ReadByte());
}

Console.WriteLine();

using (SgaArchive archive = SgaArchiveFile.Open(sgaPath, SgaMode.Write))
{
    //var drive = archive.GetDrive("gameData");
}
