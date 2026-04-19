using System.IO;
using System.Text;
using Xunit;

namespace OpenCompote.SGA.Tests.Parsers;

public class SgaV2ParserTest
{
    [Fact]
    public void V2Parser_RoundTrip_UncompressedFile()
    {
        // Create an archive with a simple uncompressed file
        var stream = new MemoryStream();
        using (var archive = new SgaArchive(stream, SgaMode.Create, SgaVersion.V2, leaveOpen: true))
        {
            archive.ArchiveName = "TestArchive";
            var drive = archive.AddDrive("TestDrive", "TestDrive");
            var file = drive.RootFolder.AddFile("test.txt", StorageType.Uncompress);
            using var writer = new StreamWriter(file.Open());
            writer.Write("Hello, World!");
        }

        // Now read it back
        stream.Position = 0;
        using (var readArchive = new SgaArchive(stream, SgaMode.Read))
        {
            Assert.Equal(SgaVersion.V2, readArchive.Version);
            Assert.Equal("TestArchive", readArchive.ArchiveName);
            Assert.Single(readArchive.Drives);
            var drive = readArchive.Drives[0];
            Assert.Equal("TestDrive", drive.Name);
            Assert.Equal("TestDrive", drive.Alias);
            Assert.Single(drive.RootFolder.Contents);
            var file = drive.RootFolder.Contents[0] as SgaFile;
            Assert.NotNull(file);
            Assert.Equal("test.txt", file.Name);
            Assert.Equal(StorageType.Uncompress, file.StorageType);
            using var reader = new StreamReader(file.Open());
            Assert.Equal("Hello, World!", reader.ReadToEnd());
        }
    }

    [Fact]
    public void V2Parser_RoundTrip_CompressedFile()
    {
        // Create an archive with a compressed file
        var stream = new MemoryStream();
        using (var archive = new SgaArchive(stream, SgaMode.Create, SgaVersion.V2, leaveOpen: true))
        {
            archive.ArchiveName = "TestArchive";
            var drive = archive.AddDrive("TestDrive", "TestDrive");
            var file = drive.RootFolder.AddFile("test.txt", StorageType.StreamCompress);
            using var writer = new StreamWriter(file.Open());
            writer.Write("This is a longer text to compress effectively. " + string.Join("", Enumerable.Repeat("Compression test data. ", 100)));
        }

        // Read it back
        stream.Position = 0;
        using (var readArchive = new SgaArchive(stream, SgaMode.Read))
        {
            Assert.Equal(SgaVersion.V2, readArchive.Version);
            var file = readArchive.Drives[0].RootFolder.Contents[0] as SgaFile;
            Assert.NotNull(file);
            Assert.Equal(StorageType.StreamCompress, file.StorageType);
            using var reader = new StreamReader(file.Open());
            var content = reader.ReadToEnd();
            Assert.StartsWith("This is a longer text", content);
            Assert.Contains("Compression test data.", content);
        }
    }

    [Fact]
    public void V2Parser_InvalidMagic_ThrowsException()
    {
        var stream = new MemoryStream();
        var writer = new BinaryWriter(stream);
        writer.Write(Encoding.ASCII.GetBytes("INVALID_")); // Wrong magic
        writer.Write(2); // Version
        stream.Position = 0;

        Assert.Throws<Exception>(() => new SgaArchive(stream, SgaMode.Read));
    }

    [Fact]
    public void V2Parser_InvalidVersion_ThrowsException()
    {
        var stream = new MemoryStream();
        var writer = new BinaryWriter(stream);
        writer.Write(Encoding.ASCII.GetBytes("_ARCHIVE"));
        writer.Write(999); // Invalid version
        stream.Position = 0;

        Assert.Throws<ArgumentException>(() => new SgaArchive(stream, SgaMode.Read));
    }

    //OK
    // 1. - Parser can open and read valid uncompress file
    // 2. - Parser can open and read valid compress file
    // 2. - Parser can open and read archive with only empty folders
    // 3. - Parser can open and read archive with only empty drives
    // 4. - Parser can open and read archive with no drives
    // 5. - Parser can open and read archive with multiple drives witch multiple nested folder and files
    
    //NOK
    // 1. Magic word is incorrect
    // 2. Version is incorrect
    // 3. Version is unsupported
    // 4. FileHash is malformed
    // 5. TOCHash is malformed
    // 6. TOC Size is incorrect
    // 7. DataOffset is incorrect
    // 8. DriveOffset and count does not match
    // 9. FolderOffset and count does not match
    // 10. FileOffset and count does not match
    // 11. Name offset is incorrect
    // 12. FirstFile/LastFile in drive does overlap
    // 13. FirstFile/LastFile in folder does overlap
    // 14. NameOffset is out of bounds of the name array
    // 15. StorageFlag is malformed
    // 16. StorageFlag is compress, but file is not compressed
    // 17. DataOffset points outside of a file
    // 18. Names in invalid text format
    // 19. Name does not have a null terminator
    // 20. Decompressed file size doesn't match actual decompressed output
    // 21. Root folder points to non-existing/invalid folder

    //Writer
    // 1. Opposite for OK 5 - writer can write valid archive
    // 2. Save valid empty archive
    // 3. Save valid archive with only empty drives
    // 4. Save valid archive with only empty folders
}