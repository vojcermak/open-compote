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
        ThrowIfDeleted(); // Test if this folder was deleted.

        if(Drive!.Archive!.Mode == SgaMode.Read)
            throw new NotSupportedException("Writing is not supported in this mode.");

        SgaFolder newFolder = new SgaFolder(name, Drive!, this);
        _contents.Add(newFolder);
        return newFolder;
    }

    public SgaFile AddFile(string name, StorageType type)
    {
        ThrowIfDeleted(); // Test if this folder was deleted.

        if(Drive!.Archive!.Mode == SgaMode.Read)
            throw new NotSupportedException("Writing is not supported in this mode.");

        SgaFile newFile = new SgaFile(name, type, Drive, this);
        _contents.Add(newFile);
        return newFile;
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
