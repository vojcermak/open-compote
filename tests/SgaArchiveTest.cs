using System.Security.Cryptography.X509Certificates;
using System.Text;
using OpenCompote.SGA.Parsers;


namespace OpenCompote.SGA.Tests;

public class SgaArchiveTest
{
    // This test tests if the default archive class functions work. Test for Archive mode, version, name and 
    // if the archive stream is disposed when the archive is disposed.
    [Fact]
    public void Archive_Open()
    {
        Stream archiveStream = new MemoryStream();
        ISgaParser parser = new MockParser("testArchive", [], []);
        TimeProvider timeProvider = new MockTimeProvider(DateTimeOffset.Now);

        using (var archive = new SgaArchive(archiveStream, SgaMode.Write, SgaVersion.V2, parser, false, timeProvider))
        {
            Assert.Equal(SgaMode.Write, archive.Mode);
            Assert.Equal(SgaVersion.V2, archive.Version);
            Assert.Equal("testArchive", archive.ArchiveName);

            archive.ArchiveName = "New Archive name.";

            Assert.Equal("New Archive name.", archive.ArchiveName);
        }
        Assert.Throws<ObjectDisposedException>(() => archiveStream.Position);
    }

    // This test tests if the read only archive does not allow writing to the archive. And that archive stream stay
    // open when leaveOpen = true
    [Fact]
    public void Archive_ReadOnly()
    {
        Stream archiveStream = new MemoryStream([], false); // Set read only stream.
        ISgaParser parser = new MockParser("testArchive", [], []);
        TimeProvider timeProvider = new MockTimeProvider(DateTimeOffset.Now);

        Assert.Throws<ArgumentException>(() => new SgaArchive(archiveStream, SgaMode.Write, SgaVersion.V2, parser, false, timeProvider));

        using (var archive = new SgaArchive(archiveStream, SgaMode.Read, SgaVersion.V2, parser, true, timeProvider))
        {
            Assert.Equal(SgaMode.Read, archive.Mode);
            Assert.Equal(SgaVersion.V2, archive.Version);
            Assert.Equal("testArchive", archive.ArchiveName);

            Assert.Throws<NotSupportedException>(() => archive.ArchiveName = "New Archive name.");
        }
        
        Assert.Equal(0, archiveStream.Position);
    }

    //This test tests if a disposed archive throws when user tries to change it.
    [Fact]
    public void SgaArchive_ThrowsWhenDisposed()
    {
        Stream archiveStream = new MemoryStream();
        ISgaParser parser = new MockParser("testArchive", [], []);
        TimeProvider timeProvider = new MockTimeProvider(DateTimeOffset.Now);

        var Archive = new SgaArchive(archiveStream, SgaMode.Read, SgaVersion.V2, parser);
        Archive.Dispose();

        Assert.Throws<ObjectDisposedException>(() => Archive.ArchiveName);
        Assert.Throws<ObjectDisposedException>(() => Archive.ArchiveName = "TEST");
        Assert.Throws<ObjectDisposedException>(() => Archive.Drives);
        Assert.Throws<ObjectDisposedException>(() => Archive.AddDrive("test", "test"));
        Assert.Throws<ObjectDisposedException>(() => Archive.GetDrive("Test"));
    }

    #region Drives

    //This test tests if Add drive function works and create new drive with a default root folder.
    [Fact]
    public void SgaDrive_AddDrive_ValidArguments()
    {
        Stream archiveStream = new MemoryStream();
        ISgaParser parser = new MockParser("testArchive", [], [
            new TestDrive{
                Alias = "Drive-Alias",
                Name = "Drive-Name",
                RootFolder = new TestFolder{
                    Name = "Drive-Name",
                    Folders = [],
                    Files = []
                }
            }
        ]);
        TimeProvider timeProvider = new MockTimeProvider(DateTimeOffset.Now);

        using (var archive = new SgaArchive(archiveStream, SgaMode.Create, SgaVersion.V2, parser, false, timeProvider))
        {
            Assert.Empty(archive.Drives);

            SgaDrive newDrive = archive.AddDrive("Drive-Alias", "Drive-Name");

            Assert.Single(archive.Drives);
            Assert.Same(newDrive, archive.Drives[0]);
        }
    }

    //This test tests if the AddDrive function handles null/invalid values
    [Fact]
    public void SgaDrive_AddDrive_InvalidArguments()
    {
        Stream archiveStream = new MemoryStream();
        ISgaParser parser = new MockParser("testArchive", [], []);
        TimeProvider timeProvider = new MockTimeProvider(DateTimeOffset.Now);

        using (var archive = new SgaArchive(archiveStream, SgaMode.Create, SgaVersion.V2, parser, false, timeProvider))
        {
            #pragma warning disable CS8625
            Assert.Throws<ArgumentNullException>(() => archive.AddDrive(null, "Name"));
            Assert.Throws<ArgumentNullException>(() => archive.AddDrive("Alias", null));
            #pragma warning restore CS8625
        }
    }

    // This test tests if the get drive function works and if the Alias and Name can be changed.
    [Fact]
    public void SgaDrive_UpdateDrive()
    {
        Stream archiveStream = new MemoryStream();
        ISgaParser parser = new MockParser("testArchive", [
            new TestDrive{
                Alias="Drive-Alias",
                Name="Drive-Name",
                RootFolder = new TestFolder{
                    Name = "Drive-Name",
                    Files = [],
                    Folders = []
                }
            }
        ], [
            new TestDrive{
                Alias = "Changed",
                Name = "Changed",
                RootFolder = new TestFolder{
                    Name = "Drive-Name",
                    Files = [],
                    Folders = []
                }
            }
        ]);
        TimeProvider timeProvider = new MockTimeProvider(DateTimeOffset.Now);

        using (var archive = new SgaArchive(archiveStream, SgaMode.Write, SgaVersion.V2, parser, false, timeProvider))
        {
            SgaDrive drive = archive.GetDrive("Drive-Name")!;

            Assert.Equal("Drive-Alias", drive.Alias);
            Assert.Equal("Drive-Name", drive.Name);

            drive.Alias = "Changed";
            drive.Name = "Changed";
        }
    }

    // This test tests if the drive.Delete deleted the specified drive from the archive and all its contents.
    [Fact]
    public void SgaDrive_RemoveDrive()
    {
        Stream archiveStream = new MemoryStream();
        ISgaParser parser = new MockParser("testArchive", [
            new TestDrive{
                Alias="Drive-Alias",
                Name="Drive-Name",
                RootFolder = new TestFolder{
                    Name = "Drive-Name",
                    Files = [],
                    Folders = []
                }
            }
        ], []);
        TimeProvider timeProvider = new MockTimeProvider(DateTimeOffset.Now);

        using (var archive = new SgaArchive(archiveStream, SgaMode.Write, SgaVersion.V2, parser, false, timeProvider))
        {
            SgaDrive drive = archive.GetDrive("Drive-Name")!;
            SgaFolder rootFolder = drive.RootFolder;

            drive.Delete();

            // Test if the deleted drive cannot be modified or read.
            Assert.Throws<ObjectDisposedException>(()=> drive.Alias);
            Assert.Throws<ObjectDisposedException>(()=> drive.Name);
            Assert.Throws<ObjectDisposedException>(()=> drive.Alias = "");
            Assert.Throws<ObjectDisposedException>(()=> drive.Alias = "");
            Assert.Throws<ObjectDisposedException>(()=> rootFolder.Name); // test if the root folder is deleted as well.

            // Test if the deleted drive is not in the archive. 
            Assert.DoesNotContain(drive, archive.Drives);
            Assert.Null(archive.GetDrive("Drive-Name"));
        }
    }

    // This test tests if all the drive manipulation methods that write to the file are throw when the archive was open in read mode.
    [Fact]
    public void SgaDrive_UpdateDrives_ThrowsWhenReadonly()
    {
        Stream archiveStream = new MemoryStream();
        ISgaParser parser = new MockParser("testArchive", [
            new TestDrive{
                Alias="Drive-Alias",
                Name="Drive-Name",
                RootFolder = new TestFolder{
                    Name = "Drive-Name",
                    Files = [],
                    Folders = []
                }
            }
        ], [
            new TestDrive{
                Alias="Drive-Alias",
                Name="Drive-Name",
                RootFolder = new TestFolder{
                    Name = "Drive-Name",
                    Files = [],
                    Folders = []
                }
            }
        ]);
        TimeProvider timeProvider = new MockTimeProvider(DateTimeOffset.Now);

        using (var archive = new SgaArchive(archiveStream, SgaMode.Read, SgaVersion.V2, parser, false, timeProvider))
        {
            SgaDrive drive = archive.GetDrive("Drive-Name")!;

            Assert.Throws<NotSupportedException>(()=> archive.AddDrive("alias", "name"));
            Assert.Throws<NotSupportedException>(()=> drive.Alias = "");
            Assert.Throws<NotSupportedException>(()=> drive.Name = "");
            Assert.Throws<NotSupportedException>(drive.Delete);
        }
    }

    // This test if all the drive manipulation methods throw when the parent archive is disposed.
    [Fact]
    public void SgaDrive_UpdateDrives_ThrowsWhenDisposed()
    {
                Stream archiveStream = new MemoryStream();
        ISgaParser parser = new MockParser("testArchive", [
            new TestDrive{
                Alias="Drive-Alias",
                Name="Drive-Name",
                RootFolder = new TestFolder{
                    Name = "Drive-Name",
                    Files = [],
                    Folders = []
                }
            }
        ], [
            new TestDrive{
                Alias="Drive-Alias",
                Name="Drive-Name",
                RootFolder = new TestFolder{
                    Name = "Drive-Name",
                    Files = [],
                    Folders = []
                }
            }
        ]);
        TimeProvider timeProvider = new MockTimeProvider(DateTimeOffset.Now);
        var archive = new SgaArchive(archiveStream, SgaMode.Write, SgaVersion.V2, parser, false, timeProvider);
        SgaDrive drive = archive.GetDrive("Drive-Name")!;

        archive.Dispose();
        
        Assert.Throws<ObjectDisposedException>(()=> archive.AddDrive("alias", "name"));
        Assert.Throws<ObjectDisposedException>(()=> drive.Alias);
        Assert.Throws<ObjectDisposedException>(()=> drive.Name);
        Assert.Throws<ObjectDisposedException>(()=> drive.Alias = "");
        Assert.Throws<ObjectDisposedException>(()=> drive.Alias = "");
        Assert.Throws<ObjectDisposedException>(drive.Delete);
    }

    #endregion
    #region Folders

    // This test tests if Add drive function works and creates new folder.
    [Fact]
    public void SgaFolder_AddFolder_ValidArguments()
    {
        Stream archiveStream = new MemoryStream();
        ISgaParser parser = new MockParser("testArchive", [
            new TestDrive{
                Alias = "Drive-Alias",
                Name = "Drive-Name",
                RootFolder = new TestFolder{
                    Name = "Drive-Name",
                    Folders = [],
                    Files = []
                }
            }
        ], [
            new TestDrive{
                Alias = "Drive-Alias",
                Name = "Drive-Name",
                RootFolder = new TestFolder{
                    Name = "Drive-Name",
                    Folders = [
                        new TestFolder{
                            Name = "NewFolder",
                            Files = [],
                            Folders = []
                        }
                    ],
                    Files = []
                }
            }
        ]);
        TimeProvider timeProvider = new MockTimeProvider(DateTimeOffset.Now);

        using (var archive = new SgaArchive(archiveStream, SgaMode.Write, SgaVersion.V2, parser, false, timeProvider))
        {
            SgaDrive drive = archive.GetDrive("Drive-Alias")!;

            SgaFolder newFolder = drive.RootFolder.AddFolder("NewFolder");

            Assert.Single(drive.RootFolder.Contents);
            Assert.Same(newFolder, drive.RootFolder.Contents[0]);
            Assert.Equal("NewFolder", newFolder.Name);
            Assert.Equal("Drive-Name\\NewFolder", newFolder.Path);
            Assert.Same(drive.RootFolder, newFolder.Parent);
            Assert.Same(drive, newFolder.Drive);
        }
    }

    [Fact]
    public void SgaFolder_AddFolder_InvalidArguments()
    {
        Stream archiveStream = new MemoryStream();
        ISgaParser parser = new MockParser("testArchive", [
            new TestDrive{
                Alias = "Drive-Alias",
                Name = "Drive-Name",
                RootFolder = new TestFolder{
                    Name = "Drive-Name",
                    Folders = [],
                    Files = []
                }
            }
        ], [
            new TestDrive{
                Alias = "Drive-Alias",
                Name = "Drive-Name",
                RootFolder = new TestFolder{
                    Name = "Drive-Name",
                    Folders = [],
                    Files = []
                }
            }
        ]);
        TimeProvider timeProvider = new MockTimeProvider(DateTimeOffset.Now);

        using (var archive = new SgaArchive(archiveStream, SgaMode.Write, SgaVersion.V2, parser, false, timeProvider))
        {
            SgaDrive drive = archive.GetDrive("Drive-Alias")!;

            #pragma warning disable CS8625
            Assert.Throws<ArgumentNullException>(() => drive.RootFolder.AddFolder(null));
            #pragma warning restore CS8625
        }
    }

    [Fact]
    public void SgaFolder_UpdateFolder()
    {
        Stream archiveStream = new MemoryStream();
        ISgaParser parser = new MockParser("testArchive", [
            new TestDrive{
                Alias = "Drive-Alias",
                Name = "Drive-Name",
                RootFolder = new TestFolder{
                    Name = "Drive-Name",
                    Folders = [
                        new TestFolder{
                            Name = "SubFolder",
                            Folders = [],
                            Files = []
                        }
                    ],
                    Files = []
                }
            }
        ], [
            new TestDrive{
                Alias = "Drive-Alias",
                Name = "Drive-Name",
                RootFolder = new TestFolder{
                    Name = "Updated",
                    Folders = [
                        new TestFolder{
                            Name = "SubFolder",
                            Folders = [],
                            Files = []
                        }
                    ],
                    Files = []
                }
            }
        ]);
        TimeProvider timeProvider = new MockTimeProvider(DateTimeOffset.Now);

        using (var archive = new SgaArchive(archiveStream, SgaMode.Write, SgaVersion.V2, parser, false, timeProvider))
        {
            SgaDrive drive = archive.GetDrive("Drive-Alias")!;
            SgaFolder folder = drive.RootFolder;


            Assert.Equal("Drive-Name", folder.Name);
            Assert.Equal("Drive-Name", folder.Path);
            Assert.Single(folder.Contents);
            
            SgaFolder subFolder = (SgaFolder)folder.Contents[0];

            Assert.Equal("SubFolder", subFolder.Name);
            Assert.Equal("Drive-Name\\SubFolder", subFolder.Path);

            folder.Name = "Updated";
            Assert.Equal("Updated", folder.Name);
            Assert.Equal("Updated", folder.Path);
            Assert.Equal("Updated\\SubFolder", subFolder.Path);
        }
    }

    [Fact]
    public void SgaFolder_RemoveFolder()
    {
        Stream archiveStream = new MemoryStream();
        ISgaParser parser = new MockParser("testArchive", [
            new TestDrive{
                Alias = "Drive-Alias",
                Name = "Drive-Name",
                RootFolder = new TestFolder{
                    Name = "Drive-Name",
                    Folders = [
                        new TestFolder{
                            Name = "SubFolder",
                            Folders = [
                                new TestFolder{
                                    Name = "SubSubFolder",
                                    Folders = [],
                                    Files = []
                                }
                            ],
                            Files = [
                                new TestFile{
                                    Name = "testFile",
                                    StorageType = StorageType.Uncompress,
                                    Modified = DateTimeOffset.Now,
                                    FileContent = "Test file contents"
                                }
                            ]
                        }
                    ],
                    Files = []
                }
            }
        ], [
            new TestDrive{
                Alias = "Drive-Alias",
                Name = "Drive-Name",
                RootFolder = new TestFolder{
                    Name = "Drive-Name",
                    Folders = [],
                    Files = []
                }
            }
        ]);
        TimeProvider timeProvider = new MockTimeProvider(DateTimeOffset.Now);
    }

    [Fact]
    public void SgaFolder_UpdateFolder_ThrowsWhenReadonly()
    {
    }

    [Fact]
    public void SgaFolder_UpdateFolder_ThrowsWhenDisposed()
    {
    }

    #endregion

    // OLD WILL BE REMOVED
    /*#region Add Tests

    [Fact]
    public void AddFile_InCreateMode_AddsNewFileSuccessfully()
    {
        var stream = new MemoryStream();
        var parser = new MockParser([], [
            new TestDrive{
                Name = "Name",
                Alias = "Alias",
                RootFolder = new TestFolder{
                    Name = "Name",
                    Folders = [],
                    Files = [
                        new TestFile{
                            Name = "TestFile.txt",
                            StorageType = StorageType.Uncompress,
                            Modified = DateTimeOffset.Now,
                            FileContent = "File Contents"
                        }
                    ]
                }
            }
        ]);

        using (var archive = new SgaArchive(stream, SgaMode.Create, SgaVersion.V2, parser))
        {
            SgaDrive drive = archive.AddDrive("Alias", "Name");
            
            Assert.Empty(drive.RootFolder.Contents);

            SgaFile newFile = drive.RootFolder.AddFile("TestFile.txt", StorageType.Uncompress);

            Assert.Single(drive.RootFolder.Contents);
            Assert.Same(newFile, drive.RootFolder.Contents[0]);
            Assert.Equal("TestFile.txt", newFile.Name);
            Assert.Equal("Name\\TestFile.txt", newFile.Path);
            Assert.Equal(StorageType.Uncompress, newFile.StorageType);
            Assert.Same(drive.RootFolder, newFile.Parent);
            Assert.Same(drive, newFile.Drive);
            Assert.Equal(0u, newFile.CompressedSize);
            Assert.Equal(0u, newFile.Size);

            using(var fileContent = newFile.Open())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes("File Contents");
                fileContent.Write(inputBytes);
            }

            Assert.Equal(13u, newFile.CompressedSize);
            Assert.Equal(13u, newFile.Size);
        }
    }

    [Fact]
    public void AddFile_InReadMode_ThrowsNotSupportedException()
    {
        var drive = new TestDrive
        {
            Name = "Drive1",
            Alias = "Drive1",
            RootFolder = new TestFolder { Name = "Drive1", Folders = [], Files = [] }
        };
        var parser = new MockParser([drive], [drive]);
        var stream = new MemoryStream();

        using (var archive = new SgaArchive(stream, SgaMode.Read, SgaVersion.V2, parser))
        {
            Assert.Throws<NotSupportedException>(() => archive.Drives[0].RootFolder.AddFile("NewFile.txt", StorageType.Uncompress));
        }
    }

    [Fact]
    public void AddFile_WithCompressedStorageType_CreatesFileSuccessfully()
    {
        var stream = new MemoryStream();
        MockTimeProvider timeProvider = new MockTimeProvider(DateTimeOffset.Now);
        var parser = new MockParser([], [
            new TestDrive{
                Name = "Name",
                Alias = "Alias",
                RootFolder = new TestFolder{
                    Name = "Name",
                    Folders = [],
                    Files = [
                        new TestFile{
                            Name = "Compressed.txt",
                            StorageType = StorageType.StreamCompress,
                            Modified = timeProvider.GetLocalNow(),
                            FileContent = ""
                        }
                    ]
                }
            }
        ]);

        // TODO: Add writing into the file.
        using (var archive = new SgaArchive(stream, SgaMode.Create, SgaVersion.V2, parser, false, timeProvider))
        {
            SgaDrive drive = archive.AddDrive("Alias", "Name");
            
            SgaFile compressedFile = drive.RootFolder.AddFile("Compressed.txt", StorageType.StreamCompress);

            Assert.Equal(StorageType.StreamCompress, compressedFile.StorageType);
        }
    }

    [Fact]
    public void AddNestedFolders_InCreateMode_CreatesHierarchySuccessfully()
    {
        var stream = new MemoryStream();
        var parser = new MockParser([], [
            new TestDrive{
                Name = "Name",
                Alias = "Alias",
                RootFolder = new TestFolder{
                    Name = "Name",
                    Folders = [
                        new TestFolder{
                            Name = "Folder1",
                            Folders = [
                                new TestFolder{
                                    Name = "Folder2",
                                    Folders = [],
                                    Files = [
                                        new TestFile{
                                            Name = "File.txt",
                                            StorageType = StorageType.Uncompress,
                                            FileContent = ""
                                        }
                                    ]
                                }
                            ],
                            Files = []
                        }
                    ],
                    Files = []
                }
            }
        ]);

        using (var archive = new SgaArchive(stream, SgaMode.Create, SgaVersion.V2, parser))
        {
            SgaDrive drive = archive.AddDrive("Alias", "Name");
            SgaFolder folder1 = drive.RootFolder.AddFolder("Folder1");
            SgaFolder folder2 = folder1.AddFolder("Folder2");
            SgaFile file = folder2.AddFile("File.txt", StorageType.Uncompress);

            Assert.Single(drive.RootFolder.Contents);
            Assert.Single(folder1.Contents);
            Assert.Single(folder2.Contents);
            
            Assert.Same(drive.RootFolder, folder1.Parent);
            Assert.Same(folder1, folder2.Parent);
            Assert.Same(folder2, file.Parent);
        }
    }


    #endregion

    #region Edit Tests

    [Fact]
    public void ChangeName_InEditMode_ChangesPathAsWell()
    {
        var stream = new MemoryStream();
        var parser = new MockParser([
            new TestDrive{
                Name = "DriveName",
                Alias = "DriveAlias",
                RootFolder = new TestFolder{
                    Name = "",
                    Folders = [new TestFolder{
                        Name= "SubFolder",
                        Folders = [new TestFolder{
                            Name = "SubSubFolder",
                            Folders = [],
                            Files = []
                        }],
                        Files = []
                    }],
                    Files = []
                }
            }
        ],[
            new TestDrive{
                Name = "DriveName",
                Alias = "DriveAlias",
                RootFolder = new TestFolder{
                    Name = "",
                    Folders = [new TestFolder{
                        Name= "Data",
                        Folders = [new TestFolder{
                            Name = "MyFolder",
                            Folders = [],
                            Files = []
                        }],
                        Files = []
                    }],
                    Files = []
                }
            }
        ]);

        using (var archive = new SgaArchive(stream, SgaMode.Write, SgaVersion.V2, parser))
        {
            SgaDrive drive = archive.Drives[0];
            SgaFolder data = (SgaFolder)drive.RootFolder.Contents[0];
            SgaFolder myFolder = (SgaFolder)data.Contents[0];

            Assert.Equal("SubFolder", data.Name);
            Assert.Equal("SubFolder", data.Path);

            Assert.Equal("SubSubFolder", myFolder.Name);
            Assert.Equal("SubFolder\\SubSubFolder", myFolder.Path);

            data.Name = "Data";
            myFolder.Name = "MyFolder";

            Assert.Equal("Data", data.Name);
            Assert.Equal("Data", data.Path);

            Assert.Equal("MyFolder", myFolder.Name);
            Assert.Equal("Data\\MyFolder", myFolder.Path);

        }
    }

    [Fact]
    public void ChangeFileContents_AndReopenTheFile()
    {
        var stream = new MemoryStream();
        var parser = new MockParser([
            new TestDrive{
                Name = "DriveName",
                Alias = "DriveAlias",
                RootFolder = new TestFolder{
                    Name = "",
                    Folders = [new TestFolder{
                        Name= "Data",
                        Folders = [],
                        Files = []
                    }],
                    Files = []
                }
            }
        ],[
            new TestDrive{
                Name = "DriveName",
                Alias = "DriveAlias",
                RootFolder = new TestFolder{
                    Name = "",
                    Folders = [new TestFolder{
                        Name= "Data",
                        Folders = [],
                        Files = [new TestFile{
                            Name= "File1",
                            StorageType = StorageType.StreamCompress,
                            FileContent = "File Contents"
                        }]
                    }],
                    Files = []
                }
            }
        ]);

        using (var archive = new SgaArchive(stream, SgaMode.Write, SgaVersion.V2, parser))
        {
            SgaFolder dataDrive = (SgaFolder)archive.GetDrive("DriveName")!.RootFolder!.Contents[0];

            SgaFile file = dataDrive.AddFile("File1", StorageType.StreamCompress);

            using(var contents = file.Open()){
                byte[] inputBytes = Encoding.UTF8.GetBytes("File Contents");
                contents.Write(inputBytes);
            }

            using(var contents = file.Open())
            {
                byte[] outputBytes = new byte[file.Size];
                contents.ReadExactly(outputBytes);
                Assert.Equal("File Contents", Encoding.Default.GetString(outputBytes));
            }
        }
    }

    [Fact]
    public void ChangeFileStorageType_ToCompressed_CompressesContents()
    {
        var stream = new MemoryStream();
        var parser = new MockParser([],[
            new TestDrive{
                Name = "DriveName",
                Alias = "DriveAlias",
                RootFolder = new TestFolder{
                    Name = "",
                    Folders = [new TestFolder{
                        Name= "Data",
                        Folders = [],
                        Files = [new TestFile{
                            Name= "File1",
                            StorageType = StorageType.StreamCompress,
                            FileContent = "File Contents"
                        }]
                    }],
                    Files = []
                }
            }
        ]);

        using (var archive = new SgaArchive(stream, SgaMode.Write, SgaVersion.V2, parser))
        {
            SgaDrive drive = archive.AddDrive("DriveAlias", "DriveName");
            drive.RootFolder.Name = "";
            SgaFolder data = drive.RootFolder.AddFolder("Data");
            SgaFile file1 = data.AddFile("File1", StorageType.Uncompress);
            
            using(var contents = file1.Open()){
                byte[] inputBytes = Encoding.UTF8.GetBytes("File Contents");
                contents.Write(inputBytes);
            }

            file1.StorageType = StorageType.StreamCompress;
            
        }
    }

    [Fact]
    public void ChangeFileStorageType_ToUncompressed_DecompressesContents()
    {
        var stream = new MemoryStream();
        var parser = new MockParser([],[
            new TestDrive{
                Name = "DriveName",
                Alias = "DriveAlias",
                RootFolder = new TestFolder{
                    Name = "",
                    Folders = [new TestFolder{
                        Name= "Data",
                        Folders = [],
                        Files = [new TestFile{
                            Name= "File1",
                            StorageType = StorageType.Uncompress,
                            FileContent = "File Contents"
                        }]
                    }],
                    Files = []
                }
            }
        ]);

        using (var archive = new SgaArchive(stream, SgaMode.Write, SgaVersion.V2, parser))
        {
            SgaDrive drive = archive.AddDrive("DriveAlias", "DriveName");
            drive.RootFolder.Name = "";
            SgaFolder data = drive.RootFolder.AddFolder("Data");
            SgaFile file1 = data.AddFile("File1", StorageType.StreamCompress);
            
            using(var contents = file1.Open()){
                byte[] inputBytes = Encoding.UTF8.GetBytes("File Contents");
                contents.Write(inputBytes);
            }

            file1.StorageType = StorageType.Uncompress;   
        }
    }

    [Fact]
    public void ChangeFileStorageType_ToCompressed_WhenContentNotLoaded_CompressesContents()
    {
        var stream = new MemoryStream();
        var parser = new MockParser([
            new TestDrive{
                Name = "DriveName",
                Alias = "DriveAlias",
                RootFolder = new TestFolder{
                    Name = "",
                    Folders = [new TestFolder{
                        Name= "Data",
                        Folders = [],
                        Files = [new TestFile{
                            Name= "File1",
                            StorageType = StorageType.Uncompress,
                            FileContent = "File Contents"
                        }]
                    }],
                    Files = []
                }
            }
        ],[
            new TestDrive{
                Name = "DriveName",
                Alias = "DriveAlias",
                RootFolder = new TestFolder{
                    Name = "",
                    Folders = [new TestFolder{
                        Name= "Data",
                        Folders = [],
                        Files = [new TestFile{
                            Name= "File1",
                            StorageType = StorageType.StreamCompress,
                            FileContent = "File Contents"
                        }]
                    }],
                    Files = []
                }
            }
        ]);

        using (var archive = new SgaArchive(stream, SgaMode.Write, SgaVersion.V2, parser))
        {
            SgaDrive drive = archive.GetDrive("DriveAlias")!;
            SgaFolder data = (SgaFolder)drive.RootFolder.Contents[0];
            SgaFile file1 = (SgaFile)data.Contents[0];

            file1.StorageType = StorageType.StreamCompress;
        }
    }

    [Fact]
    public void ChangeFileStorageType_ToUncompressed_WhenContentNotLoaded_DecompressesContents()
    {
                var stream = new MemoryStream();
        var parser = new MockParser([
            new TestDrive{
                Name = "DriveName",
                Alias = "DriveAlias",
                RootFolder = new TestFolder{
                    Name = "",
                    Folders = [new TestFolder{
                        Name= "Data",
                        Folders = [],
                        Files = [new TestFile{
                            Name= "File1",
                            StorageType = StorageType.StreamCompress,
                            FileContent = "File Contents"
                        }]
                    }],
                    Files = []
                }
            }
        ],[
            new TestDrive{
                Name = "DriveName",
                Alias = "DriveAlias",
                RootFolder = new TestFolder{
                    Name = "",
                    Folders = [new TestFolder{
                        Name= "Data",
                        Folders = [],
                        Files = [new TestFile{
                            Name= "File1",
                            StorageType = StorageType.Uncompress,
                            FileContent = "File Contents"
                        }]
                    }],
                    Files = []
                }
            }
        ]);

        using (var archive = new SgaArchive(stream, SgaMode.Write, SgaVersion.V2, parser))
        {
            SgaDrive drive = archive.GetDrive("DriveAlias")!;
            SgaFolder data = (SgaFolder)drive.RootFolder.Contents[0];
            SgaFile file1 = (SgaFile)data.Contents[0];

            file1.StorageType = StorageType.Uncompress;
        }
    }

    #endregion

    #region Delete Tests

    [Fact]
    public void DeleteFolder_InCreateMode_RemovesFolderSuccessfully()
    {
        var stream = new MemoryStream();
        var parser = new MockParser([], [
            new TestDrive{
                Name = "Name",
                Alias = "Alias",
                RootFolder = new TestFolder{
                    Name = "Name",
                    Folders = [ new TestFolder{
                        Name = "Folder2",
                        Folders = [],
                        Files = []
                    }],
                    Files = []
                }
            }
        ]);

        using (var archive = new SgaArchive(stream, SgaMode.Create, SgaVersion.V2, parser))
        {
            SgaDrive drive = archive.AddDrive("Alias", "Name");
            SgaFolder folder1 = drive.RootFolder.AddFolder("Folder1");
            SgaFolder folder2 = drive.RootFolder.AddFolder("Folder2");

            Assert.Equal(2, drive.RootFolder.Contents.Count);

            folder1.Delete();

            Assert.Single(drive.RootFolder.Contents);
            Assert.Same(folder2, drive.RootFolder.Contents[0]);
            Assert.Null(folder1.Parent);
            Assert.Null(folder1.Drive);
        }
    }

    [Fact]
    public void DeleteFolder_InReadMode_ThrowsNotSupportedException()
    {
        var drive = new TestDrive
        {
            Name = "Drive1",
            Alias = "Drive1",
            RootFolder = new TestFolder { Name = "Drive1", Folders = [new TestFolder { Name = "SubFolder", Folders = [], Files = [] }], Files = [] }
        };
        var parser = new MockParser([drive], [drive]);
        var stream = new MemoryStream();

        using (var archive = new SgaArchive(stream, SgaMode.Read, SgaVersion.V2, parser))
        {
            var subFolder = (SgaFolder)archive.Drives[0].RootFolder.Contents[0];
            Assert.Throws<NotSupportedException>(subFolder.Delete);
        }
    }

    [Fact]
    public void DeleteFolder_WithContents_DeletesAllContentsRecursively()
    {
        var stream = new MemoryStream();
        var parser = new MockParser([], [
            new TestDrive{
                Name = "Name",
                Alias = "Alias",
                RootFolder = new TestFolder{
                    Name = "Name",
                    Files = [],
                    Folders = []
                }
            }
        ]);

        using (var archive = new SgaArchive(stream, SgaMode.Create, SgaVersion.V2, parser))
        {
            SgaDrive drive = archive.AddDrive("Alias", "Name");
            SgaFolder folder1 = drive.RootFolder.AddFolder("Folder1");
            SgaFolder folder2 = folder1.AddFolder("Folder2");
            SgaFile file = folder2.AddFile("File.txt", StorageType.Uncompress);

            folder1.Delete();

            Assert.Empty(drive.RootFolder.Contents);
            Assert.Null(folder1.Parent);
            Assert.Null(folder1.Drive);
            Assert.Null(folder2.Drive);
            Assert.Null(file.Drive);
        }
    }

    [Fact]
    public void DeleteFile_InCreateMode_RemovesFileSuccessfully()
    {
        var stream = new MemoryStream();
        var parser = new MockParser([], [
            new TestDrive{
                Name = "Name",
                Alias = "Alias",
                RootFolder = new TestFolder{
                    Name = "Name",
                    Folders = [],
                    Files = [
                        new TestFile{
                            Name = "File2.txt",
                            StorageType = StorageType.Uncompress,
                            FileContent = ""
                        }
                    ]
                }
            }
        ]);

        using (var archive = new SgaArchive(stream, SgaMode.Create, SgaVersion.V2, parser))
        {
            SgaDrive drive = archive.AddDrive("Alias", "Name");
            SgaFile file1 = drive.RootFolder.AddFile("File1.txt", StorageType.Uncompress);
            SgaFile file2 = drive.RootFolder.AddFile("File2.txt", StorageType.Uncompress);

            Assert.Equal(2, drive.RootFolder.Contents.Count);

            file1.Delete();

            Assert.Single(drive.RootFolder.Contents);
            Assert.Same(file2, drive.RootFolder.Contents[0]);
            Assert.Null(file1.Parent);
            Assert.Null(file1.Drive);
        }
    }

    [Fact]
    public void DeleteFile_InReadMode_ThrowsNotSupportedException()
    {
        var drive = new TestDrive
        {
            Name = "Drive1",
            Alias = "Drive1",
            RootFolder = new TestFolder { Name = "Drive1", Folders = [], Files = [new TestFile { Name = "File.txt", StorageType = StorageType.Uncompress, FileContent = "Content" }] }
        };
        var parser = new MockParser([drive], [drive]);
        var stream = new MemoryStream();

        using (var archive = new SgaArchive(stream, SgaMode.Read, SgaVersion.V2, parser))
        {
            var file = (SgaFile)archive.Drives[0].RootFolder.Contents[0];
            Assert.Throws<NotSupportedException>(file.Delete);
        }
    }

    [Fact]
    public void DeleteFile_TwiceFromDifferentReferences_SecondDeleteThrows()
    {
        var stream = new MemoryStream();
        var parser = new MockParser([], [
            new TestDrive{
                Name = "Name",
                Alias = "Alias",
                RootFolder = new TestFolder{
                    Name = "Name",
                    Folders = [],
                    Files = []
                }
            }
        ]);

        using (var archive = new SgaArchive(stream, SgaMode.Create, SgaVersion.V2, parser))
        {
            SgaDrive drive = archive.AddDrive("Alias", "Name");
            SgaFile file = drive.RootFolder.AddFile("File.txt", StorageType.Uncompress);

            file.Delete();
            
            // Second delete should throw ObjectDisposedException because file was deleted
            Assert.Throws<ObjectDisposedException>(() => file.Delete());
        }
    }

    [Fact]
    public void DeleteFolder_TwiceFromDifferentReferences_SecondDeleteThrows()
    {
        var stream = new MemoryStream();
        var parser = new MockParser([], [
            new TestDrive{
                Name = "Name",
                Alias = "Alias",
                RootFolder = new TestFolder{
                    Name = "Name",
                    Folders = [],
                    Files = []
                }
            }
        ]);

        using (var archive = new SgaArchive(stream, SgaMode.Create, SgaVersion.V2, parser))
        {
            SgaDrive drive = archive.AddDrive("Alias", "Name");
            SgaFolder folder = drive.RootFolder.AddFolder("Folder");

            folder.Delete();
            
            // Second delete should throw ObjectDisposedException because folder was deleted
            Assert.Throws<ObjectDisposedException>(() => folder.Delete());
        }
    }

    #endregion
*/
}
