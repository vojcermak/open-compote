namespace OpenCompote.SGA;

/// <summary>
/// Provides static methods for opening, creating, and extracting SGA archives.
/// </summary>
public class SgaArchiveFile
{
    /// <summary>
    /// NOT IMPLEMENTED! DO NOT USE
    /// </summary>
    /// <exclude />
    public static void CreateFromDirectory(string sourceDirectoryName,
                                           string destinationArchiveFileName,
                                           int version,
                                           bool rootDirAsDrive = false,
                                           int defaultCompressionLevel = 0,
                                           int defaultEncryptionType = 0)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// NOT IMPLEMENTED! DO NOT USE
    /// </summary>
    /// <exclude />
    public static void CreateFromDirectory(string sourceDirectoryName,
                                           Stream destination,
                                           int version,
                                           bool rootDirAsDrive = false,
                                           int defaultCompressionLevel = 0,
                                           int defaultEncryptionType = 0)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// NOT IMPLEMENTED! DO NOT USE
    /// </summary>
    /// <exclude />
    public static void ExtractToDirectory(string sourceFileName, string destinationDirectoryName)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// NOT IMPLEMENTED! DO NOT USE
    /// </summary>
    /// <exclude />
    public static void ExtractToDirectory(Stream source, string destinationDirectoryName)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Opens a SGA Archive with specific SourceFileName in the specified SgaMode mode.
    /// </summary>
    /// <param name="sourceFileName">The path to the SGA archive.</param>
    /// <param name="mode">Mode in which the archive should operate with.</param>
    /// <returns>The open SGA archive.</returns>
    public static SgaArchive Open(string sourceFileName, SgaMode mode)
    {
        FileStream fs = File.Open(sourceFileName, FileMode.Open, mode == SgaMode.Read ? FileAccess.Read : FileAccess.ReadWrite, FileShare.Read);

        SgaVersion version = SgaVersionDetector.Detect(fs);

        return SgaParserFactory.Create(version).Parse(fs);
    }

    /// <summary>
    /// Creates a new SGA archive at the path specified by sourceFileName in the specified version.
    /// </summary>
    /// <param name="sourceFileName">The path to the file where the archive should be stored.</param>
    /// <param name="version">Version of the new SGA archive.</param>
    /// <param name="overwrite">If true, it overwrites an existing file; otherwise, it throws an exception when the file already exists.</param>
    /// <returns>The newly opened SGA archive.</returns>
    public static SgaArchive Create(string sourceFileName, SgaVersion version, bool overwrite = false)
    {
        FileStream fs = File.Open(sourceFileName, overwrite ? FileMode.Create : FileMode.CreateNew , FileAccess.ReadWrite, FileShare.Read);
        return SgaParserFactory.Create(version).Parse(fs);
    }
}