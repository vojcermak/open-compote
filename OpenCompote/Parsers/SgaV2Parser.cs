using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Net.Mime;
using OpenCompote.SGA.Parsers.Structs;

namespace OpenCompote.SGA.Parsers;

internal struct DriveTest(SgaDrive drive)
{
    public SgaDrive Drive { get; set; } = drive;
    public ushort FirstFolder {get; set;}
    public ushort LastFolder {get; set;}
    public ushort FirstFile {get; set;}
    public ushort LastFile {get; set;}
}

internal struct FolderTest(SgaFolder folder)
{
    public SgaFolder folder { get; set; } = folder;
    public ushort FirstFolder {get; set;}
    public ushort LastFolder {get; set;}
    public ushort FirstFile {get; set;}
    public ushort LastFile {get; set;}
}

internal class SgaV2Parser : ISgaParser
{
    private const int DRIVE_SIZE = 0;
    private const int FOLDER_SIZE = 0;
    private const int FILE_SIZE = 0;

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

        // var curPosition = sgaStream.Position;
        // using var tempStream = new FileStream("toc.bin", FileMode.Create, FileAccess.Write);
        // byte[] buffer = new byte[tocSize];
        // sgaStream.ReadExactly(buffer);
        // sgaStream.Position = curPosition;
        // tempStream.Write(buffer);

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
        using var tempStream = new FileStream("output.bin", FileMode.Create, FileAccess.Write);
        //LogArchive(archive);  

        // Build TOC and Data block in-memory, then write header + TOC + Data to `sgaStream`.
        List<DriveTest> driveList = new List<DriveTest>();

        // Flatten folders and files per-drive so that folder/file indices are contiguous per-drive
        List<SgaFolder> folderList = new List<SgaFolder>();
        List<SgaFile> fileList = new List<SgaFile>();
        List<string> nameList = new List<string>();

        // For mapping names to offsets later
        List<uint> folderNameOffsets = new List<uint>();
        List<uint> fileNameOffsets = new List<uint>();

        // We'll traverse drives in order and append their folder trees
        foreach (var drive in archive._drives)
        {
            // iterative preorder traversal to produce folderList
            var stack = new Stack<SgaFolder>();
            var driveTest = new DriveTest(drive);
            driveTest.FirstFolder = (ushort)folderList.Count;
            driveTest.FirstFile = (ushort)fileList.Count;

            if (drive.RootFolder != null)
            {
                stack.Push(drive.RootFolder);
                folderList.Add(drive.RootFolder);
            }

            while (stack.Count > 0)
            {
                var f = stack.Pop();

                // Add folder name to name list (we'll fill offsets later)
                nameList.Add(f.Name ?? string.Empty);
                folderNameOffsets.Add(0);

                // Add files of this folder (collect now but add their names later)
                foreach (var e in f.Contents)
                {
                    if (e is SgaFile sf)
                    {
                        // record file will be appended to fileList, but we also need to keep mapping for names
                        fileList.Add(sf);
                        nameList.Add(sf.Name ?? string.Empty);
                        fileNameOffsets.Add(0);
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

            driveTest.LastFolder = (ushort)folderList.Count;
            driveTest.LastFile = (ushort)fileList.Count;
            driveList.Add(driveTest);
        }

        
        using var toc = new MemoryStream();

        uint folderOffset = (uint)(24 + driveList.Count * DRIVE_SIZE);
        uint fileOffset = folderOffset + (uint)folderList.Count * FOLDER_SIZE;
        uint nameOffset = fileOffset + (uint)fileList.Count * FILE_SIZE;

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
            ParserUtils.WriteStaticString(toc, drive.Drive.Alias, 64);
            ParserUtils.WriteStaticString(toc, drive.Drive.Name, 64);
            ParserUtils.WriteUInt16(toc, drive.FirstFolder);
            ParserUtils.WriteUInt16(toc, drive.LastFolder);
            ParserUtils.WriteUInt16(toc, drive.FirstFile);
            ParserUtils.WriteUInt16(toc, drive.LastFile);
            ParserUtils.WriteUInt16(toc, 0);
        }

        ushort folderIndex = 1;
        ushort fileIndex = 0;

        foreach (var f in folderList)
        {
            ushort fileCount = (ushort)f.Contents.Count((item)=>{return item is SgaFile;});
            ushort folderCount = (ushort)f.Contents.Count((item)=>{return item is SgaFolder;});
            fileCount += fileIndex;
            folderCount += folderIndex;

            ParserUtils.WriteUInt32(toc, 0);
            ParserUtils.WriteUInt16(toc, folderIndex);
            ParserUtils.WriteUInt16(toc, folderCount);
            ParserUtils.WriteUInt16(toc, fileIndex);
            ParserUtils.WriteUInt16(toc, fileCount);

            folderIndex = folderCount;
            fileIndex = fileCount;
        }

        // Write file records placeholders (nameOffset, storageFlag, dataOffset, compressedSize, decompressedSize)
        foreach (var f in fileList)
        {
            using var contents = f.Open();
            uint size = (uint)contents.Length;

            ParserUtils.WriteUInt32(toc, 0);
            ParserUtils.WriteUInt32(toc, (uint)StorageType.Uncompress);
            ParserUtils.WriteUInt32(toc, 0);
            ParserUtils.WriteUInt32(toc, size);
            ParserUtils.WriteUInt32(toc, size);
        }

        /*long nameListStart = toc.Position;
        // Write name list
        foreach (var nb in nameBytesList)
            toc.Write(nb, 0, nb.Length);

        long tocSize = toc.Length;

        // Now compute data block offsets: dataBlock starts at 180 + tocSize
        uint dataOffset = (uint)(180 + tocSize);

        // Build data block into another MemoryStream and compute per-file data offsets and sizes
        using var dataBlock = new MemoryStream();
        using var dbw = new BinaryWriter(dataBlock, System.Text.Encoding.ASCII, true);

        // We'll collect file metadata: for each file write 256-byte name (padded), 4-byte modified, 4-byte CRC, then file bytes
        var fileRawOffsets = new List<uint>();
        var fileCompressedSizes = new List<uint>();
        var fileDecompressedSizes = new List<uint>();

        foreach (var f in fileList)
        {
            // record offset to start of compressed data (i.e., after metadata)
            uint rawOffset = (uint)dataBlock.Position + 264; // metadata length is 264 bytes
            fileRawOffsets.Add(rawOffset);

            // write metadata: 256-byte filename (ASCII, zero padded)
            var nameBytes = System.Text.Encoding.ASCII.GetBytes(f.Name ?? string.Empty);
            var nameBuf = new byte[256];
            Array.Clear(nameBuf, 0, nameBuf.Length);
            Array.Copy(nameBytes, nameBuf, Math.Min(nameBytes.Length, 256));
            dbw.Write(nameBuf);
            // write modified (placeholder 0)
            dbw.Write((uint)0);
            // write CRC placeholder (0)
            dbw.Write((uint)0);

            // Write file data (uncompressed)
            long dataStart = dataBlock.Position;
            // If SgaFile has a method to open stream, use it; otherwise assume we can access underlying bytes via Open() or Data property
            try
            {
                using var fs = f.Open();
                fs.Seek(0, SeekOrigin.Begin);
                fs.CopyTo(dataBlock);
            }
            catch
            {
                // If opening fails, write zero-length
            }

            uint compressedSize = (uint)(dataBlock.Position - dataStart);
            fileCompressedSizes.Add(compressedSize);
            fileDecompressedSizes.Add(compressedSize);
        }

        // Now go back and fill header and record placeholders
        // Fill header fields in toc stream
        bw.Seek(0, SeekOrigin.Begin);
        bw.Write((uint)24);
        bw.Write((ushort)drives.Count);
        bw.Write((uint)(foldersStart));
        bw.Write((ushort)folderCount);
        bw.Write((uint)(filesStart));
        bw.Write((ushort)fileCount);
        bw.Write((uint)(nameListStart));
        bw.Write((ushort)nameBytesList.Count);

        // Fill drive records: compute per-drive folder/file ranges
        long curFolderIndex = 0;
        long curFileIndex = 0;
        bw.Seek((int)drivesStart, SeekOrigin.Begin);
        foreach (var drive in drives)
        {
            // drive alias and name have already been written; skip them to write the indices
            bw.Seek(64, SeekOrigin.Current);
            bw.Seek(64, SeekOrigin.Current);

            ushort firstFolder = (ushort)curFolderIndex;
            // count folders belonging to this drive: walk its subtree
            int foldersForDrive = 0;
            if (drive.RootFolder != null)
            {
                // count nodes in subtree
                var stack2 = new Stack<SgaFolder>();
                stack2.Push(drive.RootFolder);
                while (stack2.Count > 0)
                {
                    var n = stack2.Pop();
                    foldersForDrive++;
                    foreach (var c in n.Contents)
                        if (c is SgaFolder cf)
                            stack2.Push(cf);
                }
            }

            ushort lastFolder = (ushort)(curFolderIndex + foldersForDrive);
            ushort firstFile = (ushort)curFileIndex;

            // count files in drive
            int filesForDrive = 0;
            if (drive.RootFolder != null)
            {
                var stack3 = new Stack<SgaFolder>();
                stack3.Push(drive.RootFolder);
                while (stack3.Count > 0)
                {
                    var n = stack3.Pop();
                    foreach (var c in n.Contents)
                        if (c is SgaFile ff)
                            filesForDrive++;
                        else if (c is SgaFolder cf)
                            stack3.Push(cf);
                }
            }

            ushort lastFile = (ushort)(curFileIndex + filesForDrive);
            ushort rootFolderIndex = (ushort)curFolderIndex;

            bw.Write(firstFolder);
            bw.Write(lastFolder);
            bw.Write(firstFile);
            bw.Write(lastFile);
            bw.Write(rootFolderIndex);

            curFolderIndex += foldersForDrive;
            curFileIndex += filesForDrive;
        }

        // Fill folder records
        bw.Seek((int)foldersStart, SeekOrigin.Begin);
        // Build a dictionary folder -> index
        var folderIndexMap = new Dictionary<SgaFolder, int>();
        for (int i = 0; i < folderList.Count; i++)
            folderIndexMap[folderList[i]] = i;

        // Build file index map
        var fileIndexMap = new Dictionary<SgaFile, int>();
        for (int i = 0; i < fileList.Count; i++)
            fileIndexMap[fileList[i]] = i;

        for (int i = 0; i < folderList.Count; i++)
        {
            var f = folderList[i];
            uint nameOffset = folderNameOffsets[i];

            // find child folder indices
            int firstChild = -1;
            int lastChild = -1;
            foreach (var c in f.Contents)
            {
                if (c is SgaFolder cf)
                {
                    int idx = folderIndexMap[cf];
                    if (firstChild == -1) firstChild = idx;
                    lastChild = idx;
                }
            }
            ushort firstFolder = firstChild == -1 ? (ushort)0 : (ushort)firstChild;
            ushort lastFolder = firstChild == -1 ? (ushort)0 : (ushort)(lastChild + 1);

            // files
            int firstF = -1;
            int lastF = -1;
            foreach (var c in f.Contents)
            {
                if (c is SgaFile cf)
                {
                    int idx = fileIndexMap[cf];
                    if (firstF == -1) firstF = idx;
                    lastF = idx;
                }
            }
            ushort firstFileIdx = firstF == -1 ? (ushort)0 : (ushort)firstF;
            ushort lastFileIdx = firstF == -1 ? (ushort)0 : (ushort)(lastF + 1);

            ParserUtils.WriteUInt32(toc, nameOffset);
            ParserUtils.WriteUInt16(toc, firstFolder);
            ParserUtils.WriteUInt16(toc, lastFolder);
            ParserUtils.WriteUInt16(toc, firstFileIdx);
            ParserUtils.WriteUInt16(toc, lastFileIdx);
        }

        // Fill file records
        bw.Seek((int)filesStart, SeekOrigin.Begin);
        for (int i = 0; i < fileList.Count; i++)
        {
            var f = fileList[i];
            ParserUtils.WriteUInt32(toc, fileNameOffsets[i]);
            ParserUtils.WriteUInt32(toc, (uint)StorageType.Uncompress);
            // raw data offset relative to data block start
            ParserUtils.WriteUInt32(toc, fileRawOffsets[i]);
            ParserUtils.WriteUInt32(toc, fileCompressedSizes[i]);
            ParserUtils.WriteUInt32(toc, fileDecompressedSizes[i]);
        }

        // Finally write header + toc + dataBlock into provided stream
        // Header (180 bytes total): magic(8), version(4), fileHash(16), archiveName(128), tocHash(16), tocSize(4), dataOffset(4)
        // We'll leave hashes zeroed for now
        ParserUtils.WriteStaticString(sgaStream, "ARCHIVE_", 8);
        ParserUtils.WriteUInt32(sgaStream, 2);
        byte[] zeroHash = new byte[16];
        sgaStream.Write(zeroHash);
        ParserUtils.WriteWideStaticString(sgaStream, archive.ArchiveName ?? string.Empty, 128);
        sgaStream.Write(zeroHash);
        ParserUtils.WriteUInt32(sgaStream, (uint)tocSize);
        ParserUtils.WriteUInt32(sgaStream, dataOffset);*/

        // write TOC
        toc.Seek(0, SeekOrigin.Begin);
        toc.CopyTo(tempStream);

        // write data block
        //dataBlock.Seek(0, SeekOrigin.Begin);
        //dataBlock.CopyTo(sgaStream);
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
