# GamingCommander

GamingCommander is a planned C# Windows-native game management and launcher application with a retro Norton Commander-inspired interface.

The visual goal is inspired by classic commander-style tools, but the UI should scale with modern window resizing instead of being limited to a fixed legacy text resolution.

## Current State

The repository is in Phase 2: Steam & Standalone Games. **MVP is declared complete.**

Current status:

- ✅ Steam library scanning, ACF cross-referencing, Installed/Moved/Orphaned/Missing detection
- ✅ Standalone game detection for 10 platforms (GOG, EA, Ubisoft, Epic, Blizzard, Xbox, Rockstar, Steam Emu)
- ✅ Executable scoring and noise filtering (21-tier blacklist, 320+ patterns)
- ✅ Game launching: `steam://` URI for Steam, direct `.exe` with args for standalone
- ✅ GOG metadata extraction, UE-aware exe discovery, `.lnk` shortcut resolution, container detection
- ✅ Dual-pane NC UI, first-run wizard, F2/F4/F6/F9 workflows, theme centralization
- 217 tests passing across Core, Detection, Migration, and App test projects

## Development Environment

This repository can be scaffolded and partially validated from Linux using the .NET SDK.

However, Windows-specific tasks will still require a Windows machine, especially for:

- native UI verification,
- Windows registry integration,
- launcher detection against real client installs,
- symlink/junction and manifest migration validation,
- packaging and installer testing.

See [`docs/development-environment.md`](./docs/development-environment.md) for the working model.

## Solution Layout

```text
src/
  GamingCommander.Core/        Interface definitions + domain models + shared helpers
  GamingCommander.Detection/   Game discovery abstractions + design-time stub
  GamingCommander.Migration/   Migration planning abstractions + design-time stub
  GamingCommander.UI/          ViewModels (Norton Commander shell)
  GamingCommander.App/         Avalonia app entry, windows, services, DI wiring
tests/
tools/
docs/
data/
```

## Key Documents

- [`AGENTS.md`](./AGENTS.md) — Agent guidance and reading order
- [`CONTRIBUTING.md`](./CONTRIBUTING.md) — Contribution guidelines
- [`META/SESSION/CURRENT.md`](./META/SESSION/CURRENT.md) — Current project state
- [`META/ARCHITECTURE.md`](./META/ARCHITECTURE.md) — Architecture decisions
- [`META/ROADMAP.md`](./META/ROADMAP.md) — Phase milestones and progress
- [`planning/`](./planning/) — Detailed implementation plans

## Build & Test

```bash
dotnet build
dotnet test
```

Requirements: .NET 8 SDK. See [`docs/development-environment.md`](./docs/development-environment.md) for details.
