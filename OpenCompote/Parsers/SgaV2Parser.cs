using System.Buffers.Binary;
using System.Security.Cryptography;
using OpenCompote.SGA.Parsers.Structs;

namespace OpenCompote.SGA.Parsers;

internal class SgaV2Parser : ISgaParser
{
    // Archive record sizes
    private const int DRIVE_SIZE = 138;
    private const int FOLDER_SIZE = 12;
    private const int FILE_SIZE = 20;

    private const int FILE_HEADER_SIZE = 180;
    private const int TOC_HEADER_SIZE = 24;
    private const int FILE_METADATA_SIZE = 264;

    // Static length name lengths
    private const int ARCHIVE_NAME_LENGTH = 64;
    private const int DRIVE_NAME_LENGTH = 64;

    // MD5 default hashes
    private const string TOC_HASH_INIT = "DFC9AF62-FC1B-4180-BC27-11CCE87D3EFF";
    private const string FILE_HASH_INIT = "E01519D6-2DB7-4640-AF54-0A23319C56C3";

    /// <summary>
    /// Specifies whether SGA file metadata are present in the archive.
    /// </summary>
    /// <remarks>
    /// This value is determined during parsing and is consistent for the entire archive. During writing is behaves like this:
    /// - <see cref="MetadataState.Present"/> - metadata blocks are written for all files
    /// - <see cref="MetadataState.Missing"/> - no metadata blocks are written
    /// - <c>null</c> → treated as <see cref="MetadataState.Present"/>
    /// </remarks>
    private MetadataState? _metadataState = null;

    public void Parse(SgaArchive archive, Stream sgaStream)
    {
        List<DriveRecord> driveList = new List<DriveRecord>();
        List<FolderRecord> folderList = new List<FolderRecord>();
        List<FileRecord> fileList = new List<FileRecord>();

        sgaStream.Position = 12; // Skip the Magic word and version.

        byte[] fileHash = ParserUtils.ReadHash(sgaStream);

        archive._archiveName = ParserUtils.ReadWideStaticString(sgaStream, ARCHIVE_NAME_LENGTH);

        byte[] tocHash = ParserUtils.ReadHash(sgaStream);

        uint tocSize = ParserUtils.ReadUInt32(sgaStream);
        uint dataOffset = ParserUtils.ReadUInt32(sgaStream);

        byte[]? generatedFileHash = ParserUtils.HashMD5(sgaStream, sgaStream.Length-sgaStream.Position, FILE_HASH_INIT);
        if(generatedFileHash == null || !fileHash.SequenceEqual(generatedFileHash))
            throw new InvalidDataException("File hash invalid.");

        byte[]? generatedTocHash = ParserUtils.HashMD5(sgaStream, tocSize, TOC_HASH_INIT);
        if(generatedTocHash == null || !tocHash.SequenceEqual(generatedTocHash))
            throw new InvalidDataException("Toc hash invalid.");

        if(dataOffset != sgaStream.Position + tocSize)
            throw new InvalidDataException("Data offset invalid.");

        // Read TOC header
        uint driveOffset = ParserUtils.ReadUInt32(sgaStream);
        ushort driveCount = ParserUtils.ReadUInt16(sgaStream);
        uint folderOffset = ParserUtils.ReadUInt32(sgaStream);
        ushort folderCount = ParserUtils.ReadUInt16(sgaStream);
        uint fileOffset = ParserUtils.ReadUInt32(sgaStream);
        ushort fileCount = ParserUtils.ReadUInt16(sgaStream);
        uint nameListOffset = ParserUtils.ReadUInt32(sgaStream);
        ushort nameCount = ParserUtils.ReadUInt16(sgaStream);

        // Validating TOC offsets.
        if(driveOffset != TOC_HEADER_SIZE)
            throw new InvalidDataException("TOC Drive offset invalid.");
        
        if(folderOffset != TOC_HEADER_SIZE + (DRIVE_SIZE * driveCount))
            throw new InvalidDataException("TOC folder offset invalid.");

        if(fileOffset != TOC_HEADER_SIZE + (DRIVE_SIZE * driveCount) + (FOLDER_SIZE * folderCount))
            throw new InvalidDataException("TOC file offset invalid.");

        if(nameListOffset != TOC_HEADER_SIZE + (DRIVE_SIZE * driveCount) + (FOLDER_SIZE * folderCount) + (FILE_SIZE * fileCount))
            throw new InvalidDataException("TOC name offset invalid.");

        bool isIc = fileCount != 0 && (nameListOffset - fileOffset)/fileCount == 17;

        // Read drive definitions
        for (int i = 0; i < driveCount; i++)
        {
            DriveRecord internalDrive = new DriveRecord(
                ParserUtils.ReadStaticString(sgaStream, DRIVE_NAME_LENGTH),
                ParserUtils.ReadStaticString(sgaStream, DRIVE_NAME_LENGTH),
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
            // Basic validation of Folder/File offset of the Drive record.
            // First folder must always be inside of the folder list, because all drives must have at least one folder(root folder)
            if(driveRecord.FirstFolder >= folderList.Count)
                throw new InvalidDataException("Drive FirstFolder index is out of range.");
            
            // Last folder must be bigger then the FirstFolder and must be smaller or equal to drive count.
            if(driveRecord.LastFolder <= driveRecord.FirstFolder || driveRecord.LastFolder > folderList.Count)
                throw new InvalidDataException("Drive LastFolder index is out of range.");

            if(driveRecord.FirstFile > fileList.Count)
                throw new InvalidDataException("Drive FirstFile index is out of range.");
            
            if(driveRecord.LastFile < driveRecord.FirstFile || driveRecord.LastFile > fileList.Count)
                throw new InvalidDataException("Drive LastFile index is out of range.");
            
            // Root folder always must be Greater then or equal to the FirstFolder and Smaller then the LastFolder.
            if(driveRecord.RootFolder < driveRecord.FirstFolder || driveRecord.RootFolder >= driveRecord.LastFolder)
                throw new InvalidDataException("Drive RootFolder index is out of range.");

            SgaDrive newDrive = new SgaDrive(driveRecord.DriveAlias, driveRecord.DriveName, archive);
            archive._drives.Add(newDrive);

            Queue<Tuple<FolderRecord, SgaFolder?>> stack = new ();
            stack.Enqueue(new (folderList[driveRecord.RootFolder], null));

            while(stack.Count > 0)
            {
                var item = stack.Dequeue();
                FolderRecord currentRecord = item.Item1;
                SgaFolder? parent = item.Item2;

                // Basic validation of the Folder/File records in the current record.
                if(currentRecord.FirstFolder > folderList.Count)
                    throw new InvalidDataException("Folder FirstFolder index is out of range.");

                if(currentRecord.LastFolder < currentRecord.FirstFolder || currentRecord.LastFolder > folderList.Count)
                    throw new InvalidDataException("Folder LastFolder index is out of range.");

                if(currentRecord.FirstFile > fileList.Count)
                    throw new InvalidDataException("Folder FirstFile index is out of range.");

                if(currentRecord.LastFile < currentRecord.FirstFile || currentRecord.LastFile > fileList.Count)
                    throw new InvalidDataException("Folder LastFile index is out of range.");

                uint nameStart = currentRecord.NameOffset + FILE_HEADER_SIZE + nameListOffset;
                string folderName = ParserUtils.ReadDynamicString(sgaStream, nameStart, dataOffset);

                SgaFolder currentFolder = new SgaFolder(folderName, newDrive, parent);
                
                // If parent is null that means the currentFolder is a root folder. Else is the parent set as the parent folder of the current folder.
                if(parent == null)
                    newDrive.RootFolder = currentFolder;
                else
                    parent._contents.Add(currentFolder);

                // Loop through the sub folder of this folder and add it to the queue
                for (ushort i = currentRecord.FirstFolder; i < currentRecord.LastFolder; i++)
                {
                    stack.Enqueue(new(folderList[i], currentFolder));
                }

                // Loop through the files in this folder and create them.
                for (ushort i = currentRecord.FirstFile; i < currentRecord.LastFile; i++)
                {
                    FileRecord fileRecord = fileList[i];

                    uint fileNameOffset = fileRecord.NameOffset + FILE_HEADER_SIZE + nameListOffset;
                    string fileName = ParserUtils.ReadDynamicString(sgaStream, fileNameOffset, dataOffset);

                    // If compressed size + Data offset is bigger then the file size throw exception because there is something wrong.
                    if(fileRecord.RawDataOffset + fileRecord.CompressedSize > sgaStream.Length)
                        throw new InvalidDataException("File data offset or size is invalid.");
                    
                    V2FileMetadata metadata = ReadFileMetadata(fileRecord, fileName, sgaStream);

                    SgaFile currentFile = new SgaFile(fileName,
                                                      fileRecord.StorageType,
                                                      fileRecord.RawDataOffset,
                                                      fileRecord.CompressedSize,
                                                      fileRecord.Size,
                                                      metadata.LastModified,
                                                      metadata.CRC,
                                                      newDrive,
                                                      currentFolder);
                    currentFolder._contents.Add(currentFile);
                }
            }
        }
    }

    public void Write(SgaArchive archive, Stream sgaStream)
    {
        List<DriveRecord> driveList = new List<DriveRecord>();
        List<FolderWriterRecord> folderList = new List<FolderWriterRecord>();
        List<SgaFile> fileList = new List<SgaFile>();

        // Traverse drives and append their folder trees
        foreach (var drive in archive._drives)
        {
            var stack = new Stack<FolderWriterRecord>();
            ushort firstFolder = (ushort)folderList.Count;
            ushort firstFile = (ushort)fileList.Count;

            if (drive.RootFolder != null)
            {
                FolderWriterRecord folderTest = new FolderWriterRecord(drive.RootFolder);
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
                        FolderWriterRecord child = new FolderWriterRecord(childFolder);
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
        
        using var nameBuffer = new MemoryStream();
        
        uint folderOffset = (uint)(TOC_HEADER_SIZE + driveList.Count * DRIVE_SIZE);
        uint fileOffset = folderOffset + (uint)folderList.Count * FOLDER_SIZE;
        uint nameOffset = fileOffset + (uint)fileList.Count * FILE_SIZE;

        // Create TOC buffer
        Span<byte> toc = stackalloc byte[(int)nameOffset];

        // Write TOC Header
        BinaryPrimitives.WriteUInt32LittleEndian(toc[..4],    TOC_HEADER_SIZE);           // Drive offset
        BinaryPrimitives.WriteUInt16LittleEndian(toc[4..6],   (ushort)driveList.Count);   // Drive count
        BinaryPrimitives.WriteUInt32LittleEndian(toc[6..10],  folderOffset);              // Folder offset
        BinaryPrimitives.WriteUInt16LittleEndian(toc[10..12], (ushort)folderList.Count);  // Folder count
        BinaryPrimitives.WriteUInt32LittleEndian(toc[12..16], fileOffset);                // File offset
        BinaryPrimitives.WriteUInt16LittleEndian(toc[16..18], (ushort)fileList.Count);    // File count
        BinaryPrimitives.WriteUInt32LittleEndian(toc[18..22], nameOffset);                // Name offset
        BinaryPrimitives.WriteUInt16LittleEndian(toc[22..24], (ushort)(folderList.Count + fileList.Count)); // Name count

        int tocOffset = TOC_HEADER_SIZE;

        // Write drives
        foreach (var drive in driveList)
        {
            System.Text.Encoding.UTF8.GetBytes(drive.DriveName, toc[tocOffset..(tocOffset + 64)]);
            System.Text.Encoding.UTF8.GetBytes(drive.DriveAlias, toc[(tocOffset + 64)..(tocOffset + 128)]);
            BinaryPrimitives.WriteUInt16LittleEndian(toc[(tocOffset + 128)..(tocOffset + 130)], drive.FirstFolder);
            BinaryPrimitives.WriteUInt16LittleEndian(toc[(tocOffset + 130)..(tocOffset + 132)], drive.LastFolder);
            BinaryPrimitives.WriteUInt16LittleEndian(toc[(tocOffset + 132)..(tocOffset + 134)], drive.FirstFile);
            BinaryPrimitives.WriteUInt16LittleEndian(toc[(tocOffset + 134)..(tocOffset + 136)], drive.LastFile);
            BinaryPrimitives.WriteUInt16LittleEndian(toc[(tocOffset + 136)..(tocOffset + 138)], drive.FirstFolder);

            tocOffset += DRIVE_SIZE;
        }

        // Write folders
        foreach (var f in folderList)
        {
            uint folderNameOffset = (uint)nameBuffer.Position;
            ParserUtils.WriteDynamicString(nameBuffer, f.Folder.Path);

            BinaryPrimitives.WriteUInt32LittleEndian(toc[tocOffset..(tocOffset + 4)],   folderNameOffset);
            BinaryPrimitives.WriteUInt16LittleEndian(toc[(tocOffset + 4)..(tocOffset + 6)],f.FirstFolder);
            BinaryPrimitives.WriteUInt16LittleEndian(toc[(tocOffset + 6)..(tocOffset + 8)], f.LastFolder);
            BinaryPrimitives.WriteUInt16LittleEndian(toc[(tocOffset + 8)..(tocOffset + 10)], f.FirstFile);
            BinaryPrimitives.WriteUInt16LittleEndian(toc[(tocOffset + 10)..(tocOffset + 12)], f.LastFile);

            tocOffset += FOLDER_SIZE;
        }

        uint dataOffset = 0; // Set data offset counter to 0;
        // If resulting archive will contain file metadata add the offset for the first file metadata
        if(_metadataState != MetadataState.Missing)
            dataOffset = FILE_METADATA_SIZE;
        
        // Write Files
        foreach (var f in fileList)
        {
            uint folderNameOffset = (uint)nameBuffer.Position;
            ParserUtils.WriteDynamicString(nameBuffer, f.Name);

            BinaryPrimitives.WriteUInt32LittleEndian(toc[tocOffset..(tocOffset + 4)], folderNameOffset);
            BinaryPrimitives.WriteUInt32LittleEndian(toc[(tocOffset + 4)..(tocOffset + 8)], WriteStorageType(f.StorageType));
            BinaryPrimitives.WriteUInt32LittleEndian(toc[(tocOffset + 8)..(tocOffset + 12)], dataOffset);
            BinaryPrimitives.WriteUInt32LittleEndian(toc[(tocOffset + 12)..(tocOffset + 16)], f.CompressedSize);
            BinaryPrimitives.WriteUInt32LittleEndian(toc[(tocOffset + 16)..(tocOffset + 20)], f.Size);

            tocOffset += FILE_SIZE;

            // If new archive file will contain metadata add their size as well, else only add the file size.
            if(_metadataState != MetadataState.Missing)
                dataOffset += f.CompressedSize + FILE_METADATA_SIZE;
            else
                dataOffset += f.CompressedSize;
        }

        uint tocSize = (uint)nameBuffer.Length + nameOffset;

        // Calculate toc hash
        nameBuffer.Seek(0, SeekOrigin.Begin);
        
        Span<byte> seed = stackalloc byte[256];
        int seedLength = System.Text.Encoding.UTF8.GetBytes(TOC_HASH_INIT, seed);

        using var tocHash = IncrementalHash.CreateHash(HashAlgorithmName.MD5);
        tocHash.AppendData("DFC9AF62-FC1B-4180-BC27-11CCE87D3EFF"u8);

        tocHash.AppendData(toc);
        tocHash.AppendData(nameBuffer.ToArray());

        // Create file header:
        Span<byte> fileHeader = stackalloc byte[FILE_HEADER_SIZE];

        "_ARCHIVE"u8.CopyTo(fileHeader);                                                    // Magic word
        BinaryPrimitives.WriteUInt32LittleEndian(fileHeader[8..12], (uint)archive.Version); // SGA version

        // File hash is skipped, because it does not exist yet.

        System.Text.Encoding.Unicode.GetBytes(archive.ArchiveName, fileHeader[28..156]);    // Archive name
        tocHash.GetHashAndReset(fileHeader[156..172]);                                      // TOC hash
        BinaryPrimitives.WriteUInt32LittleEndian(fileHeader[172..176], tocSize);            // TOC size
        BinaryPrimitives.WriteUInt32LittleEndian(fileHeader[176..180], tocSize + FILE_HEADER_SIZE); // Data Offset

        // Setup file hashing function
        using var fileHash = IncrementalHash.CreateHash(HashAlgorithmName.MD5);
        fileHash.AppendData("E01519D6-2DB7-4640-AF54-0A23319C56C3"u8);

        // Write the file header to the fileStream:
        sgaStream.Write(fileHeader);

        // Write the TOC to both the hash function and the output stream
        sgaStream.Write(toc);
        fileHash.AppendData(toc);
        nameBuffer.CopyTo(sgaStream);
        fileHash.AppendData(nameBuffer.ToArray());

        Span<byte> fileMetaData = stackalloc byte[FILE_METADATA_SIZE];
        byte[] buffer = GC.AllocateUninitializedArray<byte>(32 * 1024);

        // Write the actual content of the files.
        foreach(var file in fileList)
        {
            // Do not write file metadata if they were not present in the original file.
            if(_metadataState != MetadataState.Missing)
            {   
                System.Text.Encoding.UTF8.GetBytes(file.Name, fileMetaData[..256]);
                BinaryPrimitives.WriteUInt32LittleEndian(fileMetaData[256..260], ParserUtils.ConvertToSgaTimestamp(file.Modified));
                BinaryPrimitives.WriteUInt32LittleEndian(fileMetaData[260..264], file.Crc ?? 0 );

                sgaStream.Write(fileMetaData);
                fileHash.AppendData(fileMetaData);
            }

            WriteFileContents(file, sgaStream, fileHash, buffer);
        }
        
        // Write the file hash
        sgaStream.Position = 12;
        sgaStream.Write(fileHash.GetHashAndReset());
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
                _ => throw new InvalidDataException("File Storage flag invalid."),
            };   
        }

        uint storageFlag = ParserUtils.ReadUInt32(sgaStream);
        return storageFlag switch
        {
            0 => StorageType.Uncompress,
            16 => StorageType.BufferCompress,
            32 => StorageType.StreamCompress,
            _ => throw new InvalidDataException("File Storage flag invalid."),
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

    /// <summary>
    /// Retrieves file metadata for the specified file.
    /// </summary>
    private V2FileMetadata ReadFileMetadata(FileRecord file, string headerName, Stream sgaStream)
    {   
        // If whe know that archive does not contain any metadata return empty directly.
        if(_metadataState == MetadataState.Missing)
            return new V2FileMetadata();

        long currentPosition = sgaStream.Position;
        sgaStream.Position = file.RawDataOffset - FILE_METADATA_SIZE;

        // Read file metadata into stack buffer.
        Span<byte> buffer = stackalloc byte[FILE_METADATA_SIZE];
        sgaStream.ReadExactly(buffer);

        // Parser the file metadata into usable data.
        string metaFileName = System.Text.Encoding.UTF8.GetString(buffer[..256]).TrimEnd('\0');
        uint modified = BinaryPrimitives.ReadUInt32LittleEndian(buffer[256..260]);
        uint crc = BinaryPrimitives.ReadUInt32LittleEndian(buffer[260..264]);

        DateTimeOffset modifiedDate = DateTimeOffset.FromUnixTimeSeconds(modified);

        sgaStream.Position = currentPosition;

        // If _metadataState is not known test if the metadata are valid. if yes return metadata if not return empty.
        if(_metadataState == null && metaFileName != headerName)
        {
            _metadataState = MetadataState.Missing;
            return new V2FileMetadata();   
        }

        _metadataState = MetadataState.Present;
        return new V2FileMetadata(metaFileName, modifiedDate, crc);
    }

    /// <summary>
    /// Writes the file contents to the archiveStream and the fileHash
    /// </summary>
    private static void WriteFileContents(SgaFile file, Stream archiveStream, IncrementalHash fileHash, byte[] buffer)
    {
        using Stream fileContents = file.GetResultStream();

        long remaining = fileContents.Length;

        while (remaining > 0)
        {
            int readSize = (int)Math.Min(buffer.Length, remaining);
            int bytesRead = fileContents.Read(buffer, 0, readSize);

            fileHash.AppendData(buffer.AsSpan(0, bytesRead));
            archiveStream.Write(buffer.AsSpan(0, bytesRead));
            
            remaining -= bytesRead;
        }
    }

    enum MetadataState
    {
        Present,
        Missing
    }
}
