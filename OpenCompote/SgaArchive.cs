using System.Buffers.Binary;
using System.Collections.ObjectModel;
using OpenCompote.SGA.parsers;

namespace OpenCompote.SGA;

public class SgaArchive
{
    public SgaVersion Version {get;}
    public SgaMode Mode {get;}
    public string ArchiveName {get; set;}
    public ReadOnlyCollection<SgaDrive> Drives;
    public int BlockSize {get; set;}

    private Stream ArchiveStream;

    public SgaArchive(Stream stream, int mode, int version){
        throw new NotImplementedException();
    }

    public SgaArchive(Stream stream, SgaMode mode){
        ArchiveStream = stream;
        Mode = mode;

        if(!ArchiveStream.CanRead || !ArchiveStream.CanSeek)
            throw new Exception("stream is not supported");

        if((mode == SgaMode.Write || mode == SgaMode.Create) && !ArchiveStream.CanWrite)
            throw new Exception("Cannot write to the stream");

        byte[] magicBuffer = new byte[8];
        ArchiveStream.ReadExactly(magicBuffer);

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
        ArchiveStream.ReadExactly(versionBuffer);
        int version = BinaryPrimitives.ReadInt32LittleEndian(versionBuffer);
        return version;
    }
}
