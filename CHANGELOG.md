# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- Added path-based entry lookup through `SgaArchive.GetEntry`, `SgaDrive.GetEntry`, and `SgaFolder.GetEntry` methods.
- Added documentation and examples for entry paths, path lookup, and file/folder naming restrictions.

### Changed
- Drives now expose their root entries directly instead of using a separate root folder.
- Drives and folder are now index by case-insensitive names instead number indexes, enforcing unique names and enabling direct lookup.
- Enforced validation and trim for entry names tu enable cross-platform filesystem compatibility.
- Reworked the testing framework to work with the new indexing and divided the model tests to separate folders.

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
