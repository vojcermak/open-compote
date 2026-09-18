namespace OpenCompote.SGA.Tests.Models;

public class SgaArchiveTests
{
    [Fact]
	public void Archive_Constructor_RejectsInvalidStreamsAndParser()
	{
		using var readOnlyStream = new MemoryStream([], writable: false);

		Assert.Throws<ArgumentException>(() => new SgaArchive(
			readOnlyStream,
			SgaMode.Write,
			SgaVersion.V2,
			new MockParser("archive", [], [])));
		Assert.Throws<ArgumentNullException>(() => new SgaArchive(
			null!,
			SgaMode.Read,
			SgaVersion.V2,
			new MockParser("archive", [], [])));
		Assert.Throws<ArgumentNullException>(() => new SgaArchive(
			new MemoryStream(),
			SgaMode.Read,
			SgaVersion.V2,
			null!));
	}

    [Fact]
	public void Archive_ThrowsWhenReadonly()
	{
		using var archive = MockParser.CreateArchive(SgaMode.Read, new("archive", [], []));

		Assert.Throws<InvalidOperationException>(() => archive.ArchiveName = "changed");
		Assert.Throws<InvalidOperationException>(() => archive.AddDrive("alias", "Drive"));
	}

    [Fact]
	public void Archive_ThrowsWhenDisposed()
	{
		var archive = MockParser.CreateArchive(SgaMode.Read, new("archive", [], []));
		archive.Dispose();

		Assert.Throws<ObjectDisposedException>(() => archive.ArchiveName);
		Assert.Throws<ObjectDisposedException>(() => archive.ArchiveName = "changed");
		Assert.Throws<ObjectDisposedException>(() => archive.Drives);
        Assert.Throws<ObjectDisposedException>(() => archive.Mode);
        Assert.Throws<ObjectDisposedException>(() => archive.Version);
		Assert.Throws<ObjectDisposedException>(() => archive.AddDrive("alias", "Drive"));
		Assert.Throws<ObjectDisposedException>(() => archive.GetDrive("Drive"));
	}

	[Fact]
	public void Archive_ExposesModeVersionNameAndDrives()
	{
		using var archive = MockParser.CreateArchive(SgaMode.Read, new("archive", [
			new TestDrive { Alias = "alias", Name = "Drive" }
		], []));

		Assert.Equal(SgaMode.Read, archive.Mode);
		Assert.Equal(SgaVersion.V2, archive.Version);
		Assert.Equal("archive", archive.ArchiveName);
		Assert.Single(archive.Drives);
	}

    // ==================== AddDrive Tests ====================

	[Fact]
	public void Archive_AddDrive_AddsDriveWithNormalizedNames()
	{
		using var archive = MockParser.CreateArchive(SgaMode.Create, new("archive", [], [
			new TestDrive { Alias = "alias", Name = "Drive" }
		]));

		var drive = archive.AddDrive(" alias ", " Drive ");

		Assert.Single(archive.Drives);
		Assert.Same(drive, archive.Drives[0]);
		Assert.Equal("alias", drive.Alias);
		Assert.Equal("Drive", drive.Name);
		Assert.Same(archive, drive.Archive);
		Assert.Empty(drive.Contents);
	}

	[Theory]
	[InlineData(null, "Drive", typeof(ArgumentNullException))]
	[InlineData("alias", null, typeof(ArgumentNullException))]
	[InlineData("", "Drive", typeof(ArgumentException))]
	[InlineData("alias", "", typeof(ArgumentException))]
	public void Archive_AddDrive_RejectsInvalidNames(string? alias, string? name, Type exception)
	{
		using var archive = MockParser.CreateArchive(SgaMode.Create, new("archive", [], []));

		Assert.Throws(exception, () => archive.AddDrive(alias!, name!));
		Assert.Empty(archive.Drives);
	}

    // ==================== GetDrive Tests ====================

	[Fact]
	public void Archive_GetDrive_FindsByNameOrAlias()
	{
		using var archive = MockParser.CreateArchive(SgaMode.Read, new("archive", [
			new TestDrive { Alias = "alias", Name = "Drive" }
		], []));

		var drive = Assert.IsType<SgaDrive>(archive.GetDrive("Drive"));

		Assert.Same(drive, archive.GetDrive("alias"));
		Assert.Null(archive.GetDrive("missing"));
	}

	[Fact]
	public void Archive_GetDrive_RejectsNull()
	{
		using var archive = MockParser.CreateArchive(SgaMode.Read, new("archive", [], []));

		Assert.Throws<ArgumentNullException>(() => archive.GetDrive(null!));
	}

    // ==================== GetEntry Tests ====================

    [Theory]
    [InlineData("Drive:/subfolder", typeof(SgaFolder), "subfolder")]              //Get direct child folder
    [InlineData("drive:/file.txt", typeof(SgaFile), "file.txt"),]                 //Get direct child file
    [InlineData("DRIVE:/subfolder/folder1", typeof(SgaFolder), "folder1")]        //Get nested folder
    [InlineData("FIRST_DRIVE:/subfolder/file2.txt", typeof(SgaFile), "file2.txt")]//Get nested file
    [InlineData("DRIVE:/nonexistingFile", null)]                                  //Get nonexisting entry
    [InlineData("DRIVE:/subfolder/noFile.txt", null)]                             //Get nonexisting nested entry
    [InlineData("DRIVE:\\subfolder\\file2.txt", typeof(SgaFile), "file2.txt")]    //Get sub file with wrong separators
    [InlineData("alias:/subfolder", null)]                                        //Get the child of an empty drive
    [InlineData("Attr:/subfolder", null)]                                         //Get a child from nonexistent drive
    [InlineData("Drive:/", null)]                                                 //Get the drive instead of an entry
	public void Drive_GetEntry_FindsExistingEntry(string path, Type? OutputType, string expectedName = "")
	{
		using var archive = MockParser.CreateArchive(SgaMode.Read, new("archive", [
			new TestDrive
			{
				Alias = "FIRST_DRIVE",
				Name = "Drive",
				Folders = [new TestFolder { 
                    Name = "subfolder",
                    Files = [new TestFile { Name = "file2.txt" }],
                    Folders = [new TestFolder { Name = "folder1"}] 
                }],
                Files = [new TestFile { Name = "file.txt" }]
			},
            new TestDrive{
                Alias = "Drive",
                Name = "alias"
            }
		], []));

        var result = archive.GetEntry(path);

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
    [InlineData(":/", typeof(ArgumentException))]     // Empty string is not allowed
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

        Assert.Throws(exceptionType, () => archive.GetEntry(path!));
    }

    // ==================== Dispose Tests ====================

	[Fact]
	public void Archive_Dispose_ClosesStreamUnlessLeaveOpen()
	{
		var closedStream = new MemoryStream();
		var closedArchive = new SgaArchive(
			closedStream,
			SgaMode.Create,
			SgaVersion.V2,
			new MockParser("archive", [], []));

		closedArchive.Dispose();

		Assert.Throws<ObjectDisposedException>(() => closedStream.Position);

		var openStream = new MemoryStream();
		var openArchive = new SgaArchive(
			openStream,
			SgaMode.Read,
			SgaVersion.V2,
			new MockParser("archive", [], []),
			leaveOpen: true);
		Assert.Equal(0, openStream.Position);

		openArchive.Dispose();

		openStream.Dispose();
	}
}
