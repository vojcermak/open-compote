using System;
using OpenCompote.SGA;
using OpenCompote.SGA.Parsers;

namespace OpenCompote;

static class SgaParserFactory
{
    public static ISgaParser Create(SgaVersion version)
    {
        return version switch
        {
            SgaVersion.V2 => new SgaV2Parser(),
            SgaVersion.V4 => throw new NotImplementedException(),
            SgaVersion.V5 => throw new NotImplementedException(),
            SgaVersion.V7 => throw new NotImplementedException(),
            _ => throw new InvalidDataException($"SGA version '{version}' is not supported or is invalid."),
        };
    }
}
