using System.Runtime.InteropServices;

namespace OpenCompote.SGA;

/// <summary>
/// Represents a SGA archive drive.
/// </summary>
public class SgaDrive
{
    private string _Alias;
    private string _Name;
    
    /// <summary>
    /// Gets the alias of the drive.
    /// </summary>
    public string Alias
    {
        get
        {
            ThrowIfDeleted();
            return _Alias;
        }
        set
        {
            ThrowIfDeleted();
            if(Archive!.Mode == SgaMode.Read)
                throw new NotSupportedException("Writing is not supported.");
            _Alias = value;
        }
    }

    /// <summary>
    /// Gets the name of the drive.
    /// </summary>
    public string Name
    {
        get
        {
            ThrowIfDeleted();
            return _Name;
        }
        set
        {
            ThrowIfDeleted();
            if(Archive!.Mode == SgaMode.Read)
                throw new NotSupportedException("Writing is not supported.");
            _Name = value;
        }
    }

    /// <summary>
    /// Gets the SGA archive that the drive belongs to.
    /// </summary>
    public SgaArchive? Archive {get; private set;}

    /// <summary>
    /// Gets the RootFolder of this drive.
    /// </summary>
    public SgaFolder RootFolder {get; internal set;}

    internal SgaDrive(string alias, string name, SgaArchive archive)
    {
        _Alias = alias;
        _Name = name;
        Archive = archive;
        RootFolder = new SgaFolder(name, this, null);
    }

    /// <summary>
    /// Deletes the drive from the archive.
    /// </summary>
    /// <exception cref="NotSupportedException">The SGA archive for this drive was open in readonly mode.</exception>
    /// <exception cref="ObjectDisposedException">The SGA archive for this entry has been disposed.</exception>
    public void Delete()
    {
        if(Archive == null)
            return;

        if(Archive.Mode == SgaMode.Read)
            throw new NotSupportedException("Writing is not supported.");

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
    public SgaEntry GetEntry(string entryName)
    {
        throw new NotImplementedException();
    }
}
