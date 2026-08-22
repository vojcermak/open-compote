# Paths and Names

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

> These rules are enforced only when creating new sga Files or Folders. If an existing SGA archive contains files or folder with names which does not follow these rules it can open them, but exporting them could fail.