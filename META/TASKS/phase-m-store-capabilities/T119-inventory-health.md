# Task T119: Launcher-vs-Disk Health and Suggestions

**Tier:** 3 — Logic/Behavior
**Phase:** M — Store Capabilities
**Risk:** Medium
**Status:** pending
**Prerequisites:** Task T119 depends on Task T118

## Objective

Compare each launcher's inventory with the disk, set `InstallState` (`Installed` / `Moved` /
`Missing` / `Orphaned`) for every store, and surface actionable suggestions. Read-only.

## What Needs to Change

### 1. Health model
- [ ] Move `InstallState` population out of `FolderScanner`'s hardcoded `Installed` into the import/scan path.
- [ ] Define mismatch reasons: `StoreFolderMissing`, `StoreExeMissing`, `DiskOnly`, `StoreMoved`, `StoreUpdateAvailable`, `Duplicate`.
- [ ] Store-reported state is shown separately from the disk-derived `InstallState`.
- [ ] `StoreExeMissing` is a suggestion, not `Missing`.
- [ ] A game that disappears from the launcher inventory is marked `Missing`, never deleted.
- [ ] Preserve the existing Steam/Epic health behavior.

### 2. Probes
- [ ] For each store, compare the inventory path to the actual folder and exe.
- [ ] Do not claim healthy when only the launcher says so.
- [ ] Startup sync (T121) consumes the same health result so a launch check and a manual scan agree.
- [ ] No writes.

### 3. UI
- [ ] Map every store's status into the pane item and details.
- [ ] Show a concise suggestion (e.g., "Launcher lists this game but the folder is missing — verify the library").
- [ ] No automatic repair.

## Context

- Current status UI only maps Steam/Epic (`ShellViewModel.Loading`, `ShellViewModel.Details.Status`).
- `InstallInfo`/`InstallState` already support the states; only Steam/Epic populate them.
- Proposal: `planning/129` §3; matrix: `docs/detection/10-store-capabilities.md`.

## Requirements

- [ ] All stores can produce `Installed`/`Moved`/`Missing`/`Orphaned` where evidence exists.
- [ ] Suggestions are informational only.
- [ ] No writes to store files or game data.

## Verification

- [ ] Fixture per state per store group.
- [ ] `dotnet build` / `dotnet test` green.
- [ ] UI test/smoke check for the status mapping.

## Completion Notes

- **Completed:**
- **What was done:**
- **Verification:**
- **Issues encountered:**
