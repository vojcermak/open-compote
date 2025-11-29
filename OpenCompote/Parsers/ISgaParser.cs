
namespace OpenCompote.SGA.Parsers;

interface ISgaParser
{
    static abstract void Parse(SgaArchive archive, Stream sgaStream);
    void Write(SgaArchive archive, BinaryWriter writer);
}