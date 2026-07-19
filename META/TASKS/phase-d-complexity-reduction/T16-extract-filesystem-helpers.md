# Task T16: Extract FileSystemHelper Utility

**Tier:** 2 — Code Structure
**Phase:** D — Complexity Reduction
**Effort:** ~25 min
**Risk:** Minimal
**Status:** ✅ completed
**Prerequisites:** None

---

## Objective

`FolderScanner.cs` and `SteamLibraryScanner.cs` contain identical private static utility methods: `GetDirectoriesSafe()`, `GetLastWriteTimeSafe()`, and `GetFilesSafe()`. This is copy-paste duplication that violates the DRY principle. Extract these to a shared utility class so both scanners use the same implementation.

## What Needs to Change

### 1. New file: `src/GamingCommander.App/Services/FileSystemHelper.cs`

**Current state:** Does not exist.
**Actions:**
- [ ] Create `FileSystemHelper.cs` with namespace `GamingCommander.App.Services`
- [ ] Add `/// <summary>` to class: "Safe filesystem operations that return defaults on failure instead of throwing exceptions."
- [ ] Move `GetDirectoriesSafe(string path)` from FolderScanner.cs (line 712)
  - Method signature: `internal static DirectoryInfo[] GetDirectoriesSafe(string path)`
  - Returns empty array on any exception
- [ ] Move `GetFilesSafe(DirectoryInfo dir, string pattern)` from FolderScanner.cs (line 724)
  - Method signature: `internal static string[] GetFilesSafe(DirectoryInfo dir, string pattern)`
  - Returns empty array on any exception
- [ ] Move `GetLastWriteTimeSafe(DirectoryInfo dir)` from FolderScanner.cs (line 738)
  - Method signature: `internal static DateTimeOffset GetLastWriteTimeSafe(DirectoryInfo dir)`
  - Returns `DateTimeOffset.MinValue` on any exception
- [ ] Add `/// <summary>` XML docs to all three methods explaining WHY they catch exceptions (filesystem access may fail on Windows due to permissions, locked files, or removed directories)

### 2. `src/GamingCommander.App/Services/FolderScanner.cs`

**Current state:**
- `GetDirectoriesSafe` at line 712 — called at lines 93, 320, 351, 371, 426, 465 (6 call sites)
- `GetFilesSafe` at line 724 — called at lines 194, 574, 690 (3 call sites)
- `GetLastWriteTimeSafe` at line 738 — called at line 708 (1 call site)

**Actions:**
- [ ] Delete all three methods (lines 712-748)
- [ ] Update all call sites to use `FileSystemHelper.GetDirectoriesSafe()`, `FileSystemHelper.GetFilesSafe()`, and `FileSystemHelper.GetLastWriteTimeSafe()`
- [ ] No new `using` needed (same namespace)

### 3. `src/GamingCommander.App/Services/SteamLibraryScanner.cs`

**Current state:**
- `GetDirectoriesSafe` at line 434 — called at lines 49, 116, 389 (3 call sites)
- `GetLastWriteTimeSafe` at line 446 — called at lines 318, 338 (2 call sites)
- No `GetFilesSafe` (SteamScanner doesn't use it)

**Actions:**
- [ ] Delete both methods (lines 434-456)
- [ ] Update all call sites to use `FileSystemHelper.GetDirectoriesSafe()` and `FileSystemHelper.GetLastWriteTimeSafe()`
- [ ] No new `using` needed (same namespace)

## Context

- All three methods are identical between the two files — exact copy-paste
- `GetFilesSafe` is only in FolderScanner but is also a general utility — extract it to avoid T24 (ExecutableDiscovery) needing a back-reference to FolderScanner
- No external consumers — both scanners are in the same assembly (`GamingCommander.App`)

## Requirements

- [ ] `FileSystemHelper.cs` created with all three methods
- [ ] All methods have `/// <summary>` XML docs explaining WHY they catch exceptions
- [ ] FolderScanner.cs no longer contains `GetDirectoriesSafe`, `GetFilesSafe`, or `GetLastWriteTimeSafe`
- [ ] SteamLibraryScanner.cs no longer contains `GetDirectoriesSafe` or `GetLastWriteTimeSafe`
- [ ] All call sites updated to use `FileSystemHelper.*` prefix
- [ ] No behavior change — same exception handling, same return types
- [ ] `FileSystemHelper` class is `internal static` (not public, shared within assembly only)

## Verification

- [ ] `dotnet build` passes (0 errors)
- [ ] `dotnet test` passes (17 tests)
- [ ] `grep -r "GetDirectoriesSafe" src/` shows FileSystemHelper.cs definition + call sites (no private definitions in scanners)
- [ ] `grep -r "GetLastWriteTimeSafe" src/` shows FileSystemHelper.cs definition + call sites
- [ ] `grep -r "GetFilesSafe" src/` shows FileSystemHelper.cs definition + call sites
- [ ] `grep -c "private static.*GetDirectoriesSafe" src/` returns 0 (no private copies remain)
- [ ] `grep -c "private static.*GetLastWriteTimeSafe" src/` returns 0 (no private copies remain)
- [ ] `grep -c "private static.*GetFilesSafe" src/` returns 0 (no private copies remain)

## Completion Notes

- **Completed:** 2026-07-19
- **What was done:** Created `FileSystemHelper.cs` with `GetDirectoriesSafe`, `GetFilesSafe`, `GetLastWriteTimeSafe`. Removed duplicates from FolderScanner and SteamLibraryScanner. Updated all call sites.
- **Verification:** Build clean, all tests passing.
- **Issues encountered:** None.
