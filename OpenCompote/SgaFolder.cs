using System.Collections.ObjectModel;

namespace OpenCompote.SGA;

public class SgaFolder: SgaEntry
{
    internal readonly List<SgaEntry> _contents;
    private readonly ReadOnlyCollection<SgaEntry> _contentCollection;
    internal uint StartFolder {get;}
    internal uint EndFolder {get;}
    internal uint StartFile {get;}
    internal uint EndFile {get;}

    public ReadOnlyCollection<SgaEntry> Contents
    {
        get {
            ThrowIfDeleted();
            return _contentCollection;
        }
    }

    internal SgaFolder(string name, SgaDrive drive, SgaFolder? parent)
    {   
        _contents = new List<SgaEntry>();
        _contentCollection = new ReadOnlyCollection<SgaEntry>(_contents);
        Drive = drive;
        Parent = parent;
        Name = name;
    }

    internal SgaFolder(string name, uint startFolder, uint endFolder,  uint startFile, uint endFile)
    {
        _contents = new List<SgaEntry>();
        _contentCollection = new ReadOnlyCollection<SgaEntry>(_contents);
        Name = name;
        StartFolder = startFolder;
        EndFolder = endFolder;
        StartFile = startFile;
        EndFile = endFile;
    }

    public SgaFolder AddFolder(string name)
    {
        ThrowIfDeleted();
        SgaFolder newFolder = new SgaFolder(name, Drive!, this);// Drive is not null here
        _contents.Add(newFolder);
        return newFolder;
    }

    public SgaFile AddFile(string name, StorageType type)
    {
        throw new NotImplementedException();
    }

    public override void Delete()
    {
        throw new NotImplementedException();
    }

    public void ExtractToDirectory(string destination, bool overwrite = false)
    {
        throw new NotImplementedException();
    }

    private void ThrowIfDeleted()
    {
        ObjectDisposedException.ThrowIf(Drive == null || Drive.Archive == null,this);
        Drive.Archive.ThrowIfDisposed();
    }
}
