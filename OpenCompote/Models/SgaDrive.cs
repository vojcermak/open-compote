using System.Runtime.InteropServices;

namespace OpenCompote.SGA;

/// <summary>
/// Represents a drive within a SGA archive.
/// </summary>
public class SgaDrive
{
    private string _alias;
    private string _name;
    
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
            _alias = value;
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
            _name = value;
        }
    }

    /// <summary>
    /// Gets the SGA archive that the drive belongs to.
    /// </summary>
    /// <remarks>This property is <see langword="null"/> when this drive is deleted.</remarks>
    public SgaArchive? Archive {get; private set;}

    /// <summary>
    /// Gets the RootFolder of this drive.
    /// </summary>
    public SgaFolder RootFolder {get; internal set;}

    internal SgaDrive(string alias, string name, SgaArchive archive)
    {
        _alias = alias;
        _name = name;
        Archive = archive;
        RootFolder = new SgaFolder(name, this, null);
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
        RootFolder.Delete();
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
