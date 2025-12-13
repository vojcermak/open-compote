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

if (args.Length == 0)
{
    Console.WriteLine("Usage: dotnet run -- <path-to-sga-file>");
    return;
}

string sgaPath = args[0];

using (SgaArchive archive = SgaArchiveFile.Open(sgaPath, SgaMode.Read))
{
    Console.WriteLine("Archive name: {0}",archive.ArchiveName);
    Console.WriteLine("Drives:");

    foreach (var drive in archive.Drives)
    {
        Console.WriteLine("    Drive name: {0}", drive.Name);
        Console.WriteLine("    Drive alias: {0}", drive.Alias);

        Stack<SgaEntry> stack = new Stack<SgaEntry>();
        stack.Push(drive.RootFolder);

        while(stack.Count > 0)
        {
            SgaEntry entry = stack.Pop();

            if (entry is SgaFile file)
            {
                Console.WriteLine($"File: {file.Name} Type: {file.StorageType}");
                //file.Open();
            }
            else if (entry is SgaFolder folder)
            {
                Console.WriteLine($"Folder: {folder.Name}");
                // Push subentries onto the stack
                for (int i = folder.Contents.Count - 1; i >= 0; i--) // reverse to maintain order
                {
                    stack.Push(folder.Contents[i]);
                }
            }
        }
    }     
}
