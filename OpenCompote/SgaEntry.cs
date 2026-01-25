using System.ComponentModel.DataAnnotations;
using System.Reflection.Metadata.Ecma335;

namespace OpenCompote.SGA;

public abstract class SgaEntry
{
    protected string _Name = "";

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

    public SgaFolder? Parent {get; internal set;}
    public SgaDrive? Drive {get; internal set;}
    internal abstract void Delete(bool subDelete);

    public void Delete()
    {
        Delete(false);
    }
    protected void ThrowIfDeleted()
    {
        ObjectDisposedException.ThrowIf(Drive == null || Drive.Archive == null,this);
        Drive.Archive.ThrowIfDisposed();
    }
}
