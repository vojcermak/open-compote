using System.Text;
using Xunit.Sdk;


namespace OpenCompote.SGA.Tests;

public class SgaArchiveTest
{
    [Fact]
    public void Constructor_WithNullStream_ThrowsArgumentException()
    {
        // Need to test what happens when stream is null.
        #pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        Assert.Throws<ArgumentNullException>(() => new SgaArchive(null, SgaMode.Read));
        #pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
    }

    [Fact]
    public void Constructor_WithIncorrectStream_ThrowsArgumentException()
    {
        MemoryStream readOnlyStream = new MemoryStream([], false);
        MemoryStream unreadableStream = new MemoryStream();
        unreadableStream.Dispose();

        Assert.Throws<ArgumentException>(() => new SgaArchive(readOnlyStream, SgaMode.Create, SgaVersion.V2, true));
        Assert.Throws<ArgumentException>(() => new SgaArchive(unreadableStream, SgaMode.Read, true));
        Assert.Throws<ArgumentException>(() => new SgaArchive(new MemoryStream(), SgaMode.Create, null));
    }

    [Fact]
    public void test()
    {
        TestDrive item = new TestDrive
        {
            Name = "",
            Alias = "",
            RootFolder = new TestFolder
            {
                Name = "",
                Folders = [],
                Files = [new TestFile{
                    Name = "file1",
                    StorageType = StorageType.Uncompress,
                    FileContent = "Hello world file"
                },
                new TestFile{
                    Name = "file2",
                    StorageType = StorageType.StreamCompress,
                    FileContent = "File 2 contents"
                }]
            } 
        };
        var parser = new MockParser([item],[]);

        var stream = new MemoryStream();
        using (var Archive =  new SgaArchive(stream, SgaMode.Read, SgaVersion.V2, parser))
        {
            SgaFile file1 = (SgaFile)Archive.Drives[0].RootFolder.Contents[0];
            SgaFile file2 = (SgaFile)Archive.Drives[0].RootFolder.Contents[1];

            var reader = new BinaryReader(file1.Open());
            Console.WriteLine(reader.ReadString());

            using var openFile = file2.Open();
            var buffer = new byte [file2.Size];
            openFile.ReadExactly(buffer);
            Console.WriteLine(Encoding.Default.GetString(buffer));
            
        }
    }
}


