# open-compote

A modern C# library for working with Relic Entertainment's `.sga` archive format. This project provides support for reading and writing SGA archives used in games built using the Essence engine.

## About

`open-compote` is an open-source project aimed at providing mod tools developers and experienced modders with robust and easy to use tools for working with `.sga` archives. Whether you're developing mods, analysing game data, or building mod tools, this library offers a simple API for archive manipulation.

> [!NOTE]
> **Current Status:**
> **Very early-stage development**. Only SGA V2 is implemented, If you want support for additional versions early, you can contribute with issues, feature requests, or pull requests. Community contributions are welcome. And if you really want something what you can actually use now see link in [Acknowledgments](#acknowledgments).

## Supported SGA Versions

| Version | Games | Status | Documentation |
|---------|-------|--------|---|
| **V2**  | Impossible Creatures,<br> Warhammer 40,000: Dawn of war | ![](./docs/images/check-circle.svg) Supported | [SGA-V2.md](https://vojcermak.github.io/open-compote/schema/SGA-V2.html) |
| **V3**  | The Outfit                                       | ![](./docs/images/close-circle.svg) Planned | |
| **V4**  | Company of Heroes 1                              | ![](./docs/images/close-circle.svg) Planned | [SGA-V4.md](https://vojcermak.github.io/open-compote/schema/SGA-V4.html) |
| **V5**  | Warhammer 40,000: Dawn of War 2                  | ![](./docs/images/close-circle.svg) Planned | [SGA-V5.md](https://vojcermak.github.io/open-compote/schema/SGA-V5.html) |
| **V6**  | Can be created using Company of Heroes 2 archive.exe.| ![](./docs/images/close-circle.svg) Planned
| **V7**  | Company of Heroes 2                              | ![](./docs/images/close-circle.svg) Planned | [SGA-V7.md](https://vojcermak.github.io/open-compote/schema/SGA-V7.html) |
| **V9**  | Warhammer 40,000: Dawn of War 3                  | ![](./docs/images/close-circle.svg) Planned |
| **V10** | Age of Empires 4,<br> Company of Heroes 3        | ![](./docs/images/close-circle.svg) Planned |


## Key Features

- **Type-safe API** — Full C# integration with comprehensive type definitions
- **Forward-looking** — Comprehensive schema documentation for planned versions (V4, V5, V7)
- **Well-documented** — Detailed format specifications and examples
- **Modular architecture** — Clean separation between parsers and core functionality

## Getting Started

For comprehensive documentation, API reference, and detailed schema specifications, visit our [documentation page](https://vojcermak.github.io/open-compote/).

### Quick Links

- 📚 [Full Documentation](https://vojcermak.github.io/open-compote/)
- 📋 [SGA Format Schemas](https://vojcermak.github.io/open-compote/schema/SGA-V2.html)
- 🏗️ [API Documentation](https://vojcermak.github.io/open-compote/api/OpenCompote.SGA.html)

## Development

The project is organised as a .NET solution with the following components:

- **OpenCompote.SGA** — Core library for working with SGA Archives.
- **OpenCompote.Cli** — **Currently only for testing.** Command-line tools for simple manipulation with SGA archives.
- **tests** — Unit tests.

## Acknowledgments
Some resources that were useful while researching and implementing this project.

- [MAK Relic-Tool](https://github.com/MAK-Relic-Tool) – Similar project to this, but made in Python and on top of SGAs it also supports UCS and Chunky files.
- [dow_utils](https://github.com/amorgun/dow_utils) – Dawn of War modding tutorials, file extension overview, and tools.

## Disclaimer

Not affiliated with Relic Entertainment, Sega, or THQ Nordic. All rights belong to their respective parties.
