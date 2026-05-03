using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using Xunit;

namespace OpenCompote.SGA.Tests.Parsers;

public class SgaV2ParserTest
{
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

    //OK
    // 1. - Parser can open and read valid uncompress file
    // 2. - Parser can open and read valid compress file
    // 2. - Parser can open and read archive with only empty folders
    // 3. - Parser can open and read archive with only empty drives
    // 4. - Parser can open and read archive with no drives
    // 5. - Parser can open and read archive with multiple drives witch multiple nested folder and files
    
    #region Invalid input tests
    
    [Fact] // Magic word is invalid. (Opened file is not sga file.)
    public void Parser_Throw_InvalidSgaMagic()
    {
        var exception = Assert.Throws<InvalidDataException>(() => SgaArchiveFile.Open("../../../Parsers/testFiles/Nok/header-magic.sga", SgaMode.Read));
        Assert.Equal("File is not a valid SGA Archive. (invalid magic byte)", exception.Message);
    }

    [Fact] // Version is incorrect (Sga version attribute is set to value that does not corresponds to any existing sga version.)
    public void Parser_Throw_InvalidVersion()
    {
        var exception = Assert.Throws<InvalidDataException>(() => SgaArchiveFile.Open("../../../Parsers/testFiles/Nok/header-version-malformed.sga", SgaMode.Read));
        Assert.Equal("SGA version '165' is not supported or is invalid.", exception.Message);
    }

    [Fact] // Version is not yet supported.
    public void Parser_Throw_UnsupportedVersion()
    {
        Assert.Throws<NotImplementedException>(() => SgaArchiveFile.Open("../../../Parsers/testFiles/Nok/header-version-unsupported.sga", SgaMode.Read));
    }

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

    [Fact] // DriveOffset invalid
    public void Parser_Throw_DriveOffsetInvalid()
    {
        Assert.Throws<InvalidDataException>(() => SgaArchiveFile.Open("../../../Parsers/testFiles/Nok/driveOffset_invalid.sga", SgaMode.Read));
    }

    [Fact] // FolderOffset and drive count does not match
    public void Parser_Throw_FolderOffsetInvalid()
    {
        Assert.Throws<InvalidDataException>(() => SgaArchiveFile.Open("../../../Parsers/testFiles/Nok/folderOffset-invalid.sga", SgaMode.Read));
    }

    [Fact] // FileOffset and folder count does not match
    public void Parser_Throw_FileOffsetInvalid()
    {
        Assert.Throws<InvalidDataException>(() => SgaArchiveFile.Open("../../../Parsers/testFiles/Nok/fileOffset_invalid.sga", SgaMode.Read));
    }

    [Fact] // Name Offset and file count does not match
    public void Parser_Throw_NameOffsetInvalid()
    {
        Assert.Throws<InvalidDataException>(() => SgaArchiveFile.Open("../../../Parsers/testFiles/Nok/nameOffset_invalid.sga", SgaMode.Read));
    }

    #endregion

    //NOK
    // 12. FirstFile/LastFile in drive does overlap .
    // 13. FirstFile/LastFile in folder does overlap .
    // 14. NameOffset is out of bounds of the name array .
    // 15. StorageFlag is malformed .
    // 16. StorageFlag is compress, but file is not compressed
    // 17. DataOffset points outside of a file .
    // 19. Name does not have a null terminator .
    // 20. Decompressed file size doesn't match actual decompressed output .
    // 21. Root folder points to non-existing/invalid folder .

    //Writer
    // 1. Opposite for OK 5 - writer can write valid archive
    // 2. Save valid empty archive
    // 3. Save valid archive with only empty drives
    // 4. Save valid archive with only empty folders
}