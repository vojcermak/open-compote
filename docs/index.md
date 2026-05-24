# open-compote

A modern C# library for working with Relic Entertainment's `.sga` archive format. This project provides support for reading and writing SGA archives used in games built using the Essence engine.

## About

`open-compote` is an open-source project aimed at providing mod tools developers and experienced modders with robust and easy to use tools for working with `.sga` archives. Whether you're developing mods, analyzing game data, or building mod tools, this library offers a simple API for archive manipulation.

> [!NOTE]
> **Current Status:**
> **Very early-stage development.** Only SGA V2 is implemented today. If you want support for additional versions early, please contribute issues, feature requests, or pull requests. Community contributions are welcome.

## Supported SGA Versions

| Version | Games | Status | Documentation |
|---------|-------|--------|---|
| **V2**  | Impossible Creatures,<br> Warhammer 40,000: Dawn of War | ![](./images/check-circle.svg) Supported | [SGA-V2.md](./schema/SGA-V2.md) |
| **V3**  | The Outfit                                       | ![](./images/close-circle.svg) Planned | |
| **V4**  | Company of Heroes 1                              | ![](./images/close-circle.svg) Planned | [SGA-V4.md](./schema/SGA-V4.md) |
| **V5**  | Warhammer 40,000: Dawn of War 2                  | ![](./images/close-circle.svg) Planned | [SGA-V5.md](./schema/SGA-V5.md) |
| **V6**  | Can be created using Company of Heroes 2 archive.exe. | ![](./images/close-circle.svg) Planned | |
| **V7**  | Company of Heroes 2                              | ![](./images/close-circle.svg) Planned | [SGA-V7.md](./schema/SGA-V7.md) |
| **V9**  | Warhammer 40,000: Dawn of War 3                  | ![](./images/close-circle.svg) Planned | |
| **V10** | Age of Empires 4,<br> Company of Heroes 3        | ![](./images/close-circle.svg) Planned | |


## Key Features

- **Type-safe API** — Full C# integration with comprehensive type definitions
- **Forward-looking** — Comprehensive schema documentation for planned versions (V4, V5, V7)
- **Well-documented** — Detailed format specifications and examples
- **Modular architecture** — Clean separation between parsers and core functionality

## Getting Started

- 📚 [Quick start guide]() - Not implemented yet.
- 📋 [SGA Format Schemas](./schema/index.md)
- 🏗️ [API Documentation](./api/OpenCompote.SGA.yml)

## Contributing

If you want support for additional SGA versions or want to improve parser coverage, please open an issue or submit a pull request on GitHub: [GitHub Issues](https://github.com/vojcermak/open-compote/issues) or [GitHub Pull Requests](https://github.com/vojcermak/open-compote/pulls).

Contributions are the fastest way to expand version support.

## Disclaimer

Not affiliated with Relic Entertainment, Sega, or THQ Nordic. All rights belong to their respective parties.