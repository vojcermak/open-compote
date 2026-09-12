namespace OpenCompote.SGA.Tests.Models;

public class SgaEntryTests
{

	[Fact]
	public void Entry_ParentDriveAndPath_AreCorrect()
	{
        using var archive = MockParser.CreateArchive(SgaMode.Read, new("archive", [
        new TestDrive {
            Alias = "alias",
            Name = "Drive",
            Folders = [new TestFolder{
                Name = "Folder",
                Files = [new TestFile{
                    Name = "file.txt"
                }]
            }],
        }], []));
		
        SgaDrive drive = Assert.IsType<SgaDrive>(archive.GetDrive("Drive"));
		SgaFolder folder = Assert.IsType<SgaFolder>(drive.GetEntry("Folder"));
		SgaFile file = Assert.IsType<SgaFile>(folder.GetEntry("file.txt"));

		Assert.Null(folder.Parent);
		Assert.Same(drive, folder.Drive);
		Assert.Equal("DRIVE:\\Folder", folder.Path);

		Assert.Same(folder, file.Parent);
		Assert.Same(drive, file.Drive);
		Assert.Equal("DRIVE:\\Folder\\file.txt", file.Path);
	}

	[Fact]
	public void Entry_RenameUpdatesParentAndPath()
	{
        using var archive = MockParser.CreateArchive(SgaMode.Write, new("archive", [
        new TestDrive {
            Alias = "alias",
            Name = "Drive",
            Folders = [new TestFolder{
                Name = "Folder",
                Files = [new TestFile{Name = "file.txt"}]
            }]
        }], [
        new TestDrive {
            Alias = "alias",
            Name = "Drive",
            Folders = [new TestFolder{
                Name = "Renamed",
                Files = [new TestFile{Name = "renamed.txt"}]
            }],
        }]));

        SgaDrive drive = Assert.IsType<SgaDrive>(archive.GetDrive("Drive"));
		SgaFolder folder = Assert.IsType<SgaFolder>(drive.GetEntry("Folder"));
		SgaFile file = Assert.IsType<SgaFile>(folder.GetEntry("file.txt"));

		folder.Name = "Renamed";

		Assert.DoesNotContain(folder, drive.Contents.Where(entry => entry.Name == "Folder"));
		Assert.Same(folder, drive.Contents.Single(entry => entry.Name == "Renamed"));
		Assert.Equal("DRIVE:\\Renamed", folder.Path);
		Assert.Equal("DRIVE:\\Renamed\\file.txt", file.Path);

		file.Name = "renamed.txt";

		Assert.Same(file, folder.Contents.Single(entry => entry.Name == "renamed.txt"));
		Assert.Equal("DRIVE:\\Renamed\\renamed.txt", file.Path);
	}

	[Theory]
    [InlineData(null, typeof(ArgumentNullException))]
    [InlineData("", typeof(ArgumentException))]
    [InlineData("First", typeof(ArgumentException))]
	public void Entry_RenameToInvalidThrowException(string? newName, Type exception)
	{
        using var archive = MockParser.CreateArchive(SgaMode.Write, new("archive", [
        new TestDrive {
            Alias = "alias",
            Name = "Drive",
            Folders = [
                new TestFolder{Name = "First",},
                new TestFolder{Name = "Second"}
            ]
        }], [
        new TestDrive {
           Alias = "alias",
            Name = "Drive",
            Folders = [
                new TestFolder{Name = "First",},
                new TestFolder{Name = "Second"}
            ]
        }]));

        SgaDrive drive = Assert.IsType<SgaDrive>(archive.GetDrive("Drive"));
		SgaFolder second = Assert.IsType<SgaFolder>(drive.GetEntry("Second"));

		Assert.Throws(exception,() => second.Name = newName!);
	}

	[Fact]
	public void Entry_RenameThrowsWhenArchiveIsReadOnly()
	{
		using var archive = MockParser.CreateArchive(SgaMode.Read, new("archive", [
			new TestDrive
			{
                Alias = "alias",
				Name = "Drive",
                Folders = [
                    new TestFolder{Name = "Folder"}
                ]
			}
		],[]));

		SgaDrive drive = Assert.IsType<SgaDrive>(archive.GetDrive("Drive"));
		SgaFolder folder = Assert.IsType<SgaFolder>(drive.Contents.Single());

		Assert.Throws<InvalidOperationException>(() => folder.Name = "Changed");
		Assert.Equal("Folder", folder.Name);
	}

	[Fact]
	public void Entry_AccessThrowsWhenArchiveIsDisposed()
	{
		var archive = MockParser.CreateArchive(SgaMode.Read, new("archive", [
			new TestDrive
			{
                Alias = "alias",
				Name = "Drive",
                Folders = [
                    new TestFolder{Name = "Folder"}
                ],
                Files = [
                    new TestFile{Name = "file.txt"}
                ]
			}
		],[]));

        SgaDrive drive = Assert.IsType<SgaDrive>(archive.GetDrive("Drive"));
        SgaFolder folder = Assert.IsType<SgaFolder>(drive.GetEntry("Folder"));
        SgaFile file = Assert.IsType<SgaFile>(drive.GetEntry("file.txt"));

		archive.Dispose();

		Assert.Throws<ObjectDisposedException>(() => folder.Name);
		Assert.Throws<ObjectDisposedException>(() => folder.Path);
		Assert.Throws<ObjectDisposedException>(() => file.Name);
		Assert.Throws<ObjectDisposedException>(() => file.Path);
	}
}
