namespace OpenCompote.SGA;

/// <summary>
/// Specifies values for interacting with sga archive
/// </summary>
public enum SgaMode
{    
    /// <summary>
    /// Only reading functions are permitted.
    /// </summary>
    Read,
    /// <summary>
    /// Only creating new entries is permitted.
    /// </summary>
    Create,
    /// <summary>
    /// Both read and write operation are permitted.
    /// </summary>
    Write
}
