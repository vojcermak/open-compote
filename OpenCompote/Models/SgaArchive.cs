using System.Buffers.Binary;
using System.Collections.ObjectModel;
using OpenCompote.SGA.Parsers;

namespace OpenCompote.SGA;

/// <summary>
/// Represents the open SGA archive file itself.
/// </summary>
public class SgaArchive: IDisposable
{
    private readonly ReadOnlyCollection<SgaDrive> _driveCollection;
    private bool _isDisposed;
    private readonly bool _leaveOpen;
    private readonly ISgaParser _parser;
    private readonly SgaMode _mode;
    internal string _archiveName;
    internal readonly Stream _archiveStream;
    internal readonly List<SgaDrive> _drives;
    internal readonly TimeProvider _timeProvider;

    /// <summary>
    /// Gets the Mode in which the archive was opened.
    /// </summary>
    public SgaMode Mode
    {
        get
        {
            ThrowIfDisposed();
            return _mode;
        }
    }
    
    /// <summary>
    /// Gets the version of the SGA archive.
    /// </summary>
    public SgaVersion Version
    {
        get
        {
            ThrowIfDisposed();
            return field;
        }
    }

    /// <summary>
    /// Gets or sets the name of the SGA archive.
    /// </summary>
    /// <exception cref="InvalidOperationException">Setter throws this exception when the archive was opened in read-only mode.</exception>
    /// <exception cref="ObjectDisposedException">The archive was already closed.</exception>
    public string ArchiveName
    {
        get
        {
            ThrowIfDisposed();
            return _archiveName;
        }
        set
        {
            ThrowIfDisposed();
            if(_mode == SgaMode.Read)
                throw new InvalidOperationException("Cannot write to an archive opened in read-only mode.");
            _archiveName = value;
        }
    }

    /// <summary>
    /// Gets the list of SGA Drives currently in the archive.
    /// </summary>
    /// <exception cref="ObjectDisposedException">The archive was already closed.</exception>
    public ReadOnlyCollection<SgaDrive> Drives
    {
        get {
            ThrowIfDisposed();
            return _driveCollection;
        }
    }

    /// <summary>
    /// Initializes new instance of SgaArchive on the given empty stream in the specific mode, using specific SGA version, specifying whether to leave the stream open. 
    /// </summary>
    /// <remarks>This constructor should be used only for creating a new SGA archive. If you want to just open already existing fle, please use the Open constructor</remarks>
    /// <param name="stream">The stream where the SGA archive is to be stored.</param>
    /// <param name="mode">Mode in which the archive should operate with.</param>
    /// <param name="version">SGA archive version</param>
    /// <param name="parser"></param>
    /// <param name="leaveOpen">true to leave the stream open upon disposing the SgaArchive, otherwise false.</param>
    /// <param name="timeProvider">Used for file modified time assignment. Needed for testing.</param>
    internal SgaArchive(Stream stream, SgaMode mode, SgaVersion version, ISgaParser parser, bool leaveOpen = false, TimeProvider? timeProvider = null)
    {    
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(parser);
        
        if(!stream.CanRead || !stream.CanSeek)
            throw new ArgumentException("stream is not supported");

        if((mode == SgaMode.Write || mode == SgaMode.Create) && !stream.CanWrite)
            throw new ArgumentException("Cannot write to the stream");
        
        _archiveStream = stream;
        _parser = parser;
        _timeProvider = timeProvider ?? TimeProvider.System;
        _mode = mode;
        _archiveName = "";
        _isDisposed = false;
        _leaveOpen = leaveOpen;
        _drives = new List<SgaDrive>();
        _driveCollection = new ReadOnlyCollection<SgaDrive>(_drives);
        Version = version;

        if(mode != SgaMode.Create)
            parser.Parse(this, _archiveStream);
    }
    
    /// <summary>
    /// Creates new <see cref="SgaDrive"/> in the archive with the specific <paramref name="alias"/> and <paramref name="name"/>. New drive also contains a new empty RootFolder with the same name as the drive.
    /// </summary>
    /// <param name="alias">Alias of the new drive.</param>
    /// <param name="name">Name of the new drive.</param>
    /// <returns>New SgaDrive object</returns>
    /// <exception cref="InvalidOperationException">Archive was open in read-only mode.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="alias"/> or <paramref name="name"/> is <see langword="null"/>.</exception>
    /// <exception cref="ObjectDisposedException">The archive was already closed.</exception>
    public SgaDrive AddDrive(string alias, string name)
    {
        ThrowIfDisposed();
        if(_mode == SgaMode.Read)
            throw new InvalidOperationException("Cannot write to an archive opened in read-only mode.");
        
        string trimmedAlias = SgaNameValidator.ValidateDriveName(alias);
        string trimmedName = SgaNameValidator.ValidateEntryName(name);

        SgaDrive newDrive = new(trimmedAlias, trimmedName, this);
        _drives.Add(newDrive);
        return newDrive;
    }
    
    /// <summary>
    /// Returns first <see cref="SgaDrive"/> with name or alias matching the parameter. If no matching drive is found returns <see langword="null"/>.
    /// </summary>
    /// <param name="driveName"><see cref="SgaDrive.Name"/> or <see cref="SgaDrive.Alias"/> of the Drive.</param>
    /// <returns><see cref="SgaDrive"/> or <see langword="null"/> if no matching drive was found.</returns>
    /// <exception cref="ArgumentNullException">The <paramref name="driveName"/> is <see langword="null"/>.</exception>
    /// <exception cref="ObjectDisposedException">The archive was already closed.</exception>
    public SgaDrive? GetDrive(string driveName)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(driveName);

        return _drives.FirstOrDefault((drive) => { return drive.Name.Equals(driveName, StringComparison.OrdinalIgnoreCase) || drive.Alias.Equals(driveName, StringComparison.OrdinalIgnoreCase); });
    }

    /// <summary>
    /// NOT IMPLEMENTED! DO NOT USE
    /// </summary>
    /// <exclude />
    internal SgaEntry? GetEntry(string path)
    {
        ThrowIfDisposed(); // Test if the folder is deleted.
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        // Normalize separators and remove leading/trailing ones.
        path = path.Replace('\\', '/').Trim();
        
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        
        int x = path.IndexOf(":/");
        if(x <= 0)
            throw new ArgumentException("Path does not contain any drive name.");

        string archiveName = path[..x];
        string archivePath = path[(x+2)..];

        SgaDrive? selectedDrive = GetDrive(archiveName);

        if(selectedDrive == null || archivePath == "")
            return null;

        return selectedDrive.GetEntry(archivePath);
    }

    /// <summary>
    /// Disposes the SGA archive, writing any pending changes if in create or write mode.
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed)
            return;

        try
        {
            switch (_mode)
            {
                case SgaMode.Read:
                    break;
                case SgaMode.Create:
                    _parser.Write(this, _archiveStream); // When the stream is new i can write directly to the target stream
                    break;
                case SgaMode.Write:

                    // When the archive is changed i need to write to a temp stream first and the move it to the target stream.
                    // Preallocate the change stream to a reasonable default. in this case the size of the target stream.
                    var writeStream = new MemoryStream((int)_archiveStream.Length);
                    _parser.Write(this, writeStream);
                    writeStream.Position = 0;
                    writeStream.CopyTo(_archiveStream);
                    break;
            }
        }
        finally
        {
            _isDisposed = true;
            if (!_leaveOpen)
                _archiveStream.Dispose();
        }
    }

    internal void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
    }
}
