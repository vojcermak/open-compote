
namespace OpenCompote.SGA.Parsers;

public class SgaV2Parser : ISgaParser
{
    public static void Parse(SgaArchive archive, Stream sgaStream)
    {
        byte[] fileHash = ParserUtils.ReadHash(sgaStream);

        archive.ArchiveName = ParserUtils.ReadWideStaticString(sgaStream, 128);

        byte[] tocHash = ParserUtils.ReadHash(sgaStream);

        uint tocSize = ParserUtils.ReadUInt32(sgaStream);
        uint dataOffset = ParserUtils.ReadUInt32(sgaStream);

        Console.WriteLine("TOC size: {0}, Data offset: {1}",tocSize, dataOffset);

        byte[]? generatedFileHash = ParserUtils.HashMD5(sgaStream, sgaStream.Length-sgaStream.Position, "E01519D6-2DB7-4640-AF54-0A23319C56C3");
        if(generatedFileHash == null || !fileHash.SequenceEqual(generatedFileHash))
            Console.WriteLine("Hash is not valid");

        byte[]? generatedTocHash = ParserUtils.HashMD5(sgaStream, tocSize, "DFC9AF62-FC1B-4180-BC27-11CCE87D3EFF");
        if(generatedTocHash == null || !tocHash.SequenceEqual(generatedTocHash))
            Console.WriteLine("Hash is  not valid");

        // Read TOC header
        uint driveOffset = ParserUtils.ReadUInt32(sgaStream);
        ushort driveCount = ParserUtils.ReadUInt16(sgaStream);
        uint folderOffset = ParserUtils.ReadUInt32(sgaStream);
        ushort folderCount = ParserUtils.ReadUInt16(sgaStream);
        uint fileOffset = ParserUtils.ReadUInt32(sgaStream);
        ushort fileCount = ParserUtils.ReadUInt16(sgaStream);
        uint nameListOffset = ParserUtils.ReadUInt32(sgaStream);
        ushort nameCount = ParserUtils.ReadUInt16(sgaStream);

        bool isIc =  (nameListOffset - fileOffset)/fileCount == 17;

        Console.WriteLine("Drive offset: {0}\nDrive count: {1}\nFolder offset: {2}\nFolder count: {3}\nFile offset: {4}\nFile count: {5}\nName offset: {6}\nName count: {7}",
        driveOffset,driveCount,folderOffset,folderCount, fileOffset, fileCount, nameListOffset, nameCount);
        
        Console.WriteLine(sgaStream.Position);

        // Read drive definitions
        for (int i = 0; i < driveCount; i++)
        {
            string driveName = ParserUtils.ReadStaticString(sgaStream, 64);
            string driveAlias = ParserUtils.ReadStaticString(sgaStream, 64);
            ushort firstFolder = ParserUtils.ReadUInt16(sgaStream);
            ushort lastFolder = ParserUtils.ReadUInt16(sgaStream);
            ushort firstFile = ParserUtils.ReadUInt16(sgaStream);
            ushort lastFile = ParserUtils.ReadUInt16(sgaStream);
            ushort rootFolder = ParserUtils.ReadUInt16(sgaStream);
            
            archive.AddDrive(new SgaDrive(driveAlias, driveName, archive));

            Console.WriteLine("Drive name: {0}\nDrive alias: {1}\nFirst folder: {2}\nLast folder: {3}\nFirst file: {4}\nLast file: {5}\nRoot folder {6}",
            driveName, driveAlias, firstFolder, lastFolder, firstFile, lastFile, rootFolder);
        }

        Console.WriteLine(sgaStream.Position);

        for (int i = 0; i < folderCount; i++)
        {
            uint nameOffset = ParserUtils.ReadUInt32(sgaStream);
            uint firstFolder = ParserUtils.ReadUInt16(sgaStream);
            uint lastFolder = ParserUtils.ReadUInt16(sgaStream);
            uint firstFile = ParserUtils.ReadUInt16(sgaStream);
            uint lastFile = ParserUtils.ReadUInt16(sgaStream);

            Console.WriteLine("Name offset: {0}\nFirst folder: {1}\nLast folder: {2}\nFirst file: {3}\nLast file: {4}",
            nameOffset, firstFolder, lastFolder, firstFile, lastFile);
        }

        Console.WriteLine(sgaStream.Position);

        for (int i = 0; i < fileCount; i++)
        {
            uint nameOffset = ParserUtils.ReadUInt32(sgaStream);
            StorageType storageFlag = ReadStorageType(sgaStream, isIc);
            uint rawDataOffset = ParserUtils.ReadUInt32(sgaStream);
            uint compressSize = ParserUtils.ReadUInt32(sgaStream);
            uint decompressSize = ParserUtils.ReadUInt32(sgaStream);

            Console.WriteLine("Name offset: {0}\nStorage flag: {1}\nData offset: {2}\nCompressed size: {3}\nDecompressed size: {4}",
            nameOffset, storageFlag, rawDataOffset, compressSize, decompressSize);
        }

        Console.WriteLine(sgaStream.Position);
    }

    private static StorageType ReadStorageType(Stream sgaStream, bool isIC)
    {
        if (isIC)
        {   
            int shortFlag = sgaStream.ReadByte();
            return shortFlag switch
            {
                0 => StorageType.Uncompress,
                16 => StorageType.BufferCompress,
                32 => StorageType.StreamCompress,
                _ => throw new Exception("Invalid storage flag value."),
            };   
        }

        uint storageFlag = ParserUtils.ReadUInt32(sgaStream);
        return storageFlag switch
        {
            0 => StorageType.Uncompress,
            16 => StorageType.BufferCompress,
            32 => StorageType.StreamCompress,
            _ => throw new Exception("Invalid storage flag value."),
        };
    }

    public void Write(SgaArchive archive, BinaryWriter writer)
    {
        throw new NotImplementedException();
    }
}
