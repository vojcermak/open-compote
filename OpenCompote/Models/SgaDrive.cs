using System.Runtime.InteropServices;

namespace OpenCompote.SGA;

/// <summary>
/// Represents a drive within a SGA archive.
/// </summary>
public class SgaDrive
{
    private string _alias;
    private string _name;
    internal readonly Dictionary<string, SgaEntry> _entries;
    private readonly IReadOnlyCollection<SgaEntry> _contentCollection;
    
    /// <summary>
    /// Gets or sets the alias of the drive.
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
            if(Archive!.Mode == SgaMode.Read)
                throw new InvalidOperationException("Cannot write to an archive opened in read-only mode.");

            string trimmedName = SgaNameValidator.ValidateDriveName(value);
            _alias = trimmedName;
        }
    }

    /// <summary>
    /// Gets or sets the name of the drive.
    /// </summary>
    /// <exception cref="InvalidOperationException">Setter throws this exception when the parent archive was opened in read-only mode.</exception>
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
            if(Archive!.Mode == SgaMode.Read)
                throw new InvalidOperationException("Cannot write to an archive opened in read-only mode.");

            string trimmedName = SgaNameValidator.ValidateDriveName(value);
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
    public SgaArchive? Archive {get; private set;}

    internal SgaDrive(string alias, string name, SgaArchive archive)
    {
        _alias = alias;
        _name = name;
        Archive = archive;
        _entries = new Dictionary<string, SgaEntry>(StringComparer.OrdinalIgnoreCase);
        _contentCollection = _entries.Values;
    }

    public SgaFolder AddFolder(string name)
    {
        ThrowIfDeleted(); // Test if this folder was deleted.
        if(Archive!.Mode == SgaMode.Read)
            throw new InvalidOperationException("Writing is not supported in this mode.");

        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        string trimmedName = name.Trim();
        SgaNameValidator.ValidateEntryName(trimmedName);
        
        SgaFolder newFolder = new SgaFolder(trimmedName, this, null);
        
        if(!_entries.TryAdd(trimmedName, newFolder))
            throw new ArgumentException($"Sga entry named '{trimmedName}' already exists.");
        
        return newFolder;
    }

    public SgaFile AddFile(string name, StorageType type)
    {
        ThrowIfDeleted(); // Test if this folder was deleted.
        if(Archive!.Mode == SgaMode.Read)
            throw new InvalidOperationException("Writing is not supported in this mode.");

        if (!Enum.IsDefined(type))
            throw new ArgumentOutOfRangeException("Invalid file storage type value.");
        
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        string trimmedName = name.Trim();
        SgaNameValidator.ValidateEntryName(trimmedName);

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
        if(Archive == null)
            return;

        if(Archive.Mode == SgaMode.Read)
            throw new InvalidOperationException("Cannot delete from an archive opened in read-only mode.");

        Archive.ThrowIfDisposed();

        Archive._drives.Remove(this);
        
        foreach(var item in _contentCollection)
        {
            item.Delete();
        }

        Archive = null;
    }

    private void ThrowIfDeleted()
    {
        ObjectDisposedException.ThrowIf(Archive == null, this);
        Archive.ThrowIfDisposed();
    }

    // ------------------------ Extending functions ------------------------
    // List of future ideas.
    /// <summary>
    /// NOT IMPLEMENTED! DO NOT USE
    /// </summary>  
    /// <exclude />
    internal SgaEntry GetEntry(string entryName)
    {
        throw new NotImplementedException();
    }
}
