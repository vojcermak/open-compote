using System.ComponentModel.DataAnnotations;
using System.IO.Compression;
using System.IO.Hashing;
using System.Text;
using OpenCompote.SGA.Parsers;

namespace OpenCompote.SGA.Tests;

public class MockParser : ISgaParser
{
    private readonly string _archiveName;
    private readonly List<TestDrive> _drives;
    private readonly List<TestDrive> _expectedTree;

    private Stream? _testStream;

    public MockParser(string archiveName ,List<TestDrive> initialTree, List<TestDrive> expectedTree)
    {
        _archiveName = archiveName;
        _drives = initialTree;
        _expectedTree = expectedTree;
    }

    public void Parse(SgaArchive archive, Stream sgaStream)
    {
        _testStream = sgaStream;
        archive._archiveName = _archiveName;

        foreach(var testDrive in _drives)
        {
            var newDrive = new SgaDrive(testDrive.Alias, testDrive.Name, archive);
            archive._drives.Add(newDrive);

            ParseTree(testDrive.RootFolder, newDrive, null);
        }
    }

    public void Write(SgaArchive archive, Stream sgaStream)
    {
        Assert.Equal(_expectedTree.Count, archive.Drives.Count);
        
        for (int i = 0; i < archive.Drives.Count; i++){
            Assert_Drive(_expectedTree[i], archive.Drives[i], archive);
        }
    }

    private void ParseTree(TestFolder folderTemplate, SgaDrive drive, SgaFolder? parent )
    {
        // add all files to the structure.
        foreach(var subfolder in folderTemplate.Folders)
        {
            SgaFolder folder = new (subfolder.Name, drive, parent);
            if(parent == null)
                drive._entries.Add(folderTemplate.Name, folder);
            else
                parent._entries.Add(folderTemplate.Name, folder);
            ParseTree(subfolder, drive, folder);
        }

        foreach( var testFile in folderTemplate.Files)
        {
            uint dataOffset = (uint)_testStream!.Position;
            byte[] inputBytes = Encoding.UTF8.GetBytes(testFile.FileContent);
            uint size = (uint)inputBytes.Length;
            uint compressedSize = 0;
            uint crc =  Crc32.HashToUInt32(inputBytes);

            if(testFile.StorageType == StorageType.Uncompress)
            {
                _testStream.Write(inputBytes);
                compressedSize = size;
            }
            else
            {
                using (var zlib = new ZLibStream(_testStream, CompressionLevel.Optimal, leaveOpen: true))
                {
                    zlib.Write(inputBytes, 0, inputBytes.Length);
                }
                compressedSize = (uint)_testStream.Length - dataOffset;
            }

            SgaFile file = new (testFile.Name, testFile.StorageType, dataOffset, compressedSize, size, testFile.Modified, crc, drive, parent);
            if(parent == null)
                drive._entries.Add(testFile.Name, file);
            else
                parent._entries.Add(testFile.Name, file);
        }
    }

    public static void Assert_Drive(TestDrive expectedDrive, SgaDrive actualDrive, SgaArchive parentArchive)
    {
        Assert.Equal(expectedDrive.Alias, actualDrive.Alias);
        Assert.Equal(expectedDrive.Name, actualDrive.Name);
        Assert.Same(parentArchive, actualDrive.Archive);

        // Folders
        var actualFolders = actualDrive.Contents.OfType<SgaFolder>().OrderBy(f => f.Name).ToList();
        var expectedFolders = expectedDrive.RootFolder.Folders.OrderBy(f => f.Name).ToList();

        Assert.Equal(expectedFolders.Count, actualFolders.Count);

        for (int i = 0; i < actualFolders.Count; i++)
        {
            Assert_Folder(expectedFolders[i], actualFolders[i], null, actualDrive);
        }

        // Files
        var actualFiles = actualDrive.Contents.OfType<SgaFile>().OrderBy(f => f.Name).ToList();
        var expectedFiles = expectedDrive.RootFolder.Files.OrderBy(f => f.Name).ToList();

        Assert.Equal(expectedFiles.Count, actualFiles.Count);

        for (int i = 0; i < actualFiles.Count; i++)
        {
            Assert_File(expectedFiles[i], actualFiles[i], null, actualDrive);
        }

    }

    public static void Assert_Folder(TestFolder expectedFolder, SgaFolder actualFolder, SgaFolder? expectedParent, SgaDrive expectedDrive )
    {
        Assert.Equal(expectedFolder.Name, actualFolder.Name);
        Assert.Same(expectedParent, actualFolder.Parent);
        Assert.Same(expectedDrive, actualFolder.Drive);

        // Folders
        var actualFolders = actualFolder.Contents.OfType<SgaFolder>().OrderBy(f => f.Name).ToList();
        var expectedFolders = expectedFolder.Folders.OrderBy(f => f.Name).ToList();

        Assert.Equal(expectedFolders.Count, actualFolders.Count);

        for (int i = 0; i < actualFolders.Count; i++)
        {
            Assert_Folder(expectedFolders[i], actualFolders[i], actualFolder, expectedDrive);
        }

        // Files
        var actualFiles = actualFolder.Contents.OfType<SgaFile>().OrderBy(f => f.Name).ToList();
        var expectedFiles = expectedFolder.Files.OrderBy(f => f.Name).ToList();

        Assert.Equal(expectedFiles.Count, actualFiles.Count);

        for (int i = 0; i < actualFiles.Count; i++)
        {
            Assert_File(expectedFiles[i], actualFiles[i], actualFolder, expectedDrive);
        }
    }

    public static void Assert_File(TestFile expectedFile, SgaFile actualFile, SgaFolder? expectedParent, SgaDrive expectedDrive)
    {
        byte[] expectedBytes = Encoding.UTF8.GetBytes(expectedFile.FileContent);
        uint expectedSize = (uint)expectedBytes.Length;
        uint expectedCompressedSize = expectedSize;

        if(expectedFile.StorageType != StorageType.Uncompress)
        {
            var tempStream = new MemoryStream();
            using (var zlib = new ZLibStream(tempStream, CompressionLevel.Optimal, leaveOpen: true))
            {
                zlib.Write(expectedBytes, 0, expectedBytes.Length);
            }
            expectedCompressedSize = (uint)tempStream.Length;
        }

        Assert.Equal(expectedFile.Name, actualFile.Name);
        Assert.Equal(expectedFile.StorageType, actualFile.StorageType);
        Assert.Equal(expectedFile.Modified, actualFile.Modified);
        Assert.Equal(expectedSize, actualFile.Size);
        Assert.Equal(expectedCompressedSize, actualFile.CompressedSize);

        Assert.Same(expectedParent, actualFile.Parent);
        Assert.Same(expectedDrive, actualFile.Drive);

        Stream stream = actualFile.GetResultStream();
        byte[] buffer = new byte [actualFile.Size];

        if(stream != null)
        {
            if(actualFile.StorageType != StorageType.Uncompress)
            {
                using var zLib = new ZLibStream(stream, CompressionMode.Decompress);
                zLib.ReadExactly(buffer);
            }
            else
                stream.ReadExactly(buffer);

        }

        uint crc = Crc32.HashToUInt32(buffer);
        string actualContents = Encoding.Default.GetString(buffer);

        Assert.Equal(crc, actualFile.Crc ?? 0);
        Assert.Equal(expectedFile.FileContent, actualContents);
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
    public DateTimeOffset Modified {get; set;}
    public required string FileContent {get; set;}
}