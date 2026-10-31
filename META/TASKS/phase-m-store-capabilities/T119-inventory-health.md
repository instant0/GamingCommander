# Task T119: Launcher-vs-Disk Health and Suggestions

**Tier:** 3 — Logic/Behavior
**Phase:** M — Store Capabilities
**Risk:** Medium
**Status:** pending
**Prerequisites:** Task T119 depends on Task T118

## Objective

Compare each launcher's available inventory with the disk, set `InstallState` (`Installed` / `Moved` /
`Missing` / `Orphaned`) for every store where that evidence exists, and surface actionable suggestions.
Read-only.

## What Needs to Change

### 1. Health model
- [ ] Move `InstallState` population out of `FolderScanner`'s hardcoded `Installed` into the import/scan path.
- [ ] Define mismatch reasons: `StoreFolderMissing`, `StoreExeMissing`, `DiskOnly`, `StoreMoved`, `Duplicate`.
      Do **not** add `StoreUpdateAvailable`: only Steam exposes a build id, and version comparison is not
      health.
- [ ] Store-reported state is shown separately from the disk-derived `InstallState`.
- [ ] `StoreExeMissing` is a suggestion, not `Missing`.
- [ ] A game that disappears from a **trustworthy inventory** (Steam ACF set, Epic `.item` catalog) is
      marked `Missing`, never deleted. For registry/marker-only stores, absence of a record is not evidence
      of removal and must not change health.
- [ ] Preserve the existing Steam/Epic health behavior.

### 2. Probes
- [ ] For each store with evidence, compare the recorded path to the actual folder and exe.
- [ ] Stores with no inventory (Xbox; GOG/EA/Ubisoft/Battle.net/Rockstar beyond best-effort registry) get
      only the states their evidence supports — typically `Installed` or `DiskOnly`. They do not get a
      fabricated `Missing`/`Moved`.
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

- [ ] All stores produce the states their evidence supports; no state is fabricated for a store with no
      inventory.
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
