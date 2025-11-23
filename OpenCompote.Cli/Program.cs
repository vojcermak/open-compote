// See https://aka.ms/new-console-template for more information
using System.IO.Compression;
using System.Text;
using OpenCompote.SGA;

/*
- Pack archive
- Unpack archive
- List
- Get specific file/folder/drive
- Add specific file/folder/drive
- remove specific file/folder/drive
*/
SgaArchive sgaArchive = SgaArchiveFile.Open(@"/home/Vojta/Dokumenty/coh2_modding/sgas/DOW2_Ext_D.sga", 0);