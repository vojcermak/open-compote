
namespace OpenCompote.SGA.Parsers;

interface ISgaParser
{
    SgaVersion Version { get; }
    SgaArchive Parse(Stream sgaStream);
    void Write(SgaArchive archive, Stream sgaStream);
}