using System.IO;
using System.Runtime.CompilerServices;
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
    
    [Fact] // Magic word is incorrect
    public void Parser_Throw_InvalidSgaMagic()
    {
        Assert.Throws<InvalidDataException>(() => SgaArchiveFile.Open("../../../Parsers/testFiles/Nok/magic.sga", SgaMode.Read));
    }

    [Fact] // Version is incorrect
    public void Parser_Throw_InvalidVersion()
    {
        Assert.Throws<InvalidDataException>(() => SgaArchiveFile.Open("../../../Parsers/testFiles/Nok/version-malformed.sga", SgaMode.Read));
    }

    [Fact] // Version is unsupported
    public void Parser_Throw_UnsupportedVersion()
    {
        Assert.Throws<NotImplementedException>(() => SgaArchiveFile.Open("../../../Parsers/testFiles/Nok/version-unsupported.sga", SgaMode.Read));
    }

    [Fact] // FileHash is malformed
    public void Parser_Throw_FileHashInvalid()
    {
        Assert.Throws<InvalidDataException>(() => SgaArchiveFile.Open("../../../Parsers/testFiles/Nok/filehash-invalid.sga", SgaMode.Read));
    }

    [Fact] // TOCHash is malformed
    public void Parser_Throw_TocHashInvalid()
    {
        Assert.Throws<InvalidDataException>(() => SgaArchiveFile.Open("../../../Parsers/testFiles/Nok/tochash-invalid.sga", SgaMode.Read));
    }

    [Fact] // TOC Size is incorrect
    public void Parser_Throw_TocSizeInvalid()
    {
        Assert.Throws<InvalidDataException>(() => SgaArchiveFile.Open("../../../Parsers/testFiles/Nok/tocSize-invalid.sga", SgaMode.Read));
    }

    [Fact] // DataOffset is incorrect
    public void Parser_Throw_DataOffsetInvalid()
    {
        Assert.Throws<InvalidDataException>(() => SgaArchiveFile.Open("../../../Parsers/testFiles/Nok/dataOffset-invalid.sga", SgaMode.Read));
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
    // 12. FirstFile/LastFile in drive does overlap
    // 13. FirstFile/LastFile in folder does overlap
    // 14. NameOffset is out of bounds of the name array
    // 15. StorageFlag is malformed
    // 16. StorageFlag is compress, but file is not compressed
    // 17. DataOffset points outside of a file
    // 18. Names in invalid text format
    // 19. Name does not have a null terminator
    // 20. Decompressed file size doesn't match actual decompressed output
    // 21. Root folder points to non-existing/invalid folder

    //Writer
    // 1. Opposite for OK 5 - writer can write valid archive
    // 2. Save valid empty archive
    // 3. Save valid archive with only empty drives
    // 4. Save valid archive with only empty folders
}