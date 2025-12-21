
namespace OpenCompote.SGA.Parsers;

internal class SgaV2Parser : ISgaParser
{
    public void Parse(SgaArchive archive, Stream sgaStream)
    {
        List<SgaFolder> folderList = new List<SgaFolder>();
        List<SgaFile> fileList = new List<SgaFile>();

        byte[] fileHash = ParserUtils.ReadHash(sgaStream);

        archive.ArchiveName = ParserUtils.ReadWideStaticString(sgaStream, 128);

        byte[] tocHash = ParserUtils.ReadHash(sgaStream);

        uint tocSize = ParserUtils.ReadUInt32(sgaStream);
        uint dataOffset = ParserUtils.ReadUInt32(sgaStream);

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
            
            archive.AddDrive(new SgaDrive(driveAlias, driveName, archive, rootFolder, firstFolder, lastFolder, firstFile, lastFile));
        }

        // Read folder definitions
        for (int i = 0; i < folderCount; i++)
        {
            uint nameOffset = ParserUtils.ReadUInt32(sgaStream);
            uint firstFolder = ParserUtils.ReadUInt16(sgaStream);
            uint lastFolder = ParserUtils.ReadUInt16(sgaStream);
            uint firstFile = ParserUtils.ReadUInt16(sgaStream);
            uint lastFile = ParserUtils.ReadUInt16(sgaStream);

            uint nameStart = nameOffset + 180 + nameListOffset;
            string folderName = ParserUtils.ReadDynamicString(sgaStream, nameStart);
            
            var folder = new SgaFolder(folderName, firstFolder, lastFolder, firstFile, lastFile);
            folderList.Add(folder);
            
        }

        // Read file definitions
        for (int i = 0; i < fileCount; i++)
        {
            uint nameOffset = ParserUtils.ReadUInt32(sgaStream);
            StorageType storageFlag = ReadStorageType(sgaStream, isIc);
            uint rawDataOffset = ParserUtils.ReadUInt32(sgaStream);
            uint compressSize = ParserUtils.ReadUInt32(sgaStream);
            uint decompressSize = ParserUtils.ReadUInt32(sgaStream);

            uint nameStart = nameOffset + 180 + nameListOffset;
            string fileName = ParserUtils.ReadDynamicString(sgaStream, nameStart);

            var file = new SgaFile(fileName, storageFlag, rawDataOffset + dataOffset, compressSize, decompressSize);
            fileList.Add(file);
        }


        // assign all entries to drives 
        foreach (SgaDrive drive in archive.Drives)
        {
            for (int i = 0; i < (drive.EndFolder - drive.StartFolder); i++)
                folderList[i].Drive = drive;

            for (int i = 0; i < (drive.EndFile - drive.StartFile); i++)
                fileList[i].Drive = drive;
            
            drive.RootFolder = folderList[drive.RootFolderIndex];
        }

        // create folder structure
        foreach (SgaFolder folder in folderList)
        {
            for (int i = (int)folder.StartFolder; i < folder.EndFolder; i++)
            {
                folder._contents.Add(folderList[i]);
                folderList[i].Parent = folder;
            }
            for (int i = (int)folder.StartFile; i < folder.EndFile; i++)
            {
                folder._contents.Add(fileList[i]);
                fileList[i].Parent = folder;
            }
        }
    }

    public void Write(SgaArchive archive, Stream sgaStream)
    {
        // Mock implementation. For testing only. Will be replaces by actual implementation when i will be satisfied by the public interface.
        Console.WriteLine("Archive name: {0}",archive.ArchiveName);
        Console.WriteLine("Drives:");

        foreach (var drive in archive.Drives)
        {
            Console.WriteLine("    Drive name: {0}", drive.Name);
            Console.WriteLine("    Drive alias: {0}", drive.Alias);

            Stack<Tuple<SgaEntry, int>> stack = new Stack<Tuple<SgaEntry, int>>();
            stack.Push(new Tuple<SgaEntry, int>(drive.RootFolder,0));

            while(stack.Count > 0)
            {
                var item = stack.Pop();
                SgaEntry entry = item.Item1;

                if (entry is SgaFile file)
                {
                    Console.WriteLine(new string(' ', item.Item2 *2) + $"    File: {file.Name}");
                    //file.Open();
                }
                else if (entry is SgaFolder folder)
                {
                    Console.WriteLine(new string(' ', item.Item2 *2) + $"    Folder: {folder.Name}");
                    // Push subentries onto the stack
                    for (int i = folder.Contents.Count - 1; i >= 0; i--) // reverse to maintain order
                    {
                        stack.Push(new Tuple<SgaEntry, int>(folder.Contents[i], item.Item2 + 1));
                    }
                }
            }
        }
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
}
