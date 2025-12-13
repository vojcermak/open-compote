
namespace OpenCompote.SGA.Parsers;

interface ISgaParser
{
    void Parse(SgaArchive archive, Stream sgaStream);
    void Write(SgaArchive archive, Stream sgaStream);
}