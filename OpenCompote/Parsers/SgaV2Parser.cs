using System.ComponentModel.Design;
using System.IO.Compression;
using System.Runtime.InteropServices;
using OpenCompote.SGA.Parsers.Structs;

namespace OpenCompote.SGA.Parsers;

internal class FolderTest(SgaFolder folder)
{
    public SgaFolder Folder { get; set; } = folder;
    public ushort FirstFolder {get; set;}
    public ushort LastFolder {get; set;}
    public ushort FirstFile {get; set;}
    public ushort LastFile {get; set;}
}

internal class SgaV2Parser : ISgaParser
{
    private const int DRIVE_SIZE = 138;
    private const int FOLDER_SIZE = 12;
    private const int FILE_SIZE = 20;
    private const int ARCHIVE_NAME_LENGTH = 64;
    private const int FILE_HEADER_SIZE = 180;

    public void Parse(SgaArchive archive, Stream sgaStream)
    {
        List<DriveRecord> driveList = new List<DriveRecord>();
        List<FolderRecord> folderList = new List<FolderRecord>();
        List<FileRecord> fileList = new List<FileRecord>();

        byte[] fileHash = ParserUtils.ReadHash(sgaStream);

        archive._archiveName = ParserUtils.ReadWideStaticString(sgaStream, ARCHIVE_NAME_LENGTH);

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

        bool isIc = fileCount != 0 && (nameListOffset - fileOffset)/fileCount == 17;

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

            var file = new FileRecord(nameOffset, storageFlag, rawDataOffset + dataOffset, compressSize, decompressSize);
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
                    FileRecord fileRecord = fileList[i];

                    uint fileNameOffset = fileRecord.NameOffset + 180 + nameListOffset;
                    string fileName = ParserUtils.ReadDynamicString(sgaStream, fileNameOffset);

                    SgaFile currentFile = new SgaFile(fileName,
                                                      fileRecord.StorageType,
                                                      fileRecord.RawDataOffset,
                                                      fileRecord.CompressedSize,
                                                      fileRecord.Size,
                                                      newDrive,
                                                      currentFolder);
                    currentFolder._contents.Add(currentFile);
                }
            }
        }
    }

    public void Write(SgaArchive archive, Stream sgaStream)
    {
        //LogArchive(archive); //Used for debugging
        using var tempStream = new MemoryStream();

        List<DriveRecord> driveList = new List<DriveRecord>();
        List<FolderTest> folderList = new List<FolderTest>();
        List<SgaFile> fileList = new List<SgaFile>();

        // Traverse drives and append their folder trees
        foreach (var drive in archive._drives)
        {
            var stack = new Stack<FolderTest>();
            ushort firstFolder = (ushort)folderList.Count;
            ushort firstFile = (ushort)fileList.Count;

            if (drive.RootFolder != null)
            {
                FolderTest folderTest = new FolderTest(drive.RootFolder);
                stack.Push(folderTest);
                folderList.Add(folderTest);
            }

            while (stack.Count > 0)
            {
                var f = stack.Pop();
                f.FirstFolder = (ushort)folderList.Count;
                f.FirstFile = (ushort)fileList.Count;
                var Contents = f.Folder.Contents;

                // Add files of this folder (collect now but add their names later)
                foreach (var e in Contents)
                {
                    if (e is SgaFile sf)
                    {
                        // record file will be appended to fileList, but we also need to keep mapping for names
                        fileList.Add(sf);
                    }

                    if (e is SgaFolder childFolder)
                    {
                        FolderTest child = new FolderTest(childFolder);
                        folderList.Add(child);
                    }
                }

                for(int i = folderList.Count-1; i >= f.FirstFolder; i--)
                    stack.Push(folderList[i]);

                f.LastFolder = (ushort)folderList.Count;
                f.LastFile = (ushort)fileList.Count;
                
            }
            driveList.Add(new DriveRecord(drive.Name, drive.Alias, firstFolder, (ushort)folderList.Count, firstFile ,(ushort)fileList.Count, 0));
        }

        
        using var toc = new MemoryStream();
        using var nameBuffer = new MemoryStream();
        
        uint folderOffset = (uint)(24 + driveList.Count * DRIVE_SIZE);
        uint fileOffset = folderOffset + (uint)folderList.Count * FOLDER_SIZE;
        uint nameOffset = fileOffset + (uint)fileList.Count * FILE_SIZE;

        // Write TOC Header
        ParserUtils.WriteUInt32(toc, 24);                               // Drive offset
        ParserUtils.WriteUInt16(toc, (ushort)driveList.Count);          // Drive count
        ParserUtils.WriteUInt32(toc, folderOffset);                     // Folder offset
        ParserUtils.WriteUInt16(toc, (ushort)folderList.Count);         // Folder count
        ParserUtils.WriteUInt32(toc, fileOffset);                       // File offset
        ParserUtils.WriteUInt16(toc, (ushort)fileList.Count);           // File count        
        ParserUtils.WriteUInt32(toc, nameOffset);                       // Name offset
        ParserUtils.WriteUInt16(toc, (ushort)(folderList.Count + fileList.Count)); // Name count

        // Write drives
        foreach (var drive in driveList)
        {
            ParserUtils.WriteStaticString(toc, drive.DriveName, 64);
            ParserUtils.WriteStaticString(toc, drive.DriveAlias, 64);
            ParserUtils.WriteUInt16(toc, drive.FirstFolder);
            ParserUtils.WriteUInt16(toc, drive.LastFolder);
            ParserUtils.WriteUInt16(toc, drive.FirstFile);
            ParserUtils.WriteUInt16(toc, drive.LastFile);
            ParserUtils.WriteUInt16(toc, drive.FirstFolder);
        }

        // Write folders
        foreach (var f in folderList)
        {
            uint folderNameOffset = (uint)nameBuffer.Position;
            ParserUtils.WriteDynamicString(nameBuffer, f.Folder.Path);

            ParserUtils.WriteUInt32(toc, folderNameOffset);
            ParserUtils.WriteUInt16(toc, f.FirstFolder);
            ParserUtils.WriteUInt16(toc, f.LastFolder);
            ParserUtils.WriteUInt16(toc, f.FirstFile);
            ParserUtils.WriteUInt16(toc, f.LastFile);
        }

        uint dataOffset = 264; // Add temporary Data offset buffer for the optional file info. This will be replaced later.
        // Write Files
        foreach (var f in fileList)
        {
            uint folderNameOffset = (uint)nameBuffer.Position;
            ParserUtils.WriteDynamicString(nameBuffer, f.Name);

            ParserUtils.WriteUInt32(toc, folderNameOffset);
            ParserUtils.WriteUInt32(toc, WriteStorageType(f.StorageType));
            ParserUtils.WriteUInt32(toc, dataOffset);
            ParserUtils.WriteUInt32(toc, f.CompressedSize);
            ParserUtils.WriteUInt32(toc, f.Size);

            dataOffset += f.CompressedSize + 264;
        }

        // write TOC
        nameBuffer.Seek(0, SeekOrigin.Begin);
        nameBuffer.CopyTo(toc);
        toc.Seek(0, SeekOrigin.Begin);

        byte[]? tocHash = ParserUtils.HashMD5(toc, toc.Length, "DFC9AF62-FC1B-4180-BC27-11CCE87D3EFF");


        // Start writing actual file it self.
        ParserUtils.WriteStaticString(tempStream, "_ARCHIVE", 8); // Write magic world 
        ParserUtils.WriteUInt32(tempStream, (uint)archive.Version); // Write archive version
        
        // Temporarily fill the template hash with zeroes.
        byte[] emptyHash = new byte[16];
        tempStream.Write(emptyHash); 
        
        // Write Archive name
        ParserUtils.WriteWideStaticString(tempStream, archive.ArchiveName, ARCHIVE_NAME_LENGTH);
        
        //Write TOC hash
        tempStream.Write(tocHash);
        
        // Write the rest of the file header
        ParserUtils.WriteUInt32(tempStream, (uint)toc.Length); // TOC size
        ParserUtils.WriteUInt32(tempStream, (uint)(toc.Length + FILE_HEADER_SIZE)); // Data offset
        
        toc.CopyTo(tempStream); // copy the TOC to the new archive

        byte[] emptyMetaData = new byte[264];

        // Write the actual content of the files.
        foreach(var file in fileList)
        {
            tempStream.Write(emptyMetaData);
            using var contents = file.GetResultStream();
            contents.CopyTo(tempStream);
        }

        // Calculate the File hash
        tempStream.Position = 180;
        byte[]? fileHash = ParserUtils.HashMD5(tempStream, tempStream.Length-tempStream.Position, "E01519D6-2DB7-4640-AF54-0A23319C56C3");
        
        // Write the file hash
        tempStream.Position = 12;
        tempStream.Write(fileHash);

        // Copy the new archive to the original position.
        sgaStream.Position = 0;
        tempStream.Position = 0;
        tempStream.CopyTo(sgaStream);
        sgaStream.SetLength(sgaStream.Position);
    }

    // Mock logging. For testing only. this will be replaces when i will be finished with writer.
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

    private static uint WriteStorageType(StorageType type){
        return type switch
        {
            StorageType.Uncompress => 0,
            StorageType.BufferCompress => 16,
            StorageType.StreamCompress => 32,
            _ => throw new Exception("Invalid storage flag value."),
        };
    }
}
