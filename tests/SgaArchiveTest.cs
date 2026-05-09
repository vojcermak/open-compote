using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
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
        Assert.Throws<ArgumentNullException>(() => new SgaArchive(null, SgaMode.Read, SgaVersion.V2));
        #pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
    }

    [Fact]
    public void Constructor_WithIncorrectStream_ThrowsArgumentException()
    {
        MemoryStream readOnlyStream = new MemoryStream([], false);
        MemoryStream unreadableStream = new MemoryStream();
        unreadableStream.Dispose();

        Assert.Throws<ArgumentException>(() => new SgaArchive(readOnlyStream, SgaMode.Create, SgaVersion.V2, true));
        Assert.Throws<ArgumentException>(() => new SgaArchive(unreadableStream, SgaMode.Read, SgaVersion.V2 ,true));
        Assert.Throws<InvalidDataException>(() => new SgaArchive(new MemoryStream(), SgaMode.Create, (SgaVersion)128));
    }

    [Fact]
    public void Constructor_InitializeWrite_Successfully()
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
        var parser = new MockParser([item],[item]);

        var stream = new MemoryStream();
        using (var Archive =  new SgaArchive(stream, SgaMode.Write, SgaVersion.V2, parser))
        {
            Assert.Equal(SgaMode.Write, Archive.Mode);
            Assert.Equal(SgaVersion.V2, Archive.Version);

            SgaFile file1 = (SgaFile)Archive.Drives[0].RootFolder.Contents[0];
            SgaFile file2 = (SgaFile)Archive.Drives[0].RootFolder.Contents[1];

            using var openFile1 = file1.Open();
            var buffer1 = new byte [file1.Size];
            openFile1.ReadExactly(buffer1);
            Assert.Equal("Hello world file", Encoding.Default.GetString(buffer1));

            using var openFile = file2.Open();
            var buffer = new byte [file2.Size];
            openFile.ReadExactly(buffer);
            Assert.Equal("File 2 contents", Encoding.Default.GetString(buffer));
        }
    }

    [Fact]
    public void Constructor_InitializeRead_ThrowsWhenWrite()
    {
        TestDrive drive = new TestDrive
        {
            Name = "Drive1",
            Alias = "Drive1 Alias",
            RootFolder = new TestFolder
            {
                Name = "Drive1 RootFolder",
                Folders = [],
                Files = [new TestFile{
                    Name = "file1",
                    StorageType = StorageType.Uncompress,
                    FileContent = "Hello world file"
                }]
            } 
        };
        var parser = new MockParser([drive],[drive]);
        var stream = new MemoryStream();

        using (var Archive = new SgaArchive(stream, SgaMode.Read, SgaVersion.V2, parser))
        {
            Assert.Equal(SgaMode.Read, Archive.Mode);
            
            Assert.Equal(SgaVersion.V2, Archive.Version);
            
            // Not implemented, currently no other version then V2 is supported
            //Assert.Throws<NotSupportedException>(() => Archive.Version = SgaVersion.V4);
            
            Assert.Throws<NotSupportedException>(() => Archive.ArchiveName = "TEST");
            
            // Not implemented, Not used by archives with version < 7.
            //Assert.Throws<NotSupportedException>(() => Archive.BlockSize = 42);
            
            Assert.Single(Archive.Drives);
            Assert.Throws<NotSupportedException>(() => Archive.AddDrive("", ""));
        }
    }

    [Fact]
    public void SgaArchive_ThrowsWhenDisposed()
    {
        TestDrive drive = new TestDrive
        {
            Name = "Drive1",
            Alias = "Drive1 Alias",
            RootFolder = new TestFolder
            {
                Name = "Drive1 RootFolder",
                Folders = [],
                Files = [new TestFile{
                    Name = "file1",
                    StorageType = StorageType.Uncompress,
                    FileContent = "Hello world file"
                }]
            } 
        };
        var parser = new MockParser([drive],[drive]);
        var stream = new MemoryStream();
        var Archive = new SgaArchive(stream, SgaMode.Read, SgaVersion.V2, parser);
        Archive.Dispose();

        Assert.Throws<ObjectDisposedException>(() => Archive.ArchiveName);
        Assert.Throws<ObjectDisposedException>(() => Archive.ArchiveName = "TEST");
        Assert.Throws<ObjectDisposedException>(() => Archive.Drives);
        Assert.Throws<ObjectDisposedException>(() => Archive.AddDrive("test", "test"));
        Assert.Throws<ObjectDisposedException>(() => Archive.GetDrive("Test"));
    }

    #region Add Tests

    [Fact]
    public void AddDrive_InCreateMode_AddsNewDriveSuccessfully()
    {
        var stream = new MemoryStream();
        var parser = new MockParser([], [
            new TestDrive{
                Name = "DriveName",
                Alias = "DriveAlias",
                RootFolder = new TestFolder{
                    Name = "DriveName",
                    Folders = [],
                    Files = []
                }
            }
        ]);

        using (var archive = new SgaArchive(stream, SgaMode.Create, SgaVersion.V2, parser))
        {
            Assert.Empty(archive.Drives);

            SgaDrive newDrive = archive.AddDrive("DriveAlias", "DriveName");

            Assert.Single(archive.Drives);
            Assert.Same(newDrive, archive.Drives[0]);
        }
    }

    [Fact]
    public void AddDrive_WithNullParameters_ThrowsArgumentNullException()
    {
        var stream = new MemoryStream();
        var parser = new MockParser([], []);

        using (var archive = new SgaArchive(stream, SgaMode.Create, SgaVersion.V2, parser))
        {
            #pragma warning disable CS8625
            Assert.Throws<ArgumentNullException>(() => archive.AddDrive(null, "Name"));
            Assert.Throws<ArgumentNullException>(() => archive.AddDrive("Alias", null));
            #pragma warning restore CS8625
        }
    }

    [Fact]
    public void AddFolder_InCreateMode_AddsNewFolderSuccessfully()
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
                            Name = "SubFolder",
                            Folders = [],
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
            
            Assert.Empty(drive.RootFolder.Contents);

            SgaFolder newFolder = drive.RootFolder.AddFolder("SubFolder");

            Assert.Single(drive.RootFolder.Contents);
            Assert.Same(newFolder, drive.RootFolder.Contents[0]);
            Assert.Equal("SubFolder", newFolder.Name);
            Assert.Equal("Name\\SubFolder", newFolder.Path);
            Assert.Same(drive.RootFolder, newFolder.Parent);
            Assert.Same(drive, newFolder.Drive);
        }
    }

    [Fact]
    public void AddFolder_InReadMode_ThrowsNotSupportedException()
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
            Assert.Throws<NotSupportedException>(() => archive.Drives[0].RootFolder.AddFolder("NewFolder"));
        }
    }

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
                            FileContent = ""
                        }
                    ]
                }
            }
        ]);

        // TODO: Add writing into the file.
        using (var archive = new SgaArchive(stream, SgaMode.Create, SgaVersion.V2, parser))
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
    public void DeleteDrive_InCreateMode_RemovesDriveSuccessfully()
    {
        var stream = new MemoryStream();
        var parser = new MockParser([
            new TestDrive{
                Name = "Name1",
                Alias = "Drive1",
                RootFolder = new TestFolder{
                    Name = "Name1",
                    Folders = [],
                    Files = []
                }
            },
            new TestDrive{
                Name = "Name2",
                Alias = "Drive2",
                RootFolder = new TestFolder{
                    Name = "Name2",
                    Folders = [],
                    Files = []
                }
            }
        ], [
            new TestDrive{
                Name = "Name2",
                Alias = "Drive2",
                RootFolder = new TestFolder{
                    Name = "Name2",
                    Folders = [],
                    Files = []
                }
            }
        ]);

        using (var archive = new SgaArchive(stream, SgaMode.Write, SgaVersion.V2, parser))
        {
            Assert.Equal(2, archive.Drives.Count);

            SgaDrive drive1 = archive.GetDrive("Name1")!;

            drive1.Delete();

            Assert.Null(drive1.Archive);
        }
    }

    [Fact]
    public void DeleteDrive_InReadMode_ThrowsNotSupportedException()
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
            Assert.Throws<NotSupportedException>(() => archive.Drives[0].Delete());
        }
    }

    [Fact]
    public void DeleteDrive_WithContents_DeletesAllContentsAndDrive()
    {
        var stream = new MemoryStream();
        var parser = new MockParser([], []);

        using (var archive = new SgaArchive(stream, SgaMode.Create, SgaVersion.V2, parser))
        {
            SgaDrive drive = archive.AddDrive("Drive", "Name");
            SgaFolder folder = drive.RootFolder.AddFolder("Folder");
            SgaFile file = folder.AddFile("File.txt", StorageType.Uncompress);

            Assert.NotEmpty(drive.RootFolder.Contents);

            drive.Delete();

            Assert.Empty(archive.Drives);
            Assert.Null(drive.Archive);
        }
    }

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
}

