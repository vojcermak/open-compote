
namespace OpenCompote.SGA.Parsers;

interface ISgaParser
{
    SgaArchive Parse(BinaryReader reader);
    void Write(SgaArchive archive, BinaryWriter writer);
}