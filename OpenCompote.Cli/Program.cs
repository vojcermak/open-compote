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
SgaArchive sgaArchive = SgaArchiveFile.Open(@"../sgas/W40kDataKeys(dow1).sga", 0);

Console.WriteLine("Archive name: {0}",sgaArchive.ArchiveName);
Console.WriteLine("Drives:");

foreach (var drive in sgaArchive.Drives)
{
    Console.WriteLine("    Drive name: {0}", drive.Name);
    Console.WriteLine("    Drive alias: {0}", drive.Alias);
} 