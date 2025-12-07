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
        byte[] strBuffer = new byte[length];
        sgaFile.ReadExactly(strBuffer);
        return System.Text.Encoding.Unicode.GetString(strBuffer).TrimEnd('\0');
    }

    public static string ReadStaticString(Stream sgaFile, int length)
    {
        byte[] strBuffer = new byte[length];
        sgaFile.ReadExactly(strBuffer);
        return System.Text.Encoding.UTF8.GetString(strBuffer).TrimEnd('\0');
    }

    public static string ReadDynamicString(Stream sgaFile, long startPosition)
    {
        List<byte> buffer = new List<byte>();
        long currentPosition = sgaFile.Position;
        sgaFile.Position = startPosition;
        
        int b;
        while ((b = sgaFile.ReadByte()) != -1)
        {
            if(b==0)
                break;
            buffer.Add((byte)b);
        }


        sgaFile.Position = currentPosition;
        return System.Text.Encoding.ASCII.GetString(buffer.ToArray());
    }

    public static uint ReadUInt32(Stream sgaFile)
    {
        byte[] numBuffer = new byte[4];
        sgaFile.ReadExactly(numBuffer);
        return BinaryPrimitives.ReadUInt32LittleEndian(numBuffer);
    }

    public static ushort ReadUInt16(Stream sgaFile)
    {
        byte[] numBuffer = new byte[2];
        sgaFile.ReadExactly(numBuffer);
        return BinaryPrimitives.ReadUInt16LittleEndian(numBuffer);
    }

    public static byte[]? HashMD5(Stream fileStream, long dataLength, string initialValue )
    {
        var currentPosition = fileStream.Position;
        byte[] seed = System.Text.Encoding.UTF8.GetBytes(initialValue);

        using var md5 = MD5.Create();

        md5.TransformBlock(seed, 0, seed.Length, null, 0);
        byte[] buffer = new byte[dataLength];
        int bytesRead = fileStream.Read(buffer, 0, buffer.Length);
        md5.TransformFinalBlock(buffer, 0, bytesRead);

        fileStream.Position = currentPosition;

        return md5.Hash;
    }
}
