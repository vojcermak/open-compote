using System;

namespace OpenCompote.SGA.Parsers;

internal readonly struct V2FileMetadata(
    string FileName,
    DateTimeOffset LastModified,
    uint crc
)
{
    public string FileName { get; } = FileName;
    public DateTimeOffset LastModified {get;} = LastModified;
    public uint CRC {get;} = crc;
}
