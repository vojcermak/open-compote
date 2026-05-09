using System;
using System.Buffers.Binary;
using OpenCompote.SGA;

namespace OpenCompote;

internal static class SgaVersionDetector
{   
    /// <summary>
    /// Detects if `stream` contains SGA archive and his version.
    /// </summary>
    /// <param name="stream">archive stream</param>
    /// <returns>Found SGA version.</returns>
    /// <exception cref="ArgumentNullException">Stream parameter was null.</exception>
    /// <exception cref="ArgumentException">Stream is not readable or seekable.</exception>
    /// <exception cref="InvalidDataException">Stream is not sga archive or has invalid/unsupported version.</exception>
    public static SgaVersion Detect(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        if (!stream.CanRead || !stream.CanSeek)
            throw new ArgumentException("Stream must be readable and seekable.");

        long originalPosition = stream.Position;

        try
        {
            // Parse and test if current file is SGA archive.
            Span<byte> magicBuffer = stackalloc byte[8];
            stream.ReadExactly(magicBuffer);

            if (!magicBuffer.SequenceEqual("_ARCHIVE"u8))
                throw new InvalidDataException("File is not a valid SGA Archive. (invalid magic byte)");
            

            // Parser SGA version
            Span<byte> versionBuffer = stackalloc byte[4];
            stream.ReadExactly(versionBuffer);

            int version = BinaryPrimitives.ReadInt32LittleEndian(versionBuffer);

            return version switch
            {
                2 => SgaVersion.V2,
                4 => SgaVersion.V4,
                5 => SgaVersion.V5,
                7 => SgaVersion.V7,
                _ => throw new InvalidDataException($"SGA version '{version}' is not supported or is invalid.")
            };
        }
        finally
        {
            stream.Position = originalPosition;
        }
    }
}
