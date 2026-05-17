namespace OpenCompote.SGA;

/// <summary>
/// Specifies how the archive was opened.
/// </summary>
public enum SgaMode
{    
    /// <summary>
    /// Only reading functions are permitted.
    /// </summary>
    Read,
    /// <summary>
    /// Both read and write operations are permitted, but the archive has been newly created and until the file is closed, the source stream will remain empty.
    /// </summary>
    Create,
    /// <summary>
    /// Both read and write operation are permitted.
    /// </summary>
    Write
}
