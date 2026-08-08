using System.Buffers.Binary;
using System.Security.Cryptography;

namespace OpenCompote.SGA.Parsers;

internal static class ParserUtils
{
    public static string ReadDynamicString(Span<byte> stringBuffer)
    {
        if(stringBuffer.IsEmpty)
            throw new InvalidDataException("String buffer is empty");

        int stringSize = 0;
        while (stringBuffer[stringSize] != 0)
        {
            stringSize ++;
            if(stringBuffer.Length <= stringSize)
                throw new InvalidDataException("TOC name read after toc.");   
        }

        return System.Text.Encoding.ASCII.GetString(stringBuffer[..stringSize]);
    }

    public static void WriteDynamicString(Stream sgaFile, string inputString)
    {
        byte[] bytes = new byte[inputString.Length + 1];
        System.Text.Encoding.UTF8.GetBytes(inputString, bytes);
        sgaFile.Write(bytes);
    }

        public static byte[] HashMD5(Stream fileStream, long dataLength, ReadOnlySpan<byte> initialValue)
    {
        long originalPosition = fileStream.Position;

        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.MD5);
        hash.AppendData(initialValue);

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
