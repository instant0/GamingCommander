# Plan 107 — BattleNet Detection Fix + Noise Filter Cleanup

**Status:** DRAFT — awaiting approval  
**Audience:** Builder  
**Priority:** P0 (BattleNet games not detected)  
**Effort:** ~1–2 hours  
**Depends on:** None (can be done in parallel with Plan 105)  

---

## 0. Problem Statement

BattleNet games (Diablo III, World of Warcraft, etc.) are **not detected** when the library root is a parent directory containing a `blizzard\` subfolder.

**This is NOT a missing-feature gap.** The C# implementation has RICHER BattleNet detection than Python:
- `HasBattleNetGameSignal()` checks folder names (`warcraft`, `diablo`, `overwatch`, etc.)
- `HasBattleNetGameSignal()` checks exe names (`DiabloIII.exe`, `Warcraft III.exe`, etc.)
- Parent propagation checks if a child's parent has BattleNet signals

**The issue is a skip-list regression.** `"blizzard"` in two skip lists prevents the existing detection code from ever running. See `docs/GAME-DETECTION-LOGIC.md` "BattleNet Skip-List Regression" section for full analysis.

### Crash Paths (Bonus)

The rescan also crashes via `ContainerScanner` permission errors. These are documented in Plan 105 but addressed here for completeness since they're in the same scan path.

---

## 1. Root Cause Analysis

### The Core Issue: Skip-List Regression vs Python

| Skip List | C# Contains | Python Contains | Effect |
|-----------|-------------|-----------------|--------|
| `NoiseSubDirNames` / `SKIP_NAMES` | `"blizzard"`, `"battle.net"` | `"battle.net"` only | C# skips `blizzard/` entirely; Python processes it as a container |
| `s_nonGameFolderNames` / `_NON_GAME_DIR_NAMES` | `"blizzard"`, `"battle.net"` | Neither | C# skips `blizzard/` in container recursion; Python does not |

**Result:** When scanning `d:\games\` with a `blizzard/` subfolder:
- Python: processes `blizzard/` as container → scans children → finds `Diablo III/` with `.battle.net/` → **detected as BattleNet**
- C#: `blizzard/` matches `NoiseSubDirNames.Contains("blizzard")` → **entire folder skipped, games never discovered**

### Blocking Point 1: FileSystemHelper.NoiseSubDirNames

**File:** `src/GamingCommander.App/Services/FileSystemHelper.cs` — Line 22

```csharp
public static readonly HashSet<string> NoiseSubDirNames = new(StringComparer.OrdinalIgnoreCase)
{
    // ... lots of noise entries ...
    "blizzard",    // ← THIS SKIPS THE ENTIRE BLIZZARD DIRECTORY
    "battle.net",  // ← AND THIS
    // ...
};
```

**Impact:** `FolderScanner.Scan()` iterates subdirectories at line 100-101:
```csharp
if (NoiseSubDirNames.Contains(subDir.Name))
    continue;
```

When scanning `d:\games\`, the `blizzard\` subdirectory is **never visited**. BattleNet signal detection never runs. Diablo III is never discovered.

### Blocking Point 2: ContainerScanner.s_nonGameFolderNames

**File:** `src/GamingCommander.App/Services/ContainerScanner.cs` — Line 26

```csharp
private static readonly HashSet<string> s_nonGameFolderNames = new(StringComparer.OrdinalIgnoreCase)
{
    // ...
    "blizzard",
    "battle.net",
    // ...
};
```

**Impact:** `ContainerScanner.ScanContainerChildren()` at line 98:
```csharp
if (s_nonGameFolderNames.Contains(child.Name))
    continue;
```

Even if the noise filter is removed from `FileSystemHelper`, this secondary filter would still block `blizzard\` from being treated as a container with sub-games.

### Detection Logic (Working But Unreachable)

**File:** `src/GamingCommander.App/Services/StoreSignalDetector.cs`

`HasBattleNetGameSignal()` checks for `.battle.net/` directory + `Agent.exe` + `Launch.exe`. This works correctly but is never reached because the directories are skipped before detection runs.

### Parent Propagation (Incomplete)

`FolderScanner.Scan()` has parent propagation logic that checks if the parent directory contains BattleNet/Steam/Epic signal files. However, when scanning `d:\games\`, the parent is the root itself — there's no "parent" to check. The propagation only works when scanning subdirectories of the root.

---

## 2. Fix Strategy

### Fix 1: Remove "blizzard" and "battle.net" from NoiseSubDirNames

**File:** `src/GamingCommander.App/Services/FileSystemHelper.cs` — Line 22

Remove `"blizzard"` and `"battle.net"` from `NoiseSubDirNames`. These are publisher container directories, not noise. They contain game subdirectories that should be scanned.

**Rationale:** A folder named `blizzard` under a library root is a publisher container (like `steamapps` or `Origin`). It should be scanned for games, not skipped.

### Fix 2: Remove "blizzard" and "battle.net" from ContainerScanner.s_nonGameFolderNames

**File:** `src/GamingCommander.App/Services/ContainerScanner.cs` — Line 26

Remove `"blizzard"` and `"battle.net"` from the non-game folder list. Same reasoning — these are publisher containers.

### Fix 3: Add Name-Based BattleNet Signal Fallback

**File:** `src/GamingCommander.App/Services/StoreSignalDetector.cs`

Current `HasBattleNetGameSignal()` requires `.battle.net/` directory. Add fallback: if the directory name matches known BattleNet publisher patterns (`blizzard`, `battle.net`, `overwatch`), classify as BattleNet.

```csharp
public static bool HasBattleNetGameSignal(string directoryPath)
{
    // Existing checks...
    if (Directory.Exists(Path.Combine(directoryPath, ".battle.net")))
        return true;
    if (File.Exists(Path.Combine(directoryPath, "Agent.exe")))
        return true;
    if (File.Exists(Path.Combine(directoryPath, "Launch.exe")))
        return true;

    // NEW: Name-based fallback
    string dirName = Path.GetFileName(directoryPath);
    if (s_battleNetPublisherNames.Contains(dirName))
        return true;

    return false;
}

private static readonly HashSet<string> s_battleNetPublisherNames = new(StringComparer.OrdinalIgnoreCase)
{
    "blizzard",
    "battle.net",
    "overwatch",
};
```

### Fix 4: Add Parent Propagation for BattleNet Publisher Folders

**File:** `src/GamingCommander.App/Services/FolderScanner.cs`

When scanning `d:\games\blizzard\diablo iii\`, the parent (`blizzard\`) should be checked for BattleNet signals. Currently, parent propagation only works for Steam/Epic/Origin. Add BattleNet to the parent check:

```csharp
// In Scan(), after existing store detection:
if (storeType == GameStoreType.Unknown && parentGame is null)
{
    string? parentDir = Directory.GetParent(dirPath)?.FullName;
    if (parentDir != null)
    {
        if (StoreSignalDetector.HasBattleNetGameSignal(parentDir))
            storeType = GameStoreType.BattleNet;
        else if (StoreSignalDetector.HasSteamGameSignal(parentDir))
            storeType = GameStoreType.Steam;
        // ... existing checks ...
    }
}
```

### Fix 5: ContainerScanner Propagation

**File:** `src/GamingCommander.App/Services/ContainerScanner.cs`

In `ScanContainerChildren()` at line 104, after `GetFiles`/`GetDirectories` (which now has try-catch from Plan 105), propagate parent store type to children:

```csharp
// When scanning children of a BattleNet container:
GameStoreType childStoreType = parentGame?.StoreType ?? GameStoreType.Unknown;
```

This ensures that `d:\games\blizzard\diablo iii\` inherits BattleNet store type from the `blizzard\` parent.

---

## 3. Noise Filter Cleanup (Related)

While fixing BattleNet detection, clean up other noise entries that block legitimate game detection:

### Add to NoiseSubDirNames (new noise entries)

| Entry | Reason |
|-------|--------|
| `"steam controller configs"` | Steam internal folder, not a game (Bug 10) |
| `"steam"` | Steam metadata folder, not a game |
| `"directx"` | Runtime installer, not a game |
| `"vcredist"` | Runtime installer, not a game |
| `"redistributable"` | Runtime installer, not a game |
| `"dotnet"` | Runtime installer, not a game |
| `"jdk"` | Development kit, not a game |

### Remove from NoiseSubDirNames (publisher containers)

| Entry | Reason |
|-------|--------|
| `"blizzard"` | Publisher container with games |
| `"battle.net"` | Publisher container with games |

### Remove from ContainerScanner.s_nonGameFolderNames

| Entry | Reason |
|-------|--------|
| `"blizzard"` | Publisher container with games |
| `"battle.net"` | Publisher container with games |

---

## 4. Files Changed

| File | Change |
|------|--------|
| `Core/Services/FileSystemHelper.cs` | Remove `"blizzard"`, `"battle.net"` from NoiseSubDirNames; add new noise entries |
| `Core/Services/ContainerScanner.cs` | Remove `"blizzard"`, `"battle.net"` from s_nonGameFolderNames; add null-guard for parentGame |
| `Core/Services/StoreSignalDetector.cs` | Add name-based BattleNet signal fallback |
| `Core/Services/FolderScanner.cs` | Add BattleNet to parent propagation check |

---

## 5. Tests

- `StoreSignalDetectorTests.cs`: Test BattleNet signal detection for directories with `.battle.net/`, `Agent.exe`, `Launch.exe`, and name-based fallback
- `FolderScannerTests.cs`: Test BattleNet detection when root contains `blizzard\diablo iii\`
- `FolderScannerTests.cs`: Test that `"steam controller configs"` is skipped as noise
- `ContainerScannerTests.cs`: Test BattleNet container propagation
- `FileSystemHelperTests.cs`: Test updated noise filter list

---

## 6. Success Criteria

- [ ] BattleNet games detected when library root contains `blizzard\` subdirectory
- [ ] BattleNet games classified as `GameStoreType.BattleNet` (not Standalone)
- [ ] Parent propagation works for BattleNet publisher containers
- [ ] Steam Controller Configs, DirectX, VCRedist folders are filtered as noise
- [ ] `"blizzard"` and `"battle.net"` removed from all noise/skip lists
- [ ] No regressions in existing store detection
- [ ] Build clean, all tests pass

---

## 7. Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| Removing "blizzard" from noise causes false positives | Low | Low | ContainerScanner + StoreSignalDetector still filter non-game dirs |
| Name-based fallback causes misclassification | Low | Medium | Only apply to known publisher names; fallback only when other signals fail |
| Parent propagation conflicts with existing detection | Low | Low | Store type priority: explicit > propagation > fallback |
