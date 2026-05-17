using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using Xunit;

namespace OpenCompote.SGA.Tests.Parsers;

public class SgaV2ParserTest
{
    #region Writer tests

    [Fact]
    public void Writer_Pass_SaveEmptyArchive()
    {
        string testFilePath = Path.GetTempFileName();

        try
        {
            using (SgaArchive archive = SgaArchiveFile.Create(testFilePath, SgaVersion.V2, true)){}    

            byte[] expected = File.ReadAllBytes("../../../Parsers/testFiles/Ok/empty.sga");
            byte[] actual = File.ReadAllBytes(testFilePath);

            Assert.Equal(expected, actual);
        }
        finally
        {
            if(File.Exists(testFilePath))
                File.Delete(testFilePath);
        }
    }

    #endregion

    //Writer
    // 1. Opposite for OK 5 - writer can write valid archive
    // 2. Save valid empty archive
    // 4. Save valid archive with only empty folders

    //OK
    // 1. - Parser can open and read valid uncompress file
    // 2. - Parser can open and read valid compress file
    // 2. - Parser can open and read archive with only empty folders
    // 3. - Parser can open and read archive with only empty drives
    // 4. - Parser can open and read archive with no drives
    // 5. - Parser can open and read archive with multiple drives witch multiple nested folder and files
    
    #region Invalid input tests
    
    [Fact] // FileHash is malformed
    public void Parser_Throw_FileHashInvalid()
    {
        var exception = Assert.Throws<InvalidDataException>(() => SgaArchiveFile.Open("../../../Parsers/testFiles/Nok/header-fileHash-invalid.sga", SgaMode.Read));
        Assert.Equal("File hash invalid.", exception.Message);
    }

    [Fact] // TOCHash is malformed
    public void Parser_Throw_TocHashInvalid()
    {
        var exception = Assert.Throws<InvalidDataException>(() => SgaArchiveFile.Open("../../../Parsers/testFiles/Nok/header-tocHash-invalid.sga", SgaMode.Read));
        Assert.Equal("Toc hash invalid.", exception.Message);
    }

    [Fact] // TOC Size is incorrect (Toc size attribute is set to value bigger then its actual size.)
    public void Parser_Throw_TocSizeInvalid()
    {
        var exception = Assert.Throws<InvalidDataException>(() => SgaArchiveFile.Open("../../../Parsers/testFiles/Nok/header-tocSize-invalid.sga", SgaMode.Read));
        Assert.Equal("Toc hash invalid.", exception.Message);
    }

    [Fact] // DataOffset is incorrect (DataOffset attribute is set to value bigger then ist actual value.)
    public void Parser_Throw_DataOffsetInvalid()
    {
        var exception = Assert.Throws<InvalidDataException>(() => SgaArchiveFile.Open("../../../Parsers/testFiles/Nok/header-dataOffset-invalid.sga", SgaMode.Read));
        Assert.Equal("Data offset invalid.", exception.Message);
    }

    [Fact] // DriveOffset invalid (Toc drive offset attribute is set to be bigger then the actual size)
    public void Parser_Throw_DriveOffsetInvalid()
    {
        var exception =  Assert.Throws<InvalidDataException>(() => SgaArchiveFile.Open("../../../Parsers/testFiles/Nok/toc-driveOffset-invalid.sga", SgaMode.Read));
        Assert.Equal("TOC Drive offset invalid.", exception.Message);
    }

    [Fact] // FolderOffset and drive count does not match
    public void Parser_Throw_FolderOffsetInvalid()
    {
        var exception = Assert.Throws<InvalidDataException>(() => SgaArchiveFile.Open("../../../Parsers/testFiles/Nok/toc-folderOffset-invalid.sga", SgaMode.Read));
        Assert.Equal("TOC folder offset invalid.", exception.Message);
    }

    [Fact] // FileOffset and folder count does not match
    public void Parser_Throw_FileOffsetInvalid()
    {
        var exception = Assert.Throws<InvalidDataException>(() => SgaArchiveFile.Open("../../../Parsers/testFiles/Nok/toc-fileOffset-invalid.sga", SgaMode.Read));
        Assert.Equal("TOC file offset invalid.", exception.Message);
    }

    [Fact] // Name Offset and file count does not match
    public void Parser_Throw_NameOffsetInvalid()
    {
        var exception = Assert.Throws<InvalidDataException>(() => SgaArchiveFile.Open("../../../Parsers/testFiles/Nok/toc-nameOffset-invalid.sga", SgaMode.Read));
        Assert.Equal("TOC name offset invalid.", exception.Message);
    }

    [Fact] // Last name have no \0 string terminator set, allowing reading beyond toc.
    public void Parser_Throw_WhenNameReadAfterTOC()
    {
        var exception = Assert.Throws<InvalidDataException>(() => SgaArchiveFile.Open("../../../Parsers/testFiles/Nok/toc-name-noTerminator.sga", SgaMode.Read));
        Assert.Equal("TOC name read after toc.", exception.Message);
    }

    [Fact] // First drive in the archive have FirstFolder set to too big of a value, so the value points outside of the folder array.
    public void Parser_Throw_DriveFirstFolderInvalid()
    {
        var exception = Assert.Throws<InvalidDataException>(() => SgaArchiveFile.Open("../../../Parsers/testFiles/Nok/drive-firstFolder-outofrange.sga", SgaMode.Read));
        Assert.Equal("Drive FirstFolder index is out of range.", exception.Message);
    }

    [Fact] // First drive in the archive have LastFolder set to too big of a value, so the value points outside of the folder array.
    public void Parser_Throw_DriveLastFolderInvalid()
    {
        var exception = Assert.Throws<InvalidDataException>(() => SgaArchiveFile.Open("../../../Parsers/testFiles/Nok/drive-lastFolder-outofrange.sga", SgaMode.Read));
        Assert.Equal("Drive LastFolder index is out of range.", exception.Message);
    }

    [Fact] // First drive in the archive have FirstFile set to too big of a value, so the value points outside of the file array.
    public void Parser_Throw_DriveFirstFileInvalid()
    {
        var exception = Assert.Throws<InvalidDataException>(() => SgaArchiveFile.Open("../../../Parsers/testFiles/Nok/drive-firstFile-outofrange.sga", SgaMode.Read));
        Assert.Equal("Drive FirstFile index is out of range.", exception.Message);
    }

    [Fact] // First drive in the archive have LastFile set to too big of a value, so the value points outside of the file array.
    public void Parser_Throw_DriveLastFileInvalid()
    {
        var exception = Assert.Throws<InvalidDataException>(() => SgaArchiveFile.Open("../../../Parsers/testFiles/Nok/drive-lastFile-outofrange.sga", SgaMode.Read));
        Assert.Equal("Drive LastFile index is out of range.", exception.Message);
    }

    [Fact] // First drive in the archive have RootFolder set to too big of a value, so the value points outside of the folder array.
    public void Parser_Throw_RootFolderInvalid()
    {
        var exception = Assert.Throws<InvalidDataException>(() => SgaArchiveFile.Open("../../../Parsers/testFiles/Nok/drive-rootFolder-invalid.sga", SgaMode.Read));
        Assert.Equal("Drive RootFolder index is out of range.", exception.Message);
    }

    [Fact] // Drive in the list does not have any root folder present.
    public void Parser_Throw_DriveWithoutRootFolder()
    {
        var exception = Assert.Throws<InvalidDataException>(() => SgaArchiveFile.Open("../../../Parsers/testFiles/Nok/drive-noroot.sga", SgaMode.Read));
        Assert.Equal("Drive FirstFolder index is out of range.", exception.Message);
    }

    [Fact] // One of the folder in the list have NameOffset to bo out of range.
    public void Parser_Throw_FolderNameOffsetInvalid()
    {
        var exception = Assert.Throws<InvalidDataException>(() => SgaArchiveFile.Open("../../../Parsers/testFiles/Nok/folder-nameOffset-outofrange.sga", SgaMode.Read));
        Assert.Equal("TOC name read after toc.", exception.Message);
    }

    [Fact] // First Folder in the list have FirstFolder set to too big of a value, so the value points outside of the folder array.
    public void Parser_Throw_FolderFirstFolderInvalid()
    {
        var exception = Assert.Throws<InvalidDataException>(() => SgaArchiveFile.Open("../../../Parsers/testFiles/Nok/folder-firstFolder-outofrange.sga", SgaMode.Read));
        Assert.Equal("Folder FirstFolder index is out of range.", exception.Message);
    }

    [Fact] // First Folder in the list have LastFolder set to too big of a value, so the value points outside of the folder array.
    public void Parser_Throw_FolderLastFolderInvalid()
    {
        var exception = Assert.Throws<InvalidDataException>(() => SgaArchiveFile.Open("../../../Parsers/testFiles/Nok/folder-lastFolder-outofrange.sga", SgaMode.Read));
        Assert.Equal("Folder LastFolder index is out of range.", exception.Message);
    }

    [Fact] // First Folder in the list have FirstFile set to too big of a value, so the value points outside of the file array.
    public void Parser_Throw_FolderFirstFileInvalid()
    {
        var exception = Assert.Throws<InvalidDataException>(() => SgaArchiveFile.Open("../../../Parsers/testFiles/Nok/folder-firstFile-outofrange.sga", SgaMode.Read));
        Assert.Equal("Folder FirstFile index is out of range.", exception.Message);
    }

    [Fact] // First Folder in the list have LastFile set to too big of a value, so the value points outside of the file array.
    public void Parser_Throw_FolderLastFileInvalid()
    {
        var exception = Assert.Throws<InvalidDataException>(() => SgaArchiveFile.Open("../../../Parsers/testFiles/Nok/folder-lastFile-outofrange.sga", SgaMode.Read));
        Assert.Equal("Folder LastFile index is out of range.", exception.Message);
    }

    [Fact]
    public void Parser_Throw_FileNameOffsetInvalid()
    {
        var exception = Assert.Throws<InvalidDataException>(() => SgaArchiveFile.Open("../../../Parsers/testFiles/Nok/file-nameOffset-invalid.sga", SgaMode.Read));
        Assert.Equal("TOC name read after toc.", exception.Message);
    }

    [Fact] // File StorageFlag is malformed
    public void Parser_Throw_FileStorageTypeInvalid()
    {
        var exception = Assert.Throws<InvalidDataException>(() => SgaArchiveFile.Open("../../../Parsers/testFiles/Nok/file-storageType-invalid.sga", SgaMode.Read));
        Assert.Equal("File Storage flag invalid.", exception.Message);
    }

    [Fact] // DataOffset points outside of a file.
    public void Parser_Throw_FileDataOffsetInvalid()
    {
        var exception = Assert.Throws<InvalidDataException>(() => SgaArchiveFile.Open("../../../Parsers/testFiles/Nok/file-dataOffset-invalid.sga", SgaMode.Read));
        Assert.Equal("File data offset or size is invalid.", exception.Message);
    }

    #endregion
}