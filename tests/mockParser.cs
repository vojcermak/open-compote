using System.Collections.ObjectModel;
using System.IO.Compression;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using OpenCompote.SGA.Parsers;

namespace OpenCompote.SGA.Tests;

public class MockParser : ISgaParser
{
    private readonly List<TestDrive> _drives;
    private readonly List<TestDrive> _expectedTree;

    private Stream? _testStream;

    public MockParser(List<TestDrive> initialTree, List<TestDrive> expectedTree)
    {
        _drives = initialTree;
        _expectedTree = expectedTree;
    }

    public void Parse(SgaArchive archive, Stream sgaStream)
    {
        _testStream = sgaStream;

        foreach(var testDrive in _drives)
        {
            var newDrive = new SgaDrive(testDrive.Alias, testDrive.Name, archive);
            archive.AddDrive(newDrive);

            newDrive.RootFolder = ParseTree(testDrive.RootFolder, newDrive, null);
        }
    }

    public void Write(SgaArchive archive, Stream sgaStream)
    {
    }

    private SgaFolder ParseTree(TestFolder folderTemplate, SgaDrive drive, SgaFolder? parent )
    {
        SgaFolder folder = new (folderTemplate.Name, drive, parent);
        parent?._contents.Add(folder);

        foreach( var subfolder in folderTemplate.Folders)
        {
            ParseTree(subfolder, drive, folder);
        }

        foreach( var testFile in folderTemplate.Files)
        {
            uint dataOffset = (uint)_testStream!.Position;
            uint size = 0;
            uint compressedSize = 0;
            

            if(testFile.StorageType == StorageType.Uncompress)
            {
                BinaryWriter writer = new (_testStream!, Encoding.Default, true);
                writer.Write(testFile.FileContent);
                writer.Close();
                size = (uint)_testStream.Length - dataOffset;
                compressedSize = size;
            }
            else
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(testFile.FileContent);
                using (var zlib = new ZLibStream(_testStream, CompressionLevel.Optimal, leaveOpen: true))
                {
                    zlib.Write(inputBytes, 0, inputBytes.Length);
                }
                size = (uint)inputBytes.Length;
                compressedSize = (uint)_testStream.Length - dataOffset;
            }

            SgaFile file = new (testFile.Name, testFile.StorageType, dataOffset, compressedSize, size);
            file.Drive = drive;
            file.Parent = parent;
            folder._contents.Add(file);
        }

        return folder;
    }
}

public class TestDrive
{
    public required string Name {get; set;}
    public required string Alias {get; set;}
    public required TestFolder RootFolder {get; set;}
}

public class TestFolder
{
    public required string Name {get; set;}
    public required List<TestFolder> Folders {get; set;}
    public required List<TestFile> Files {get; set;}
}

public class TestFile
{
    public required string Name {get; set;}
    public required StorageType StorageType {get; set;}
    public required string FileContent {get; set;}
}