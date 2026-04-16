# GamingCommander

GamingCommander is a planned C# Windows-native game management and launcher application with a retro Norton Commander-inspired interface.

The visual goal is inspired by classic commander-style tools, but the UI should scale with modern window resizing instead of being limited to a fixed legacy text resolution.

## Current State

The repository is in Phase 0 bootstrap.

Current work focuses on:

- establishing the solution layout,
- documenting architecture and safety constraints,
- preparing the first implementation slice for stand-alone games and Steam,
- keeping Linux-based repository work separate from Windows-only validation.

## Development Environment

This repository can be scaffolded and partially validated from Linux using the .NET SDK.

However, Windows-specific tasks will still require a Windows machine, especially for:

- native UI verification,
- Windows registry integration,
- launcher detection against real client installs,
- symlink/junction and manifest migration validation,
- packaging and installer testing.

See [`docs/development-environment.md`](./docs/development-environment.md) for the working model.

## Planned Solution Layout

```text
src/
tests/
tools/
docs/
data/
```

## Key Documents

- [`AGENTS.md`](./AGENTS.md)
- [`CONTRIBUTING.md`](./CONTRIBUTING.md)
- [`.sisyphus/plans/`](./.sisyphus/plans/)
