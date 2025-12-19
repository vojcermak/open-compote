// See https://aka.ms/new-console-template for more information
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

using(var newSga = new SgaArchive(new FileStream(@"./test.sga", FileMode.CreateNew), SgaMode.Create, SgaVersion.V2))
{
    Console.WriteLine(newSga.ArchiveName);
}

