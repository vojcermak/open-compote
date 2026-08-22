using System;

namespace OpenCompote.SGA;

internal class SgaNameValidator
{
    private static readonly char[] InvalidCharacters =
    [
        '\"', '<', '>', '|', '\0', ':', '*', '?', '\\', '/',
        (char)1, (char)2, (char)3, (char)4, (char)5, (char)6, (char)7, (char)8, (char)9, (char)10,
        (char)11, (char)12, (char)13, (char)14, (char)15, (char)16, (char)17, (char)18, (char)19, (char)20,
        (char)21, (char)22, (char)23, (char)24, (char)25, (char)26, (char)27, (char)28, (char)29, (char)30,
        (char)31
    ];

    public static void ValidateEntryName(string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        if (name == "." || name == "..")
            throw new ArgumentException($"'{name}' is not valid sga name.", nameof(name));

        if (name.IndexOfAny(InvalidCharacters) >= 0)
            throw new ArgumentException(
                $"'{name}' contains characters that are not valid for sga entry name.",
                nameof(name));

        // Windows does not allow filenames or directory names to end
        // with a space or period.
        if (name.EndsWith('.'))
            throw new ArgumentException($"Sga entry name cannot end with a period.", nameof(name));
    }
}
