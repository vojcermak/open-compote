using System.Buffers.Binary;
using System.Security.Cryptography;

namespace OpenCompote.SGA.Parsers;

internal static class ParserUtils
{
    public static byte[] ReadHash(Stream sgaFile)
    {
        byte[] hash = new byte[16];
        sgaFile.ReadExactly(hash);
        return hash;
    }

    public static string ReadWideStaticString(Stream sgaFile, int length)
    {
        Span<byte> strBuffer = stackalloc byte[length*2];
        sgaFile.ReadExactly(strBuffer);
        return System.Text.Encoding.Unicode.GetString(strBuffer).TrimEnd('\0');
    }

    public static void WriteWideStaticString(Stream sgaFile, string inputString, int length)
    {
        Span<byte> buffer = stackalloc byte[length*2];
        System.Text.Encoding.Unicode.GetBytes(inputString, buffer);
        sgaFile.Write(buffer);
    }

    public static string ReadStaticString(Stream sgaFile, int length)
    {
        Span<byte> strBuffer = stackalloc byte[length];
        sgaFile.ReadExactly(strBuffer);
        return System.Text.Encoding.UTF8.GetString(strBuffer).TrimEnd('\0');
    }

    public static void WriteStaticString(Stream sgaFile, string inputString, int length)
    {
        Span<byte> buffer = stackalloc byte[length];
        System.Text.Encoding.UTF8.GetBytes(inputString, buffer);
        sgaFile.Write(buffer);
    }

    public static string ReadDynamicString(Stream sgaFile, long startPosition, long maxPosition)
    {
        List<byte> buffer = new List<byte>();
        long currentPosition = sgaFile.Position;
        sgaFile.Position = startPosition;
        
        if(startPosition >= maxPosition)
            throw new InvalidDataException("TOC name read after toc.");

        int b;
        while ((b = sgaFile.ReadByte()) != -1)
        {
            if(b==0)
                break;
            if(sgaFile.Position >= maxPosition)
                throw new InvalidDataException("TOC name read after toc.");

            buffer.Add((byte)b);
        }


        sgaFile.Position = currentPosition;
        return System.Text.Encoding.ASCII.GetString(buffer.ToArray());
    }

    public static void WriteDynamicString(Stream sgaFile, string inputString)
    {
        byte[] bytes = new byte[inputString.Length + 1];
        System.Text.Encoding.UTF8.GetBytes(inputString, bytes);
        sgaFile.Write(bytes);
    }

    public static uint ReadUInt32(Stream sgaFile)
    {
        Span<byte> numBuffer = stackalloc byte[4];
        sgaFile.ReadExactly(numBuffer);
        return BinaryPrimitives.ReadUInt32LittleEndian(numBuffer);
    }

    public static void WriteUInt32(Stream sgaFile, uint value)
    {
        Span<byte> numBuffer = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32LittleEndian(numBuffer,value);
        sgaFile.Write(numBuffer);
    }

    public static ushort ReadUInt16(Stream sgaFile)
    {
        Span<byte> numBuffer = stackalloc byte[2];
        sgaFile.ReadExactly(numBuffer);
        return BinaryPrimitives.ReadUInt16LittleEndian(numBuffer);
    }

    public static void WriteUInt16(Stream sgaFile, ushort value)
    {
        Span<byte> numBuffer = stackalloc byte [2];
        BinaryPrimitives.WriteUInt16LittleEndian(numBuffer, value);
        sgaFile.Write(numBuffer);
    }

    public static byte[] HashMD5(Stream fileStream, long dataLength, string initialValue )
    {
        long originalPosition = fileStream.Position;

        Span<byte> seed = stackalloc byte[256];
        int seedLength = System.Text.Encoding.UTF8.GetBytes(initialValue, seed);

        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.MD5);
        hash.AppendData(seed[..seedLength]);

        byte[] buffer = GC.AllocateUninitializedArray<byte>(32 * 1024);

        long remaining = dataLength;

        while (remaining > 0)
        {
            int readSize = (int)Math.Min(buffer.Length, remaining);
            int bytesRead = fileStream.Read(buffer, 0, readSize);
            hash.AppendData(buffer.AsSpan(0, bytesRead));
            remaining -= bytesRead;
        }

        fileStream.Position = originalPosition;

        return hash.GetHashAndReset();
    }

    public static uint ConvertToSgaTimestamp(DateTimeOffset? value)
    {
        if (value is null)
            return 0;

        long seconds = value.Value.ToUnixTimeSeconds();

        if (seconds < 0 || seconds > uint.MaxValue)
            throw new InvalidDataException(
                $"Modification time '{value}' cannot be represented in SGA (UInt32 Unix timestamp).");

        return (uint)seconds;
    }
}
