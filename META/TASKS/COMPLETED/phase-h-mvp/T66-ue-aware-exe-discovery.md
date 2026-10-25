# Task T66: UE-Aware Executable Discovery

**Tier:** 3 — Logic/Behavior
**Phase:** H — MVP
**Effort:** ~40 min
**Risk:** Medium
**Status:** Complete
**Prerequisites:** None
**WP:** WP-3 (3.2)

---

## Objective

Unreal Engine games have a distinctive directory layout (`GameName/Binaries/Win64/Game.exe`) but the C# executable discovery doesn't check several UE paths that `detect.py` covers: `child/bin/` (older games), `Binaries/Win32/` (UE3), `Binaries/Steam/` (Steam-specific builds), and a 2-level recursive fallback for the BioShock pattern (`root/Binaries/Win64/` with no game-name directory). Port these patterns from `detect.py`.

## Design Principles

### Python is the authoritative reference

The Python `detect.py` is the tested and developed reference. Our C# implementation should match its scanning behavior faithfully. Two Python functions are relevant:

| Function | Lines | Purpose | Scanning behavior |
|----------|-------|---------|-------------------|
| `_find_game_executables` | 866-932 | **Primary exe discovery** — used during main scan | Scans **all** platforms and paths. No early break. Collects everything. |
| `_find_exe_in_subdirs` | 1280-1364 | Secondary targeted scan — UE-specific fast path | Breaks on first platform with results. More optimized, narrower scope. |

**T66 follows `_find_game_executables`** as the primary reference because:
1. It is the function called by `_pick_primary_executable` (line 938) which is the main exe selection entry point
2. It scans **all** platforms and paths — this is the thorough behavior we want
3. It has been tested and developed against real game libraries

The platform loop in `FindExecutablesDeep` scans **all** of `Win64`, `Win32`, `WinGDK`, `Steam` — matching `_find_game_executables` lines 917-921 which iterate both `Win64` and `WinGDK` without breaking. This ensures maximum coverage. The downstream `ScoreExecutable()` method handles disambiguation when multiple candidates are found.

### Thorough background scanning

The exe discovery runs during folder scanning, which is a one-time background operation per library root. The scan must be:

1. **Thorough** — find all possible executables across all known UE layouts. Missing an exe means a game has no launch target. False positives (extra candidates) are acceptable — the scoring system filters them.
2. **Fast** — use efficient filesystem APIs (`Directory.EnumerateFiles` not `GetFiles`), skip noise directories early, and avoid redundant work. The recursive fallback is bounded to `maxDepth=2`.
3. **Robust** — handle permission errors, broken symlinks, and missing directories gracefully. Never crash during scanning.
4. **Noise-filtered at every level** — apply exe noise patterns, directory noise patterns, and `NoiseSubDirNames` at every stage of the scan.

## What Needs to Change

### 1. `src/GamingCommander.App/Services/ExecutableDiscovery.cs` — `FindExecutablesDeep()`

**Current state:** Lines 21-82. Checks root-level exes, immediate child dirs, `child/Binaries/Win64/`, and `child/Binaries/WinGDK/`. Missing: `child/bin/`, `Win32/`, `Steam/`, recursive fallback.

**Actions:**

#### a. Replace hardcoded Win64/WinGDK probes with a platform loop (lines 49-68)

Replace the two separate `win64`/`winGdk` blocks with a single loop:

```csharp
// 3. UE Binaries paths — Win64, Win32, WinGDK, Steam
foreach (string platform in new[] { "Win64", "Win32", "WinGDK", "Steam" })
{
    string platPath = Path.Combine(child.FullName, "Binaries", platform);
    if (Directory.Exists(platPath))
    {
        foreach (string exe in Directory.EnumerateFiles(platPath, "*.exe", SearchOption.TopDirectoryOnly))
        {
            if (!IsNoiseExeByPath(exe, noiseExePatterns))
                candidates.Add(exe);
        }
    }
}
```

**Reference:** `detect.py` `_find_game_executables` lines 917-921 iterate `("Binaries/Win64", "Binaries/WinGDK")` collecting from **all** platforms without breaking. T66 extends this to `("Win64", "Win32", "WinGDK", "Steam")`. C# drops `Linux` (Windows-only app) and adds `Win32` + `Steam` from `_find_exe_in_subdirs` lines 1293, 1312.

#### b. Add `child/bin/` probe after the platform loop

```csharp
// 4. Older UE games — child/bin/ (Gothic, Jagged Alliance)
string binPath = Path.Combine(child.FullName, "bin");
if (Directory.Exists(binPath))
{
    foreach (string exe in Directory.EnumerateFiles(binPath, "*.exe", SearchOption.TopDirectoryOnly))
    {
        if (!IsNoiseExeByPath(exe, noiseExePatterns))
            candidates.Add(exe);
    }
}
```

**Reference:** `detect.py` lines 922-925 check `child / "bin"`.

#### c. Add 2-level recursive fallback (after the child-dir loop, before dedup)

```csharp
// 5. BioShock pattern — root has no exes, scan 2 levels deep
if (candidates.Count == 0)
{
    candidates.AddRange(FindExesRecursive(dir, noiseExePatterns, noiseDirectoryPatterns, maxDepth: 2));
}
```

**Reference:** `detect.py` lines 927-930 — `if not root_has_exes and not candidates: _add_exes_recursive(d, max_depth=2)`.

#### d. Implement `FindExesRecursive` private helper

```csharp
/// <summary>
/// Walks subdirectories up to maxDepth, collecting non-noise executables.
/// Used as a fallback when explicit path probes find nothing (BioShock pattern).
/// </summary>
private static List<string> FindExesRecursive(
    DirectoryInfo dir,
    IReadOnlyList<string> noiseExePatterns,
    IReadOnlySet<string> noiseDirectoryPatterns,
    int maxDepth,
    int depth = 0)
{
    var results = new List<string>();
    if (depth > maxDepth) return results;

    try
    {
        foreach (DirectoryInfo child in FileSystemHelper.GetDirectoriesSafe(dir.FullName))
        {
            if (IsNoiseDirectory(child.Name, noiseDirectoryPatterns)
                || FileSystemHelper.NoiseSubDirNames.Contains(child.Name))
                continue;

            // Collect exes from this directory
            foreach (string exe in Directory.EnumerateFiles(child.FullName, "*.exe", SearchOption.TopDirectoryOnly))
            {
                if (!IsNoiseExeByPath(exe, noiseExePatterns))
                    results.Add(exe);
            }

            // Recurse if within depth limit
            if (depth < maxDepth)
            {
                results.AddRange(FindExesRecursive(child, noiseExePatterns, noiseDirectoryPatterns, maxDepth, depth + 1));
            }
        }
    }
    catch (IOException) { }
    catch (UnauthorizedAccessException) { }
    catch (DirectoryNotFoundException) { }

    return results;
}
```

**Key design decisions:**
- Noise dir filtering at every level (matching Python `_is_noise_dir` check)
- `NoiseSubDirNames` check included (matches `FindExecutablesDeep` child loop)
- Exception handling per-directory: catches `IOException`, `UnauthorizedAccessException`, `DirectoryNotFoundException` (matching Python's `PermissionError` catching, but broader for .NET)
- Returns results from all matched directories (not just the first) — matches Python behavior
- Bounded: `maxDepth=2` prevents runaway traversal

### 2. No changes to `FolderScanner.HasUnrealLayoutSignal()`

This method (lines 212-239) is used for source type **detection** (is this a UE game?), not for exe discovery. It only checks `Binaries/Win64/`.

**Known gap:** UE3-only games (no `Win64/` directory) won't trigger this signal. They will still be discovered via:
- `HasRootExecutableSignal()` if they have root-level exes
- The new recursive fallback in `FindExecutablesDeep` if they don't
- The GOG `.info` parser (T65) for GOG games

This gap is acceptable for MVP. A future task can add `Win32/` and `Steam/` checks to `HasUnrealLayoutSignal()`.

### 3. Update XML doc for `FindExecutablesDeep()`

Update the method's XML summary (line 12-17) to reflect the new search paths:

```xml
/// Finds all non-noise executables within a game folder, searching:
/// 1. Root directory
/// 2. Immediate child directories (skipping noise dirs)
/// 3. Binaries/{Win64,Win32,WinGDK,Steam}/ paths in children
/// 4. child/bin/ for older UE games
/// 5. 2-level recursive fallback when no candidates found
```

## Context

- **Primary reference:** `detect.py` lines 866-932 (`_find_game_executables`) — the authoritative exe discovery function used in the main scan pipeline. Scans all platforms, collects all candidates, no early breaks.
- **Secondary reference:** `detect.py` lines 1280-1364 (`_find_exe_in_subdirs`) — UE-specific targeted scan. Used for the platform list values (`Win64`, `Win32`, `Steam`). Has break-on-first behavior but is a different context (targeted UE scan, not broad discovery).
- UE3 games: `GameName/Binaries/Win32/Game.exe`
- UE4-5 games: `GameName/Binaries/Win64/Game.exe`
- Steam-specific: `GameName/Binaries/Steam/Game.exe`
- Older games: `GameName/bin/Game.exe`
- BioShock pattern: `Root/Binaries/Win64/Game.exe` (no GameName directory)
- The existing `Win64`/`WinGDK` check already works for most UE4-5 games — this fills the gaps
- The recursive fallback is bounded (`maxDepth=2`) to prevent runaway traversal
- Existing `ExecutableScoringTests.cs` tests only `ScoreExecutable()` — zero tests for `FindExecutablesDeep()` or `FindPrimaryExecutable()` today

## Requirements

- [ ] `child/bin/` checked for older UE games
- [ ] `Binaries/Win32/`, `Binaries/Steam/` checked alongside `Win64`/`WinGDK`
- [ ] All platforms scanned (no early break) — matches `_find_game_executables` behavior
- [ ] 2-level recursive fallback when root has no candidates
- [ ] `FindExesRecursive` bounded to `maxDepth` parameter
- [ ] `FindExesRecursive` catches `IOException`, `UnauthorizedAccessException`, `DirectoryNotFoundException` per-directory
- [ ] Noise filtering applied in all new probe paths (exe patterns + directory patterns + `NoiseSubDirNames`)
- [ ] XML doc updated for `FindExecutablesDeep()`
- [ ] Existing executable discovery tests still pass

## Verification

- [ ] `dotnet build` passes (0 errors)
- [ ] `dotnet test` passes (no regressions)
- [ ] Unit test: synthetic `Game/bin/Game.exe` → found via `child/bin/` probe
- [ ] Unit test: synthetic `Game/Binaries/Win32/Game.exe` → found via platform loop
- [ ] Unit test: synthetic `Game/Binaries/Steam/Game.exe` → found via platform loop
- [ ] Unit test: synthetic `Root/Binaries/Win64/Game.exe` (no GameName dir) → found via recursive fallback
- [ ] Unit test: noise exe in `child/bin/` → filtered out

## Completion Notes

- **Completed:**
- **What was done:**
- **Verification:**
- **Issues encountered:**
