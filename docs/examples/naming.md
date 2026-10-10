# Names and Paths

Every file and folder in an archive has a path. You can access the path of a specific entry by calling the `SgaEntry.Path` property. The path returned by this function is fully qualified path of the entry.

The fully qualified path has two parts:

- **Drive name** is the name of the SgaDrive in which is the entry located in upper case followed by `:`.
- **Entry path**, which lists the entry's containing folders and its name, starting from the drive's root.

For example this path:

```text
DATA:\sound\vehicles\british\uk_aec_mkiii_armoured_car\uk_aec_mkiii_engine_braking.bsc
```
is a path to a file named `uk_aec_mkiii_engine_braking.bsc` inside of the `DATA` drive.

Relic in official archiving tools using `/` as separators, but open compote support both `/` and `\` as valid separators for simplicity.

## Path queries

You can find specific entry by its absolute or relative path using the `GetEntry` functions. Archive level queries use absolute path, drive and folder level queries use paths relative to that drive or folder. The method returns the matching `SgaFile` or `SgaFolder` as an `SgaEntry`, or `null` if the drive or entry does not exist. All matching is case-insensitive with leading and trailing whitespaces trimmed.

### Archive level

`GetEntry` on an `SgaArchive` is used to get specific sga entry by its absolute path. The drive name before the colon selects the drive. The rest of the path is resolved from that drive's root. For the drive part only the name must be used, but it does not need to be in upper case.

```csharp
SgaEntry? entry = archive.GetEntry(@"DATA:\sound\vehicles\british\uk_aec_mkiii_armoured_car\uk_aec_mkiii_engine_braking.bsc");
SgaEntry? sameEntry = archive.GetEntry(entry!.Path);
```

### Drive level

`GetEntry` on an `SgaDrive` is used to get specific entry with a path relative to that drive's root. Do not include the drive name or alias.

```csharp
SgaEntry? entry = dataDrive.GetEntry(@"sound\vehicles\british\uk_aec_mkiii_armoured_car\uk_aec_mkiii_engine_braking.bsc");
```
> Relative paths are only relative to the select drive and folder and supports getting child entries only. `.` and `..` are not navigation components, so they cannot be used to refer to the current or a parent folder.

### Folder level

`GetEntry` on an `SgaFolder` is used to get specific entry with a path relative to that folder. The path can name a direct child or continue through child folders.

```csharp
SgaEntry? entry = vehiclesFolder.GetEntry(@"british\uk_aec_mkiii_armoured_car\uk_aec_mkiii_engine_braking.bsc");
``` 

## File/Folder naming restrictions
Open compote enforces this set of rules for sga folder and file names to allow easy export of files and folders on both windows and linux. The rules are:

1. Names are case-insensitive. `file.txt` and `File.txt` cannot coexist in the same folder. Same restriction applies to folder names as well.

2. Following characters are not allowed in the name:
    - `U+0000–U+001F` - ASCII characters which number representation is between 0 - 31 
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

> These rules are enforced only when creating/changing sga Files or Folders through the public API. If an existing SGA archive contains files or folder with names which does not follow these rules it can open them, but exporting them could fail.

## Drive name and Alias restrictions
Drive names have the same restrictions as the file/folder names. The alias does not have this restriction. But on top of the character restrictions there is also a length restriction. Both Drive Name and Alias must be max 64 characters long. Both the Name and Alias setters and `SgaArchive.AddDrive` function does truncate the provided string to 64 characters when is longer.