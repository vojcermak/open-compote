using System;
using OpenCompote.SGA;

namespace OpenCompote;

internal readonly struct FileRecord(
    uint nameOffset,
    StorageType storageType,
    uint rawDataOffset,
    uint compressSize,
    uint decompressSize

)
{
    public uint NameOffset { get; } = nameOffset;
    public StorageType StorageType { get; } = storageType;
    public uint RawDataOffset { get; } = rawDataOffset;
    public uint CompressedSize { get; } = compressSize;
    public uint Size { get; } = decompressSize;
}
