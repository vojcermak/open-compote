using System.Runtime.CompilerServices;
using OpenCompote.SGA.Parsers;

namespace OpenCompote.SGA;

public class SgaArchiveFile
{
    public static void CreateFromDirectory(string sourceDirectoryName,
                                           string destinationArchiveFileName,
                                           int version,
                                           bool rootDirAsDrive = false,
                                           int defaultCompressionLevel = 0,
                                           int defaultEncryptionType = 0)
    {
        throw new NotImplementedException();
    }

    public static void CreateFromDirectory(string sourceDirectoryName,
                                           Stream destination,
                                           int version,
                                           bool rootDirAsDrive = false,
                                           int defaultCompressionLevel = 0,
                                           int defaultEncryptionType = 0)
    {
        throw new NotImplementedException();
    }

    public static void ExtractToDirectory(string sourceFileName, string destinationDirectoryName)
    {
        throw new NotImplementedException();
    }

    public static void ExtractToDirectory(Stream source, string destinationDirectoryName)
    {
        throw new NotImplementedException();
    }

    public static SgaArchive Open(string sourceFileName, SgaMode mode)
    {
        FileStream fs = File.Open(sourceFileName, FileMode.Open);
        return new SgaArchive(fs, mode);
        
    }

    public static SgaArchive Open(string sourceFileName, int mode, int version)
    {
        throw new NotImplementedException();
    }
}