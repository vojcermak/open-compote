# open-compote

A modern C# library for working with Relic Entertainment's `.sga` archive format. This project provides support for reading and writing SGA archives used in games build using  Essence engine.

## About

`open-compote` is a open source project aimed at providing  mod tools developers and experienced modders with robust tools for working with `.sga` archives. Whether you're developing mods, analyzing game data, or building mod tools, this library offers a simple API for archive manipulation.

> **Current Status:** Early-stage development · Schema documentation complete · V2 parser implemented

## Supported SGA Versions

| Version | Games | Status | Documentation |
|---------|-------|--------|---|
| **V2** | Company of Heroes (original) | ![](./docs/images/check-circle.svg) Supported | [SGA-V2.md](docs/schema/SGA-V2.md) |
| **V4** | Company of Heroes 2 (early) | ![](./docs/images/close-circle.svg) Planned | [SGA-V4.md](docs/schema/SGA-V4.md) |
| **V5** | Company of Heroes 2, Dawn of War II | ![](./docs/images/close-circle.svg) Planned | [SGA-V5.md](docs/schema/SGA-V5.md) |
| **V7** | Company of Heroes 3 | ![](./docs/images/close-circle.svg) Planned | [SGA-V7.md](docs/schema/SGA-V7.md) |

## Key Features

- **Type-safe API** — Full C# integration with comprehensive type definitions
- **Forward-looking** — Comprehensive schema documentation for planned versions (V4, V5, V7)
- **Well-documented** — Detailed format specifications and examples
- **Modular architecture** — Clean separation between parsers and core functionality

## Getting Started

For comprehensive documentation, API reference, and detailed schema specifications, visit our [documentation page](https://vojcermak.github.io/open-compote/).

### Quick Links

- 📚 [Full Documentation](https://vojcermak.github.io/open-compote/)
- 📋 [SGA Format Schemas](docs/schema/)
- 🏗️ [API Documentation](docs/api/)

## Development

The project is organized as a .NET solution with the following components:

- **OpenCompote** — Core library with parsers and archive handling
- **OpenCompote.Cli** — Command-line tools for working with SGA archives
- **tests** — Unit tests ensuring correctness and reliability

## Disclaimer

Not affiliated with Relic Entertainment, Sega, or THQ Nordic. All rights belong to their respective parties.
