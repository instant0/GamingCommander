# Task T83: Refresh META/CODE_MAP.md

**Tier:** 1 — Documentation
**Phase:** K — Audit Follow-ups
**Risk:** Minimal
**Status:** pending
**Prerequisites:** None

> Documentation-only. Do not run build/test.

## Objective

`META/CODE_MAP.md` predates Plan 127 and the detector review: it omits new services/models and
still describes removed fields. Refresh it so it matches the code.

## What Needs to Change

### 1. Services / interfaces
- [ ] Add `DetectionNoise` (`src/GamingCommander.App/Services/DetectionNoise.cs`) with its three sets (`NonGameExe`, `LnkSkip`, `SteamCommonNoise`) and the rule that tiered `blacklist.json` drives scoring.
- [ ] Add `ICollectionsService` (`src/GamingCommander.Core/`) and `CollectionsDatabaseService` (`App/Services/`) — owns `data/collections.json`.

### 2. `GameEntry`
- [ ] Extend the model row with `Install`, `Tracking`, `UserState`, and `Relations` (Plan 127 B7–B11).
- [ ] Replace the removed `GameEntry.Extra` documentation with: "status/ACF facts live in `Acf` (`SteamAcfInfo`) and `Install` (`InstallInfo`); no `Extra` bag exists after Plan 127 A2."

### 3. Test baseline
- [ ] Replace the stale `~217` snapshot with the verified 607 total (Core 173, Migration 1, App 433), or delete the snapshot and point to `META/SESSION/CURRENT.md`.

## Context

- Findings: `planning/128-doc-and-task-audit.md` §1.4.
- Verify every added item exists in `src/` before writing it.

## Requirements

- [ ] `DetectionNoise`, `ICollectionsService`, and the four `GameEntry` body records are documented.
- [ ] No `Extra`/`PlatformMetadata`/`GameRoot` live entries remain.
- [ ] Test counts match `META/SESSION/CURRENT.md`.

## Verification

- [ ] `grep -n "DetectionNoise\|ICollectionsService\|TrackingInfo\|UserStateInfo" META/CODE_MAP.md` returns entries.
- [ ] `grep -n "GameEntry.Extra\|PlatformMetadata\|217" META/CODE_MAP.md` returns nothing.
- [ ] No build/test (docs-only).

## Completion Notes

- **Completed:**
- **What was done:**
- **Verification:**
- **Issues encountered:**
