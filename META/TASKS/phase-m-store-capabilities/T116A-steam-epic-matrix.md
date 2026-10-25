# Task T116A: Steam + Epic Matrix Rows

**Tier:** 1 — Documentation
**Phase:** M — Store Capabilities
**Risk:** Minimal
**Status:** merged
**Prerequisites:** Task T116A depends on Task T116

> Merged into T116 — the parent task covers this section in one document. Kept as reference.

> Documentation-only. Do not run build/test.

## Objective

Fill the Steam and Epic sections of the capability matrix with verified facts, and correct any
"complete" claim the code does not support.

## What Needs to Change

### 1. Steam
- [ ] Launcher locator: record that only `HKLM\SOFTWARE\Valve\Steam\InstallPath` is read; list the missing HKCU/`WOW6432Node`/default-path fallbacks.
- [ ] Inventory: `libraryfolders.vdf` + `appmanifest_*.acf` + `steamapps/common`; note `apps` blocks are ignored and ACF lookup is first-wins by `installdir`.
- [ ] Metadata: `LastUpdated` parsed but discarded; `StateFlags` stored but not interpreted; only `installdir` validated.
- [ ] Exe/launch: heuristic exe; `steam://rungameid/{appid}` launch.
- [ ] Health: Installed/Moved/Orphaned/Missing shipped; repair is text-only.

### 2. Epic
- [ ] Launcher locator: none; only the fixed ProgramData manifests folder.
- [ ] Inventory: ProgramData `*.item` only; `LauncherInstalled.dat` unread; `EpicOrphanDiscovery` has no callers.
- [ ] Metadata: fields read vs ignored (`InstallSize`, `AppVersionString`, `InstallationGuid`, `CompleteManifestPath`).
- [ ] Exe/launch: `.item` exe or scored heuristic; no `com.epicgames.launcher://`.
- [ ] Health: Installed/Missing/Orphaned; matching has a leaf-name fallback.

## Context

- Matrix home: `docs/detection/10-store-capabilities.md`.
- Evidence files: `SteamLibraryScanner.cs`, `SteamAcfParser.cs`, `SteamInstallPathLocator.cs`, `EpicLibraryScanner.cs`, `EpicManifestParser.cs`, `EpicOrphanDiscovery.cs`.
- Do not repeat the unsupported "orphan discovery shipped" claim.

## Requirements

- [ ] Each shipped claim cites `file:line` or a research doc.
- [ ] Unverified mechanisms are marked, not asserted.
- [ ] No task IDs in the matrix.

## Verification

- [ ] `grep -n "Steam\|Epic" docs/detection/10-store-capabilities.md` shows both sections.
- [ ] Spot-check each citation against the named file.
- [ ] No build/test (docs-only).

## Completion Notes

- **Completed:**
- **What was done:**
- **Verification:**
- **Issues encountered:**
