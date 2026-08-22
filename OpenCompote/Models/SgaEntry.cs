namespace OpenCompote.SGA;

/// <summary>
/// Represents an item in the SGA archive like file or folder.
/// </summary>
public abstract class SgaEntry
{
    /// <exclude />
    protected string _name = "";

    /// <summary>
    /// Gets or sets the name of the entry in the SGA archive. Entry name must be a valid sga name. For more info see <see href="/examples/naming.html">File/Folder naming restrictions</see>.
    /// </summary>
    /// <exception cref="InvalidOperationException">The SGA archive for this folder was open in readonly mode.</exception>
    /// <exception cref="ObjectDisposedException">The SGA archive for this folder has been disposed, or this entry is deleted.</exception>
    /// <exception cref="ArgumentException">The <paramref name="value"/> is not a valid sga entry name, or entry with this name already exists in the parent folder.</exception>
    /// <exception cref="ArgumentNullException">The <paramref name="value"/> is <see langword="null"/>.</exception>
    public string Name
    {
        get
        {
            ThrowIfDeleted();
            return _name;
        }
        set
        {
            // Validate if the entry is open and writable.
            ThrowIfDeleted();
            if(Drive!.Archive!.Mode == SgaMode.Read)
                throw new InvalidOperationException("Cannot write to an archive opened in read-only mode.");
            
            // Validate if the new name is valid.
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
            string trimmedName = value.Trim();

            // Quick exit when the name did not changed.
            if(trimmedName.Equals(_name, StringComparison.OrdinalIgnoreCase))
                return;

            SgaNameValidator.ValidateEntryName(trimmedName);

            // If this is not a root folder we also need to update the parent Dictionary.
            if(Parent != null)
            {
                if(!Parent._entries.TryAdd(trimmedName,this))
                    throw new ArgumentException($"Sga entry named '{trimmedName}' already exists.");

                Parent._entries.Remove(_name);
            }

            // Set the new value.
            _name = trimmedName;
        }
    }

    /// <summary>
    /// Gets the relative path of the entry in the SGA drive.
    /// </summary>
    public string Path
    {
        get
        {
            ThrowIfDeleted();
            var pathParts = new List<string>();
            
            SgaEntry? current = this;
            while (current != null)
            {
                if(current._name != "")
                    pathParts.Add(current._name);

                current = current.Parent;
            }
            
            pathParts.Reverse();
            return string.Join("\\", pathParts);
        }
    }

    /// <summary>
    /// Gets the parent drive of this entry.
    /// </summary>
    public SgaFolder? Parent {get; internal set;}

    /// <summary>
    /// Gets the SGA drive that the entry belongs to.
    /// </summary>
    public SgaDrive? Drive {get; internal set;}
    
    internal abstract void Delete(bool subDelete);

    /// <summary>
    /// Deletes the entry and all its sub entries from the archive.
    /// </summary>
    /// <exception cref="InvalidOperationException">The parent <see cref="SgaArchive"/> for this entry was opened in readonly mode.</exception>
    /// <exception cref="ObjectDisposedException">The parent <see cref="SgaArchive"/> for this entry was already closed.</exception>
    public void Delete()
    {
        Delete(false);
    }

    protected internal void ThrowIfDeleted()
    {
        ObjectDisposedException.ThrowIf(Drive == null || Drive.Archive == null,this);
        Drive.Archive.ThrowIfDisposed();
    }
}
