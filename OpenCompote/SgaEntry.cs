using System.ComponentModel.DataAnnotations;
using System.Reflection.Metadata.Ecma335;

namespace OpenCompote.SGA;

public abstract class SgaEntry
{
    protected string _Name = "";

    /// <summary>
    /// Gets the name of the entry in the zip archive.
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
            if(Drive!.Archive!.Mode == SgaMode.Read)
                throw new NotSupportedException("Writing is not supported.");
            _Name = value;
        }
    }

    /// <summary>
    /// Gets the relative path of the entry in the zip archive.
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
                if(current._Name != "")
                    pathParts.Add(current._Name);

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
    /// Deletes the entry from the archive.
    /// </summary>
    /// <exception cref="NotSupportedException">The SGA archive for this drive was open in readonly mode.</exception>
    /// <exception cref="ObjectDisposedException">The SGA archive for this entry has been disposed.</exception>
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
