namespace OpenCompote.SGA;

/// <summary>
/// Represents a drive within a SGA archive.
/// </summary>
public class SgaDrive
{
    private string _alias;
    private string _name;
    private SgaArchive? _archive;
    internal readonly Dictionary<string, SgaEntry> _entries;
    private readonly IReadOnlyCollection<SgaEntry> _contentCollection;
    
    /// <summary>
    /// Gets or sets the alias of the drive. Alias must be 64 characters long or shorter. Longer input will be trimmed to 64 characters. Alias can be an empty string.
    /// </summary>
    /// <exception cref="InvalidOperationException">Setter throws this exception when the parent archive was opened in read-only mode.</exception>
    /// <exception cref="ArgumentNullException">Setter throws this exception when the new value is <see langword="null"/>.</exception>
    /// <exception cref="ObjectDisposedException">The parent archive was already closed, or this drive is deleted.</exception>
    /// <remarks>Alias cannot be <see langword="null"/>, but can be set to an empty string.</remarks>
    public string Alias
    {
        get
        {
            ThrowIfDeleted();
            return _alias;
        }
        set
        {
            ThrowIfDeleted();
            ArgumentNullException.ThrowIfNull(value);
            if(_archive!.Mode == SgaMode.Read)
                throw new InvalidOperationException("Cannot write to an archive opened in read-only mode.");

            _alias = SgaNameValidator.TrimDriveName(value);
        }
    }

    /// <summary>
    /// Gets or sets the name of the drive. Name must contain only valid characters and must be 64 characters long or shorter.
    /// Longer names are trimmed to 64 characters. 2 drives with with the same name cannot exists in the archive.
    /// </summary>
    /// <exception cref="InvalidOperationException">Setter throws this exception when the parent archive was opened in read-only mode.</exception>
    /// <exception cref="ArgumentException">Setter throws this exception when the new name is not valid drive name, or drive with this name already exists.</exception>
    /// <exception cref="ArgumentNullException">Setter throws this exception when the new name value is <see langword="null"/>.</exception>
    /// <exception cref="ObjectDisposedException">The parent archive was already closed, or this drive is deleted.</exception>
    public string Name
    {
        get
        {
            ThrowIfDeleted();
            return _name;
        }
        set
        {
            ThrowIfDeleted();
            if(_archive!.Mode == SgaMode.Read)
                throw new InvalidOperationException("Cannot write to an archive opened in read-only mode.");

            string validName = SgaNameValidator.ValidateEntryName(value);
            string trimmedName = SgaNameValidator.TrimDriveName(validName);

            // Quick exit when the name did not changed.
            if(trimmedName.Equals(_name, StringComparison.OrdinalIgnoreCase))
                return;

            // try adding the new value. If the name already exists throw an exception.
            if(!Archive._drives.TryAdd(trimmedName,this))
                    throw new ArgumentException($"Sga entry named '{trimmedName}' already exists.");

            // If the new name does not exists remove the old name and changed the drive name to the new value.
            Archive._drives.Remove(_name);
            _name = trimmedName;
        }
    }

    /// <summary>
    /// Gets the collection of entries that are currently in this drive.
    /// </summary>
    /// <exception cref="ObjectDisposedException">The parent archive was already closed, or this drive is deleted.</exception>
    public IReadOnlyCollection<SgaEntry> Contents
    {
        get {
            ThrowIfDeleted();
            return _contentCollection;
        }
    }

    /// <summary>
    /// Gets the SGA archive that the drive belongs to.
    /// </summary>
    /// <exception cref="ObjectDisposedException">The drive is deleted or parent archive was closed(disposed).</exception>
    public SgaArchive Archive
    {
        get
        {
            ThrowIfDeleted();
            return _archive!;
        }
    }

    internal SgaDrive(string alias, string name, SgaArchive archive)
    {
        _alias = alias;
        _name = name;
        _archive = archive;
        _entries = new Dictionary<string, SgaEntry>(StringComparer.OrdinalIgnoreCase);
        _contentCollection = _entries.Values;
    }

    /// <summary>
    /// Creates an empty <see cref="SgaFolder"/> with <paramref name="name"/> in the this drive.
    /// </summary>
    /// <param name="name">
    /// The name of the folder to be created. Folder name must be a valid sga name. for more info see <see href="/examples/naming.html">File/Folder naming restrictions</see>.
    /// </param>
    /// <returns>New empty folder.</returns>
    /// <exception cref="InvalidOperationException">The parent SGA archive was open in readonly mode.</exception>
    /// <exception cref="ObjectDisposedException">The parent SGA archive has been disposed, or this drive is deleted.</exception>
    /// <exception cref="ArgumentException">The <paramref name="name"/> is not a valid sga entry name, or entry with this name already exists in this drive.</exception>
    /// <exception cref="ArgumentNullException">The <paramref name="name"/> is <see langword="null"/>.</exception>
    public SgaFolder AddFolder(string name)
    {
        ThrowIfDeleted(); // Test if this folder was deleted.
        if(_archive!.Mode == SgaMode.Read)
            throw new InvalidOperationException("Writing is not supported in this mode.");

        string trimmedName = SgaNameValidator.ValidateEntryName(name);
        
        SgaFolder newFolder = new SgaFolder(trimmedName, this, null);
        
        if(!_entries.TryAdd(trimmedName, newFolder))
            throw new ArgumentException($"Sga entry named '{trimmedName}' already exists.");
        
        return newFolder;
    }
    
    /// <summary>
    /// Creates an empty <see cref="SgaFile"/> in this drive.
    /// </summary>
    /// <param name="name">The name of the new file. File name must be a valid sga name. for more info see <see href="/examples/naming.html">File/Folder naming restrictions</see>.</param>
    /// <param name="type">The storage type of the new file.</param>
    /// <returns>New empty file.</returns>
    /// <exception cref="InvalidOperationException">The parent SGA archive was open in readonly mode.</exception>
    /// <exception cref="ObjectDisposedException">The parent SGA archive has been disposed, or drive is deleted.</exception>
    /// <exception cref="ArgumentException">The <paramref name="name"/> is not a valid sga file name, or entry with this name already exists in this drive.</exception>
    /// <exception cref="ArgumentNullException">The <paramref name="name"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The <paramref name="type"/> is invalid.</exception>
    public SgaFile AddFile(string name, StorageType type)
    {
        ThrowIfDeleted(); // Test if this folder was deleted.
        if(_archive!.Mode == SgaMode.Read)
            throw new InvalidOperationException("Writing is not supported in this mode.");

        if (!Enum.IsDefined(type))
            throw new ArgumentOutOfRangeException("Invalid file storage type value.");

        string trimmedName = SgaNameValidator.ValidateEntryName(name);

        SgaFile newFile = new SgaFile(trimmedName, type, this, null);
        
        if(!_entries.TryAdd(newFile.Name, newFile))
            throw new ArgumentException($"Sga entry named '{trimmedName}' already exists.");
        
        return newFile;
    }

    /// <summary>
    /// Finds an existing entry inside of this drive by its relative path or <see langword="null"/> when an entry for selected path does not exist.
    /// </summary>
    /// <param name="path">Relative path to the entry.</param>
    /// <returns>Existing <see cref="SgaEntry"/> or <see langword="null"/> when entry with <paramref name="path"/> does not exist.</returns>
    /// <exception cref="ObjectDisposedException">The parent SGA archive has been disposed, or drive is deleted.</exception>
    /// <exception cref="ArgumentException"><paramref name="path"/> is empty string or will be after trimming separators and whitespaces.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
    public SgaEntry? GetEntry(string path)
    {
        ThrowIfDeleted(); // Test if the folder is deleted.
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        // Normalize separators and remove leading/trailing ones.
        path = path.Replace('\\', '/').Trim().Trim('/');

        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        string[] parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);

        if (!_entries.TryGetValue(parts[0], out SgaEntry? firstEntry))
            return null;
        
        if (parts.Length == 1)
            return firstEntry;

        if (firstEntry is not SgaFolder firstFolder)
            return null;

        SgaFolder current = firstFolder;
        for (int i = 1; i < parts.Length; i++)
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

    /// <summary>
    /// Deletes the drive and all its contents from the archive.
    /// </summary>
    /// <exception cref="InvalidOperationException">The parent <see cref="SgaArchive"/> was open in readonly mode.</exception>
    /// <exception cref="ObjectDisposedException">The parent <see cref="SgaArchive"/> has already been closed, or this drive is deleted.</exception>
    /// <remarks> 
    ///     Deleting already deleted drive again throws the <see cref="ObjectDisposedException"/>.
    /// </remarks>
    public void Delete()
    {
        ThrowIfDeleted();

        if(_archive!.Mode == SgaMode.Read)
            throw new InvalidOperationException("Cannot delete from an archive opened in read-only mode.");

        _archive._drives.Remove(Name);
        
        foreach(var item in _contentCollection)
        {
            item.Delete();
        }

        _archive = null;
    }

    private void ThrowIfDeleted()
    {
        ObjectDisposedException.ThrowIf(_archive == null, this);
        _archive.ThrowIfDisposed();
    }
}
