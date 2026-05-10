using System.IO;
using OpenCompote;
using OpenCompote.SGA;
using Xunit;

namespace OpenCompote.SGA.Tests;

/// <summary>
/// Tests for SgaVersionDetector class. These tests are the same for all SGA archive versions.
/// </summary>
public class SgaVersionDetectorTest
{
    [Fact] // Valid SGA version detection test
    public void Detect_ReturnsV2_ForValidSgaArchive()
    {
        using FileStream stream = File.Open("../../../Parsers/testFiles/Ok/empty.sga", FileMode.Open, FileAccess.Read, FileShare.Read);

        SgaVersion version = SgaVersionDetector.Detect(stream);

        Assert.Equal(SgaVersion.V2, version);
    }

    [Fact] // Magic word is invalid. (Opened file is not sga file.)
    public void Detect_ThrowsInvalidDataException_ForInvalidMagic()
    {
        using FileStream stream = File.Open("../../../Parsers/testFiles/Nok/header-magic.sga", FileMode.Open, FileAccess.Read, FileShare.Read);

        var exception = Assert.Throws<InvalidDataException>(() => SgaVersionDetector.Detect(stream));

        Assert.Equal("File is not a valid SGA Archive. (invalid magic byte)", exception.Message);
    }

    [Fact] // Version is incorrect (Sga version attribute is set to value that does not corresponds to any existing sga version.)
    public void Detect_ThrowsInvalidDataException_ForMalformedVersion()
    {
        using FileStream stream = File.Open("../../../Parsers/testFiles/Nok/header-version-malformed.sga", FileMode.Open, FileAccess.Read, FileShare.Read);

        var exception = Assert.Throws<InvalidDataException>(() => SgaVersionDetector.Detect(stream));

        Assert.Equal("SGA version '165' is not supported or is invalid.", exception.Message);
    }
}
