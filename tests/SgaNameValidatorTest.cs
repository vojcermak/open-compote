using OpenCompote.SGA;

namespace OpenCompote.SGA.Tests;

public class SgaNameValidatorTest
{
    [Fact]
    public void ValidateEntryName_ReturnsOriginalName_ForValidName()
    {
        const string name = "Valid name";

        string result = SgaNameValidator.ValidateEntryName(name);

        Assert.Equal(name, result);
    }

    [Fact]
    public void ValidateEntryName_ReturnsTrimmedName_ForSpacedName()
    {
        const string name = " Valid name ";

        string result = SgaNameValidator.ValidateEntryName(name);

        Assert.Equal("Valid name", result);
    }

    [Theory]
    [InlineData(null, typeof(ArgumentNullException))]
    [InlineData("", typeof(ArgumentException))]
    [InlineData(" ", typeof(ArgumentException))]
    [InlineData("	", typeof(ArgumentException))]
    [InlineData(".", typeof(ArgumentException))]
    [InlineData("..", typeof(ArgumentException))]
    [InlineData(" . ", typeof(ArgumentException))]
    [InlineData(" .. ", typeof(ArgumentException))]
    [InlineData("name.", typeof(ArgumentException))]
    [InlineData(" name. ", typeof(ArgumentException))]
    public void ValidateEntryName_ThrowsArgumentException_ForReservedOrTrailingPeriodName(string? name, Type exceptionType)
    {
        Assert.Throws(exceptionType, () => SgaNameValidator.ValidateEntryName(name!));
    }

    [Fact]
    public void ValidateEntryName_ThrowsArgumentException_ForInvalidCharacters()
    {
        char[] invalidCharacters =
        [
            '"', '<', '>', '|', '\0', ':', '*', '?', '\\', '/',
            (char)1, (char)2, (char)3, (char)4, (char)5, (char)6, (char)7, (char)8, (char)9, (char)10,
            (char)11, (char)12, (char)13, (char)14, (char)15, (char)16, (char)17, (char)18, (char)19, (char)20,
            (char)21, (char)22, (char)23, (char)24, (char)25, (char)26, (char)27, (char)28, (char)29, (char)30,
            (char)31
        ];

        foreach (char invalidCharacter in invalidCharacters)
        {
            Assert.Throws<ArgumentException>(() => SgaNameValidator.ValidateEntryName($"name{invalidCharacter}name"));
        }
    }

    [Fact]
    public void ValidateDriveName_ReturnsNameUnchanged_WhenNameIsAtMost64Characters()
    {
        const string name = "Drive name";

        string result = SgaNameValidator.ValidateDriveName(name);

        Assert.Equal(name, result);
    }

    [Fact]
    public void ValidateDriveName_TruncatesNameTo64Characters()
    {
        string name = new('a', 65);

        string result = SgaNameValidator.ValidateDriveName(name);

        Assert.Equal(64, result.Length);
        Assert.Equal(name[..64], result);
    }
}