using System.Security.Cryptography.X509Certificates;

namespace OpenCompote.SGA.Tests.Models;

public class SgaFileTests
{

    [Fact]
    public void File_ThrowsWhenReadonly()
    {
        using var archive = MockParser.CreateArchive(SgaMode.Read, new("archive", [
            new TestDrive
            {
                Name = "Drive",
                Alias = "alias",
                Folders = [
                    new TestFolder
                    {
                        Name = "Folder",
                        Files = [new TestFile {Name = "file1.txt"}]
                    }
                ]
            }
        ], []));

        var drive = Assert.IsType<SgaDrive>(archive.GetDrive("Drive"));
        var file = Assert.IsType<SgaFile>(drive.Contents.Single());

        Assert.Throws<InvalidOperationException>(() => file.StorageType = StorageType.Uncompress);
        Assert.Throws<InvalidOperationException>(() => file.Modified = DateTimeOffset.Now);
        Assert.Throws<InvalidOperationException>(file.Delete);
    }

	[Fact]
	public void File_ThrowsWhenDisposed()
	{
		using var archive = MockParser.CreateArchive(SgaMode.Read, new("archive", [
            new TestDrive
            {
                Name = "Drive",
                Alias = "alias",
                Folders = [
                    new TestFolder
                    {
                        Name = "Folder",
                        Files = [new TestFile {Name = "file1.txt"}]
                    }
                ]
            }
        ], []));

        var drive = Assert.IsType<SgaDrive>(archive.GetDrive("Drive"));
        var file = Assert.IsType<SgaFile>(drive.Contents.Single());

        Assert.Throws<ObjectDisposedException>(() => file.StorageType);
        Assert.Throws<ObjectDisposedException>(() => file.CompressedSize);
        Assert.Throws<ObjectDisposedException>(() => file.Size);
        Assert.Throws<ObjectDisposedException>(() => file.Modified);
        Assert.Throws<ObjectDisposedException>(() => file.Crc);
        Assert.Throws<ObjectDisposedException>(file.Open);
        Assert.Throws<ObjectDisposedException>(file.Delete);
        Assert.Throws<ObjectDisposedException>(() => file.ExtractToFile("output.txt"));
	}

    // ==================== StorageType Tests ====================

	[Theory]
    [InlineData(StorageType.Uncompress, StorageType.Uncompress)]
    [InlineData(StorageType.Uncompress, StorageType.StreamCompress)]
    [InlineData(StorageType.StreamCompress, StorageType.Uncompress)]
    [InlineData(StorageType.StreamCompress, StorageType.BufferCompress)]
	public void File_StorageType_ConvertsExistingContent(StorageType sourceType, StorageType newType)
	{
		string contents = "This is a small file contents.";
		using var archive = MockParser.CreateArchive(SgaMode.Write, new("archive", [
            new TestDrive
            {
                Name = "Drive",
                Alias = "alias",
                Files = [new TestFile {
					Name = "file1.txt",
					StorageType = sourceType,
					FileContent = contents
				}]
            }
        ], [
			new TestDrive
            {
                Name = "Drive",
                Alias = "alias",
                Files = [new TestFile {
					Name = "file1.txt",
					StorageType = newType,
					FileContent = contents
				}]
            }
		]));

        var drive = Assert.IsType<SgaDrive>(archive.GetDrive("Drive"));
        var file = Assert.IsType<SgaFile>(drive.Contents.Single());

		file.StorageType = newType;
		Assert.Equal(newType, file.StorageType);
		
		using var stream = file.Open();
		using var reader = new StreamReader(stream);
		Assert.Equal(contents, reader.ReadToEnd());
	}
	
	[Theory]
	[InlineData(null, typeof(InvalidOperationException))]
	[InlineData((StorageType)69, typeof(ArgumentOutOfRangeException))]
	public void File_StorageType_ConvertToInvalidThrows(StorageType? invalidType, Type exception)
	{
        using var archive = MockParser.CreateArchive(SgaMode.Write, new("archive", [
            new TestDrive
            {
                Name = "Drive",
                Alias = "alias",
                Files = [new TestFile {Name = "file1.txt"}]
            }
        ], [
			new TestDrive
            {
                Name = "Drive",
                Alias = "alias",
                Files = [new TestFile {Name = "file1.txt"}]
            }
		]));

		var drive = Assert.IsType<SgaDrive>(archive.GetDrive("Drive"));
        var file = Assert.IsType<SgaFile>(drive.Contents.Single());

		Assert.Throws(exception, ()=> file.StorageType = (StorageType)invalidType!);
	}

    // ==================== Open Tests ====================

	[Theory]
    [InlineData(StorageType.Uncompress)]
	[InlineData(StorageType.StreamCompress)]
	[InlineData(StorageType.BufferCompress)]
	public void File_Open_ReadsContent(StorageType storageType)
	{
        string contents = "This is a file contents.";
		using var archive = MockParser.CreateArchive(SgaMode.Read, new("archive", [
            new TestDrive
            {
                Name = "Drive",
                Alias = "alias",
                Files = [new TestFile {Name = "file1.txt", StorageType = storageType, FileContent = contents}]
            }
        ], []));

        var drive = Assert.IsType<SgaDrive>(archive.GetDrive("Drive"));
        var file = Assert.IsType<SgaFile>(drive.Contents.Single());

		using var stream = file.Open();
		using var reader = new StreamReader(stream);

		Assert.Equal(contents, reader.ReadToEnd());
	}

	[Fact]
	public void File_OpenWrite_UpdatesMetadataWhenClosed()
	{
		var fixture = CreateArchive(SgaMode.Write, StorageType.Uncompress, "updated content");
		using var archive = fixture.Archive;

		using (var stream = fixture.File.Open())
		using (var writer = new StreamWriter(stream))
		{
			writer.Write("updated content");
		}

		Assert.Equal((uint)15, fixture.File.Size);
		Assert.Equal((uint)15, fixture.File.CompressedSize);
		Assert.NotNull(fixture.File.Crc);
		Assert.Equal(MockParser.FixedTime, fixture.File.Modified);
	}

	[Fact]
	public void File_OpenWrite_RejectsMultipleOpenStreams()
	{
		var fixture = CreateArchive(SgaMode.Write, StorageType.Uncompress, "content");
		using var archive = fixture.Archive;
		using var firstStream = fixture.File.Open();

		Assert.Throws<IOException>(() => fixture.File.Open());
	}

    // ==================== ExtractToFile Tests ====================

	[Fact]
	public void File_ExtractToFile_WritesDecompressedContent()
	{
		var fixture = CreateArchive(SgaMode.Read, StorageType.StreamCompress, "extract me");
		using var archive = fixture.Archive;
		string destination = Path.Combine(Path.GetTempPath(), $"open-compote-{Guid.NewGuid():N}");

		try
		{
			fixture.File.ExtractToFile(destination);

			Assert.Equal("extract me", File.ReadAllText(Path.Combine(destination, "file.txt")));
		}
		finally
		{
			if (Directory.Exists(destination))
				Directory.Delete(destination, true);
		}
	}

	[Fact]
	public void File_ExtractToFile_ThrowsWhenDestinationFileExistsWithoutOverwrite()
	{
		var fixture = CreateArchive(SgaMode.Read, StorageType.Uncompress, "content");
		using var archive = fixture.Archive;
		string destination = Path.Combine(Path.GetTempPath(), $"open-compote-{Guid.NewGuid():N}");

		try
		{
			Directory.CreateDirectory(destination);
			File.WriteAllText(Path.Combine(destination, "file.txt"), "old content");

			Assert.Throws<IOException>(() => fixture.File.ExtractToFile(destination));
		}
		finally
		{
			if (Directory.Exists(destination))
				Directory.Delete(destination, true);
		}
	}

    // ==================== Delete Tests ====================

	[Fact]
	public void File_Delete_RemovesFileAndInvalidatesIt()
	{
		var fixture = CreateArchive(
			SgaMode.Write,
			StorageType.Uncompress,
			"content",
			StorageType.Uncompress,
			expectedFileCount: 0);
		using var archive = fixture.Archive;
		var folder = Assert.IsType<SgaFolder>(fixture.File.Parent);

		fixture.File.Delete();

		Assert.Empty(folder.Contents);
		Assert.Null(fixture.File.Parent);
		Assert.Null(fixture.File.Drive);
		Assert.Throws<ObjectDisposedException>(() => fixture.File.Name);
		Assert.Throws<ObjectDisposedException>(() => fixture.File.Open());
	}

	private static (SgaArchive Archive, SgaFile File) CreateArchive(
		SgaMode mode,
		StorageType storageType,
		string content,
		StorageType? expectedStorageType = null,
		int expectedFileCount = 1)
	{
		List<TestFile> expectedFiles = expectedFileCount == 0
			? []
			: [new TestFile
			{
				Name = "file.txt",
				StorageType = expectedStorageType ?? storageType,
				Modified = MockParser.FixedTime,
				FileContent = content
			}];

		var archive = MockParser.CreateArchive(mode, new("archive", [
			new TestDrive
			{
				Name = "Drive",
				Alias = "alias",
				Folders = [new TestFolder
				{
					Name = "Folder",
					Files = [new TestFile
					{
						Name = "file.txt",
						StorageType = storageType,
						Modified = MockParser.FixedTime,
						FileContent = content
					}]
				}]
			}
		], [new TestDrive
		{
			Name = "Drive",
			Alias = "alias",
			Folders = [new TestFolder
			{
				Name = "Folder",
				Files = expectedFiles
			}]
		}]));

		var drive = Assert.IsType<SgaDrive>(archive.GetDrive("Drive"));
		var folder = Assert.IsType<SgaFolder>(drive.Contents.Single());
		var file = Assert.IsType<SgaFile>(folder.Contents.Single());
		return (archive, file);
	}
}

