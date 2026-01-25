using System.Runtime.InteropServices;

namespace OpenCompote.SGA;

public class SgaDrive
{
    private string _Alias;
    private string _Name;
    
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
    public SgaArchive? Archive {get; private set;}
    public SgaFolder RootFolder {get; internal set;}

    internal SgaDrive(string alias, string name, SgaArchive archive)
    {
        _Alias = alias;
        _Name = name;
        Archive = archive;
        RootFolder = new SgaFolder(name, this, null);
    }

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
    public SgaEntry GetEntry(string entryName)
    {
        throw new NotImplementedException();
    }
}
