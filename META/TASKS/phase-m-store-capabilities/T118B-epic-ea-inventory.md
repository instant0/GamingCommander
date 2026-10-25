# Task T118B: Epic + EA Inventory Readers

**Tier:** 3 — Logic/Behavior
**Phase:** M — Store Capabilities
**Risk:** High
**Status:** pending
**Prerequisites:** Task T118B depends on Task T118

## Objective

Read the Epic and EA launcher inventories read-only. Epic is file-based; EA is legacy-file/registry
only until the modern encrypted inventory is verified.

## What Needs to Change

### 1. Epic
- [ ] Read `C:\ProgramData\Epic\UnrealEngineLauncher\LauncherInstalled.dat` `InstallationList` as the primary inventory.
- [ ] Merge ProgramData `*.item`, local `.mancpn`, and binary `.manifest` for identity when a `.item` is absent.
- [ ] Wire the existing orphan discovery (currently no callers) into the Epic catalog scan.
- [ ] Read `InstallSize`, `AppVersionString`, `InstallationGuid`, `CompleteManifestPath`.
- [ ] Never write or register `.item`; detection only.

### 2. EA
- [ ] Parse `__Installer/installerdata.xml`: `contentID`, `gameVersion`, `gameTitle`, `runtime/launcher/filePath`, uninstall path.
- [ ] Read legacy Origin `%ProgramData%\Origin\LocalContent\*.mfst` and per-game registry keys.
- [ ] Enumerate registry entries even when the folder is missing.
- [ ] Persist the EA content ID in `StoreInfo`/identity.
- [ ] Modern encrypted EA App `IS` is out of scope; report `Unsupported`/needs verification.

## Context

- Existing: `EpicItemCatalog`, `EpicManifestParser`, `EpicBinaryManifest`, `EpicOrphanDiscovery`, `EaInstallLogParser`, `RegistryFallbackDetector`.
- Research: `docs/research/ea_format.md`, `epic_item_format.md`, `planning/126-facts-eosh.md`.
- Read-only: no `.item` writes, no registry writes.

## Requirements

- [ ] Epic: catalog + `LauncherInstalled.dat` merged and deduped; orphan rows surfaced.
- [ ] EA: legacy installs imported; modern encrypted installs marked unsupported, not guessed.
- [ ] No writes.

## Verification

- [ ] Fixtures for Epic catalog/local-orphan/LauncherInstalled and EA legacy/registry.
- [ ] `dotnet build` / `dotnet test` green.
- [ ] No regression to existing Epic catalog tests.

## Completion Notes

- **Completed:**
- **What was done:**
- **Verification:**
- **Issues encountered:**
