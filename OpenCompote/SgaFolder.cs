using System.Collections.ObjectModel;

namespace OpenCompote.SGA;

public class SgaFolder: SgaEntry
{
    internal readonly List<SgaEntry> _contents;
    private readonly ReadOnlyCollection<SgaEntry> _contentCollection;

    public ReadOnlyCollection<SgaEntry> Contents
    {
        get {
            ThrowIfDeleted();
            return _contentCollection;
        }
    }

    internal SgaFolder(string name, SgaDrive drive, SgaFolder? parent)
    {   
        _contents = new List<SgaEntry>();
        _contentCollection = new ReadOnlyCollection<SgaEntry>(_contents);
        Drive = drive;
        Parent = parent;
        _Name = name;
    }

    public SgaFolder AddFolder(string name)
    {
        ThrowIfDeleted(); // Test if this folder was deleted.

        if(Drive!.Archive!.Mode == SgaMode.Read)
            throw new NotSupportedException("Writing is not supported in this mode.");

        SgaFolder newFolder = new SgaFolder(name, Drive!, this);
        _contents.Add(newFolder);
        return newFolder;
    }

    public SgaFile AddFile(string name, StorageType type)
    {
        ThrowIfDeleted(); // Test if this folder was deleted.

        if(Drive!.Archive!.Mode == SgaMode.Read)
            throw new NotSupportedException("Writing is not supported in this mode.");

        SgaFile newFile = new SgaFile(name, type, Drive, this);
        _contents.Add(newFile);
        return newFile;
    }

    internal override void Delete(bool subDelete)
    {
        ThrowIfDeleted();

        if(Drive!.Archive!.Mode == SgaMode.Read)
            throw new NotSupportedException("Deleting is not supported in this mode.");

        foreach (var item in _contents)
        {
            item.Delete(true);
        }
        
        if(!subDelete)
            Parent?._contents.Remove(this);

        Parent = null;
        Drive = null;
        
    }

    // ------------------------ Extending functions ------------------------
    // List of future ideas. 
    public void ExtractToDirectory(string destination, bool overwrite = false)
    {
        throw new NotImplementedException();
    }
}
