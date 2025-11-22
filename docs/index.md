This page contains documentation for the open-compote libraries and schema documentation for all supported formats and versions. Currently, only .sga versions 2, 4, 5 and 7 are supported. But I would like to add support for all .sga versions in the future.

If you find any errors with this documentation, please open a new GitHub issue.

# SGA Archive
Relic game archive is a binary file format. It is used for storing compressed folder structures similar to ZIP or RAR formats. SGA format is developed by Relic Entertainment for their Essence game engine. Can be identified by the .sga file extension.

SGA folder structure is similar to the Windows file system. The structure is divided into drives, folders and files. Where a drive functions as a root object of the folder structure, similarly to Windows drives.(`C:\`, `D:\`, ...) But unlike Windows, SGA drives do not contain files directly. Each Drive needs to have one root folder. Each folder can contain files and other subfolders.

List of Essence engine games and associated .sga versions:

<div style="max-width: 600px;">

| Version        | Games                                                          |
| --------       | -------------                                                  |
| [2.0](schema/SGA-V2.md)| Impossible Creatures,<br> Warhammer 40,000: Dawn of War|
| 3.0            | The Outfit                                                     |
| [4.0](schema/SGA-V4.md)| Company of heroes                                      |
| [5.0](schema/SGA-V5.md)| Warhammer 40,000: Dawn of War 2                        |
| 6.0            | Can be created using Company of Heroes 2 archive.exe.          |
| [7.0](schema/SGA-V7.md)| Company of Heroes 2                                    |
| 9.0            | Warhammer 40,000: Dawn of War 3                                |
| 10.0           | Age of Empires 4,<br> Company of Heroes 3                      |

</div>

> SGA archives used by Impossible Creatures and Warhammer 40,000: Dawn of War are both marked as version 2, but both formats are incompatible with each other. More info in [2.0](schema/SGA-V2.md#file-definition).

## SGA structure overview 
SGA archive is divided into three main parts: Archive header, Table of contents and Data block. Position, size and exact fields of all parts are different between SGA versions.

### SGA Archive header
It`s always at the beginning of the file. It always starts with an ASCII string "ARCHVIE_" followed by the version field and other global information about the archive, like its size or checksum. 

### Table of contents (TOC)
The table of contents always contains metadata about files and folders in the archive, like their names, structure and sizes. It is usually positioned directly after the archive header, but in some versions(v5.0) of SGA format can also be positioned at the end of the file.

### Data block
The data block contains the actual content of archived files. In most cases, the files are compressed using ZLIB compression, but SGA also supports storing uncompressed files. All file contents are put after each other without any spacing or metadata. Position, size and compression type are stored in file metadata in TOC.

Beginning of the data block does not need to be aligned with the start of the first file content.