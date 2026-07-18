# Task T22: Unify NormalizeDisplayName Across Scanners

**Tier:** 2 — Code Structure
**Phase:** D — Complexity Reduction
**Effort:** ~20 min
**Risk:** Low
**Status:** pending
**Prerequisites:** None

---

## Objective

`FolderScanner.cs` (lines 644-657) and `SteamLibraryScanner.cs` (lines 426-432) both have `NormalizeDisplayName` methods, but they do different things. The FolderScanner version strips suffixes like "Remastered", "Definitive Edition", etc. The Steam version only replaces `_` and `-` with spaces. This inconsistency means Steam games get worse display names than standalone games. Unify to the more complete version.

## What Needs to Change

### 1. New method in `src/GamingCommander.App/Services/FileSystemHelper.cs`

**Current state:** Created in T16 with `GetDirectoriesSafe` and `GetLastWriteTimeSafe`
**Actions:**
- [ ] Add `NormalizeDisplayName(string folderName)` static method:
  ```csharp
  /// <summary>
  /// Normalizes a game folder name into a human-readable display name.
  /// Strips common suffixes (Remastered, Definitive Edition, etc.) and
  /// replaces underscores/hyphens with spaces.
  /// </summary>
  internal static string NormalizeDisplayName(string folderName)
  {
      string name = folderName
          .Replace("_", " ")
          .Replace("-", " ");

      string[] suffixesToRemove =
      [
          " Remastered", " Definitive Edition", " Enhanced Edition",
          " Ultimate Edition", " Special Edition", " GOTY", " Edition"
      ];

      foreach (string suffix in suffixesToRemove)
      {
          if (name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
              name = name[..^suffix.Length];
      }

      return name.Trim();
  }
  ```

### 2. `src/GamingCommander.App/Services/FolderScanner.cs`

**Current state:** Lines 644-657 contain `private static string NormalizeDisplayName(string folderName)`
**Actions:**
- [ ] Delete the `NormalizeDisplayName` method (lines 644-657)
- [ ] Update the call site in `AddGameEntry()` (line ~695) to use `FileSystemHelper.NormalizeDisplayName()`

### 3. `src/GamingCommander.App/Services/SteamLibraryScanner.cs`

**Current state:** Lines 426-432 contain `private static string NormalizeDisplayName(string folderName)`
**Actions:**
- [ ] Delete the `NormalizeDisplayName` method (lines 426-432)
- [ ] Update the call site in `CreateEntry()` (line ~295) to use `FileSystemHelper.NormalizeDisplayName()`

## Context

- The FolderScanner version strips 7 common suffixes — this is the complete version
- The Steam version only does character replacement — this is the incomplete version
- Both are called when building `GameEntry.DisplayName` from the folder name
- Steam games like "The Witcher 3 - Wild Hunt" and "Civilization VI - Rise and Fall" benefit from the suffix stripping
- No test data currently exercises the suffix stripping — but it's a cosmetic improvement

## Requirements

- [ ] `FileSystemHelper.NormalizeDisplayName` exists with suffix-stripping logic
- [ ] `FolderScanner.cs` no longer contains `NormalizeDisplayName`
- [ ] `SteamLibraryScanner.cs` no longer contains `NormalizeDisplayName`
- [ ] Both scanners call `FileSystemHelper.NormalizeDisplayName()`
- [ ] `FileSystemHelper.NormalizeDisplayName` has `/// <summary>` XML doc
- [ ] No behavior change for FolderScanner (same logic, just moved)
- [ ] SteamLibraryScanner now strips suffixes (improved behavior)

## Verification

- [ ] `dotnet build` passes (0 errors)
- [ ] `dotnet test` passes (17 tests)
- [ ] `grep -c "NormalizeDisplayName" src/GamingCommander.App/Services/FolderScanner.cs` returns 0 (removed)
- [ ] `grep -c "NormalizeDisplayName" src/GamingCommander.App/Services/SteamLibraryScanner.cs` returns 0 (removed)
- [ ] `grep -c "NormalizeDisplayName" src/GamingCommander.App/Services/FileSystemHelper.cs` returns 1 (single definition)

## Completion Notes

- **Completed:**
- **What was done:**
- **Verification:**
- **Issues encountered:**
