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
	public void File_Open_ReadOnlyContent(StorageType storageType)
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
		Assert.False(stream.CanWrite);
	}

	[Theory]
    [InlineData(StorageType.Uncompress)]
	[InlineData(StorageType.StreamCompress)]
	[InlineData(StorageType.BufferCompress)]
	public void File_Open_ReadWriteContents(StorageType storageType)
	{
        string contents = "This is a file contents.";
		using var archive = MockParser.CreateArchive(SgaMode.Write, new("archive", [
            new TestDrive
            {
                Name = "Drive",
                Alias = "alias",
                Files = [new TestFile {Name = "file1.txt", StorageType = storageType, FileContent = contents}]
            }
        ], [
            new TestDrive
            {
                Name = "Drive",
                Alias = "alias",
                Files = [new TestFile {Name = "file1.txt", StorageType = storageType, FileContent = contents}]
            }
        ]));

        var drive = Assert.IsType<SgaDrive>(archive.GetDrive("Drive"));
        var file = Assert.IsType<SgaFile>(drive.Contents.Single());

		using var stream = file.Open();
		using var reader = new StreamReader(stream);

		Assert.Equal(contents, reader.ReadToEnd());
		Assert.True(stream.CanWrite);
	}

	[Fact]
	public void File_OpenWrite_UpdatesMetadataWhenClosed()
	{
		string contents = "This is a new content.";
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
                Files = [new TestFile {Name = "file1.txt", FileContent = contents}]
            }
		]));

		var drive = Assert.IsType<SgaDrive>(archive.GetDrive("Drive"));
        var file = Assert.IsType<SgaFile>(drive.Contents.Single());

		using (var stream = file.Open())
		using (var writer = new StreamWriter(stream))
		{
			writer.Write(contents);
		}

		Assert.Equal((uint)22, file.Size);
		Assert.Equal((uint)22, file.CompressedSize);
		Assert.NotNull(file.Crc);
		Assert.Equal(MockParser.FixedTime, file.Modified);
	}

	[Fact]
	public void File_OpenWrite_RejectsMultipleOpenStreams()
	{
		string contents = "This is a new content.";
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
                Files = [new TestFile {Name = "file1.txt", FileContent = contents}]
            }
		]));

		var drive = Assert.IsType<SgaDrive>(archive.GetDrive("Drive"));
        var file = Assert.IsType<SgaFile>(drive.Contents.Single());

		using var stream = file.Open();

		Assert.Throws<IOException>(() => file.Open());
		Assert.Throws<IOException>(() => file.Delete());
		Assert.Throws<IOException>(() => file.StorageType = StorageType.StreamCompress);
	}

    // ==================== ExtractToFile Tests ====================

	[Fact]
	public void File_ExtractToFile_WritesDecompressedContent()
	{
		string contents = "This is a file contents.";
		using var archive = MockParser.CreateArchive(SgaMode.Read, new("archive", [
            new TestDrive
            {
                Name = "Drive",
                Alias = "alias",
                Files = [new TestFile {Name = "file1.txt", FileContent = contents}]
            }
        ], []));

		var drive = Assert.IsType<SgaDrive>(archive.GetDrive("Drive"));
        var file = Assert.IsType<SgaFile>(drive.Contents.Single());

		string destination = Path.Combine(Path.GetTempPath(), $"open-compote-{Guid.NewGuid():N}");

		try
		{
			file.ExtractToFile(destination);

			Assert.Equal(contents, File.ReadAllText(Path.Combine(destination, "file.txt")));
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
		string contents = "This is a file contents.";
		using var archive = MockParser.CreateArchive(SgaMode.Read, new("archive", [
            new TestDrive
            {
                Name = "Drive",
                Alias = "alias",
                Files = [new TestFile {Name = "file1.txt", FileContent = contents}]
            }
        ], []));

		string destination = Path.Combine(Path.GetTempPath(), $"open-compote-{Guid.NewGuid():N}");

		var drive = Assert.IsType<SgaDrive>(archive.GetDrive("Drive"));
        var file = Assert.IsType<SgaFile>(drive.Contents.Single());

		try
		{
			Directory.CreateDirectory(destination);
			File.WriteAllText(Path.Combine(destination, "file.txt"), "old content");

			Assert.Throws<IOException>(() => file.ExtractToFile(destination));
		}
		finally
		{
			if (Directory.Exists(destination))
				Directory.Delete(destination, true);
		}
	}

    // ==================== Delete Tests ====================

	[Fact]
	public void File_Delete_FromFolderRemovesFileAndInvalidatesIt()
	{
		string contents = "This is a small file contents.";
		using var archive = MockParser.CreateArchive(SgaMode.Write, new("archive", [
            new TestDrive
            {
                Name = "Drive",
                Alias = "alias",
				Folders = [
					new TestFolder {
						Name = "folder",
						Files = [
							new TestFile {
								Name = "file1.txt",
								FileContent = contents
							}
						]
					}
				],
            }
        ], [
			new TestDrive
            {
                Name = "Drive",
                Alias = "alias",
				Folders = [new TestFolder {Name = "folder"}]
            }
		]));

        var drive = Assert.IsType<SgaDrive>(archive.GetDrive("Drive"));
		var folder = Assert.IsType<SgaFolder>(drive.Contents.Single());
        var file = Assert.IsType<SgaFile>(folder.Contents.Single());

		file.Delete();

		Assert.Empty(folder.Contents);
		Assert.Null(file.Parent);
		Assert.Null(file.Drive);
		Assert.Throws<ObjectDisposedException>(() => file.Name);
		Assert.Throws<ObjectDisposedException>(() => file.Path);
		Assert.Throws<ObjectDisposedException>(() => file.StorageType);
		Assert.Throws<ObjectDisposedException>(() => file.Size);
		Assert.Throws<ObjectDisposedException>(() => file.CompressedSize);
		Assert.Throws<ObjectDisposedException>(() => file.Modified);
		Assert.Throws<ObjectDisposedException>(() => file.Crc);
		Assert.Throws<ObjectDisposedException>(() => file.Open());
		Assert.Throws<ObjectDisposedException>(() => file.Delete());
	}

		[Fact]
	public void File_Delete_FromDriveRemovesFileAndInvalidatesIt()
	{
		string contents = "This is a small file contents.";
		using var archive = MockParser.CreateArchive(SgaMode.Write, new("archive", [
            new TestDrive
            {
                Name = "Drive",
                Alias = "alias",
                Files = [new TestFile {
					Name = "file1.txt",
					FileContent = contents
				}]
            }
        ], [
			new TestDrive
            {
                Name = "Drive",
                Alias = "alias"
            }
		]));

        var drive = Assert.IsType<SgaDrive>(archive.GetDrive("Drive"));
        var file = Assert.IsType<SgaFile>(drive.Contents.Single());

		file.Delete();

		Assert.Empty(drive.Contents);
		Assert.Null(file.Parent);
		Assert.Null(file.Drive);
		Assert.Throws<ObjectDisposedException>(() => file.Name);
		Assert.Throws<ObjectDisposedException>(() => file.Path);
		Assert.Throws<ObjectDisposedException>(() => file.StorageType);
		Assert.Throws<ObjectDisposedException>(() => file.Size);
		Assert.Throws<ObjectDisposedException>(() => file.CompressedSize);
		Assert.Throws<ObjectDisposedException>(() => file.Modified);
		Assert.Throws<ObjectDisposedException>(() => file.Crc);
		Assert.Throws<ObjectDisposedException>(() => file.Open());
		Assert.Throws<ObjectDisposedException>(() => file.Delete());
	}
}