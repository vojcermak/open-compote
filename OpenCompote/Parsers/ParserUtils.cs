using System.Buffers.Binary;

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
        return System.Text.Encoding.Unicode.GetString(strBuffer);
    }

    public static string ReadStaticString(Stream sgaFile, int length)
    {
        byte[] strBuffer = new byte[length];
        sgaFile.ReadExactly(strBuffer);
        return System.Text.Encoding.UTF8.GetString(strBuffer);
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
}
