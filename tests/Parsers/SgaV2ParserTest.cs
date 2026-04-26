using System.IO;
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
    
    //NOK
    // 1. Magic word is incorrect .
    // 2. Version is incorrect .
    // 3. Version is unsupported .
    // 4. FileHash is malformed .
    // 5. TOCHash is malformed .
    // 6. TOC Size is incorrect .
    // 7. DataOffset is incorrect .
    // 8. DriveOffset invalid .
    // 9. FolderOffset and drive count does not match .
    // 10. FileOffset and folder count does not match .
    // 11. Name Offset and file count does not match .
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