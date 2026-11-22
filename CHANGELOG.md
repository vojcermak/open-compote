# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- Added path-based entry lookup through `SgaArchive.GetEntry`, `SgaDrive.GetEntry`, and `SgaFolder.GetEntry` methods.
- Added documentation and examples for entry paths, path lookup, and file/folder naming restrictions.
- Added methods for importing and exporting entries directly using OS files/folders:
    - `SgaFolder.ImportFromFile` - Imports a file into the folder as a new SGA file.
    - `SgaFolder.ExtractToDirectory` - Extracts the folder and its contents into a subdirectory.
    - `SgaFolder.ImportFromDirectory` - Imports a directory and its contents as a new subfolder.
    - `SgaDrive.ImportFromFile` - Imports a file into the drive as a new SGA file.
    - `SgaDrive.ExtractToDirectory` - Extracts the drive and its contents into a subdirectory.
    - `SgaDrive.ImportFromDirectory` - Imports a directory and its contents as a new folder in the drive.
    - `SgaArchive.ExtractToDirectory` - Extracts all drives and their contents into subdirectories.
    - `SgaArchive.ImportDriveFromDirectory` - Imports a directory and its contents as a new drive.
    - `SgaArchiveFile.ExtractToDirectory` - Extracts an SGA archive directly to a directory.
    - `SgaArchiveFile.CreateFromDirectory` - Creates an SGA archive from the source directory's subdirectories, each imported as a drive.

### Changed
- Drives now expose their root entries directly instead of using a separate root folder.
- Drives and folders are now indexed by case-insensitive names instead of numeric indexes, enforcing unique names and enabling direct lookup.
- Enforced validation and trimming for entry names to enable cross-platform filesystem compatibility.
- Reworked the testing framework to work with the new indexing and divided the model tests to separate folders.
- Improved file metadata handling so `Modified` timestamps are preserved when files are exported.
- Updated timestamp behavior to use UTC consistently for file modification metadata.
- Refined stream disposal logic so modification timestamps are only updated when the file contents actually changed.

### Fixed
- Fixed file export so the exported file keeps the original last-write time when metadata is available.
- Fixed read/write stream behavior to avoid marking files as modified when they were only opened without changes.

### Breaking changes
- The ability to get objects from `SgaArchive.Drives` and `SgaFolder.Contents` properties by numbered indexes is removed.
- the `SgaDrive.RooFolder` was removed and replaces by the ability to access its contents directly by `SgaDrive.Contents`.

## [0.2.0] - 2026-08-09

### Added
- Added the `SgaFile.ExtractToFile` method.
- Added support for Reading and writing SGA-V2 [file metadata](https://vojcermak.github.io/open-compote/schema/SGA-V2.html#file-metadata).
- Added new `SgaFile` properties:
    - `Crc` - Gets the Crc-32 checksum of the file. (Only when supported by archive version.) 
    - `Modified`- Gets or sets the last write time of the file in the archive.
- Added dependencies:
    - System.IO.Hashing - Used for crc-32 calculation.

### Changed
- Added missing input validations and unified exceptions.
- Added missing exception references in the api reference docs.
- Reworked writer API and optimized SGA V2 Parser.
- Reworked tests for core classes.
- Fixed offsets in sga-V2 schema documentation.

## [0.1.0] - 2026-06-20

First beta release of OpenCompote. This is an early preview release: only SGA V2 support is available. All previous releases were only for testing and are unlisted.

### Added

- SGA V2 support.
- Full project Documentation with How-tos, API and Schema documentation.
- CI-CD pipeline for testing and releases.
