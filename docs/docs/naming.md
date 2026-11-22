# Names and Paths

Every file and folder in an archive has a path. You can access the path of a specific entry by calling the `SgaEntry.Path` property. The path returned by this function is fully qualified path of the entry.

The fully qualified path has two parts:

- **Drive name** is the name of the `SgaDrive` containing the entry, converted to upper case and followed by `:`.
- **Entry path**, which lists the entry's containing folders and its name, starting from the drive's root.

For example this path:

```text
DATA:\sound\vehicles\british\uk_aec_mkiii_armoured_car\uk_aec_mkiii_engine_braking.bsc
```
is a path to a file named `uk_aec_mkiii_engine_braking.bsc` inside the `DATA` drive.

Relic's official archiving tools use `/` as the separator, but OpenCompote supports both `/` and `\` for simplicity.

## Path queries

You can find a specific entry by its absolute or relative path using the `GetEntry` methods. Archive-level queries use absolute paths; drive- and folder-level queries use paths relative to that drive or folder. The method returns the matching `SgaFile` or `SgaFolder` as an `SgaEntry`, or `null` if the drive or entry does not exist. All matching is case-insensitive, and leading and trailing whitespace is trimmed.

### Archive level

`GetEntry` on an `SgaArchive` gets a specific SGA entry by its absolute path. The drive name before the colon selects the drive. The rest of the path is resolved from that drive's root. Only the drive name is used for the drive part. For use in the `GetEntry` method It does not need to be in upper case.

```csharp
SgaEntry? entry = archive.GetEntry(@"DATA:\sound\vehicles\british\uk_aec_mkiii_armoured_car\uk_aec_mkiii_engine_braking.bsc");
SgaEntry? sameEntry = archive.GetEntry(entry!.Path);
```

### Drive level

`GetEntry` on an `SgaDrive` is used to get specific entry with a path relative to that drive's root. Do not include the drive name or alias.

```csharp
SgaEntry? entry = dataDrive.GetEntry(@"sound\vehicles\british\uk_aec_mkiii_armoured_car\uk_aec_mkiii_engine_braking.bsc");
```
> Relative paths are resolved from the selected drive or folder and support child entries only. `.` and `..` are not navigation components, so they cannot refer to the current or parent folder.

### Folder level

`GetEntry` on an `SgaFolder` is used to get specific entry with a path relative to that folder. The path can name a direct child or continue through child folders.

```csharp
SgaEntry? entry = vehiclesFolder.GetEntry(@"british\uk_aec_mkiii_armoured_car\uk_aec_mkiii_engine_braking.bsc");
``` 

## File/Folder naming restrictions
OpenCompote enforces these rules for SGA folder and file names to allow easy export on both Windows and Linux:

1. Names are case-insensitive. `file.txt` and `File.txt` cannot coexist in the same folder. Same restriction applies to folder names as well.

2. Following characters are not allowed in the name:
    - `U+0000–U+001F` - ASCII control characters with numeric values between 0 - 31
    - `<` (less than)
    - `>` (greater than)
    - `:` (colon)
    - `"` (double quote)
    - `/` (forward slash)
    - `\` (backslash)
    - `|` (vertical bar or pipe)
    - `?` (question mark)
    - `*` (asterisk)

3. Forbidden names
    - Empty name
    - Names consisting only of whitespaces
    - Names ending with . (period)
    - `.`
    - `..`

4. Whitespace
    
    Leading and trailing whitespace(spaces, tabs, newlines,...) are trimmed during the entry creation.

> These rules are enforced only when creating or changing SGA files or folders through the public API. Existing SGA archives containing names that do not follow these rules can still be opened, but exporting them may fail.

## Drive name and Alias restrictions
Drive names have the same restrictions as file and folder names, while aliases do not have those character restrictions. Both drive names and aliases must be at most 64 characters long. The name and alias setters and `SgaArchive.AddDrive` truncate values longer than 64 characters.