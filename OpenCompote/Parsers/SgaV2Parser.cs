using System.Collections;
using System.Diagnostics.CodeAnalysis;
using OpenCompote.SGA.Parsers.Structs;

namespace OpenCompote.SGA.Parsers;

internal class SgaV2Parser : ISgaParser
{
    public void Parse(SgaArchive archive, Stream sgaStream)
    {
        List<DriveRecord> driveList = new List<DriveRecord>();
        List<FolderRecord> folderList = new List<FolderRecord>();
        List<SgaFile> fileList = new List<SgaFile>();

        byte[] fileHash = ParserUtils.ReadHash(sgaStream);

        archive._archiveName = ParserUtils.ReadWideStaticString(sgaStream, 128);

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
            DriveRecord internalDrive = new DriveRecord(
                ParserUtils.ReadStaticString(sgaStream, 64),
                ParserUtils.ReadStaticString(sgaStream, 64),
                ParserUtils.ReadUInt16(sgaStream),
                ParserUtils.ReadUInt16(sgaStream),
                ParserUtils.ReadUInt16(sgaStream),
                ParserUtils.ReadUInt16(sgaStream),
                ParserUtils.ReadUInt16(sgaStream)
            );

            driveList.Add(internalDrive);
        }

        // Read folder definitions
        for (int i = 0; i < folderCount; i++)
        {
            FolderRecord folder = new FolderRecord(
                ParserUtils.ReadUInt32(sgaStream),
                ParserUtils.ReadUInt16(sgaStream),
                ParserUtils.ReadUInt16(sgaStream),
                ParserUtils.ReadUInt16(sgaStream),
                ParserUtils.ReadUInt16(sgaStream)
            );

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

        // build the archive tree 
        foreach (DriveRecord driveRecord in driveList)
        {
            SgaDrive newDrive = new SgaDrive(driveRecord.DriveAlias, driveRecord.DriveName, archive);
            archive._drives.Add(newDrive);

            Queue<Tuple<FolderRecord, SgaFolder?>> stack = new ();
            stack.Enqueue(new (folderList[driveRecord.RootFolder], null));

            while(stack.Count > 0)
            {
                var item = stack.Dequeue();
                FolderRecord currentRecord = item.Item1;
                SgaFolder? parent = item.Item2;
                
                uint nameStart = currentRecord.NameOffset + 180 + nameListOffset;
                string folderName = ParserUtils.ReadDynamicString(sgaStream, nameStart);

                SgaFolder currentFolder = new SgaFolder(folderName, newDrive, parent);
                
                if(parent == null)
                    newDrive.RootFolder = currentFolder;
                else
                    parent._contents.Add(currentFolder);

                for (ushort i = currentRecord.FirstFolder; i < currentRecord.LastFolder; i++)
                {
                    stack.Enqueue(new(folderList[i], currentFolder));
                }

                for (ushort i = currentRecord.FirstFile; i < currentRecord.LastFile; i++)
                {
                    SgaFile currentFile = fileList[i];
                    currentFile.Drive = newDrive;
                    currentFile.Parent = currentFolder;
                    currentFolder._contents.Add(currentFile);
                }
            }
        }
    }

    public void Write(SgaArchive archive, Stream sgaStream)
    {
        // Currently writing directly into specific file. Only for testing. The final implementation will not use hardcoded paths.
        //using var tempStream = new FileStream("output.bin", FileMode.Create, FileAccess.Write);
        //LogArchive(archive);  

        // Build TOC and Data block in-memory, then write header + TOC + Data to `sgaStream`.
        List<SgaDrive> drives = archive._drives;

        // Flatten folders and files per-drive so that folder/file indices are contiguous per-drive
        List<SgaFolder> folderList = new List<SgaFolder>();
        List<SgaFile> fileList = new List<SgaFile>();

        // We'll traverse drives in order and append their folder trees
        foreach (var drive in drives)
        {
            // iterative preorder traversal to produce folderList
            var stack = new Stack<SgaFolder>();
            if (drive.RootFolder != null)
            {
                stack.Push(drive.RootFolder);
                folderList.Add(drive.RootFolder);
            }

            while (stack.Count > 0)
            {
                var f = stack.Pop();

                // Add files of this folder (collect now but add their names later)
                foreach (var e in f.Contents)
                {
                    if (e is SgaFile sf)
                    {
                        // record file will be appended to fileList, but we also need to keep mapping for names
                        fileList.Add(sf);
                    }

                    if (e is SgaFolder child)
                        folderList.Add(child);
                }

                for(int i = f.Contents.Count-1; i >= 0; i--)
                {
                    if (f.Contents[i] is SgaFolder child)
                        stack.Push(child);
                }
            }
        }
    }

    // Mock implementation. For testing only. Will be replaces by actual implementation when i will be satisfied by the public interface.
    private static void LogArchive(SgaArchive archive)
    {
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
