using System.ComponentModel.DataAnnotations;

namespace OpenCompote.SGA;

public abstract class SgaEntry
{
    public required string Name { get; set; }
    public SgaFolder? Parent {get; internal set;}
    public SgaDrive? Drive {get; internal set;}
}
