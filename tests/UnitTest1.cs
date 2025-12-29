using OpenCompote.SGA;
using Xunit.Sdk;


namespace OpenCompote.SGA.Tests;

public class SgaArchiveTest
{
    [Fact]
    public void Constructor_WithNullStream_ThrowsArgumentException()
    {
        // Need to test what happens when stream is null.
        #pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        Assert.Throws<ArgumentNullException>(() => new SgaArchive(null, SgaMode.Read));
        #pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
    }

}
