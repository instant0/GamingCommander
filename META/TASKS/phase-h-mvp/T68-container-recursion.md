# Task T68: Container Recursion & Organization Folder Detection

**Tier:** 3 — Logic/Behavior
**Phase:** H — MVP
**Effort:** ~60 min
**Risk:** Medium
**Status:** Pending
**Prerequisites:** T66 (UE platform loop)
**WP:** WP-3 (3.4)

---

## Objective

`FolderScanner.ScanContainerChildren()` only checks Tier 1 store signals (launcher markers) on immediate children of container folders. This misses:
1. **Standalone games under publishers**: `EA/Battlefield 2042/game.exe`
2. **UE games under publishers**: `EA/SomeGame/Binaries/Win64/SomeGame.exe`
3. **Nested publisher patterns**: `Publisher/SubPublisher/GameA/game.exe`

The Python `detect.py` handles these via container scanning with standalone signal detection and publisher folder recursion. Port the key improvements.

### Key insight: Organization folder detection

When a folder contains **multiple game folders** at one level below, it's an **organization folder** (like "Program Files (x86)"), not a game folder. Examples:
- `D:\Games\EA\` → contains `Battlefield 2042/`, `Need for Speed/`, `FIFA/` → EA is organization
- `D:\Games\Steam\steamapps\common\` → contains many game folders → is organization
- `D:\Games\GOG Games\` → contains game folders → is organization

This pattern should be detected automatically: if a folder has ≥2 children with game signals (exe, UE layout, store signals), the parent is an organization folder.

## Design Principles

### 1. Win32 is a valid UE signal (not just Win64)

UE3 games (e.g., Unreal Tournament 3, Gothic 3) use `Binaries/Win32/`. After T66, `ExecutableDiscovery.FindExecutablesDeep` already scans all platforms. `HasUnrealLayoutSignal` must also check Win32, WinGDK, and Steam — not just Win64.

### 2. Organization folders are detected by content, not name

"EA", "Ubisoft", "Blizzard" are obvious. But `D:\Games\My Collection\` could also be an organization. Detection rule:
- ≥2 children with game signals → parent is organization → recurse into all children
- 1 child with game signals → could be a nested game (e.g., `GameName/DLC/`) → treat as game
- 0 children with game signals → not a container → skip

### 3. Non-game folder filtering

Some folders inside containers are never games: `Soundtrack/`, `Manuals/`, `_CommonRedist/`. These should be skipped during recursion. Extend `FileSystemHelper.NoiseSubDirNames` or create a separate set.

---

## What Needs to Change

### 1. `FolderScanner.cs` — Update `HasUnrealLayoutSignal()` to check all UE platforms

**Current state:** Lines 211-239. Only checks `Binaries/Win64/`.

**Change:** Use the same platform loop as T66's `ExecutableDiscovery`:
```csharp
private static readonly string[] s_uePlatformNames = ["Win64", "Win32", "WinGDK", "Steam"];

private bool HasUnrealLayoutSignal(DirectoryInfo dir)
{
    string enginePath = Path.Combine(dir.FullName, "Engine");
    if (!Directory.Exists(enginePath))
        return false;

    try
    {
        foreach (DirectoryInfo child in FileSystemHelper.GetDirectoriesSafe(dir.FullName))
        {
            if (child.Name == "Engine") continue;
            foreach (string platform in s_uePlatformNames)
            {
                string platPath = Path.Combine(child.FullName, "Binaries", platform);
                if (!Directory.Exists(platPath)) continue;
                foreach (string exe in Directory.EnumerateFiles(platPath, "*.exe", SearchOption.TopDirectoryOnly))
                {
                    string name = Path.GetFileNameWithoutExtension(exe).ToLowerInvariant();
                    if (!IsNoiseExeName(name))
                        return true;
                }
            }
        }
    }
    catch { }
    return false;
}
```

### 2. `FolderScanner.cs` — Rewrite `ScanContainerChildren()`

**Current state:** Lines 276-293. Only checks Tier 1 store signals.

**New logic:**
```csharp
private void ScanContainerChildren(
    List<GameEntry> entries, DirectoryInfo containerDir,
    string rootPath, GameSourceKind defaultType, int depth = 0)
{
    if (depth > 1) return; // Bounded: max 2 levels (container → child → grandchild)

    var children = FileSystemHelper.GetDirectoriesSafe(containerDir.FullName);
    int gameSignalCount = 0;

    // First pass: count children with game signals (for organization detection)
    foreach (DirectoryInfo child in children)
    {
        if (IsNonGameFolder(child)) continue;
        if (StoreSignalDetector.DetectType(child) != GameSourceKind.Unknown
            || HasRootExecutableSignal(child)
            || HasUnrealLayoutSignal(child))
        {
            gameSignalCount++;
        }
    }

    // Second pass: process children
    foreach (DirectoryInfo child in children)
    {
        if (_hiddenFolderNames.Contains(child.Name))
            continue;
        if (IsNonGameFolder(child))
            continue;

        GameSourceKind childType = StoreSignalDetector.DetectType(child);

        // Tier 1 — Store signals (GOG, EA, Ubisoft, etc.)
        if (childType != GameSourceKind.Unknown)
        {
            AddGameEntry(entries, child, rootPath, childType, defaultType);
            continue;
        }

        // Standalone signals (exe at root, UE layout)
        if (HasRootExecutableSignal(child) || HasUnrealLayoutSignal(child))
        {
            AddGameEntry(entries, child, rootPath, GameSourceKind.Standalone, defaultType);
            continue;
        }

        // Organization folder: multiple game children → recurse
        if (gameSignalCount >= 2)
        {
            ScanContainerChildren(entries, child, rootPath, defaultType, depth + 1);
            continue;
        }

        // Publisher folder pattern: only subdirs, no files → recurse
        if (gameSignalCount == 0)
        {
            FileInfo[] files = child.GetFiles("*", SearchOption.TopDirectoryOnly);
            if (files.Length == 0 && child.GetDirectories().Length > 0)
            {
                ScanContainerChildren(entries, child, rootPath, defaultType, depth + 1);
                continue;
            }
        }
    }
}
```

### 3. `FolderScanner.cs` — Add `IsNonGameFolder()` helper

```csharp
private static readonly HashSet<string> s_nonGameFolderNames = new(StringComparer.OrdinalIgnoreCase)
{
    "Soundtrack", "Soundtracks", "Manuals", "Manual", "Item Data",
    "Misc", "Bonus Content", "Artwork", "Wallpapers", "Music",
    "Redist", "Support", "Tools", "_CommonRedist", "CommonRedist",
    "vcredist", "dotnet", "directx", "physx", "installer",
    "_installer", "install", "easyanticheat", "devtools", "docs",
    "licenses", "steam controller configs", "steamworks shared",
};

private static bool IsNonGameFolder(DirectoryInfo dir)
{
    return s_nonGameFolderNames.Contains(dir.Name)
        || FileSystemHelper.NoiseSubDirNames.Contains(dir.Name);
}
```

### 4. `FolderScanner.cs` — Update `Scan()` Pass 3 call

Add depth parameter to the initial call:
```csharp
// Line 122:
ScanContainerChildren(entries, subDir, rootPath, defaultType, depth: 0);
```

---

## Detection Algorithm Summary

```
Scan(rootPath):
  for each child in rootPath:
    Pass 1: StoreSignalDetector → Tier 1 (GOG, EA, Ubisoft, etc.) → create entry
    Pass 2: FallbackType → exe at root, UE layout, .lnk → create entry
    Pass 3: Container detection:
      1. Count children with game signals (store, exe, UE layout)
      2. If ≥2 signals → organization folder → recurse into all children
      3. If 0 signals → publisher folder pattern (dirs-only) → recurse
      4. For each child: apply same Pass 1/2/3 logic (bounded depth)
```

### Example scenarios

| Path | Detection |
|------|-----------|
| `EA/Battlefield 2042/game.exe` | Pass 3: EA has game signals → recurse → BF2042 has exe → Standalone |
| `EA/SomeGame/Binaries/Win64/SomeGame.exe` | Pass 3: EA has game signals → recurse → SomeGame has UE layout → Standalone |
| `EA/Battlefield 2042/Binaries/Win32/game.exe` + `EA/Need for Speed/game.exe` | Pass 3: EA has ≥2 signals → organization → both detected |
| `Publisher/Soundtrack/` | Pass 3: IsNonGameFolder → skip |
| `Publisher/OnlyDirs/SubGame/game.exe` | Pass 3: dirs-only → recurse → SubGame detected |

---

## Context

- **Reference:** `detect.py` lines 1550-1602 (container scanning logic)
- **T66 already:** Platform loop in `ExecutableDiscovery.FindExecutablesDeep` (Win64, Win32, WinGDK, Steam)
- **T66 gap:** `HasUnrealLayoutSignal` in FolderScanner still only checks Win64 — fix in this task
- Organization detection: "if multiple game folders exist at one level, parent is organization"
- Bounded recursion: max depth 2 (container → child → grandchild)
- Non-game filtering: reuse `FileSystemHelper.NoiseSubDirNames` + extend with publisher-specific names

---

## Requirements

- [ ] `HasUnrealLayoutSignal` checks all UE platforms (Win64, Win32, WinGDK, Steam)
- [ ] `ScanContainerChildren` promotes standalone children (exe, UE layout)
- [ ] Organization folder detection: ≥2 game children → recurse into all
- [ ] Publisher folder pattern: dirs-only root → recurse into grandchildren
- [ ] Non-game folder filtering (Soundtrack, Manuals, _CommonRedist, etc.)
- [ ] Recursion bounded (max depth 2)
- [ ] No double-counting of games already found in Pass 1/2
- [ ] Existing scanner tests still pass
- [ ] Unit tests for all scenarios

---

## Verification

- [ ] `dotnet build` passes (0 errors)
- [ ] `dotnet test` passes (no regressions)
- [ ] Unit test: `Publisher/GameA/game.exe` → GameA detected as standalone
- [ ] Unit test: `Publisher/Soundtrack/` → skipped
- [ ] Unit test: `EA/Game1/game.exe` + `EA/Game2/game.exe` → both detected (organization)
- [ ] Unit test: `Publisher/OnlyDirs/SubGame/game.exe` → SubGame via recursion
- [ ] Unit test: `Game/Binaries/Win32/Game.exe` → UE3 game detected (Win32 signal)
- [ ] Unit test: `Game/Engine/` + `Game/GameName/Binaries/Win32/Game.exe` → UE layout detected
- [ ] Unit test: depth > 2 → not recursed (bounded)

---

## Completion Notes

- **Completed:**
- **What was done:**
- **Verification:**
- **Issues encountered:**
