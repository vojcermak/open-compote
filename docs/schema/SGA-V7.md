# SGA V7

## Archive Header

| Start | Stop | Size  | Type              | Name         |
| ----- | ---- | ----- | ----------------- | ------------ |
| 0     | 7    | 8     | ASCII             | Magic word   |
| 8     | 9    | 2     | UInt-16           | Version major|
| 10    | 11   | 2     | UInt-16           | Version minor|
| 12    | 139  | 128   | String (utf-16-le)| Archive name |
| 140   | 143  | 4     | UInt-32           | TOC size     |
| 144   | 147  | 4     | UInt-32           | Data offset  |
| 148   | 151  | 4     | UInt-32           | Unknown      |

1. Magic word - Used to identify a file as an SGA archive. Always contains an ASCII string with the value "ARCHIVE_".
2. Version Major - Used to identify the exact version of the SGA archive. For SGA V7, it should always be 7. 
3. Version Minor - Sometimes called platform. Identifies the platform for which the archive was bundled.
4. Archive name - UTF-16-LE string with the name of the archive. This name does not need to be the same as the name of the file itself.
6. TOC size - Table of contents size in bytes. 
7. Data offset - The starting position of the data block.
8. Unknown - Unknown value, but is always 1.

## Table of Contents (TOC)
The table of contents is located directly after the archive header. TOC is divided into 6 parts: TOC header, Drive definition list, Folder definition list, File definition list, Name list and Hash list. 

### TOC Header

| Start | Stop | Size  | Type              | Name         |
| ----- | ---- | ----- | ----------------- | ------------ |
| 0     | 3    | 4     | UInt-32           | Drive offset |
| 4     | 7    | 4     | UInt-32           | Drive count  |
| 8     | 11   | 4     | UInt-32           | Folder offset|
| 12    | 15   | 4     | UInt-32           | Folder count |
| 16    | 19   | 4     | UInt-32           | File offset  |
| 20    | 23   | 4     | UInt-32           | File count   |
| 24    | 27   | 4     | UInt-32           | Name offset  |
| 28    | 31   | 4     | UInt-32           | Name count   |
| 32    | 35   | 4     | UInt-32           | Hash offset  |
| 36    | 39   | 4     | UInt-32           | Block Size   |

1. Drive offset - Relative offset to start of the drive definition list. (Always 24)
2. Drive count - Count of drives(Root folders) in the archive.
3. Folder offset - Relative offset to start of the folder's definition list. 
4. Folder count - Count of folders in the archive.
5. File offset - Relative offset to the start of the file definitions list.
6. File count - Count of files in archives.
7. Name offset - Relative offset to the start of the name array.
8. Name count - Count of names in the archive.
9. Hash offset - Relative offset to the start of the hash array.
10. Block size - Size of hash block. Used in [Error correction](./Error-Correction.md#newer-error-correction-v7---).

Offsets represent the count of bytes from the start of the TOC header. (Toc starts after the data offset field, at position 152.) 

### Drives definition

| Start | Stop | Size  | Type              | Name         |
| ----- | ---- | ----- | ----------------- | ------------ |
| 0     | 63   | 64    | String (UTF-8)    | Alias        |
| 64    | 127  | 64    | String (UTF-8)    | Name         |
| 128   | 131  | 4     | UInt-32           | First folder |
| 132   | 135  | 4     | UInt-32           | Last folder  |
| 136   | 139  | 4     | UInt-32           | First file   |
| 140   | 143  | 4     | UInt-32           | Last file    |
| 144   | 147  | 4     | UInt-32           | Root folder  |

1. Alias - 64 characters long UTF-8 encoded string representing the name of the drive.
2. Name - 64 characters long UTF-8 encoded string working as a name alias.

> I'm not sure what the difference is between the Name and Alias fields. Some archives have these values the same, some have different values. The Company of Heroes 2 archive viewer shows the Alias as the name of the drive.

3. First folder - Index into the folder definition list to the first folder in the drive.
4. Last folder - Index into the folder definition list to the last folder in the drive.
5. First file - Index into the file definition list to the first file in the drive.
6. Last file - Index into the file definition list to the last file in the drive.
7. Root folder - Index into the folder definition of this drive root folder. 

>  File and folder definitions are indexed from 0. The root folder is generally the first folder in the folder list. The last file/folder index is the index of the first object that is not present in the folder. If the first and last indexes have the same value, that means the driver or folder does not contain any folders/files.

>  Names in the name list, drive names and aliases are converted to lower case letters when they are written to the archive. 

> Fun fact: Archive viewer from Company of Heroes 2 mod tools reads all names from the name list as an ASCII string, which means that files/folders using non-ASCII characters appear as question marks. But archive.exe from game files can write UTF-8 strings without issues. Drive aliases are not affected.

### Folders definition

| Start | Stop | Size  | Type              | Name         |
| ----- | ---- | ----- | ----------------- | ------------ |
| 0     | 3    | 4     | UInt-32           | Name offset  |
| 4     | 7    | 4     | UInt-32           | First folder |
| 8     | 11   | 4     | UInt-32           | Last folder  |
| 12    | 15   | 4     | UInt-32           | First file   |
| 16    | 19   | 4     | UInt-32           | Last file    |

1. Name offset - Byte offset to the beginning of the name of this folder in the name list. 
2. First folder - Index into the folder definition list to the first subfolder in the folder.
3. Last folder - Index into the folder definition list to the last subfolder in the folder.
4. First file - Index into the file definition list to the first file in the folder.
5. Last file - Index into the file definition list to the last file in the folder.

### File definition

| Start | Stop | Size  | Type              | Name              |
| ----- | ---- | ----- | ----------------- | ----------------- |
| 0     | 3    | 4     | UInt-32           | Name offset       |
| 4     | 7    | 4     | UInt-32           | Data Offset       |
| 8     | 11   | 4     | UInt-32           | Compressed Size   |
| 12    | 15   | 4     | UInt-32           | Decompressed Size |
| 16    | 19   | 4     | 32-bit Unix time  | Modified          |
| 20    | 20   | 1     | UInt-8            | Verification type |
| 21    | 21   | 1     | UInt-8            | Storage type      |
| 22    | 25   | 1     | CRC-32            | CRC               |
| 26    | 29   | 1     | UInt-32           | Hash offset       |

1. Name offset - Byte offset to the beginning of the name of this file in the name list. 
2. Data Offset - Relative byte offset to the beginning of the buffer containing the file data in the data block.
3. Compressed Size - Size of the compressed data in the data block. (bytes)
4. Decompressed Size - Size of the decompressed file. (bytes)
5. Modified - Linux timestamp when the file was changed.
6. Verification type - Determines what verification type should be used. For more info, see [Error correction](./Error-Correction.md#newer-error-correction-v7---). 
7. Storage type - type of compression used. 
9. CRC - 32-bit CRC of the compressed file. CRC is always populated even when the Verification type is not set to `crc`. 
10. Hash offset - Byte offset to the beginning of the first hash in the hash list. For more info, see [Error correction](./Error-Correction.md#newer-error-correction-v7---).

#### Storage type:
The storage type flag determines whether the file is compressed and, if so, which compression type was used. It can be one of three values:

- 0 - Uncompress
- 1 - Stream compress
- 2 - Buffer compress

### Name list 
Name list is an Array of UTF-8 null-terminated strings representing the names of all files and folders in the archive.

### Hash list
A Hash list is a combined array of block CRC, MD5 or SHA-1 hashes. For more info, see [Error correction](./Error-Correction.md#newer-error-correction-v7---).

## Data Block
The Data block remains the same between all SGA versions. For more info, see [Data block](index.md#data-block).