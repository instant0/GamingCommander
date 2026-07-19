# Task T42: Update Stale Phase D Task Files

**Tier:** 1 — Documentation
**Phase:** F — Docs & Bug Fixes
**Effort:** ~15 min
**Risk:** Minimal
**Status:** pending

---

## Objective

Six Phase D task files (T16, T18, T19, T20, T21, T22) still say "Status: pending" with empty completion notes, despite the work being fully complete and verified. Update them to match reality.

## What Needs to Change

### `META/TASKS/phase-d-complexity-reduction/T16-extract-filesystem-helpers.md`
- [ ] Set status to "✅ completed"
- [ ] Add completion notes: extracted `FileSystemHelper.cs` with `GetDirectoriesSafe`, `GetFilesSafe`, `GetLastWriteTimeSafe`, `NormalizeDisplayName`

### `META/TASKS/phase-d-complexity-reduction/T18-extract-available-types-constant.md`
- [ ] Set status to "✅ completed"
- [ ] Add completion notes: extracted `GameSourceParser.AvailableTypes` to Core

### `META/TASKS/phase-d-complexity-reduction/T19-rename-ambiguous-variables.md`
- [ ] Set status to "✅ completed"
- [ ] Add completion notes: renamed `p`→`pattern`, `a/b`→`versions`, `sid/eid`→`steamAppId/epicCatalogItemId`, etc.

### `META/TASKS/phase-d-complexity-reduction/T20-add-xml-docs-public-members.md`
- [ ] Set status to "✅ completed"
- [ ] Add completion notes: added XML docs to ~20 public members across 8 files

### `META/TASKS/phase-d-complexity-reduction/T21-consolidate-noise-check-methods.md`
- [ ] Set status to "✅ completed"
- [ ] Add completion notes: deleted dead `IsNoiseExePattern`, renamed `IsNonGameExe`→`IsNoiseExeByPath`

### `META/TASKS/phase-d-complexity-reduction/T22-unify-normalize-display-name.md`
- [ ] Set status to "✅ completed"
- [ ] Add completion notes: unified in FileSystemHelper, removed from FolderScanner and SteamLibraryScanner

## Context

These tasks were completed during a previous session but the task files were not updated at the time. CURRENT.md and NEXT.md both confirm these as complete.

## Requirements

- [ ] All 6 task files have status "✅ completed"
- [ ] All 6 task files have non-empty completion notes
- [ ] No other fields accidentally modified

## Verification

- [ ] `grep -c "pending" META/TASKS/phase-d-complexity-reduction/*.md` — returns 0 (or only T26/T28/T29 which are "skipped")

## Completion Notes

- **Completed:**
- **What was done:**
- **Verification:**
- **Issues encountered:**
