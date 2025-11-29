using OpenCompote.SGA.Parsers;

namespace OpenCompote.SGA.parsers;

public class SgaV2Parser : ISgaParser
{
    public static void Parse(SgaArchive archive, Stream sgaStream)
    {
        byte[] fileHash = ParserUtils.ReadHash(sgaStream);
        Console.WriteLine(Convert.ToHexString(fileHash));

        Console.WriteLine(ParserUtils.ReadLongStaticString(sgaStream, 128));

        byte[] tocHash = ParserUtils.ReadHash(sgaStream);
        Console.WriteLine(Convert.ToHexString(tocHash));

        uint tocSize = ParserUtils.ReadUInt32(sgaStream);
        uint dataOffset = ParserUtils.ReadUInt32(sgaStream);

        Console.WriteLine("TOC size: {0}, Data offset: {1}",tocSize, dataOffset);
    }

    public void Write(SgaArchive archive, BinaryWriter writer)
    {
        throw new NotImplementedException();
    }
}
