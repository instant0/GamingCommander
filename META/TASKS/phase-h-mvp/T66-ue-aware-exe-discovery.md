# Task T66: UE-Aware Executable Discovery

**Tier:** 3 — Logic/Behavior
**Phase:** H — MVP
**Effort:** ~40 min
**Risk:** Medium
**Status:** Pending
**Prerequisites:** None
**WP:** WP-3 (3.2)

---

## Objective

Unreal Engine games have a distinctive directory layout (`GameName/Binaries/Win64/Game.exe`) but the C# executable discovery doesn't check several UE paths that `detect.py` covers: `child/bin/` (older games), `Binaries/Win32/` (UE3), `Binaries/Steam/` (Steam-specific builds), and a 2-level recursive fallback for the BioShock pattern (`root/Binaries/Win64/` with no game-name directory). Port these patterns from `detect.py`.

## What Needs to Change

### 1. `src/GamingCommander.App/Services/ExecutableDiscovery.cs` — `FindExecutablesDeep()`

**Current state:** Lines 21-82. Checks root-level exes, immediate child dirs, `child/Binaries/Win64/`, and `child/Binaries/WinGDK/`. Missing: `child/bin/`, `Win32/`, `Steam/`, recursive fallback.

**Actions:**
- [ ] Add `child/bin/` probe after `child/Binaries/WinGDK/` check (line ~65):
  ```csharp
  // Older UE games (Gothic, Jagged Alliance)
  string binPath = Path.Combine(child.FullName, "bin");
  if (Directory.Exists(binPath))
  {
      string[] binExes = FileSystemHelper.GetFilesSafe(new DirectoryInfo(binPath), "*.exe");
      candidates.AddRange(binExes.Where(e => !IsNoiseExe(e)));
  }
  ```
- [ ] Add `Binaries/Win32/` and `Binaries/Steam/` alongside existing `Win64`/`WinGDK` checks:
  ```csharp
  foreach (string platform in new[] { "Win64", "Win32", "WinGDK", "Steam" })
  {
      string platPath = Path.Combine(child.FullName, "Binaries", platform);
      if (Directory.Exists(platPath))
      {
          string[] platExes = FileSystemHelper.GetFilesSafe(new DirectoryInfo(platPath), "*.exe");
          candidates.AddRange(platExes.Where(e => !IsNoiseExe(e)));
      }
  }
  ```
- [ ] Add 2-level recursive fallback when root has no exes and no child dirs with exes:
  ```csharp
  // BioShock pattern: root has no exes, check Binaries/Win64 directly
  if (candidates.Count == 0)
  {
      candidates.AddRange(FindExesRecursive(subDir, maxDepth: 2));
  }
  ```
- [ ] Implement private `FindExesRecursive(DirectoryInfo dir, int maxDepth)` helper:
  - Iterate immediate subdirectories
  - For each, collect `*.exe` files (excluding noise)
  - If no exes found and `maxDepth > 0`, recurse with `maxDepth - 1`
  - Return all found exes

### 2. No changes to `FolderScanner.HasUnrealLayoutSignal()`

This method is used for source type detection (is this a UE game?), not for exe discovery. It already checks `Engine/` + child dirs + `Binaries/Win64/`. Leave it as-is for MVP.

## Context

- **Reference:** `detect.py` lines 866-932 (`_find_game_executables`), 1280-1364 (`_find_exe_in_subdirs`)
- UE3 games: `GameName/Binaries/Win32/Game.exe`
- UE4-5 games: `GameName/Binaries/Win64/Game.exe`
- Steam-specific: `GameName/Binaries/Steam/Game.exe`
- Older games: `GameName/bin/Game.exe`
- BioShock pattern: `Root/Binaries/Win64/Game.exe` (no GameName directory)
- The existing `Win64`/`WinGDK` check already works for most UE4-5 games — this fills the gaps
- The recursive fallback is bounded (max 2 levels) to prevent runaway traversal

## Requirements

- [ ] `child/bin/` checked for older UE games
- [ ] `Binaries/Win32/`, `Binaries/Steam/` checked alongside `Win64`/`WinGDK`
- [ ] 2-level recursive fallback when root has no candidates
- [ ] `FindExesRecursive` bounded to `maxDepth` parameter
- [ ] Noise filtering applied in all new probe paths
- [ ] Existing executable discovery tests still pass

## Verification

- [ ] `dotnet build` passes (0 errors)
- [ ] `dotnet test` passes (no regressions)
- [ ] Unit test: synthetic `Game/bin/Game.exe` → found
- [ ] Unit test: synthetic `Game/Binaries/Win32/Game.exe` → found
- [ ] Unit test: synthetic `Root/Binaries/Win64/Game.exe` (no GameName dir) → found via fallback

## Completion Notes

- **Completed:**
- **What was done:**
- **Verification:**
- **Issues encountered:**
