# Task T16: Extract FileSystemHelper Utility

**Tier:** 2 — Code Structure
**Phase:** D — Complexity Reduction
**Effort:** ~25 min
**Risk:** Minimal
**Status:** pending

---

## Objective

`FolderScanner.cs` (lines 712-748) and `SteamLibraryScanner.cs` (lines 434-456) contain identical private static methods: `GetDirectoriesSafe()` and `GetLastWriteTimeSafe()`. This is exact copy-paste duplication that violates the DRY principle. Extract these to a shared utility class so both scanners use the same implementation.

## What Needs to Change

### 1. New file: `src/GamingCommander.App/Services/FileSystemHelper.cs`

**Current state:** Does not exist.
**Actions:**
- [ ] Create `FileSystemHelper.cs` with namespace `GamingCommander.App.Services`
- [ ] Add `/// <summary>` to class: "Safe filesystem operations that return defaults on failure instead of throwing exceptions."
- [ ] Move `GetDirectoriesSafe(string path)` from FolderScanner.cs (lines 712-722)
  - Method signature: `internal static DirectoryInfo[] GetDirectoriesSafe(string path)`
  - Returns empty array on any exception
- [ ] Move `GetLastWriteTimeSafe(DirectoryInfo dir)` from FolderScanner.cs (lines 738-748)
  - Method signature: `internal static DateTimeOffset GetLastWriteTimeSafe(DirectoryInfo dir)`
  - Returns `DateTimeOffset.MinValue` on any exception
- [ ] Add `/// <summary>` XML docs to both methods explaining WHY they catch exceptions (filesystem access may fail on Windows due to permissions, locked files, or removed directories)

### 2. `src/GamingCommander.App/Services/FolderScanner.cs`

**Current state:** Lines 712-722 contain `private static DirectoryInfo[] GetDirectoriesSafe(string path)`, lines 738-748 contain `private static DateTimeOffset GetLastWriteTimeSafe(DirectoryInfo dir)`
**Actions:**
- [ ] Delete `GetDirectoriesSafe()` method (lines 712-722)
- [ ] Delete `GetLastWriteTimeSafe()` method (lines 738-748)
- [ ] Update all call sites to use `FileSystemHelper.GetDirectoriesSafe()` and `FileSystemHelper.GetLastWriteTimeSafe()`
  - `GetDirectoriesSafe` is called at lines ~93, ~305, ~365, ~458, ~473
  - `GetLastWriteTimeSafe` is called at lines ~112, ~300, ~454, ~493, ~705
- [ ] No new `using` needed (same namespace)

### 3. `src/GamingCommander.App/Services/SteamLibraryScanner.cs`

**Current state:** Lines 434-444 contain `private static DirectoryInfo[] GetDirectoriesSafe(string path)`, lines 446-456 contain `private static DateTimeOffset GetLastWriteTimeSafe(DirectoryInfo dir)`
**Actions:**
- [ ] Delete `GetDirectoriesSafe()` method (lines 434-444)
- [ ] Delete `GetLastWriteTimeSafe()` method (lines 446-456)
- [ ] Update all call sites to use `FileSystemHelper.GetDirectoriesSafe()` and `FileSystemHelper.GetLastWriteTimeSafe()`
  - `GetDirectoriesSafe` is called at lines ~49, ~112, ~135
  - `GetLastWriteTimeSafe` is called at lines ~63, ~118
- [ ] No new `using` needed (same namespace)

## Context

- Both methods are identical — exact copy-paste between the two files
- `GetDirectoriesSafe` is called ~8 times in FolderScanner, ~5 times in SteamLibraryScanner
- `GetLastWriteTimeSafe` is called ~5 times in FolderScanner, ~3 times in SteamLibraryScanner
- No external consumers — both scanners are in the same assembly (`GamingCommander.App`)
- These are the most frequently duplicated utilities in the codebase

## Requirements

- [ ] `FileSystemHelper.cs` created with both methods
- [ ] Both methods have `/// <summary>` XML docs explaining WHY they catch exceptions
- [ ] FolderScanner.cs no longer contains `GetDirectoriesSafe` or `GetLastWriteTimeSafe` private methods
- [ ] SteamLibraryScanner.cs no longer contains `GetDirectoriesSafe` or `GetLastWriteTimeSafe` private methods
- [ ] All call sites updated to use `FileSystemHelper.*` prefix
- [ ] No behavior change — same exception handling, same return types
- [ ] `FileSystemHelper` class is `internal static` (not public, shared within assembly only)

## Verification

- [ ] `dotnet build` passes (0 errors)
- [ ] `dotnet test` passes (17 tests)
- [ ] `grep -r "GetDirectoriesSafe" src/` shows FileSystemHelper.cs definition + call sites (no private definitions in scanners)
- [ ] `grep -r "GetLastWriteTimeSafe" src/` shows FileSystemHelper.cs definition + call sites
- [ ] `grep -c "private static.*GetDirectoriesSafe" src/` returns 0 (no private copies remain)
- [ ] `grep -c "private static.*GetLastWriteTimeSafe" src/` returns 0 (no private copies remain)

## Completion Notes

- **Completed:**
- **What was done:**
- **Verification:**
- **Issues encountered:**
