# GamingCommander

GamingCommander is a planned C# Windows-native game management and launcher application with a retro Norton Commander-inspired interface.

The visual goal is inspired by classic commander-style tools, but the UI should scale with modern window resizing instead of being limited to a fixed legacy text resolution.

## Current State

The repository is in Phase 2: Steam & Standalone Games.

Current work focuses on:

- standalone game detection and metadata collection,
- Steam library scanning and ACF cross-referencing,
- executable scoring and noise filtering,
- UI polish and theme system.

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
