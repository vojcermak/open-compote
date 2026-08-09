# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.2.0] - 2026-08-09

### Added
- Added the `SgaFile.ExtractToFile` method.
- Added support for Reading and writing SGA-V2 [file metadata](https://vojcermak.github.io/open-compote/schema/SGA-V2.html#file-metadata).
- Added new `SgaFile` properties:
    - `Crc` - Gets the Crc-32 checksum of the file. (Only when supported by archive version.) 
    - `Modified`- Gets or sets the last write time of the file in the archive.
- Added dependencies:
    - System.IO.Hashing - Used for crc-32 calculation.

## Changed
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
