using System.Buffers.Binary;
using System.Collections.ObjectModel;
using System.ComponentModel;
using OpenCompote.SGA.Parsers;

namespace OpenCompote.SGA;

public class SgaArchive: IDisposable
{
    private readonly List<SgaDrive> _drives;
    private readonly ReadOnlyCollection<SgaDrive> _driveCollection;
    private bool _isDisposed;
    private bool _leaveOpen;
    private readonly ISgaParser _parser;

    internal readonly Stream _archiveStream;

    public SgaVersion Version {get;}
    public SgaMode Mode {get;}
    public string ArchiveName {get; set;}
    
    public ReadOnlyCollection<SgaDrive> Drives
    {
        get {
            ThrowIfDisposed();
            return _driveCollection;
        }
    }
    
    public int BlockSize {get; set;}

    /// <summary>
    /// Create constructor. - Initializes new instance of SgaArchive on the given stream in the specific mode, using specific Sga version, specifying whether to leave the stream open.
    /// </summary>
    /// <param name="stream">The stream where the SGA archive is to be stored.</param>
    /// <param name="mode">Mode in which the archive should operate with.</param>
    /// <param name="version">SGA version of the new archive.</param>
    /// <param name="leaveOpen">true to leave the stream open upon disposing the SgaArchive, otherwise false.</param>
    public SgaArchive(Stream stream, SgaMode mode, SgaVersion version, bool leaveOpen = false){
        throw new NotImplementedException();
    }

    /// <summary>
    /// Open constructor. - Initialize new instance of SgaArchive on the given stream in the specific mode, specifying whether to leave the stream open.
    /// </summary>
    /// <param name="stream">The stream containing the SGA archive.</param>
    /// <param name="mode">Mode in which the archive should operate with.</param>
    /// <param name="leaveOpen">true to leave the stream open upon disposing the SgaArchive, otherwise false.</param>
    /// <remarks>This constructor cannot be used with SgaMode.Create. For Creating new empty archives please use the other constructor.</remarks>
    public SgaArchive(Stream stream, SgaMode mode, bool leaveOpen = false){

        switch (mode)
        {
            case SgaMode.Write:
                break;
            case SgaMode.Read:
                break;
            default:
                throw new ArgumentException("Constructor used does not support creating new archives.");
        }

        _archiveStream = stream;
        Mode = mode;
        ArchiveName = "";
        _isDisposed = false;
        _leaveOpen = leaveOpen;
        _drives = new List<SgaDrive>();
        _driveCollection = new ReadOnlyCollection<SgaDrive>(_drives);

        if(!_archiveStream.CanRead || !_archiveStream.CanSeek)
            throw new Exception("stream is not supported");

        if((mode == SgaMode.Write || mode == SgaMode.Create) && !_archiveStream.CanWrite)
            throw new Exception("Cannot write to the stream");

        byte[] magicBuffer = new byte[8];
        _archiveStream.ReadExactly(magicBuffer);

        string text = System.Text.Encoding.ASCII.GetString(magicBuffer);
        
        if(text != "_ARCHIVE")
            throw new Exception("File is not SGA Archive");

        switch (ParseVersion())
        {
            case 2:
                Version = SgaVersion.V2;
                _parser = new SgaV2Parser();
                break;
            case 4: 
                Version = SgaVersion.V4;
                throw new NotImplementedException();
            case 5: 
                Version = SgaVersion.V5;
                throw new NotImplementedException();
            case 7: 
                Version = SgaVersion.V7;
                throw new NotImplementedException();
            default: throw new Exception("version is not supported");
        }
        
        _parser.Parse(this, stream);
    }


    internal void AddDrive(SgaDrive newDrive)
    {
        ThrowIfDisposed();

        _drives.Add(newDrive);
    }

    public SgaDrive AddDrive(string alias, string name)
    {
        ThrowIfDisposed();

        SgaDrive newDrive = new SgaDrive(alias, name, this);
        _drives.Add(newDrive);
        return newDrive;
    }

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
                {
                    _archiveStream.Dispose();
                }
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
