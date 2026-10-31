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
- [ ] **Primary inventory is ProgramData `*.item`** (already read by `EpicItemCatalog`). Local `.item`/
      `.mancpn` already supply the metadata; this task imports identity, not extra fields.
- [ ] `LauncherInstalled.dat` (`InstallationList`) is a **legacy cross-check only**: verify it exists and its
      shape on a real install before reading it, and never make the import depend on it.
- [ ] When an `.item` is absent, use local `.mancpn` and the binary `.manifest` for identity (catalog ids,
      install path, launch exe).
- [ ] `EpicLibraryScanner` already marks Orphaned for entries it knows. Use `EpicOrphanDiscovery` only to find
      Epic folders that are not in the database at all; do not duplicate the scanner's claim logic.
- [ ] Read `InstallSize`, `AppVersionString`, `InstallationGuid`, `CompleteManifestPath` from the `.item` when
      present. Do not require them.
- [ ] Never write or register `.item`; detection only. `.item` regeneration is a separate, deferred subject.

### 2. EA
- [ ] Parse `__Installer/installerdata.xml` if confirmed on a real install: `contentID`, `gameVersion`,
      `gameTitle`, `runtime/launcher/filePath`, uninstall path.
- [ ] Read legacy Origin `%ProgramData%\Origin\LocalContent\*.mfst` and per-game registry keys.
- [ ] Enumerate registry entries even when the folder is missing.
- [ ] Persist the EA content ID in `StoreInfo`/identity.
- [ ] Modern encrypted EA App `IS` is out of scope; report `Unsupported`/needs verification.

## Context

- Existing: `EpicItemCatalog`, `EpicManifestParser`, `EpicBinaryManifest`, `EpicOrphanDiscovery`, `EaInstallLogParser`, `RegistryFallbackDetector`.
- Research: `docs/research/ea_format.md`, `epic_item_format.md`, `planning/126-facts-eosh.md`.
- Read-only: no `.item` writes, no registry writes.

## Requirements

- [ ] Epic: `.item` catalog is the inventory; local `.mancpn`/`.manifest` fill identity gaps; orphan rows
      surfaced without duplicating the scanner's claims.
- [ ] EA: legacy installs imported; modern encrypted installs marked unsupported, not guessed.
- [ ] No writes.

## Verification

- [ ] Fixtures for Epic catalog, local `.mancpn` fallback, local orphan, and EA legacy/registry. A
      `LauncherInstalled.dat` fixture only if that file is verified and used as a cross-check.
- [ ] `dotnet build` / `dotnet test` green.
- [ ] No regression to existing Epic catalog tests.

## Completion Notes

- **Completed:**
- **What was done:**
- **Verification:**
- **Issues encountered:**
