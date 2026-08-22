using System.IO.Hashing;
using System.Reflection.Metadata;
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

            Assert.Throws<InvalidOperationException>(() => archive.ArchiveName = "New Archive name.");
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

            Assert.Throws<InvalidOperationException>(()=> archive.AddDrive("alias", "name"));
            Assert.Throws<InvalidOperationException>(()=> drive.Alias = "");
            Assert.Throws<InvalidOperationException>(()=> drive.Name = "");
            Assert.Throws<InvalidOperationException>(drive.Delete);
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

    // This test tests if Add folder function works and creates new folder.
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
            Assert.Same(newFolder, drive.RootFolder.Contents.First());
            Assert.Equal("NewFolder", newFolder.Name);
            Assert.Equal("Drive-Name\\NewFolder", newFolder.Path);
            Assert.Same(drive.RootFolder, newFolder.Parent);
            Assert.Same(drive, newFolder.Drive);
        }
    }

    // This tests if the add folder function throws when have invalid arguments.
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

    // This tests if the folder name can be updated and the path updates as well.
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
            
            SgaFolder? subFolder = folder.GetEntry("Subfolder") as SgaFolder;

            Assert.Equal("SubFolder", subFolder!.Name);
            Assert.Equal("Drive-Name\\SubFolder", subFolder.Path);

            folder.Name = "Updated";
            Assert.Equal("Updated", folder.Name);
            Assert.Equal("Updated", folder.Path);
            Assert.Equal("Updated\\SubFolder", subFolder.Path);
        }
    }

    // This tests if folder delete function work and deletes the folder and all its content
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

        using (var archive = new SgaArchive(archiveStream, SgaMode.Write, SgaVersion.V2, parser, false, timeProvider))
        {
            SgaDrive drive = archive.GetDrive("Drive-Name")!;
            SgaFolder folder = (SgaFolder)drive.RootFolder.GetEntry("SubFolder")!;
            SgaFolder subFolder = (SgaFolder)drive.RootFolder.GetEntry("SubFolder/SubSubFolder")!;
            SgaFile subFile = (SgaFile)drive.RootFolder.GetEntry("SubFolder/testFile")!;
            
            folder.Delete();

            // Test if the folder is deleted
            Assert.Throws<ObjectDisposedException>(() => folder.Name);
            Assert.Throws<ObjectDisposedException>(() => folder.Name = "");
            Assert.Throws<ObjectDisposedException>(() => folder.Path);
            Assert.Throws<ObjectDisposedException>(() => folder.Contents);
            Assert.Null(folder.Parent);
            Assert.Null(folder.Drive);
            Assert.Empty(drive.RootFolder.Contents);

            // Test if the subfolder is deleted
            Assert.Throws<ObjectDisposedException>(() => subFolder.Name);
            Assert.Throws<ObjectDisposedException>(() => subFolder.Name = "");
            Assert.Throws<ObjectDisposedException>(() => subFolder.Path);
            Assert.Throws<ObjectDisposedException>(() => subFolder.Contents);
            Assert.Null(subFolder.Parent);
            Assert.Null(subFolder.Drive);

            // Test if the subFile is deleted
            Assert.Null(subFile.Parent);
            Assert.Null(subFile.Drive);
        }
    }

     // Test if the root folder only deleted the content and not the folder it self. Root folder cannot be deleted.
    [Fact]
    public void SgaFolder_Remove_RootFolder()
    {
        Stream archiveStream = new MemoryStream();
        ISgaParser parser = new MockParser("testArchive", [
            new TestDrive{
                Alias = "Drive-Alias",
                Name = "Drive-Name",
                RootFolder = new TestFolder{
                    Name = "RootFolder",
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

        using (var archive = new SgaArchive(archiveStream, SgaMode.Write, SgaVersion.V2, parser, false, timeProvider))
        {
            SgaDrive drive = archive.GetDrive("Drive-Name")!;
            SgaFolder folder = drive.RootFolder;

            folder.Delete();

            Assert.Empty(folder.Contents);
            Assert.Equal(drive.Name, folder.Name);
            Assert.Equal(drive.RootFolder, folder);
        }
    }

    // This tests if any action that would result in archive modification throws when archive is read only.
    [Fact]
    public void SgaFolder_UpdateFolder_ThrowsWhenReadonly()
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

        using (var archive = new SgaArchive(archiveStream, SgaMode.Read, SgaVersion.V2, parser, false, timeProvider))
        {
            SgaDrive drive = archive.GetDrive("Drive-Name")!;
            SgaFolder folder = drive.RootFolder;

            Assert.Throws<InvalidOperationException>(() => folder.AddFolder("New Folder"));
            Assert.Throws<InvalidOperationException>(() => folder.Name = "changed");
            Assert.Throws<InvalidOperationException>(folder.Delete);
        }
    }

    // This tests if a disposed archive throws when user tries to change any part of a folder in the disposed archive.
    [Fact]
    public void SgaFolder_UpdateFolder_ThrowsWhenDisposed()
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
        var archive = new SgaArchive(archiveStream, SgaMode.Read, SgaVersion.V2, parser, false, timeProvider);
        SgaDrive drive = archive.GetDrive("Drive-Name")!;
        SgaFolder folder = drive.RootFolder;
        archive.Dispose();

        Assert.Throws<ObjectDisposedException>(() => folder.Name);
        Assert.Throws<ObjectDisposedException>(() => folder.Name = "");
        Assert.Throws<ObjectDisposedException>(() => folder.Path);
        Assert.Throws<ObjectDisposedException>(() => folder.Contents);
        Assert.Throws<ObjectDisposedException>(() => folder.AddFolder("NewFolder"));
        Assert.Throws<ObjectDisposedException>(folder.Delete);
    }

    #endregion
    #region Files

    // This tests if the Add file function works correctly with valid arguments
    [Fact]
    public void SgaFile_CreateFile_ValidArguments()
    {
        Stream archiveStream = new MemoryStream();
        TimeProvider timeProvider = new MockTimeProvider(DateTimeOffset.Now);
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
                    Files = [new TestFile{
                        Name = "New file",
                        StorageType = StorageType.Uncompress,
                        Modified = timeProvider.GetLocalNow(),
                        FileContent = ""
                    }]
                }
            }
        ]);
        using (var archive = new SgaArchive(archiveStream, SgaMode.Write, SgaVersion.V2, parser, false, timeProvider))
        {
            SgaDrive drive = archive.GetDrive("Drive-Name")!;
            SgaFolder folder = drive.RootFolder;

            SgaFile file = folder.AddFile("New file", StorageType.Uncompress);

            Assert.Equal("New file", file.Name);
            Assert.Equal(StorageType.Uncompress, file.StorageType);
            Assert.Equal(timeProvider.GetLocalNow(), file.Modified);
            Assert.Equal(0u, file.Size);
            Assert.Equal(0u, file.CompressedSize);
            Assert.Equal(0u, file.Crc);
        }
    }
    
    // This tests if the add file function correctly reject(throw) invalid arguments.
    [Fact]
    public void SgaFile_CreateFile_InvalidArguments()
    {
        Stream archiveStream = new MemoryStream();
        TimeProvider timeProvider = new MockTimeProvider(DateTimeOffset.Now);
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
        using (var archive = new SgaArchive(archiveStream, SgaMode.Write, SgaVersion.V2, parser, false, timeProvider))
        {
            SgaDrive drive = archive.GetDrive("Drive-Name")!;
            SgaFolder folder = drive.RootFolder;

            #pragma warning disable CS8625
            Assert.Throws<ArgumentNullException>(() => folder.AddFile(null, StorageType.Uncompress));
            Assert.Throws<ArgumentOutOfRangeException>(() => folder.AddFile("Test", (StorageType)256));
            #pragma warning restore CS8625
        }
    }
    
    // This tests if the file Name and Modified can be modified
    [Fact]
    public void SgaFile_UpdateFileData()
    {
        Stream archiveStream = new MemoryStream();
        TimeProvider timeProvider = new MockTimeProvider(DateTimeOffset.Now);
        ISgaParser parser = new MockParser("testArchive", [
            new TestDrive{
                Alias = "Drive-Alias",
                Name = "Drive-Name",
                RootFolder = new TestFolder{
                    Name = "Drive-Name",
                    Folders = [],
                    Files = [
                        new TestFile{
                            Name = "testFile",
                            StorageType = StorageType.Uncompress,
                            Modified = DateTimeOffset.Now,
                            FileContent = ""
                        }
                    ]
                }
            }
        ], [
            new TestDrive{
                Alias = "Drive-Alias",
                Name = "Drive-Name",
                RootFolder = new TestFolder{
                    Name = "Drive-Name",
                    Folders = [],
                    Files = [
                        new TestFile{
                            Name = "Updated",
                            StorageType = StorageType.Uncompress,
                            Modified = timeProvider.GetLocalNow(),
                            FileContent = ""
                        }
                    ]
                }
            }
        ]);

        using (var archive = new SgaArchive(archiveStream, SgaMode.Write, SgaVersion.V2, parser, false, timeProvider))
        {
            SgaDrive drive = archive.GetDrive("Drive-Name")!;
            SgaFolder folder = drive.RootFolder;
            SgaFile file = (SgaFile)drive.RootFolder.GetEntry("testFile")!;

            Assert.Equal("testFile", file.Name);
            Assert.Equal("Drive-Name\\testFile", file.Path);

            file.Modified = timeProvider.GetLocalNow();
            file.Name = "Updated";

            Assert.Equal("Updated", file.Name);
            Assert.Equal("Drive-Name\\Updated", file.Path);
            Assert.Equal(timeProvider.GetLocalNow(), file.Modified);
        }
    }

    // This tests if the file contents changes when i open and change the content stream.
    [Fact]
    public void SgaFile_UpdateFileContents()
    {
        Stream archiveStream = new MemoryStream();
        TimeProvider timeProvider = new MockTimeProvider(DateTimeOffset.Now);
        ISgaParser parser = new MockParser("testArchive", [
            new TestDrive{
                Alias = "Drive-Alias",
                Name = "Drive-Name",
                RootFolder = new TestFolder{
                    Name = "Drive-Name",
                    Folders = [],
                    Files = [
                        new TestFile{
                            Name = "testFile",
                            StorageType = StorageType.Uncompress,
                            Modified = DateTimeOffset.Now,
                            FileContent = ""
                        }
                    ]
                }
            }
        ], [
            new TestDrive{
                Alias = "Drive-Alias",
                Name = "Drive-Name",
                RootFolder = new TestFolder{
                    Name = "Drive-Name",
                    Folders = [],
                    Files = [
                        new TestFile{
                            Name = "testFile",
                            StorageType = StorageType.Uncompress,
                            Modified = timeProvider.GetLocalNow(),
                            FileContent = "This is a file contents"
                        }
                    ]
                }
            }
        ]);

        using var archive = new SgaArchive(archiveStream, SgaMode.Write, SgaVersion.V2, parser, false, timeProvider);
        SgaDrive drive = archive.GetDrive("Drive-Name")!;
        SgaFile file = (SgaFile)drive.RootFolder.GetEntry("testFile")!;
        
        byte[] content = Encoding.UTF8.GetBytes("This is a file contents");
        uint crc =  Crc32.HashToUInt32(content);


        using ( var fileContents = file.Open())
        {
            fileContents.Write(content);
        }

        Assert.Equal(23u, file.Size);
        Assert.Equal(23u, file.CompressedSize);
        Assert.Equal(crc, file.Crc);
        Assert.Equal(timeProvider.GetLocalNow(), file.Modified);
    }

    // This tests if compressed and uncompressed files could be open and read in read only mode.
    [Fact]
    public void SgaFile_Open_ReadOnlyFile()
    {
        Stream archiveStream = new MemoryStream();
        TimeProvider timeProvider = new MockTimeProvider(DateTimeOffset.Now);
        ISgaParser parser = new MockParser("testArchive", [
            new TestDrive{
                Alias = "Drive-Alias",
                Name = "Drive-Name",
                RootFolder = new TestFolder{
                    Name = "Drive-Name",
                    Folders = [],
                    Files = [
                        new TestFile{
                            Name = "uncompressedFile",
                            StorageType = StorageType.Uncompress,
                            Modified = timeProvider.GetLocalNow(),
                            FileContent = "This is a file contents"
                        },
                        new TestFile{
                            Name = "compressedTestFile",
                            StorageType = StorageType.StreamCompress,
                            Modified = timeProvider.GetLocalNow(),
                            FileContent = "This is a compressed file contents"
                        }
                    ]
                }
            }
        ], [
            new TestDrive{
                Alias = "Drive-Alias",
                Name = "Drive-Name",
                RootFolder = new TestFolder{
                    Name = "Drive-Name",
                    Folders = [],
                    Files = [
                        new TestFile{
                            Name = "uncompressedFile",
                            StorageType = StorageType.Uncompress,
                            Modified = timeProvider.GetLocalNow(),
                            FileContent = "This is a file contents"
                        },
                        new TestFile{
                            Name = "compressedTestFile",
                            StorageType = StorageType.StreamCompress,
                            Modified = timeProvider.GetLocalNow(),
                            FileContent = "This is a compressed file contents"
                        }
                    ]
                }
            }
        ]);

        using var archive = new SgaArchive(archiveStream, SgaMode.Read, SgaVersion.V2, parser, false, timeProvider);
        SgaDrive drive = archive.GetDrive("Drive-Name")!;
        SgaFile UncompressedFile = (SgaFile)drive.RootFolder.GetEntry("uncompressedFile")!;
        SgaFile compressedFile = (SgaFile)drive.RootFolder.GetEntry("compressedTestFile")!;
        
        // Uncompressed file
        var content = "This is a file contents"u8;
        uint crc =  Crc32.HashToUInt32(content);

        Assert.Equal(23u, UncompressedFile.Size);
        Assert.Equal(23u, UncompressedFile.CompressedSize);
        Assert.Equal(crc, UncompressedFile.Crc);

        using ( var fileContents = UncompressedFile.Open())
        {
            Span<byte> contentsBuffer = stackalloc byte[23];
            fileContents.ReadExactly(contentsBuffer);

            Assert.Equal(content, contentsBuffer);
        }

        // Compressed file
        var CompressedContent = "This is a compressed file contents"u8;
        uint compressedCrc =  Crc32.HashToUInt32(CompressedContent);

        Assert.Equal(34u, compressedFile.Size);
        Assert.Equal(42u, compressedFile.CompressedSize);
        Assert.Equal(compressedCrc, compressedFile.Crc);

        using ( var fileContents = compressedFile.Open())
        {
            Span<byte> contentsBuffer = stackalloc byte[34];
            fileContents.ReadExactly(contentsBuffer);

            Assert.Equal(CompressedContent, contentsBuffer);
        }
    }

    // This tests if storage type can be changed from uncompress to compressed without braking file contents.
    [Fact]
    public void SgaFile_UpdateStorageType_ToCompressed()
    {
        Stream archiveStream = new MemoryStream();
        TimeProvider timeProvider = new MockTimeProvider(DateTimeOffset.Now);
        ISgaParser parser = new MockParser("testArchive", [
            new TestDrive{
                Alias = "Drive-Alias",
                Name = "Drive-Name",
                RootFolder = new TestFolder{
                    Name = "Drive-Name",
                    Folders = [],
                    Files = [
                        new TestFile{
                            Name = "testFile",
                            StorageType = StorageType.Uncompress,
                            Modified = timeProvider.GetLocalNow(),
                            FileContent = "This is a file contents"
                        },
                    ]
                }
            }
        ], [
            new TestDrive{
                Alias = "Drive-Alias",
                Name = "Drive-Name",
                RootFolder = new TestFolder{
                    Name = "Drive-Name",
                    Folders = [],
                    Files = [
                        new TestFile{
                            Name = "testFile",
                            StorageType = StorageType.StreamCompress,
                            Modified = timeProvider.GetLocalNow(),
                            FileContent = "This is a file contents"
                        }
                    ]
                }
            }
        ]);

        using( var archive = new SgaArchive(archiveStream, SgaMode.Write, SgaVersion.V2, parser, false, timeProvider))
        {
            SgaDrive drive = archive.GetDrive("Drive-Name")!;
            SgaFile testFile = (SgaFile)drive.RootFolder.GetEntry("testFile")!;

            Assert.Equal(StorageType.Uncompress, testFile.StorageType);

            testFile.StorageType = StorageType.StreamCompress;

            Assert.Equal(StorageType.StreamCompress, testFile.StorageType);

            using var fileContents = testFile.Open();
            Span<byte> contentsBuffer = stackalloc byte[23];
            fileContents.ReadExactly(contentsBuffer);

            Assert.Equal("This is a file contents"u8, contentsBuffer);
        }
    }

    // This tests if storage type can be changed from compressed to uncompressed without braking file contents.
    [Fact]
    public void SgaFile_UpdateStorageType_ToUncompressed()
    {
        Stream archiveStream = new MemoryStream();
        TimeProvider timeProvider = new MockTimeProvider(DateTimeOffset.Now);
        ISgaParser parser = new MockParser("testArchive", [
            new TestDrive{
                Alias = "Drive-Alias",
                Name = "Drive-Name",
                RootFolder = new TestFolder{
                    Name = "Drive-Name",
                    Folders = [],
                    Files = [
                        new TestFile{
                            Name = "testFile",
                            StorageType = StorageType.BufferCompress,
                            Modified = timeProvider.GetLocalNow(),
                            FileContent = "This is a file contents"
                        },
                    ]
                }
            }
        ], [
            new TestDrive{
                Alias = "Drive-Alias",
                Name = "Drive-Name",
                RootFolder = new TestFolder{
                    Name = "Drive-Name",
                    Folders = [],
                    Files = [
                        new TestFile{
                            Name = "testFile",
                            StorageType = StorageType.Uncompress,
                            Modified = timeProvider.GetLocalNow(),
                            FileContent = "This is a file contents"
                        }
                    ]
                }
            }
        ]);

        using( var archive = new SgaArchive(archiveStream, SgaMode.Write, SgaVersion.V2, parser, false, timeProvider))
        {
            SgaDrive drive = archive.GetDrive("Drive-Name")!;
            SgaFile testFile = (SgaFile)drive.RootFolder.GetEntry("testFile")!;

            Assert.Equal(StorageType.BufferCompress, testFile.StorageType);

            testFile.StorageType = StorageType.Uncompress;

            Assert.Equal(StorageType.Uncompress, testFile.StorageType);

            using var fileContents = testFile.Open();
            Span<byte> contentsBuffer = stackalloc byte[23];
            fileContents.ReadExactly(contentsBuffer);

            Assert.Equal("This is a file contents"u8, contentsBuffer);
        }
    }

    // This tests if file delete function works.
    [Fact]
    public void SgaFile_Delete()
    {
        Stream archiveStream = new MemoryStream();
        TimeProvider timeProvider = new MockTimeProvider(DateTimeOffset.Now);
        ISgaParser parser = new MockParser("testArchive", [
            new TestDrive{
                Alias = "Drive-Alias",
                Name = "Drive-Name",
                RootFolder = new TestFolder{
                    Name = "Drive-Name",
                    Folders = [],
                    Files = [
                        new TestFile{
                            Name = "testFile",
                            StorageType = StorageType.BufferCompress,
                            Modified = timeProvider.GetLocalNow(),
                            FileContent = "This is a file contents"
                        },
                    ]
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

        using( var archive = new SgaArchive(archiveStream, SgaMode.Write, SgaVersion.V2, parser, false, timeProvider))
        {
            SgaDrive drive = archive.GetDrive("Drive-Name")!;
            SgaFile file = (SgaFile)drive.RootFolder.GetEntry("testFile")!;

            file.Delete();

            // Test if the folder is deleted
            Assert.Throws<ObjectDisposedException>(() => file.Name);
            Assert.Throws<ObjectDisposedException>(() => file.Name = "");
            Assert.Throws<ObjectDisposedException>(() => file.Path);
            Assert.Throws<ObjectDisposedException>(() => file.Modified);
            Assert.Throws<ObjectDisposedException>(() => file.Modified = DateTimeOffset.Now);
            Assert.Throws<ObjectDisposedException>(() => file.Crc);
            Assert.Throws<ObjectDisposedException>(() => file.StorageType);
            Assert.Throws<ObjectDisposedException>(() => file.StorageType = StorageType.Uncompress);
            Assert.Null(file.Parent);
            Assert.Null(file.Drive);
            Assert.Empty(drive.RootFolder.Contents);
        }
    }

    // Tests if all write functions prevent writing when the archive was open in read only mode.
    [Fact]
    public void SgaFile_UpdateFile_ThrowsWhenReadonly()
    {
        Stream archiveStream = new MemoryStream();
        TimeProvider timeProvider = new MockTimeProvider(DateTimeOffset.Now);
        ISgaParser parser = new MockParser("testArchive", [
            new TestDrive{
                Alias = "Drive-Alias",
                Name = "Drive-Name",
                RootFolder = new TestFolder{
                    Name = "Drive-Name",
                    Folders = [],
                    Files = [
                        new TestFile{
                            Name = "testFile",
                            StorageType = StorageType.Uncompress,
                            Modified = timeProvider.GetLocalNow(),
                            FileContent = "This is a file contents"
                        },
                    ]
                }
            }
        ], [
            new TestDrive{
                Alias = "Drive-Alias",
                Name = "Drive-Name",
                RootFolder = new TestFolder{
                    Name = "Drive-Name",
                    Folders = [],
                    Files = [
                        new TestFile{
                            Name = "testFile",
                            StorageType = StorageType.Uncompress,
                            Modified = timeProvider.GetLocalNow(),
                            FileContent = "This is a file contents"
                        },
                    ]
                }
            }
        ]);

        using( var archive = new SgaArchive(archiveStream, SgaMode.Read, SgaVersion.V2, parser, false, timeProvider))
        {
            SgaDrive drive = archive.GetDrive("Drive-Name")!;
            SgaFile file = (SgaFile)drive.RootFolder.GetEntry("testFile")!;

            Assert.Throws<InvalidOperationException>(() => drive.RootFolder.AddFile("New File", StorageType.Uncompress));
            Assert.Throws<InvalidOperationException>(() => file.Name = "changed");
            Assert.Throws<InvalidOperationException>(() => file.Modified = DateTimeOffset.Now);
            Assert.Throws<InvalidOperationException>(() => file.StorageType = StorageType.Uncompress);
            Assert.Throws<InvalidOperationException>(file.Delete);

            using Stream fileContents = file.Open();
            Assert.Throws<NotSupportedException>(() => fileContents.Write(" HI!"u8));
        }
    }

    // This tests if a disposed archive throws when user tries to change any part of a file in the disposed archive.
    [Fact]
    public void SgaFile_UpdateFile_ThrowsWhenDisposed()
    {
        Stream archiveStream = new MemoryStream();
        TimeProvider timeProvider = new MockTimeProvider(DateTimeOffset.Now);
        ISgaParser parser = new MockParser("testArchive", [
            new TestDrive{
                Alias = "Drive-Alias",
                Name = "Drive-Name",
                RootFolder = new TestFolder{
                    Name = "Drive-Name",
                    Folders = [],
                    Files = [
                        new TestFile{
                            Name = "testFile",
                            StorageType = StorageType.BufferCompress,
                            Modified = timeProvider.GetLocalNow(),
                            FileContent = "This is a file contents"
                        },
                    ]
                }
            }
        ], [
            new TestDrive{
                Alias = "Drive-Alias",
                Name = "Drive-Name",
                RootFolder = new TestFolder{
                    Name = "Drive-Name",
                    Folders = [],
                    Files = [
                        new TestFile{
                            Name = "testFile",
                            StorageType = StorageType.BufferCompress,
                            Modified = timeProvider.GetLocalNow(),
                            FileContent = "This is a file contents"
                        },
                    ]
                }
            }
        ]);

        SgaArchive archive = new SgaArchive(archiveStream, SgaMode.Read, SgaVersion.V2, parser, false, timeProvider);
        SgaDrive drive = archive.GetDrive("Drive-Name")!;
        SgaFile file = (SgaFile)drive.RootFolder.GetEntry("testFile")!;

        archive.Dispose();

        Assert.Throws<ObjectDisposedException>(() => file.Name);
        Assert.Throws<ObjectDisposedException>(() => file.Name = "");
        Assert.Throws<ObjectDisposedException>(() => file.Path);
        Assert.Throws<ObjectDisposedException>(() => file.Crc);
        Assert.Throws<ObjectDisposedException>(() => file.Modified);
        Assert.Throws<ObjectDisposedException>(() => file.Modified = DateTimeOffset.Now);
        Assert.Throws<ObjectDisposedException>(() => file.StorageType);
        Assert.Throws<ObjectDisposedException>(() => file.StorageType = StorageType.Uncompress);
        Assert.Throws<ObjectDisposedException>(() => drive.RootFolder.AddFile("NewFolder", StorageType.Uncompress));
        Assert.Throws<ObjectDisposedException>(file.Open);
        Assert.Throws<ObjectDisposedException>(file.Delete);
    }
    
    [Fact]
    public void SgaFile_ExtractToFile_ThrowSExceptionWhenInputInvalid()
    {
        Stream archiveStream = new MemoryStream();
        TimeProvider timeProvider = new MockTimeProvider(DateTimeOffset.Now);
        ISgaParser parser = new MockParser("testArchive", [
            new TestDrive{
                Alias = "Drive-Alias",
                Name = "Drive-Name",
                RootFolder = new TestFolder{
                    Name = "Drive-Name",
                    Folders = [],
                    Files = [
                        new TestFile{
                            Name = "testFile",
                            StorageType = StorageType.Uncompress,
                            Modified = timeProvider.GetLocalNow(),
                            FileContent = "This is a file contents"
                        },
                    ]
                }
            }
        ], [
            new TestDrive{
                Alias = "Drive-Alias",
                Name = "Drive-Name",
                RootFolder = new TestFolder{
                    Name = "Drive-Name",
                    Folders = [],
                    Files = [
                        new TestFile{
                            Name = "testFile",
                            StorageType = StorageType.Uncompress,
                            Modified = timeProvider.GetLocalNow(),
                            FileContent = "This is a file contents"
                        },
                    ]
                }
            }
        ]);

        using( var archive = new SgaArchive(archiveStream, SgaMode.Read, SgaVersion.V2, parser, false, timeProvider))
        {
            SgaDrive drive = archive.GetDrive("Drive-Name")!;
            SgaFile file = (SgaFile)drive.RootFolder.GetEntry("testFile")!;

            Assert.Throws<ArgumentException>(() => file.ExtractToFile("  "));
        }
    }
    #endregion
}
