# GamingCommander

GamingCommander is a C# Windows-native game management and launcher with a Norton Commander-inspired dual-pane interface.

The visual goal is inspired by classic commander-style tools, but the UI should scale with modern window resizing instead of being limited to a fixed legacy text resolution.

## Current State

**MVP is complete.** Phase 2 baseline (scan + launch) is complete. Remaining Phase 2 work is SyncMove (manifest repair). Live status: [`META/SESSION/CURRENT.md`](./META/SESSION/CURRENT.md).

Current status:

- ✅ Steam library scanning, ACF cross-referencing, Installed/Moved/Orphaned/Missing detection
- ✅ Standalone game detection for 10 platforms (GOG, EA, Ubisoft, Epic, Blizzard, Xbox, Rockstar, Steam Emu)
- ✅ Executable scoring and noise filtering (21-tier blacklist; embedded `blacklist.json` restore)
- ✅ Game launching: `steam://` URI for Steam, direct `.exe` with args for standalone
- ✅ GOG metadata extraction, UE-aware exe discovery, `.lnk` shortcut resolution, container detection
- ✅ Dual-pane NC UI, first-run / F2 setup, F4 configure, **F5** async rescan, F9 library roots
- ✅ Epic local `.item` / `.mancpn` enrichment; right-pane colored tags
- 365 tests passing (73 Core + 1 Migration + 291 App)

## Development Environment

This repository can be scaffolded and partially validated from Linux using the .NET SDK.

However, Windows-specific tasks will still require a Windows machine, especially for:

- native UI verification,
- Windows registry integration,
- launcher detection against real client installs,
- symlink/junction and manifest migration validation,
- packaging and installer testing.

Dev notes (archived Phase 0): [`META/COMPLETED/archived-docs/development-environment.md`](./META/COMPLETED/archived-docs/development-environment.md).

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

Requirements: .NET 8 SDK. Live session state: [`META/SESSION/CURRENT.md`](./META/SESSION/CURRENT.md).
