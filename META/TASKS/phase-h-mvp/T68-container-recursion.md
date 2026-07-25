# Task T68: Container Recursion Improvements

**Tier:** 3 — Logic/Behavior
**Phase:** H — MVP
**Effort:** ~40 min
**Risk:** Medium
**Status:** Pending
**Prerequisites:** None
**WP:** WP-3 (3.4)

---

## Objective

`FolderScanner.ScanContainerChildren()` only checks Tier 1 store signals (launcher markers) on immediate children of container folders. Standalone games nested under publisher folders (e.g. `Publisher/GameA/game.exe`) are dropped. The Python `detect.py` recursively scans container children with all detection phases and filters non-game subfolders. Port the key improvements.

## What Needs to Change

### 1. `src/GamingCommander.App/Services/FolderScanner.cs` — `ScanContainerChildren()`

**Current state:** Lines 276-293. Iterates immediate children, checks `StoreSignalDetector.DetectType()`, only promotes children with Tier 1 signals.

**Actions:**
- [ ] When `childType == GameSourceKind.Unknown`, also check for standalone game signals:
  ```csharp
  if (childType == GameSourceKind.Unknown)
  {
      // Check for standalone game signals (exe at root, UE layout)
      string[] childExes = FileSystemHelper.GetFilesSafe(child, "*.exe");
      if (childExes.Length > 0)
      {
          AddGameEntry(entries, child, rootPath, GameSourceKind.Standalone, defaultType);
          continue;
      }
      // UE layout check
      if (HasUnrealLayoutSignal(child))
      {
          AddGameEntry(entries, child, rootPath, GameSourceKind.Standalone, defaultType);
          continue;
      }
  }
  ```
- [ ] Add publisher folder pattern: if a directory contains only subdirectories and no files, recurse into grandchildren:
  ```csharp
  if (childType == GameSourceKind.Unknown && childExes.Length == 0 && !HasUnrealLayoutSignal(child))
  {
      // Publisher folder pattern: only subdirs, no files → recurse
      FileInfo[] files = child.GetFiles("*", SearchOption.TopDirectoryOnly);
      if (files.Length == 0 && child.GetDirectories().Length > 0)
      {
          ScanContainerChildren(entries, child, rootPath, defaultType);
          continue;
      }
  }
  ```
- [ ] Add non-game folder filtering inside containers. Create a `HashSet<string>` of known non-game folder names:
  ```csharp
  private static readonly HashSet<string> _nonGameFolderNames = new(StringComparer.OrdinalIgnoreCase)
  {
      "Soundtrack", "Soundtracks", "Manuals", "Manual", "Item Data",
      "Misc", "Bonus Content", "Artwork", "Wallpapers", "Music",
      "Redist", "Support", "Tools", "_CommonRedist"
  };
  ```
  Check this set before recursing into children.

### 2. No changes to the top-level `Scan()` Pass 3

The container check is already triggered when Pass 1 (root exe) and Pass 2 (subdir scan) both fail. The changes above improve what happens inside the container — no changes needed to the trigger logic.

## Context

- **Reference:** `detect.py` lines 1550-1602 (container scanning logic)
- Publisher folder pattern: `D:\Games\EA\Battlefield 2042\` — EA folder has no exe, but `Battlefield 2042/` subfolder does
- Non-game folders: `Soundtrack/`, `Manuals/`, `_CommonRedist/` — these should be skipped during container recursion
- The existing `_hiddenFolderNames` set already handles `.`, `..`, hidden dirs — extend with non-game patterns
- Container recursion is bounded: only immediate children + one level of grandchildren (not infinite recursion)
- Risk of overcounting: the non-game filter reduces false positives from soundtrack/manual folders

## Requirements

- [ ] Container children with standalone signals (exe, UE layout) are promoted as games
- [ ] Publisher folder pattern (dirs-only root) recurses into grandchildren
- [ ] Non-game folder names are skipped during container recursion
- [ ] Container recursion is bounded (not infinite)
- [ ] Existing scanner tests still pass
- [ ] No double-counting of games already found in Pass 1/2

## Verification

- [ ] `dotnet build` passes (0 errors)
- [ ] `dotnet test` passes (no regressions)
- [ ] Unit test: `Publisher/GameA/game.exe` → GameA detected as standalone
- [ ] Unit test: `Publisher/Soundtrack/` → skipped (no entry created)
- [ ] Unit test: `Publisher/OnlyDirs/SubGame/game.exe` → SubGame detected via recursion

## Completion Notes

- **Completed:**
- **What was done:**
- **Verification:**
- **Issues encountered:**
