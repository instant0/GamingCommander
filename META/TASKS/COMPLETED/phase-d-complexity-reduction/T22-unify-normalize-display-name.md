# Task T22: Unify NormalizeDisplayName Across Scanners

**Tier:** 2 — Code Structure
**Phase:** D — Complexity Reduction
**Effort:** ~20 min
**Risk:** Low
**Status:** ✅ completed
**Prerequisites:** T16 (FileSystemHelper created)

---

## Objective

`FolderScanner.cs` (lines 644-657) and `SteamLibraryScanner.cs` (lines 426-432) both have `NormalizeDisplayName` methods, but they do different things. The FolderScanner version strips 7 common suffixes (Remastered, Definitive Edition, etc.) AND replaces `_`/`-` with spaces. The Steam version only replaces `_`/`-` with spaces. Steam games get worse display names than standalone games. Unify to the more complete FolderScanner version.

## What Needs to Change

### 1. `src/GamingCommander.App/Services/FileSystemHelper.cs`

**Current state:** Created in T16 with filesystem utilities.
**Actions:**
- [ ] Add `NormalizeDisplayName(string folderName)` static method
- [ ] Use the **original FolderScanner order** (strip suffixes FIRST, then replace characters) to preserve existing behavior:
  ```csharp
  /// <summary>
  /// Normalizes a game folder name into a human-readable display name.
  /// Strips common suffixes (Remastered, Definitive Edition, etc.) and
  /// replaces underscores/hyphens with spaces.
  /// </summary>
  internal static string NormalizeDisplayName(string folderName)
  {
      string name = folderName
          .Replace("Remastered", "")
          .Replace("Definitive Edition", "")
          .Replace("Enhanced Edition", "")
          .Replace("Ultimate Edition", "")
          .Replace("Special Edition", "")
          .Replace("GOTY", "")
          .Replace("Edition", "")
          .Replace("_", " ")
          .Replace("-", " ")
          .Trim();
      return name;
  }
  ```

### 2. `src/GamingCommander.App/Services/FolderScanner.cs`

**Current state:** Lines 644-657 contain `private static string NormalizeDisplayName(string folderName)`
**Actions:**
- [ ] Delete the `NormalizeDisplayName` method (lines 644-657)
- [ ] Update the call site in `AddGameEntry()` (line 694) to use `FileSystemHelper.NormalizeDisplayName()`

### 3. `src/GamingCommander.App/Services/SteamLibraryScanner.cs`

**Current state:** Lines 426-432 contain `private static string NormalizeDisplayName(string folderName)` — only replaces `_`/`-`
**Actions:**
- [ ] Delete the `NormalizeDisplayName` method (lines 426-432)
- [ ] Update call sites in `CreateEntry()` (line 288), `CreateOrphanedEntry()` (line 330), and `CreateMissingAcfEntry()` (line 357) to use `FileSystemHelper.NormalizeDisplayName()`

## Context

- The FolderScanner version strips 7 common suffixes — this is the complete version
- The Steam version only does character replacement — this is the incomplete version
- Both are called when building `GameEntry.DisplayName` from the folder name
- **Using the original FolderScanner order** (suffix stripping before character replacement) preserves existing behavior for FolderScanner. SteamLibraryScanner will now also get suffix stripping — an improvement, not a regression.
- No test data currently exercises the suffix stripping — but it's a cosmetic improvement for Steam games like "Civilization VI - Definitive Edition"

## Requirements

- [ ] `FileSystemHelper.NormalizeDisplayName` exists with suffix-stripping logic (original FolderScanner order)
- [ ] `FolderScanner.cs` no longer contains `NormalizeDisplayName`
- [ ] `SteamLibraryScanner.cs` no longer contains `NormalizeDisplayName`
- [ ] Both scanners call `FileSystemHelper.NormalizeDisplayName()`
- [ ] `FileSystemHelper.NormalizeDisplayName` has `/// <summary>` XML doc
- [ ] No behavior change for FolderScanner (same logic, same order)
- [ ] SteamLibraryScanner now strips suffixes (improved display names)

## Verification

- [ ] `dotnet build` passes (0 errors)
- [ ] `dotnet test` passes (17 tests)
- [ ] `grep -c "NormalizeDisplayName" src/GamingCommander.App/Services/FolderScanner.cs` returns 0 (removed)
- [ ] `grep -c "NormalizeDisplayName" src/GamingCommander.App/Services/SteamLibraryScanner.cs` returns 0 (removed)
- [ ] `grep -c "NormalizeDisplayName" src/GamingCommander.App/Services/FileSystemHelper.cs` returns 1 (single definition)

## Completion Notes

- **Completed:** 2026-07-19
- **What was done:** Unified `NormalizeDisplayName` in `FileSystemHelper` with full suffix stripping (Remastered, Definitive Edition, etc.). Removed duplicates from FolderScanner and SteamLibraryScanner. Steam games now get improved display names.
- **Verification:** Build clean, all tests passing.
- **Issues encountered:** None.
