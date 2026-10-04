using System.Runtime.InteropServices;

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
    /// <exception cref="ObjectDisposedException">The parent archive was already closed.</exception>
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
            if(_archive!.Mode == SgaMode.Read)
                throw new InvalidOperationException("Cannot write to an archive opened in read-only mode.");

            _alias = SgaNameValidator.TrimDriveName(value);
        }
    }

    /// <summary>
    /// Gets or sets the name of the drive. Name must contain only valid characters and must be 64 characters long or shorter.
    /// Longer names are trimmed to 64 characters.
    /// </summary>
    /// <exception cref="InvalidOperationException">Setter throws this exception when the parent archive was opened in read-only mode.</exception>
    /// <exception cref="ArgumentException">Setter throws this exception when the new name is not valid drive name.</exception>
    /// <exception cref="ObjectDisposedException">The parent archive was already closed.</exception>
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
    /// Gets the collection of entries that are currently in the current folder.
    /// </summary>
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
    /// <remarks>This property is <see langword="null"/> when this drive is deleted.</remarks>
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
    /// Deletes the drive and all its contents from the archive.
    /// </summary>
    /// <exception cref="InvalidOperationException">The parent <see cref="SgaArchive"/> was open in readonly mode.</exception>
    /// <exception cref="ObjectDisposedException">The parent <see cref="SgaArchive"/> has already been closed.</exception>
    /// <remarks> 
    ///     <para>When <see cref="SgaDrive"/> is deleted it is removed from the <see cref="SgaArchive.Drives"/> list and the <see cref="SgaDrive.Archive"/> property is set to <see langword="null"/></para>
    ///     <para>Deleting already deleted drive do not change the state of the drive or throw any exception.</para>
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

    // ------------------------ Extending functions ------------------------
    // List of future ideas.
    /// <summary>
    /// NOT IMPLEMENTED! DO NOT USE
    /// </summary>  
    /// <exclude />
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
}
