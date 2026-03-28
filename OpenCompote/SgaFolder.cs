using System.Collections.ObjectModel;

namespace OpenCompote.SGA;

public class SgaFolder: SgaEntry
{
    internal readonly List<SgaEntry> _contents;
    private readonly ReadOnlyCollection<SgaEntry> _contentCollection;

    /// <summary>
    /// Gets the collection of entries that are currently in the current folder.
    /// </summary>
    public ReadOnlyCollection<SgaEntry> Contents
    {
        get {
            ThrowIfDeleted();
            return _contentCollection;
        }
    }

    internal SgaFolder(string path, SgaDrive drive, SgaFolder? parent)
    {   
        _contents = new List<SgaEntry>();
        _contentCollection = new ReadOnlyCollection<SgaEntry>(_contents);
        Drive = drive;
        Parent = parent;

        _name = path.Split('\\').Last();
    }

    /// <summary>
    /// Creates an empty subfolder in the current folder.
    /// </summary>
    /// <param name="name">The name of the folder to be created</param>
    /// <returns>New empty subfolder.</returns>
    /// <exception cref="NotSupportedException">The SGA archive for this drive was open in readonly mode.</exception>
    /// <exception cref="ObjectDisposedException">The SGA archive for this entry has been disposed.</exception>
    public SgaFolder AddFolder(string name)
    {
        ThrowIfDeleted(); // Test if this folder was deleted.

        if(Drive!.Archive!.Mode == SgaMode.Read)
            throw new NotSupportedException("Writing is not supported in this mode.");

        SgaFolder newFolder = new SgaFolder(Path + '\\' + name, Drive!, this);
        _contents.Add(newFolder);
        return newFolder;
    }

    /// <summary>
    /// Created an empty file in the current folder.
    /// </summary>
    /// <param name="name">The name of the new file.</param>
    /// <param name="type">The storage type of the new file.</param>
    /// <returns>New empty file.</returns>
    /// <exception cref="NotSupportedException">The SGA archive for this drive was open in readonly mode.</exception>
    /// <exception cref="ObjectDisposedException">The SGA archive for this folder has been disposed.</exception>
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
    /// <summary>
    /// NOT IMPLEMENTED! DO NOT USE
    /// </summary> 
    public void ExtractToDirectory(string destination, bool overwrite = false)
    {
        throw new NotImplementedException();
    }
}
