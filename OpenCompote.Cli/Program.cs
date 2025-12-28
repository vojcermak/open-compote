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

// using(var newSga = new SgaArchive(new MemoryStream(), SgaMode.Create, SgaVersion.V2))
// {
//     newSga.ArchiveName = "MyNewArchive";
//     var dataDrive = newSga.AddDrive("data", "gameData");
//     var attrDrive = newSga.AddDrive("attribs", "gameAttributes");

//     dataDrive.RootFolder.Name = ""; 
//     var subfolder = dataDrive.RootFolder.AddFolder("subfolder");   
//     subfolder.AddFolder("subsubFolder");
//     subfolder.AddFolder("folder2");
//     var file1 = subfolder.AddFile("file1", StorageType.Uncompress);
//     var fileStream = file1.Open();

//     using(var fileWriter = new StreamWriter(fileStream, leaveOpen: true))
//     {
//         fileWriter.WriteLine("Hello world!");        
//     }

//     Console.WriteLine(fileStream.Position);
//     fileStream.Position = 0;
//     Console.WriteLine(fileStream.ReadByte());
// }

using (SgaArchive archive = SgaArchiveFile.Open(sgaPath, SgaMode.Read))
{
    var dataDrive = archive.GetDrive("data");
    var artFolder = (SgaFolder)dataDrive.RootFolder.Contents.First((item) => { return item.Name == "art";});
    var decalsFolder = (SgaFolder)artFolder.Contents.First((item) => { return item.Name == "art\\decals";});
    var deathFolder = (SgaFolder)decalsFolder.Contents.First((item) => { return item.Name == "art\\decals\\death";});
    var grassImage = (SgaFile)deathFolder.Contents.First((item) => {return item.Name == "splat_1_grass.dds";});

    using(var openImage = grassImage.Open())
    {
        Console.WriteLine(openImage.ReadByte());
    }

    Console.WriteLine(grassImage.Name);

    using(var openImage = grassImage.Open())
    {
        Console.WriteLine(openImage.ReadByte());
    }

}
