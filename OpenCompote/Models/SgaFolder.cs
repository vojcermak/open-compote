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
    /// <exception cref="ObjectDisposedException">This folder was deleted or the parent archive has been disposed.</exception>
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
        if(Drive!.Archive.Mode == SgaMode.Read)
            throw new InvalidOperationException("Writing is not supported in this mode.");

        string trimmedName = SgaNameValidator.ValidateEntryName(name);
        
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
    /// <exception cref="ArgumentOutOfRangeException">The <paramref name="type"/> is invalid.</exception>
    public SgaFile AddFile(string name, StorageType type)
    {
        ThrowIfDeleted(); // Test if this folder was deleted.
        if(Drive!.Archive.Mode == SgaMode.Read)
            throw new InvalidOperationException("Writing is not supported in this mode.");

        if (!Enum.IsDefined(type))
            throw new ArgumentOutOfRangeException("Invalid file storage type value.");
        
        string trimmedName = SgaNameValidator.ValidateEntryName(name);

        SgaFile newFile = new SgaFile(trimmedName, type, Drive, this);
        
        if(!_entries.TryAdd(newFile.Name, newFile))
            throw new ArgumentException($"Sga entry named '{trimmedName}' already exists.");
        
        return newFile;
    }

    /// <summary>
    /// Finds an existing entry inside of this folder by its relative path or <see langword="null"/> when an entry for selected path did not exist.
    /// </summary>
    /// <param name="path">Relative path to the entry.</param>
    /// <returns>Found sgaEntry or null when entry with <paramref name="path"/> does not exist.</returns>
    /// <exception cref="ObjectDisposedException">This folder is deleted or the archive is disposed.</exception>
    /// <exception cref="ArgumentException"><paramref name="path"/> is empty string or will be after trimming separators and whitespaces.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
    public SgaEntry? GetEntry(string path)
    {
        ThrowIfDeleted(); // Test if the folder is deleted.
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        // Normalize separators and remove leading/trailing ones.
        path = path.Replace('\\', '/').Trim().Trim('/');

        ArgumentException.ThrowIfNullOrWhiteSpace(path);

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

        if(Drive!.Archive.Mode == SgaMode.Read)
            throw new InvalidOperationException("Deleting is not supported in this mode.");

        foreach (var item in _entries.Values)
        {
            item.Delete(true);
        }

        if (!subDelete)
        {
            // if the folder is in subFolder remove it from the parent, if is not remove it from drive.
            if(Parent != null)
                Parent._entries.Remove(_name);
            else
                Drive._entries.Remove(_name);
        }
        
        Parent = null;
        Drive = null;   
    }

    // ------------------------ Extending functions ------------------------

    /// <summary>
    /// Creates a new SgaFile and fill it with the contents of the file with <paramref name="filePath"/>.
    /// </summary>
    /// <param name="filePath">File path to the file we want to import.</param>
    /// <param name="type">Storage type that we want to use.</param>
    /// <returns>New sgaFile with contents</returns>
    public SgaFile ImportFromFile(string filePath, StorageType type)
    {
        string fileName = System.IO.Path.GetFileName(filePath);
        using FileStream openedFile = File.OpenRead(filePath);

        SgaFile newFile = AddFile(fileName, type);
        
        using (Stream openedSgaFile = newFile.Open())
            openedFile.CopyTo(openedSgaFile);
        
        newFile.Modified = File.GetLastWriteTimeUtc(filePath);    
        return newFile;
    }

    /// <summary>
    /// Extracts this SgaFolder and its contents to a subdirectory named after the folder in the <paramref name="parentFolder"/>.
    /// </summary>
    /// <param name="parentFolder">The path to the parent directory to which SgaFolder will be exported.</param>
    /// <param name="overwrite">If the value is true, it overwrites existing files. Else, it throws an exception if a file with the same name already exists.</param>
    /// <exception cref="ArgumentException"><paramref name="parentFolder"/> is invalid path, empty or whitespace.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="parentFolder"/> is <see langword="null"/> </exception>
    /// <exception cref="ObjectDisposedException">The SGA archive for this folder has been disposed, or this folder is deleted.</exception>
    /// <exception cref="IOException">
    /// One of the <see cref="SgaFile"/> in this folder is currently open for writing and cannot be exported. 
    /// 
    /// -OR-
    /// 
    /// Other IOException occurs, for more potential exceptions see <see cref="Directory.CreateDirectory(string)"/> and <see cref="File.Open(string, FileMode)"/> methods.
    /// </exception>
    /// <exception cref="InvalidOperationException">Archive <see cref="SgaMode"/> value is invalid.</exception>
    public void ExtractToDirectory(string parentFolder, bool overwrite = false)
    {
        ThrowIfDeleted();
        ArgumentException.ThrowIfNullOrWhiteSpace(parentFolder);

        string folderPath = System.IO.Path.Combine(parentFolder, Name);
        Directory.CreateDirectory(folderPath);

        foreach(var entry in Contents)
        {
            if(entry is SgaFile file)
                file.ExtractToFile(folderPath, overwrite);

            if(entry is SgaFolder folder)
                folder.ExtractToDirectory(folderPath, overwrite);
        }
    }
}
