namespace OpenCompote.SGA;

/// <summary>
/// Represents a folder within a SGA archive.
/// </summary>
public class SgaFolder: SgaEntry
{
    internal readonly Dictionary<string, SgaEntry> _entries;
    private readonly IReadOnlyCollection<SgaEntry> _contentCollection;
    
    /// <summary>
    /// Gets the collection of entries that are currently in the current folder.
    /// </summary>
    public IReadOnlyCollection<SgaEntry> Contents
    {
        get {
            ThrowIfDeleted();
            return _contentCollection;
        }
    }

    internal SgaFolder(string path, SgaDrive drive, SgaFolder? parent)
    {   
        _entries = new Dictionary<string, SgaEntry>(StringComparer.OrdinalIgnoreCase);
        _contentCollection = _entries.Values;
        Drive = drive;
        Parent = parent;

        _name = path.Split('\\').Last();
    }

    /// <summary>
    /// Creates an empty <see cref="SgaFolder"/> with <paramref name="name"/> in the current folder.
    /// </summary>
    /// <param name="name">
    /// The name of the folder to be created. Folder name must be a valid sga name. for more info see <see href="/examples/naming.html">File/Folder naming restrictions</see>.
    /// </param>
    /// <returns>New empty subfolder.</returns>
    /// <exception cref="InvalidOperationException">The SGA archive for this folder was open in readonly mode.</exception>
    /// <exception cref="ObjectDisposedException">The SGA archive for this folder has been disposed, or the folder is deleted.</exception>
    /// <exception cref="ArgumentException">The <paramref name="name"/> is not a valid sga entry name, or entry with this name already exists in this folder.</exception>
    /// <exception cref="ArgumentNullException">The <paramref name="name"/> is <see langword="null"/>.</exception>
    public SgaFolder AddFolder(string name)
    {
        ThrowIfDeleted(); // Test if this folder was deleted.
        if(Drive!.Archive!.Mode == SgaMode.Read)
            throw new InvalidOperationException("Writing is not supported in this mode.");

        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        string trimmedName = name.Trim();
        SgaNameValidator.ValidateEntryName(trimmedName);
        
        SgaFolder newFolder = new SgaFolder(Path + '\\' + trimmedName, Drive!, this);
        
        if(!_entries.TryAdd(trimmedName, newFolder))
            throw new ArgumentException($"Sga entry named '{trimmedName}' already exists.");
        
        return newFolder;
    }

    /// <summary>
    /// Created an empty <see cref="SgaFile"/> in the current folder.
    /// </summary>
    /// <param name="name">The name of the new file. File name must be a valid sga name. for more info see <see href="/examples/naming.html">File/Folder naming restrictions</see>.</param>
    /// <param name="type">The storage type of the new file.</param>
    /// <returns>New empty file.</returns>
    /// <exception cref="InvalidOperationException">The SGA archive for this folder was open in readonly mode.</exception>
    /// <exception cref="ObjectDisposedException">The SGA archive for this folder has been disposed, or this folder is deleted.</exception>
    /// <exception cref="ArgumentException">The <paramref name="name"/> is not a valid sga file name, or entry with this name already exists in this folder.</exception>
    /// <exception cref="ArgumentNullException">The <paramref name="name"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The <paramref name="type"/> is <see langword="null"/> or invalid.</exception>
    public SgaFile AddFile(string name, StorageType type)
    {
        ThrowIfDeleted(); // Test if this folder was deleted.
        if(Drive!.Archive!.Mode == SgaMode.Read)
            throw new InvalidOperationException("Writing is not supported in this mode.");

        if (!Enum.IsDefined(type))
            throw new ArgumentOutOfRangeException("Invalid file storage type value.");
        
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        string trimmedName = name.Trim();
        SgaNameValidator.ValidateEntryName(trimmedName);

        SgaFile newFile = new SgaFile(trimmedName, type, Drive, this);
        
        if(!_entries.TryAdd(newFile.Name, newFile))
            throw new ArgumentException($"Sga entry named '{trimmedName}' already exists.");
        
        return newFile;
    }

    public SgaEntry? GetEntry(string path)
    {
        ThrowIfDeleted(); // Test if the folder is deleted.
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        // Normalize separators and remove leading/trailing ones.
        path = path.Replace('\\', '/').Trim('/');

        if (string.IsNullOrEmpty(path))
            return this;

        string[] parts = path.Split(
            '/',
            StringSplitOptions.RemoveEmptyEntries);

        SgaFolder current = this;
        for (int i = 0; i < parts.Length; i++)
        {
            string part = parts[i];

            if (!current._entries.TryGetValue(part, out SgaEntry? entry))
                return null;

            // Last component = requested entry.
            if (i == parts.Length - 1)
                return entry;

            // We still have path components, so this must be a folder.
            if (entry is not SgaFolder folder)
                return null;

            current = folder;
        }
        return null;
    }
    
    internal override void Delete(bool subDelete)
    {
        ThrowIfDeleted();

        if(Drive!.Archive!.Mode == SgaMode.Read)
            throw new InvalidOperationException("Deleting is not supported in this mode.");

        foreach (var item in _entries.Values)
        {
            item.Delete(true);
        }
        
        if(!subDelete)
            Parent?._entries.Remove(_name);
        
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
