using OpenCompote.SGA.Parsers;

namespace OpenCompote.SGA;

/// <summary>
/// Provides static methods for opening, creating, and extracting SGA archives.
/// </summary>
public class SgaArchiveFile
{
    /// <summary>
    /// Opens a SGA Archive at the specific source path, in the specified mode.
    /// </summary>
    /// <param name="sourceFileName">The path to the SGA archive to open, specified as a relative or absolute path. Relative path is relative to the current working directory.</param>
    /// <param name="mode">One of the enumeration values that specifies the actions that are allowed on the opened archive.</param>
    /// <returns>The opened SGA archive.</returns>
    /// <remarks>
    /// This function cannot be used with <paramref name="mode"/> set to <see cref="SgaMode.Create"/>. For creating a new archive use the <see cref="SgaArchiveFile.Create(string, OpenCompote.SGA.SgaVersion, bool)"/> function. When you set the <paramref name="mode"/> to <see cref="SgaMode.Create"/> an <see cref="ArgumentException"/> is thrown.
    /// 
    /// When you set the <paramref name="mode"/> to <see cref="SgaMode.Read"/> or <see cref="SgaMode.Write"/>, the archive is opened with Open from the FileMode enumeration as the file mode value. If the archive does not exist, a FileNotFoundException exception is thrown.
    /// 
    /// If the <paramref name="mode"/> is <see cref="SgaMode.Write"/>, the archive entries can be modified.
    /// If the <paramref name="mode"/> is <see cref="SgaMode.Read"/>, you can only read the archive entries. Attempt to write to the archive will cause <see cref="NotSupportedException"/> exception.
    /// 
    /// The archive <see cref="SgaVersion"/> is automatically detected based on the input file. If the version cannot be detected, a <see cref="InvalidDataException"/> is thrown.
    /// </remarks>
    /// <exception cref="ArgumentNullException"> The <paramref name="sourceFileName"/> parameter is null.</exception>
    /// <exception cref="ArgumentException">The <paramref name="mode"/> is set to <see cref="SgaMode.Create"/>.</exception>
    public static SgaArchive Open(string sourceFileName, SgaMode mode)
    {
        ArgumentNullException.ThrowIfNull(sourceFileName);

        FileStream fs = File.Open(sourceFileName, FileMode.Open, mode == SgaMode.Write ? FileAccess.ReadWrite : FileAccess.Read, FileShare.Read);

        SgaVersion version = SgaVersionDetector.Detect(fs);
        ISgaParser parser =  SgaParserFactory.Create(version);

        return new SgaArchive(fs, mode, version, parser);
    }

    /// <summary>
    /// Opens a SGA archive from existing stream, in the specified 'SgaMode' mode.
    /// </summary>
    /// <param name="existingStream">Stream containing the SGA archive.</param>
    /// <param name="mode">One of the enumeration values that specifies the actions that are allowed on the opened archive.</param>
    /// <param name="leaveOpen">`true` to leave the <paramref name="existingStream"/> open after sgaArchive is disposed. `false` to close the <paramref name="existingStream"/> when the archive is disposed.</param>
    /// <returns>The opened SGA archive.</returns>
    /// <remarks>
    /// <paramref name="existingStream"/> must support reading and seeking and if the <paramref name="mode"/> is set to <see cref="SgaMode.Write"/> also reading. If the <paramref name="existingStream"/> does not meet this criteria an <see cref="ArgumentException"/> is thrown.
    /// 
    /// This function cannot be used with <paramref name="mode"/> set to <see cref="SgaMode.Create"/>. For creating a new archive use the <see cref="SgaArchiveFile.Create(Stream, OpenCompote.SGA.SgaVersion, bool)"/> function. When you set the <paramref name="mode"/> to <see cref="SgaMode.Create"/> an <see cref="ArgumentException"/> is thrown.
    /// 
    /// When you set the <paramref name="mode"/> to <see cref="SgaMode.Read"/> or <see cref="SgaMode.Write"/>, the archive is opened with Open from the FileMode enumeration as the file mode value. If the archive does not exist, a FileNotFoundException exception is thrown.
    /// 
    /// If the <paramref name="mode"/> is <see cref="SgaMode.Write"/>, the archive entries can be modified.
    /// If the <paramref name="mode"/> is <see cref="SgaMode.Read"/>, you can only read the archive entries. Attempt to write to the archive will cause <see cref="NotSupportedException"/> exception.
    /// 
    /// The archive <see cref="SgaVersion"/> is automatically detected based on the input file. If the version cannot be detected, a <see cref="InvalidDataException"/> is thrown.
    /// </remarks>
    /// <exception cref="ArgumentNullException"> The <paramref name="existingStream"/> parameter is null.</exception>
    /// <exception cref="ArgumentException">The <paramref name="existingStream"/> does not support reading, writing or seeking, or <paramref name="mode"/> is set to <see cref="SgaMode.Create"/>.</exception>
    public static SgaArchive Open(Stream existingStream, SgaMode mode, bool leaveOpen = false)
    {
        ArgumentNullException.ThrowIfNull(existingStream);

        SgaVersion version = SgaVersionDetector.Detect(existingStream);
        ISgaParser parser =  SgaParserFactory.Create(version);

        return new SgaArchive(existingStream, mode, version, parser, leaveOpen);
    }

    /// <summary>
    /// Creates a new SGA archive at the path specified by sourceFileName in the specified version.
    /// </summary>
    /// <param name="sourceFileName">The path to the new SGA archive, specified as a relative or absolute path. Relative path is relative to the current working directory.</param>
    /// <param name="version">Version of the new SGA archive.</param>
    /// <param name="overwrite">If true, it overwrites an existing file; otherwise, it throws an exception when the file already exists.</param>
    /// <returns>The newly opened SGA archive.</returns>
    /// <remarks>
    /// The new 'SgaArchive' is always created with `mode` set to <see cref="SgaMode.Create"/>.
    /// 
    /// Not all version of Sga archive file are supported. For more info see status page.
    /// </remarks>
    /// <exception cref="ArgumentNullException"> The <paramref name="sourceFileName"/> parameter is null.</exception>
    public static SgaArchive Create(string sourceFileName, SgaVersion version, bool overwrite = false)
    {
        ArgumentNullException.ThrowIfNull(sourceFileName);

        FileStream fs = File.Open(sourceFileName, overwrite ? FileMode.Create : FileMode.CreateNew , FileAccess.ReadWrite, FileShare.Read);

        ISgaParser parser =  SgaParserFactory.Create(version);

        return new SgaArchive(fs, SgaMode.Create, version, parser);
    }

    /// <summary>
    /// Creates new SGA archive from existing stream, in the specified <see cref="SgaVersion"/>.
    /// </summary>
    /// <param name="existingStream">Empty Stream for the archive.</param>
    /// <param name="version">Version of the new SGA archive.</param>
    /// <param name="leaveOpen">`true` to leave the <paramref name="existingStream"/> open after sgaArchive is disposed. `false` to close the <paramref name="existingStream"/> when the archive is disposed.</param>
    /// <returns>The newly opened SGA archive.</returns>
    /// <remarks>
    /// The new <paramref name="existingStream"/> is always created with `mode` set to <see cref="SgaMode.Create"/>.
    /// 
    /// Not all version of Sga archive file are supported. For more info see status page.
    /// 
    /// The <paramref name="existingStream"/> must be empty and support reading, writing and seeking. If the <paramref name="existingStream"/> does not meet this criteria an <see cref="ArgumentException"/> is thrown.
    /// </remarks>
    /// <exception cref="ArgumentNullException">The <paramref name="existingStream"/> parameter is null.</exception>
    /// <exception cref="ArgumentException">The <paramref name="existingStream"/> does not support reading, writing or seeking.</exception>
    public static SgaArchive Create(Stream existingStream, SgaVersion version, bool leaveOpen)
    {
        ArgumentNullException.ThrowIfNull(existingStream);

        ISgaParser parser = SgaParserFactory.Create(version);

        return new SgaArchive(existingStream, SgaMode.Create, version, parser, leaveOpen);
    }

    /// <summary>
    /// Extracts all the files in the specified sga archive to a directory on the file system.
    /// </summary>
    /// <param name="sourceFileName">The path on the file system to the archive that is to be extracted.</param>
    /// <param name="destinationDirectoryName">
    /// The path to the destination directory to which the archive will be exported.
    /// If the directory is missing it creates it.
    /// </param>
    /// <param name="overwrite"><see langword="true"/> to overwrite files; <see langword="false"/> otherwise.</param>
    /// <exception cref="ArgumentNullException">
    ///  The <paramref name="sourceFileName"/> or <paramref name="destinationDirectoryName"/> parameter is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    ///  The <paramref name="sourceFileName"/> or <paramref name="destinationDirectoryName"/> is empty string or invalid path./>.
    /// </exception>
    public static void ExtractToDirectory(string sourceFileName, string destinationDirectoryName, bool overwrite = false)
    {
        using SgaArchive archive = Open(sourceFileName, SgaMode.Read);
        archive.ExtractToDirectory(destinationDirectoryName, overwrite);
    }

    /// <summary>
    /// Extracts all the files in the specified sga archive to a directory on the file system.
    /// </summary>
    /// <param name="existingStream">Stream containing the SGA archive.</param>
    /// <param name="destinationDirectoryName">
    /// The path to the destination directory to which the archive will be exported.
    /// If the directory is missing it creates it.
    /// </param>
    /// <param name="leaveOpen">
    /// <see langword="true"/> to leave the <paramref name="existingStream"/> open after this function. <see langword="false"/> to close
    /// the <paramref name="existingStream"/> when this method finishes.</param>
    /// <param name="overwrite"><see langword="true"/> to overwrite files; <see langword="false"/> otherwise.</param>
    /// <exception cref="ArgumentNullException">
    ///  The <paramref name="existingStream"/> or <paramref name="destinationDirectoryName"/> parameter is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    ///  The <paramref name="destinationDirectoryName"/> parameter is empty string or invalid path./>.
    /// </exception>
    public static void ExtractToDirectory(Stream existingStream, string destinationDirectoryName, bool leaveOpen = false, bool overwrite = false)
    {
        using SgaArchive archive = Open(existingStream, SgaMode.Read, leaveOpen);
        archive.ExtractToDirectory(destinationDirectoryName, overwrite);
    }

    /// <summary>
    /// Creates a SGA archive that contains the files and directories from the specified directory.
    /// </summary>
    /// <param name="sourceDirectoryName">The path to the directory to be archived.</param>
    /// <param name="destinationArchiveFileName">The path of the archive to be created</param>
    /// <param name="version">Version of the new SGA archive.</param>
    /// <param name="defaultStorageType">Default storage type that should be used while importing files.</param>
    /// <param name="overwrite">If true, it overwrites an existing file; otherwise, it throws an exception when the file already exists.</param>
    public static void CreateFromDirectory(string sourceDirectoryName,
                                           string destinationArchiveFileName,
                                           SgaVersion version,
                                           StorageType defaultStorageType,
                                           bool overwrite = false )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceDirectoryName);

        using SgaArchive newArchive = Create(destinationArchiveFileName, version, overwrite);

        foreach(string directory in Directory.EnumerateDirectories(sourceDirectoryName))
        {
            FileAttributes attributes = File.GetAttributes(directory);
            
            // if entry is ReparsePoint(sim-link) skip it.
            if(attributes.HasFlag(FileAttributes.ReparsePoint))
                continue;

            newArchive.ImportDriveFromDirectory(directory, defaultStorageType);                
        }
    }

    /// <summary>
    /// Creates a SGA archive in the specified stream that contains the files and directories from the specified directory.
    /// </summary>
    /// <param name="sourceDirectoryName">The path to the directory containing the drives to be archived.</param>
    /// <param name="destination">Empty Stream for the archive.</param>
    /// <param name="version">Version of the new SGA archive.</param>
    /// <param name="defaultStorageType">Default storage type that should be used while importing files.</param>
    /// <param name="leaveOpen">
    /// <see langword="true"/> to leave the <paramref name="destination"/> open after this method finishes. <see langword="false"/> to close
    /// the <paramref name="destination"/> when this method finishes.
    /// </param>
    public static void CreateFromDirectory(string sourceDirectoryName,
                                           Stream destination,
                                           SgaVersion version,
                                           StorageType defaultStorageType,
                                           bool leaveOpen = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceDirectoryName);

        using SgaArchive newArchive = Create(destination, version, leaveOpen);

        foreach(string directory in Directory.EnumerateDirectories(sourceDirectoryName))
        {
            FileAttributes attributes = File.GetAttributes(directory);
            
            // if entry is ReparsePoint(sim-link) skip it.
            if(attributes.HasFlag(FileAttributes.ReparsePoint))
                continue;

            newArchive.ImportDriveFromDirectory(directory, defaultStorageType);                
        }
    }
}