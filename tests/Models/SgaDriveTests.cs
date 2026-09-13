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
	public void Drive_AddFolderAndFile_AddEntriesToDrive()
	{
		using var archive = MockParser.CreateArchive(SgaMode.Write, new("archive", [
			new TestDrive { Alias = "alias", Name = "Drive" }
		], [
			new TestDrive
			{
				Alias = "alias",
				Name = "Drive",
				Folders = [new TestFolder { Name = "Folder" }],
				Files = [new TestFile { Name = "file.txt", StorageType = StorageType.StreamCompress }]
			}
		]));

		var drive = Assert.IsType<SgaDrive>(archive.GetDrive("Drive"));
		var folder = drive.AddFolder("Folder");
		var file = drive.AddFile("file.txt", StorageType.StreamCompress);

		Assert.Same(drive, folder.Drive);
		Assert.Same(drive, file.Drive);
		Assert.Equal("DRIVE:\\Folder", folder.Path);
		Assert.Equal("DRIVE:\\file.txt", file.Path);
		Assert.Contains(folder, drive.Contents);
		Assert.Contains(file, drive.Contents);
	}

	[Theory]
	[InlineData(null, typeof(ArgumentNullException))]
	[InlineData("", typeof(ArgumentException))]
	[InlineData("Folder", typeof(ArgumentException))]
	public void Drive_AddFolder_RejectsInvalidOrDuplicateNames(string? name, Type exception)
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

	[Theory]
	[InlineData(null, StorageType.Uncompress, typeof(ArgumentNullException))]
	[InlineData("", StorageType.Uncompress, typeof(ArgumentException))]
	[InlineData("file.txt", StorageType.Uncompress, typeof(ArgumentException))]
	[InlineData("new.txt", (StorageType)69, typeof(ArgumentOutOfRangeException))]
	public void Drive_AddFile_RejectsInvalidOrDuplicateNames(string? name, StorageType type, Type exception)
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

		Assert.Throws(exception, () => drive.AddFile(name!, type));
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
		Assert.Throws<ObjectDisposedException>(() => drive.Contents);
		drive.Delete();
	}


    // ==================== GetEntry Tests ====================

	[Fact]
	public void Drive_GetEntry_FindsEntriesByNormalizedPath()
	{
		using var archive = MockParser.CreateArchive(SgaMode.Read, new("archive", [
			new TestDrive
			{
				Alias = "alias",
				Name = "Drive",
				Folders = [new TestFolder
				{
					Name = "Folder",
					Files = [new TestFile { Name = "file.txt" }]
				}]
			}
		], []));

		var drive = Assert.IsType<SgaDrive>(archive.GetDrive("Drive"));

		Assert.Same(drive.GetEntry("Folder"), drive.GetEntry("/Folder/"));
		Assert.IsType<SgaFile>(drive.GetEntry("Folder\\file.txt"));
		Assert.Null(drive.GetEntry("Folder/missing.txt"));
		Assert.Null(drive.GetEntry("file.txt/more"));
	}
}
