using System.Collections.ObjectModel;

namespace OpenCompote.SGA;

/// <summary>
/// Represents a folder within a SGA archive.
/// </summary>
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
    /// Creates an empty <see cref="SgaFolder"/> with <paramref name="name"/> in the current folder.
    /// </summary>
    /// <param name="name">The name of the folder to be created</param>
    /// <returns>New empty subfolder.</returns>
    /// <exception cref="NotSupportedException">The SGA archive for this folder was open in readonly mode.</exception>
    /// <exception cref="ObjectDisposedException">The SGA archive for this folder has been disposed, or the folder is deleted.</exception>
    public SgaFolder AddFolder(string name)
    {
        ThrowIfDeleted(); // Test if this folder was deleted.
        ArgumentNullException.ThrowIfNull(name);

        if(Drive!.Archive!.Mode == SgaMode.Read)
            throw new NotSupportedException("Writing is not supported in this mode.");

        SgaFolder newFolder = new SgaFolder(Path + '\\' + name, Drive!, this);
        _contents.Add(newFolder);
        return newFolder;
    }

    /// <summary>
    /// Created an empty <see cref="SgaFile"/> in the current folder.
    /// </summary>
    /// <param name="name">The name of the new file.</param>
    /// <param name="type">The storage type of the new file.</param>
    /// <returns>New empty file.</returns>
    /// <exception cref="NotSupportedException">The SGA archive for this folder was open in readonly mode.</exception>
    /// <exception cref="ObjectDisposedException">The SGA archive for this folder has been disposed, or this folder is deleted.</exception>
    public SgaFile AddFile(string name, StorageType type)
    {
        ThrowIfDeleted(); // Test if this folder was deleted.
        ArgumentNullException.ThrowIfNull(name);
        if (!Enum.IsDefined(type))
            throw new ArgumentOutOfRangeException("Invalid file storage type value.");

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

        // Do not delete the root folder. SGA expects the root folder to exists.
        // So i only delete contents and set the name to the default one.
        if(Drive.RootFolder == this)
        {
            Name = Drive.Name;
            _contents.Clear();
            return;
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
    /// <exclude />
    internal void ExtractToDirectory(string destination, bool overwrite = false)
    {
        throw new NotImplementedException();
    }
}
