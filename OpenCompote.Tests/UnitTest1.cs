using OpenCompote.SGA;
using Xunit.Sdk;


namespace OpenCompote.SGA.Tests;

public class SgaArchiveTest
{
    [Fact]
    public void Constructor_WithNullStream_ThrowsArgumentException()
    {
        MemoryStream stream = new MemoryStream();

        Assert.Throws<Exception>(()=>new SgaArchive(null, SgaMode.Read));
        Assert.Throws<Exception>(()=>new SgaArchive(stream, SgaMode.Create));
    }

}
