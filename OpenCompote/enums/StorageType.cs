namespace OpenCompote.SGA;

/// <summary>
/// Specifies allowed compression options for files stored in SGA archives.
/// </summary>
/// <remarks>
/// > I am not sure what is the difference between the `StreamCompress` and `BufferCompress`, because files using this storage types are both compressed using 
/// Zlib compression. Probably the game engine load these files differently. 
/// </remarks>
public enum StorageType
{
    /// <summary>
    /// File is stored as uncompressed blob.
    /// </summary>
    Uncompress,
    /// <summary>
    /// File is stored as ZLIB compressed blob.
    /// </summary>
    StreamCompress,
    /// <summary>
    /// File is stored as ZLIB compressed blob.
    /// </summary>
    BufferCompress
}