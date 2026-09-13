namespace OpenCompote.SGA.Tests.Models;

public class SgaDriveTests
{
    [Fact]
	public void Drive_ThrowsWhenReadonly()
	{
		using var archive = MockParser.CreateArchive(SgaMode.Read, new("archive", [
			new TestDrive { Alias = "alias", Name = "Drive" }
		], []));

		var drive = Assert.IsType<SgaDrive>(archive.GetDrive("Drive"));

		Assert.Throws<InvalidOperationException>(() => drive.Alias = "newAlias");
		Assert.Throws<InvalidOperationException>(() => drive.Name = "NewDrive");
		Assert.Throws<InvalidOperationException>(() => drive.AddFolder("Folder"));
		Assert.Throws<InvalidOperationException>(() => drive.AddFile("file.txt", StorageType.Uncompress));
		Assert.Throws<InvalidOperationException>(() => drive.Delete());
	}

	[Fact]
	public void Drive_ThrowsWhenDisposed()
	{
		var archive = MockParser.CreateArchive(SgaMode.Write, new("archive", [
			new TestDrive { Alias = "alias", Name = "Drive" }
		], [
			new TestDrive { Alias = "alias", Name = "Drive" }
		]));
		var drive = Assert.IsType<SgaDrive>(archive.GetDrive("Drive"));

		archive.Dispose();

		Assert.Throws<ObjectDisposedException>(() => drive.Alias);
		Assert.Throws<ObjectDisposedException>(() => drive.Name);
		Assert.Throws<ObjectDisposedException>(() => drive.Contents);
        Assert.Throws<ObjectDisposedException>(() => drive.Archive);
		Assert.Throws<ObjectDisposedException>(() => drive.AddFolder("Folder"));
		Assert.Throws<ObjectDisposedException>(() => drive.AddFile("file.txt", StorageType.Uncompress));
		Assert.Throws<ObjectDisposedException>(() => drive.GetEntry("Folder"));
		Assert.Throws<ObjectDisposedException>(() => drive.Delete());
	}

	[Fact]
	public void Drive_ExposesMetadataAndContents()
	{
		using var archive = MockParser.CreateArchive(SgaMode.Read, new("archive", [
			new TestDrive
			{
				Alias = "alias",
				Name = "Drive",
				Folders = [new TestFolder { Name = "Folder" }],
				Files = [new TestFile { Name = "file.txt" }]
			}
		], []));

		var drive = Assert.IsType<SgaDrive>(archive.GetDrive("Drive"));

		Assert.Equal("alias", drive.Alias);
		Assert.Equal("Drive", drive.Name);
		Assert.Same(archive, drive.Archive);
		Assert.Equal(2, drive.Contents.Count);
		Assert.Single(drive.Contents.OfType<SgaFolder>());
		Assert.Single(drive.Contents.OfType<SgaFile>());
	}

    // ==================== AddFolder Tests ====================

	[Fact]
	public void Folder_AddFolder_CreatesSubfolderWithCorrectPath()
	{
		using var archive = MockParser.CreateArchive(SgaMode.Write, new("archive", [
			new TestDrive { Alias = "alias", Name = "Drive" }
		], [
			new TestDrive
			{
				Alias = "alias",
				Name = "Drive",
				Folders = [new TestFolder { Name = "Folder" }]
			}
		]));

		var drive = Assert.IsType<SgaDrive>(archive.GetDrive("Drive"));
		var folder = drive.AddFolder("Folder");

		Assert.NotNull(folder);
        Assert.Equal("Folder", folder.Name);
        Assert.Equal("Folder", folder.Path);
        Assert.Null(folder.Parent);
        Assert.Single(drive.Contents);
        Assert.IsType<SgaFolder>(drive.Contents.Single());
        Assert.Equal(folder, drive.Contents.Single());
	}

	[Theory]
	[InlineData(null, typeof(ArgumentNullException))]
    [InlineData("", typeof(ArgumentException))]
    [InlineData("folder", typeof(ArgumentException))]
    [InlineData(" folder ", typeof(ArgumentException))]
	public void Drive_AddFolder_RejectsInvalidNames(string? name, Type exception)
	{
		using var archive = MockParser.CreateArchive(SgaMode.Write, new("archive", [
			new TestDrive
			{
				Alias = "alias",
				Name = "Drive",
				Folders = [new TestFolder { Name = "Folder" }]
			}
		], [
			new TestDrive
			{
				Alias = "alias",
				Name = "Drive",
				Folders = [new TestFolder { Name = "Folder" }]
			}
		]));

		var drive = Assert.IsType<SgaDrive>(archive.GetDrive("Drive"));

		Assert.Throws(exception, () => drive.AddFolder(name!));
	}

    // ==================== AddFile Tests ====================

    [Fact]
	public void Folder_AddFile_CreatesFileWithCorrectPath()
    {
        using var archive = MockParser.CreateArchive(SgaMode.Write, new("archive", [
            new TestDrive { Alias = "alias", Name = "Drive" }
        ], [
            new TestDrive{
                Alias = "alias",
                Name = "Drive",
                Files = [new TestFile { Name = "Folder" }]
            }
        ]));

        var drive = Assert.IsType<SgaDrive>(archive.GetDrive("Drive"));
        var file = drive.AddFile("Folder", StorageType.Uncompress);

        Assert.NotNull(file);
        Assert.Equal("test.txt", file.Name);
        Assert.Equal("test.txt", file.Path);
        Assert.Null(file.Parent);
        Assert.Same(drive, file.Drive);
        Assert.Single(drive.Contents);
        Assert.IsType<SgaFile>(drive.Contents.Single());
        Assert.Same(file, drive.Contents.Single());
    }

	[Theory]
	[InlineData(null, StorageType.Uncompress, typeof(ArgumentNullException))]
    [InlineData("", StorageType.Uncompress, typeof(ArgumentException))]
    [InlineData("file.txt", StorageType.Uncompress, typeof(ArgumentException))]
    [InlineData(" File.txt ", StorageType.Uncompress, typeof(ArgumentException))]
    [InlineData("newFile.txt", null, typeof(ArgumentOutOfRangeException))]
    [InlineData("newFile.txt", (StorageType)69, typeof(ArgumentOutOfRangeException))]
	public void Drive_AddFile_RejectsInvalidOrDuplicateNames(string? name, StorageType? type, Type exception)
	{
		using var archive = MockParser.CreateArchive(SgaMode.Write, new("archive", [
			new TestDrive
			{
				Alias = "alias",
				Name = "Drive",
				Files = [new TestFile { Name = "file.txt" }]
			}
		], [
			new TestDrive
			{
				Alias = "alias",
				Name = "Drive",
				Files = [new TestFile { Name = "file.txt" }]
			}
		]));

		var drive = Assert.IsType<SgaDrive>(archive.GetDrive("Drive"));

		Assert.Throws(exception, () => drive.AddFile(name!, (StorageType)type!));
	}

    // ==================== Delete Tests ====================

    [Fact]
	public void Drive_Delete_RemovesDriveAndInvalidatesIt()
	{
		using var archive = MockParser.CreateArchive(SgaMode.Write, new("archive", [
			new TestDrive
			{
				Alias = "alias",
				Name = "Drive",
				Folders = [new TestFolder { Name = "Folder" }],
				Files = [new TestFile { Name = "file.txt" }]
			}
		], []));

		var drive = Assert.IsType<SgaDrive>(archive.GetDrive("Drive"));

		drive.Delete();

		Assert.Empty(archive.Drives);
		Assert.Null(drive.Archive);
        Assert.Throws<ObjectDisposedException>(() => drive.Name);
        Assert.Throws<ObjectDisposedException>(() => drive.Alias);
		Assert.Throws<ObjectDisposedException>(() => drive.Contents);
        Assert.Throws<ObjectDisposedException>(() => drive.AddFolder("newFile"));
        Assert.Throws<ObjectDisposedException>(() => drive.AddFile("newFile", StorageType.Uncompress));
        Assert.Throws<ObjectDisposedException>(() => drive.Delete());
        Assert.Throws<ObjectDisposedException>(() => drive.GetEntry("Folder"));
	}


    // ==================== GetEntry Tests ====================

	[Theory]
    [InlineData("subfolder", typeof(SgaFolder), "subfolder")]            //Get direct child folder
    [InlineData("file.txt", typeof(SgaFile), "file.txt"),]               //Get direct child file
    [InlineData("subfolder/folder1", typeof(SgaFolder), "folder1")]      //Get nested folder
    [InlineData("subfolder/file2.txt", typeof(SgaFile), "file2.txt")]    //Get nested file
    [InlineData("nonexistingFile", null)]                                //Get nonexisting entry
    [InlineData("subfolder/noFile.txt", null)]                           //Get nonexisting nested entry
    [InlineData("\\subfolder\\file2.txt", typeof(SgaFile), "file2.txt")] //Get sub file with wrong separators
    [InlineData("./subfolder", null)]                                    //Get the current folder using ./ folder
	public void Drive_GetEntry_FindsExistingEntry(string path, Type? OutputType, string expectedName = "")
	{
		using var archive = MockParser.CreateArchive(SgaMode.Read, new("archive", [
			new TestDrive
			{
				Alias = "alias",
				Name = "Drive",
				Folders = [new TestFolder { 
                    Name = "subfolder",
                    Files = [new TestFile { Name = "file2.txt" }],
                    Folders = [new TestFolder { Name = "folder1"}] 
                }],
                Files = [new TestFile { Name = "file.txt" }]
			}
		], []));

		var drive = Assert.IsType<SgaDrive>(archive.GetDrive("Drive"));

        var result = drive.GetEntry(path);

        if (OutputType == null)
            Assert.Null(result);
        else
        {
            Assert.IsType(OutputType, result);
            Assert.Equal(expectedName, result.Name);
        }
	}

    [Theory]
    [InlineData("", typeof(ArgumentException))]       // Empty string is not allowed
    [InlineData(" ", typeof(ArgumentException))]      // Empty string is not allowed
    [InlineData(null, typeof(ArgumentNullException))] // Null string is not allowed
    [InlineData("/", typeof(ArgumentException))]      // Empty string is not allowed
    [InlineData(" / ", typeof(ArgumentException))]    // Empty string is not allowed
    public void Drive_GetEntry_InvalidInputThrowsException(string? path, Type exceptionType)
    {
        using var archive = MockParser.CreateArchive(SgaMode.Read, new("archive", [
            new TestDrive
            {
                Name = "Drive",
                Alias = "alias",
                Folders = [
                    new TestFolder { 
                        Name = "subfolder",
                        Files = [new TestFile { Name = "file2.txt" }],
                        Folders = [new TestFolder { Name = "folder1"}] 
                    }
                ],
                Files = [new TestFile { Name = "file.txt" }]
            }
        ], []));

        var drive = Assert.IsType<SgaDrive>(archive.GetDrive("Drive"));
        Assert.Throws(exceptionType, () => drive.GetEntry(path!));
    }
}
