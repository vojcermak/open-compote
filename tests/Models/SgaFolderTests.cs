using System.Linq.Expressions;

namespace OpenCompote.SGA.Tests.Models;

public class SgaFolderTests
{
    [Fact]
    public void Folder_Contents_ExposesAllEntries()
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
                        Folders = [
                            new TestFolder { Name = "SubFolder", Folders = [], Files = [] }
                        ],
                        Files = [
                            new TestFile { Name = "file1.txt", StorageType = StorageType.Uncompress, Modified = MockParser.FixedTime, FileContent = "" },
                            new TestFile { Name = "file2.txt", StorageType = StorageType.Uncompress, Modified = MockParser.FixedTime, FileContent = "" }
                        ]
                    }
                ]
            }
        ], []));

        var drive = Assert.IsType<SgaDrive>(archive.GetDrive("Drive"));
        var folder = Assert.IsType<SgaFolder>(drive.Contents.Single());

        Assert.Equal(3, folder.Contents.Count);
        Assert.Single(folder.Contents.OfType<SgaFolder>());
        Assert.Equal(2, folder.Contents.OfType<SgaFile>().Count());
    }

    [Fact]
    public void Folder_ThrowsWhenReadonly()
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
                        Folders = [
                            new TestFolder {Name = "SubFolder"}
                        ],
                        Files = [
                            new TestFile {Name = "file1.txt"},
                            new TestFile {Name = "file2.txt"}
                        ]
                    }
                ]
            }
        ], []));

        var drive = Assert.IsType<SgaDrive>(archive.GetDrive("Drive"));
        var folder = Assert.IsType<SgaFolder>(drive.Contents.Single());

        Assert.Throws<InvalidOperationException>(() => folder.AddFolder("SubFolder"));
        Assert.Throws<InvalidOperationException>(() => folder.AddFile("file1.txt", StorageType.Uncompress));
        Assert.Throws<InvalidOperationException>(folder.Delete);
    }

    [Fact]
    public void Folder_ThrowsWhenDisposed()
    {
        using var archive = MockParser.CreateArchive(SgaMode.Write, new("archive", [
            new TestDrive
            {
                Name = "Drive",
                Alias = "alias",
                Folders = [
                    new TestFolder
                    {
                        Name = "Folder",
                        Folders = [
                            new TestFolder {Name = "SubFolder"}
                        ],
                        Files = [
                            new TestFile {Name = "file1.txt"},
                            new TestFile {Name = "file2.txt"}
                        ]
                    }
                ]
            }
        ], [
            new TestDrive
            {
                Name = "Drive",
                Alias = "alias",
                Folders = [
                    new TestFolder
                    {
                        Name = "Folder",
                        Folders = [
                            new TestFolder {Name = "SubFolder"}
                        ],
                        Files = [
                            new TestFile {Name = "file1.txt"},
                            new TestFile {Name = "file2.txt"}
                        ]
                    }
                ]
            }
        ]));

        var drive = Assert.IsType<SgaDrive>(archive.GetDrive("Drive"));
        var folder = Assert.IsType<SgaFolder>(drive.Contents.Single());

        archive.Dispose();

        Assert.Throws<ObjectDisposedException>(() => folder.Contents);
        Assert.Throws<ObjectDisposedException>(() => folder.AddFolder("SubFolder"));
        Assert.Throws<ObjectDisposedException>(() => folder.AddFile("file1.txt", StorageType.Uncompress));
        Assert.Throws<ObjectDisposedException>(folder.Delete);
        Assert.Throws<ObjectDisposedException>(() => folder.GetEntry("file1.txt"));
    }

    // ==================== AddFolder Tests ====================

    [Fact]
    public void Folder_AddFolder_CreatesSubfolderWithCorrectPath()
    {
        using var archive = MockParser.CreateArchive(SgaMode.Write, new("archive", [
            new TestDrive
            {
                Name = "Drive",
                Alias = "alias",
                Folders = [new TestFolder {Name = "Parent"}]
            }
        ], [
            new TestDrive
            {
                Name = "Drive",
                Alias = "alias",
                Folders = [
                    new TestFolder { Name = "Parent", Folders = [new TestFolder{Name="SubFolder"}] }
                ]
            }
        ]));

        var drive = Assert.IsType<SgaDrive>(archive.GetDrive("Drive"));
        var parent = Assert.IsType<SgaFolder>(drive.Contents.Single());
        Assert.Empty(parent.Contents);

        var subfolder = parent.AddFolder("SubFolder");

        Assert.NotNull(subfolder);
        Assert.Equal("SubFolder", subfolder.Name);
        Assert.Equal("DRIVE:\\Parent\\SubFolder", subfolder.Path);
        Assert.Same(parent, subfolder.Parent);
        Assert.Single(parent.Contents);
        Assert.IsType<SgaFolder>(parent.Contents.Single());
        Assert.Equal(subfolder, parent.Contents.Single());
    }

    [Theory]
    [InlineData(null, typeof(ArgumentNullException))]
    [InlineData("", typeof(ArgumentException))]
    [InlineData("SubFolder", typeof(ArgumentException))]
    [InlineData(" subfolder ", typeof(ArgumentException))]
    public void Folder_AddFolder_ThrowsWithInvalidInput(string? newName, Type exception)
    {
        using var archive = MockParser.CreateArchive(SgaMode.Write, new("archive", [
            new TestDrive
            {
                Name = "Drive",
                Alias = "alias",
                Folders = [
                    new TestFolder { Name = "Folder", Folders = [new TestFolder{Name = "SubFolder"}]}
                ]
            }
        ], [
            new TestDrive
            {
                Name = "Drive",
                Alias = "alias",
                Folders = [
                    new TestFolder { Name = "Folder", Folders = [new TestFolder{Name = "SubFolder"}]}
                ]
            }
        ]));

        var drive = Assert.IsType<SgaDrive>(archive.GetDrive("Drive"));
        var folder = Assert.IsType<SgaFolder>(drive.Contents.Single());

        var ex = Assert.Throws(exception, () => folder.AddFolder(newName!));
    }

    // ==================== AddFile Tests ====================

    [Fact]
    public void Folder_AddFile_CreatesFileWithCorrectProperties()
    {
        using var archive = MockParser.CreateArchive(SgaMode.Write, new("archive", [
            new TestDrive
            {
                Name = "Drive",
                Alias = "alias",
                Folders = [new TestFolder {Name = "Folder"}]
            }
        ], [
            new TestDrive
            {
                Name = "Drive",
                Alias = "alias",
                Folders = [
                    new TestFolder { 
                        Name = "Folder", 
                        Files = [new TestFile{Name = "test.txt", StorageType = StorageType.StreamCompress}]
                    }
                ]
            }
        ]));

        var drive = Assert.IsType<SgaDrive>(archive.GetDrive("Drive"));
        var folder = Assert.IsType<SgaFolder>(drive.Contents.Single());
        Assert.Empty(folder.Contents);

        var file = folder.AddFile("test.txt", StorageType.StreamCompress);

        Assert.NotNull(file);
        Assert.Equal("test.txt", file.Name);
        Assert.Equal("DRIVE:\\Folder\\test.txt", file.Path);
        Assert.Same(folder, file.Parent);
        Assert.Same(drive, file.Drive);
        Assert.Single(folder.Contents);
        Assert.IsType<SgaFile>(folder.Contents.Single());
        Assert.Same(file, folder.Contents.Single());
    }

    [Theory]
    [InlineData(null, StorageType.Uncompress, typeof(ArgumentNullException))]
    [InlineData("", StorageType.Uncompress, typeof(ArgumentException))]
    [InlineData("file.txt", StorageType.Uncompress, typeof(ArgumentException))]
    [InlineData(" File.txt ", StorageType.Uncompress, typeof(ArgumentException))]
    [InlineData("newFile.txt", null, typeof(InvalidOperationException))]
    [InlineData("newFile.txt", (StorageType)69, typeof(ArgumentOutOfRangeException))]
    public void Folder_AddFile_ThrowsOnDuplicateName(string? newName, StorageType? type, Type exception)
    {
        using var archive = MockParser.CreateArchive(SgaMode.Write, new("archive", [
            new TestDrive
            {
                Name = "Drive",
                Alias = "alias",
                Folders = [
                    new TestFolder { Name = "Folder", Files = [new TestFile{Name = "file.txt"}] }
                ]
            }
        ], [
            new TestDrive
            {
                Name = "Drive",
                Alias = "alias",
                Folders = [
                    new TestFolder { Name = "Folder", Files = [new TestFile{Name = "file.txt"}] }
                ]
            }
        ]));

        var drive = Assert.IsType<SgaDrive>(archive.GetDrive("Drive"));
        var folder = Assert.IsType<SgaFolder>(drive.Contents.Single());

        Assert.Throws(exception, () => folder.AddFile(newName!, (StorageType)type!));
    }

    // ==================== Delete Tests ====================

    [Fact]
    public void Folder_Delete_RemovesFromParentFolder()
    {
        using var archive = MockParser.CreateArchive(SgaMode.Write, new("archive", [
            new TestDrive
            {
                Name = "Drive",
                Alias = "alias",
                Folders = [
                    new TestFolder
                    {
                        Name = "Parent",
                        Folders = [new TestFolder { Name = "Child", Folders = [] }]
                    }
                ]
            }
        ], [
            new TestDrive
            {
                Name = "Drive",
                Alias = "alias",
                Folders = [new TestFolder{Name = "Parent"}]
            }
        ]));

        var drive = Assert.IsType<SgaDrive>(archive.GetDrive("Drive"));
        var parent = Assert.IsType<SgaFolder>(drive.Contents.Single());
        var child = Assert.IsType<SgaFolder>(parent.Contents.Single());

        Assert.Single(parent.Contents);

        child.Delete();

        Assert.Empty(parent.Contents);
        Assert.Null(child.Parent);
        Assert.Null(child.Drive);
        Assert.Throws<ObjectDisposedException>(() => child.Name);
        Assert.Throws<ObjectDisposedException>(() => child.Path);
        Assert.Throws<ObjectDisposedException>(() => child.Contents);
        Assert.Throws<ObjectDisposedException>(() => child.GetEntry(""));
        Assert.Throws<ObjectDisposedException>(() => child.AddFile("name", StorageType.Uncompress));
        Assert.Throws<ObjectDisposedException>(() => child.AddFolder("name"));
    }

    [Fact]
    public void Folder_Delete_RemovesFromParentDrive()
    {
        using var archive = MockParser.CreateArchive(SgaMode.Write, new("archive", [
            new TestDrive
            {
                Name = "Drive",
                Alias = "alias",
                Folders = [new TestFolder{Name = "Child"}]
            }
        ], [
            new TestDrive
            {
                Name = "Drive",
                Alias = "alias"
            }
        ]));

        var drive = Assert.IsType<SgaDrive>(archive.GetDrive("Drive"));
        var folder = Assert.IsType<SgaFolder>(drive.Contents.Single());

        Assert.Single(drive.Contents);

        folder.Delete();

        Assert.Empty(drive.Contents);
        Assert.Null(folder.Drive);
        Assert.Throws<ObjectDisposedException>(() => folder.Name);
        Assert.Throws<ObjectDisposedException>(() => folder.Path);
        Assert.Throws<ObjectDisposedException>(() => folder.Contents);
        Assert.Throws<ObjectDisposedException>(() => folder.GetEntry(""));
        Assert.Throws<ObjectDisposedException>(() => folder.AddFile("name", StorageType.Uncompress));
        Assert.Throws<ObjectDisposedException>(() => folder.AddFolder("name"));
    }

    [Fact]
    public void Folder_Delete_DeletesChildrenRecursively()
    {
        using var archive = MockParser.CreateArchive(SgaMode.Write, new("archive", [
            new TestDrive
            {
                Name = "Drive",
                Alias = "alias",
                Folders = [
                    new TestFolder
                    {
                        Name = "Parent",
                        Folders = [
                            new TestFolder
                            {
                                Name = "Child",
                                Folders = [new TestFolder{Name = "Subfolder"}],
                                Files = [new TestFile {Name = "file.txt",}]
                            }
                        ]
                    }
                ]
            }
        ], [
            new TestDrive
            {
                Name = "Drive",
                Alias = "alias",
                Folders = [new TestFolder{Name = "Parent"}]
            }
        ]));

        var drive = Assert.IsType<SgaDrive>(archive.GetDrive("Drive"));
        var parent = Assert.IsType<SgaFolder>(drive.Contents.Single());
        var child = Assert.IsType<SgaFolder>(parent.Contents.Single());
        var file = Assert.IsType<SgaFile>(child.GetEntry("file.txt"));
        var subFolder = Assert.IsType<SgaFolder>(child.GetEntry("Subfolder"));

        child.Delete();

        Assert.Throws<ObjectDisposedException>(() => file.Path);
        Assert.Throws<ObjectDisposedException>(() => file.Name);
        Assert.Null(file.Drive);

        Assert.Throws<ObjectDisposedException>(() => subFolder.Path);
        Assert.Throws<ObjectDisposedException>(() => subFolder.Name);
        Assert.Null(subFolder.Drive);
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
    public void Folder_GetEntry_FindValidInputs(string path, Type? OutputType, string expectedName = "")
    {
        using var archive = MockParser.CreateArchive(SgaMode.Read, new("archive", [
            new TestDrive
            {
                Name = "Drive",
                Alias = "alias",
                Folders = [
                    new TestFolder
                    {
                        Name = "TestFolder",
                        Folders = [new TestFolder { 
                            Name = "subfolder",
                            Files = [new TestFile { Name = "file2.txt" }],
                            Folders = [new TestFolder { Name = "folder1"}] 
                        }],
                        Files = [new TestFile { Name = "file.txt" }]
                    }
                ]
            }
        ], []));

        var drive = Assert.IsType<SgaDrive>(archive.GetDrive("Drive"));
        var testFolder = Assert.IsType<SgaFolder>(drive.Contents.Single());

        var result = testFolder.GetEntry(path);

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
    public void Folder_GetEntry_InvalidInputThrowsException(string? path, Type exceptionType)
    {
        using var archive = MockParser.CreateArchive(SgaMode.Read, new("archive", [
            new TestDrive
            {
                Name = "Drive",
                Alias = "alias",
                Folders = [
                    new TestFolder
                    {
                        Name = "TestFolder",
                        Folders = [new TestFolder { 
                            Name = "subfolder",
                            Files = [new TestFile { Name = "file2.txt" }],
                            Folders = [new TestFolder { Name = "folder1"}] 
                        }],
                        Files = [new TestFile { Name = "file.txt" }]
                    }
                ]
            }
        ], []));

        var drive = Assert.IsType<SgaDrive>(archive.GetDrive("Drive"));
        var testFolder = Assert.IsType<SgaFolder>(drive.Contents.Single());
        Assert.Throws(exceptionType, () => testFolder.GetEntry(path!));
    }
}
