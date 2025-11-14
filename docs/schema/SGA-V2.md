## Archive Header

| Start | Stop | Size  | Type              | Name         |
| ----- | ---- | ----- | ----------------- | ------------ |
| 0     | 7    | 8     | ASCII             | Magic word   |
| 8     | 11   | 4     | UInt-32           | Version      |
| 12    | 27   | 16    | MD5 Hash          | File hash    |
| 28    | 155  | 112   | String (utf-16-le)| Archive name |
| 156   | 171  | 16    | MD5 Hash          | TOC hash     |
| 172   | 175  | 4     | UInt-32           | TOC size     |
| 176   | 179  | 4     | UInt-32           | Data offset  |

1. Magic word - Used to identify a file as an SGA archive. Always contains an ASCII string with the value "ARCHIVE_".
2. Version - Used to identify the exact version of sga archive. For SGA V2, it should always be 2. 

> Other SGA versions can divide this field into major and minor(platform) values, but I couldn`t verify this for SGA v2 archives. 

3. File hash - Used for [error correction](./Error-Correction).
4. Archive name - UTF-16-LE string with the name of the archive. This name does not need to be the same as the name of the file itself.
5. TOC hash - Used for [error correction](./Error-Correction).
6. TOC size - Table of contents size in bytes. 
7. Data offset - The starting position of the data block.

## Table of Contents (TOC)
The table of contents is located directly after the archive header. TOC is divided into 5 parts: TOC header, Drive definition list, Folder definition list, File definition list and name list. 

### TOC Header

| Start | Stop | Size  | Type              | Name         |
| ----- | ---- | ----- | ----------------- | ------------ |
| 0     | 3    | 4     | UInt-32           | Drive offset |
| 4     | 5    | 2     | UInt-16           | Drive count  |
| 6     | 9    | 4     | UInt-32           | Folder offset|
| 10    | 11   | 2     | UInt-16           | Folder count |
| 12    | 15   | 4     | UInt-32           | File offset  |
| 16    | 17   | 2     | UInt-16           | File count   |
| 18    | 21   | 4     | UInt-32           | Name offset  |
| 22    | 23   | 2     | UInt-16           | Name count   |

1. Drive offset - Relative offset to start of the drive definition list. (Always 24)
2. Drive count - Count of drives(Root folders) in the archive.
3. Folder offset - Relative offset to start of the folder's definition list. 
4. Folder count - Count of folders in the archive.
5. File offset - Relative offset to the start of the file definitions list.
6. File count - Count of files in archives.
7. Name offset - Relative offset to the start of the name array.
8. Name count - Count of names in the archive.

Offsets represent the count of bytes from the start of the TOC header. (Toc starts after the data offset field, at position 180.) 

### Drives definition

| Start | Stop | Size  | Type              | Name         |
| ----- | ---- | ----- | ----------------- | ------------ |
| 0     | 63   | 64    | String            | Alias        |
| 64    | 127  | 64    | String            | Name         |
| 128   | 129  | 2     | UInt-16           | First folder |
| 130   | 131  | 2     | UInt-16           | Last folder  |
| 132   | 133  | 2     | UInt-16           | First file   |
| 134   | 135  | 2     | UInt-16           | Last file    |
| 136   | 137  | 2     | UInt-16           | Root folder  |

1. Alias - 64 characters long ASCII string representing the name of the drive.
2. Name - 64 characters long ASCII string working as a name alias.

> I'm not sure what the difference is between the Name and Alias fields. Some archives have these values the same, some have different values. 

3. First folder - Index into the folder definition list to the first folder in the drive.
4. Last folder - Index into the folder definition list to the last folder in the drive.
5. First file - Index into the file definition list to the first file in the drive.
6. Last file - Index into the file definition list to the last file in the drive.
7. Root folder - Index into the folder definition of this drive root folder. 

>  File and folder definitions are indexed from 0. The root folder is generally the first folder in the folder list. The last file/folder index is the index of the first object that is not present in the folder. If the first and last indexes have the same value, that means the driver or folder does not contain any folders/files.

### Folders definition

| Start | Stop | Size  | Type              | Name         |
| ----- | ---- | ----- | ----------------- | ------------ |
| 0     | 3    | 4     | UInt-32           | Name offset  |
| 4     | 5    | 2     | UInt-16           | First folder |
| 5     | 6    | 2     | UInt-16           | Last folder  |
| 7     | 8    | 2     | UInt-16           | First file   |
| 8     | 9    | 2     | UInt-16           | Last file    |

1. Name offset - Byte offset to the beginning of the name of this folder in the name list. 
2. First folder - Index into the folder definition list to the first subfolder in the folder.
3. Last folder - Index into the folder definition list to the last subfolder in the folder.
4. First file - Index into the file definition list to the first file in the folder.
5. Last file - Index into the file definition list to the last file in the folder.

### File definition

| Start | Stop | Size  | Type              | Name              |
| ----- | ---- | ----- | ----------------- | ----------------- |
| 0     | 3    | 4     | UInt-32           | Name offset       |
| 0     | 3    | 4     | UInt-32           | Storage Flag      |
| 0     | 3    | 4     | UInt-32           | Data Offset       |
| 0     | 3    | 4     | UInt-32           | Compressed Size   |
| 0     | 3    | 4     | UInt-32           | Decompressed Size |

1. Name offset - Byte offset to the beginning of the name of this folder in the name list. 
2. Storage Flag - Type [file compression](./SGA-V2#storage-flag).

> The difference between the Impossible Creatures archives and DOW archives is the size of the storage flag; Dawn of War uses 4 bytes, whereas Impossible Creatures uses only 1 byte.

3. Data Offset - Relative byte offset to the beginning of the buffer containing the file data in the data block.
4. Compressed Size - Size of the compressed data in the data block. (bytes)
5. Decompressed Size - Size of the decompressed file. (bytes)

#### Storage flag
Storage flag determines if the file is compressed and, if so, what compression type was used. It can be one of three values:

- 0 - Raw
- 16 - Buffer Compressed
- 32 - Stream Compressed

For more info about compression, see [Data block](./Home#data-block).

### Name list
Name list is an Array of ASCII null-terminated strings representing the names of all files and folders in the archive.

## Data Block
Data block is always the same between all versions, for more info see [Data block](./Home#data-block).