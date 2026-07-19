# Task T60: Consolidate Duplicated Noise-Check Methods

**Tier:** 3 — Code Quality
**Phase:** G — Code Quality & Tests
**Effort:** ~25 min
**Risk:** Low
**Status:** completed

---

## Objective

Three files implement the same noise-check logic independently, and two files implement the same noise-directory check. This violates DRY and makes maintenance harder — a pattern change requires updating 3+ locations.

## What Needs to Change

### Cross-file duplication found:

| Method | File 1 | File 2 | File 3 |
|--------|--------|--------|--------|
| Check if exe name matches noise patterns | `FolderScanner.IsNoiseExeName` (line 307) | `ExecutableDiscovery.IsNoiseExeByPath` (line 273) | `SteamLibraryScanner.IsNoiseExe` (line 331) |
| Check if directory name is noise | `FolderScanner.IsNoiseDirectory` (line 293) | `ExecutableDiscovery.IsNoiseDirectory` (line 279) | — |

### Plan:

**Option A — Consolidate into FileSystemHelper:**
- [ ] Move `IsNoiseExeName(string name, IReadOnlyList<string> patterns)` to `FileSystemHelper`
- [ ] Move `IsNoiseDirectory(string dirName, IReadOnlySet<string> patterns)` to `FileSystemHelper`
- [ ] Update all 3 callers for exe check (FolderScanner, ExecutableDiscovery, SteamLibraryScanner)
- [ ] Update both callers for directory check (FolderScanner, ExecutableDiscovery)

**Option B — Leave as-is (acceptable):**
- SteamLibraryScanner uses a smaller, Steam-specific subset (7 items vs full list)
- Each file has slightly different pattern lists
- Consolidation would require passing pattern lists as parameters

### Recommendation:
- **Option A** if the method signatures can be unified (accept patterns as parameter)
- **Option B** if SteamLibraryScanner's subset logic is intentionally different

## Context

- `FolderScanner.IsNoiseExeName` takes `string name` and checks against `_noiseExePatterns`
- `ExecutableDiscovery.IsNoiseExeByPath` takes `string exePath`, extracts filename, checks against `noiseExePatterns`
- `SteamLibraryScanner.IsNoiseExe` takes `string exeName` and checks against a hardcoded 7-item subset
- The core logic is identical: `patterns.Any(p => name.Contains(p, OrdinalIgnoreCase))`
- `IsNoiseDirectory` implementations are identical in both files

## Requirements

- [ ] Choose Option A or Option B (document choice in completion notes)
- [ ] If Option A: single implementation in FileSystemHelper
- [ ] If Option B: document why duplication is acceptable
- [ ] No behavior change either way

## Verification

- [ ] `dotnet build` passes (0 errors)
- [ ] `dotnet test` passes

## Completion Notes

- **Completed:**
- **What was done:**
- **Verification:**
- **Issues encountered:**
