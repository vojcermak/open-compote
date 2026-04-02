using System.Buffers.Binary;
using System.Collections.ObjectModel;
using OpenCompote.SGA.Parsers;

namespace OpenCompote.SGA;

/// <summary>
/// Represents the open archive file itself.
/// </summary>
public class SgaArchive: IDisposable
{
    private readonly ReadOnlyCollection<SgaDrive> _driveCollection;
    private bool _isDisposed;
    private readonly bool _leaveOpen;
    private readonly ISgaParser _parser;

    internal string _archiveName;
    internal readonly Stream _archiveStream;
    internal readonly List<SgaDrive> _drives;

    /// <summary>
    /// Mode in which the archive was opened.
    /// </summary>
    public SgaMode Mode {get;}
    
    /// <summary>
    /// Version of the SGA archive.
    /// </summary>
    public SgaVersion Version {get;}

    /// <summary>
    /// Name of the SGA archive.
    /// </summary>
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
            if(Mode == SgaMode.Read)
                throw new NotSupportedException("Writing is not supported.");
            _archiveName = value;
        }
    }

    /// <summary>
    /// List of SGA Drives currently in the archive.
    /// </summary>
    public ReadOnlyCollection<SgaDrive> Drives
    {
        get {
            ThrowIfDisposed();
            return _driveCollection;
        }
    }
    public int BlockSize {get; set;}

    /// <summary>
    /// Create constructor. - Initializes new instance of SgaArchive on the given empty stream in the specific mode, using specific SGA version, specifying whether to leave the stream open. 
    /// </summary>
    /// <remarks>This constructor should be used only for creating a new SGA archive. If you want to just open already existing fle, please use the Open constructor</remarks>
    /// <param name="stream">The stream where the SGA archive is to be stored.</param>
    /// <param name="mode">Mode in which the archive should operate with.</param>
    /// <param name="version">Expected SGA version of the archive.</param>
    /// <param name="leaveOpen">true to leave the stream open upon disposing the SgaArchive, otherwise false.</param>
    public SgaArchive(Stream stream, SgaMode mode, SgaVersion? version, bool leaveOpen = false){
        
        ArgumentNullException.ThrowIfNull(stream);
        
        if(!stream.CanRead || !stream.CanSeek)
            throw new ArgumentException("stream is not supported");

        if((mode == SgaMode.Write || mode == SgaMode.Create) && !stream.CanWrite)
            throw new ArgumentException("Cannot write to the stream");
        
        _archiveStream = stream;
        Mode = mode;
        _archiveName = "";
        _isDisposed = false;
        _leaveOpen = leaveOpen;
        _drives = new List<SgaDrive>();
        _driveCollection = new ReadOnlyCollection<SgaDrive>(_drives);

        if(Mode == SgaMode.Create)
        {
            if(version == null)
                throw new ArgumentException("Creating new archives without version is not allowed");
            Version = (SgaVersion)version;
        }
        else
        {
            byte[] magicBuffer = new byte[8];
            _archiveStream.ReadExactly(magicBuffer);

            string text = System.Text.Encoding.ASCII.GetString(magicBuffer);
            
            if(text != "_ARCHIVE")
                throw new Exception("File is not SGA Archive");
            
            Version = (SgaVersion)ParseVersion();
        }

        _parser = Version switch
        {
            SgaVersion.V2 => new SgaV2Parser(),
            SgaVersion.V4 => throw new NotImplementedException(),
            SgaVersion.V5 => throw new NotImplementedException(),
            SgaVersion.V7 => throw new NotImplementedException(),
            _ => throw new ArgumentException("version is not supported"),
        };

        if(Mode != SgaMode.Create)
            _parser.Parse(this, _archiveStream);
    }

    /// <summary>
    /// Open constructor. - Initialize new instance of SgaArchive on the given stream in the specific mode, specifying whether to leave the stream open.
    /// </summary>
    /// <param name="stream">The stream containing the SGA archive.</param>
    /// <param name="mode">Mode in which the archive should operate with.</param>
    /// <param name="leaveOpen">true to leave the stream open upon disposing the SgaArchive, otherwise false.</param>
    /// <remarks>This constructor cannot be used with SgaMode.Create. For creating new empty archives please use the Create constructor.</remarks>
    public SgaArchive(Stream stream, SgaMode mode, bool leaveOpen = false): this(stream, mode, null, leaveOpen) {}

    #if DEBUG // For unit testing of the public API only, Do not include to release builds (Not very nice, but works.) 
    internal SgaArchive(Stream stream, SgaMode mode, SgaVersion version, ISgaParser parser, bool leaveOpen = false)
    {
        Mode = mode;
        Version = version;
        _archiveName = "";
        _archiveStream = stream;
        _isDisposed = false;
        _leaveOpen = leaveOpen;
        _drives = new List<SgaDrive>();
        _driveCollection = new ReadOnlyCollection<SgaDrive>(_drives);
        _parser = parser;

        if(Mode != SgaMode.Create)
            _parser.Parse(this, _archiveStream);
    }
    #endif
    
    /// <summary>
    /// Creates new SgaDrive in the archive with the specific name and alias. New drive also contains a new empty RootFolder with the same name as the drive.
    /// </summary>
    /// <param name="alias">Alias of the new drive.</param>
    /// <param name="name">Name of the new drive.</param>
    /// <returns>New SgaDrive object</returns>
    /// <exception cref="NotSupportedException">Archive does not support writing.</exception>
    /// <exception cref="ArgumentNullException">alias or name is null.</exception>
    /// <exception cref="ObjectDisposedException">The archive was already closed.</exception>
    public SgaDrive AddDrive(string alias, string name)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(alias);
        ArgumentNullException.ThrowIfNull(name);

        if(Mode == SgaMode.Read)
            throw new NotSupportedException("Writing is not supported.");

        SgaDrive newDrive = new(alias, name, this);
        _drives.Add(newDrive);
        return newDrive;
    }
    
    /// <summary>
    /// Returns SgaDrive with name or alias matching the parameter. If no matching drive is found returns null.
    /// </summary>
    /// <param name="driveName">Name or alias of the Drive.</param>
    /// <returns>SgaDrive or null if no matching drive was found.</returns>
    /// <exception cref="ArgumentNullException">driveName is null.</exception>
    /// <exception cref="ObjectDisposedException">The archive was already closed.</exception>
    public SgaDrive? GetDrive(string driveName)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(driveName);

        return _drives.FirstOrDefault((drive)=>{return drive.Name == driveName || drive.Alias == driveName;});
    }

    /// <summary>
    /// NOT IMPLEMENTED! DO NOT USE
    /// </summary>
    /// <exclude />
    public SgaEntry GetEntry(string entryName)
    {
        throw new NotImplementedException();
    }

    public void Dispose()
    {
        if(!_isDisposed)
        {
            try
            {
                switch (Mode)
                {
                    case SgaMode.Read:
                        break;
                    case SgaMode.Create:
                    case SgaMode.Write:
                        _parser.Write(this, _archiveStream);
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
    }
    
    private int ParseVersion()
    {
        byte[] versionBuffer = new byte[4];
        _archiveStream.ReadExactly(versionBuffer);
        int version = BinaryPrimitives.ReadInt32LittleEndian(versionBuffer);
        return version;
    }

    internal void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
    }
}
