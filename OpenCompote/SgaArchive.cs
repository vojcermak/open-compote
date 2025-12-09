using System.Buffers.Binary;
using System.Collections.ObjectModel;
using OpenCompote.SGA.Parsers;

namespace OpenCompote.SGA;

public class SgaArchive
{
    private readonly List<SgaDrive> _drives;
    private readonly ReadOnlyCollection<SgaDrive> _driveCollection;

    public SgaVersion Version {get;}
    public SgaMode Mode {get;}
    public string ArchiveName {get; set;}
    
    public ReadOnlyCollection<SgaDrive> Drives
    {
        get {return _driveCollection;}
    }
    
    public int BlockSize {get; set;}

    internal readonly Stream _archiveStream;

    public SgaArchive(Stream stream, int mode, int version){
        throw new NotImplementedException();
    }

    public SgaArchive(Stream stream, SgaMode mode){
        _archiveStream = stream;
        Mode = mode;
        ArchiveName = "";

        if(!_archiveStream.CanRead || !_archiveStream.CanSeek)
            throw new Exception("stream is not supported");

        if((mode == SgaMode.Write || mode == SgaMode.Create) && !_archiveStream.CanWrite)
            throw new Exception("Cannot write to the stream");

        _drives = new List<SgaDrive>();
        _driveCollection = new ReadOnlyCollection<SgaDrive>(_drives);

        byte[] magicBuffer = new byte[8];
        _archiveStream.ReadExactly(magicBuffer);

        string text = System.Text.Encoding.ASCII.GetString(magicBuffer);
        
        if(text != "_ARCHIVE")
            throw new Exception("File is not SGA Archive");

        Console.WriteLine(text);

        switch (ParseVersion())
        {
            case 2:
                Version = SgaVersion.V2;
                SgaV2Parser.Parse(this, stream);
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
    }

    private int ParseVersion()
    {
        byte[] versionBuffer = new byte[4];
        _archiveStream.ReadExactly(versionBuffer);
        int version = BinaryPrimitives.ReadInt32LittleEndian(versionBuffer);
        return version;
    }

    internal void AddDrive(SgaDrive newDrive)
    {
        _drives.Add(newDrive);
    }
}
