# Plan 116: BattleNet Detection — Signal Files Only

**Created:** 2026-07-27
**Priority:** P2
**Status:** ✅ COMPLETE
**Source:** Real game directory listings + real `.build.info`/`.product.db` samples

---

## 1. Problem Statement

BattleNet detection has two bugs:

1. **ContainerScanner uses path-based logic** — checks if parent is named "blizzard" instead of checking signal files inside the game folder
2. **`HasBattleNetGameSignal()` checks folder names** — should check signal files instead

**Core principle**: A game can be installed ANYWHERE. Only SteamLibrary has a fixed path. Detection must be based on **signal files inside the folder**, not on path names.

### Real Examples

| Location | Has `.build.info`? | Has `.product.db`? | Should Detect? |
|----------|-------------------|-------------------|---------------|
| `D:\Games\Blizzard\Diablo III\` | ✅ (hidden) | ✅ (hidden) | ✅ BattleNet |
| `D:\Games\Diablo III\` | ✅ (hidden) | ✅ (hidden) | ✅ BattleNet |
| `Q:\random\Diablo III\` | ✅ (if installed) | ✅ (if installed) | ✅ BattleNet |
| `C:\My Games\Diablo IV\` | ✅ (if installed) | ✅ (if installed) | ✅ BattleNet |

**Path is irrelevant. Signal files are ground truth.**

---

## 2. Current Bugs

### Bug 1: ContainerScanner path-based check (line 96-97)

```csharp
// WRONG — checks parent directory name
if (containerDir.Name.Equals("blizzard", StringComparison.OrdinalIgnoreCase)
    || Directory.Exists(Path.Combine(containerDir.FullName, "battle.net")))
```

**Fix**: Check `HasBlizzardSignal(child)` instead — this checks for `.build.info`, `.product.db`, `.battle.net/` inside the child directory.

### Bug 2: `HasBattleNetGameSignal()` checks folder names (line 238-247)

```csharp
// WRONG — checks folder name
string[] battleNetGameNames = ["warcraft", "diablo", ...];
string dirName = dir.Name.ToLowerInvariant();
if (battleNetGameNames.Any(name => dirName.Contains(name)))
```

**Fix**: Check signal files instead. If a folder has `.build.info` or `.product.db`, it's a BattleNet game — regardless of what the folder is named.

---

## 3. Architecture Changes

### 3A. Fix ContainerScanner — Signal files, not path names

```csharp
// BEFORE (line 94-104):
// BattleNet container detection: check if a sibling "battle.net" directory exists
if (containerDir.Name.Equals("blizzard", StringComparison.OrdinalIgnoreCase)
    || Directory.Exists(Path.Combine(containerDir.FullName, "battle.net")))
{
    if (StoreSignalDetector.HasBattleNetGameSignal(child))
    {
        addGameEntry(entries, child, rootPath, GameSourceKind.BattleNet);
        continue;
    }
}

// AFTER:
// BattleNet detection: check signal files inside the child directory
if (StoreSignalDetector.HasBlizzardSignal(child))
{
    addGameEntry(entries, child, rootPath, GameSourceKind.BattleNet);
    continue;
}
```

### 3B. Fix `HasBattleNetGameSignal()` — Signal files, not folder names

```csharp
// BEFORE (line 235-264):
// Checks folder names and specific executables — WRONG

// AFTER:
internal static bool HasBattleNetGameSignal(DirectoryInfo dir)
{
    // Check signal files — these are the ground truth
    return HasBlizzardSignal(dir);
}
```

**Why this is correct**: `HasBlizzardSignal()` already checks for `.build.info`, `.product.db`, `.battle.net/`. If a folder has any of these, it's a BattleNet game. Period.

### 3C. Add `BlizzardBrowser.exe`/`BlizzardError.exe` as secondary signals

Add to `HasBlizzardSignal()` after the primary checks:

```csharp
// Secondary: Blizzard-specific executables at game root
// These appear in game folders (not just the launcher)
if (File.Exists(Path.Combine(dir.FullName, "BlizzardBrowser.exe"))
    || File.Exists(Path.Combine(dir.FullName, "BlizzardError.exe")))
    return true;
```

**Why this is safe**: `BlizzardBrowser.exe` and `BlizzardError.exe` are Blizzard-proprietary executables. No other software creates them. False positive risk is minimal.

---

## 4. Implementation Steps

### Step 1: Fix ContainerScanner — Remove path-based check

**File:** `src/GamingCommander.App/Services/ContainerScanner.cs`

Replace lines 94-104 with signal-file-based detection.

### Step 2: Fix `HasBattleNetGameSignal()` — Use signal files

**File:** `src/GamingCommander.App/Services/StoreSignalDetector.cs`

Replace folder-name checking with `HasBlizzardSignal()` call.

### Step 3: Add BlizzardBrowser/BlizzardError signals

**File:** `src/GamingCommander.App/Services/StoreSignalDetector.cs`

Add two `File.Exists` checks to `HasBlizzardSignal()`.

### Step 4: Update tests

**Files:** `StoreSignalDetectorTests.cs`, `ContainerScannerTests.cs`

| Test | Expected |
|------|----------|
| Game folder with `.build.info` at any path | BattleNet detected |
| Game folder with `BlizzardBrowser.exe` at root | BattleNet detected |
| Game folder with `BlizzardError.exe` at root | BattleNet detected |
| Game folder named "random" with `.build.info` | BattleNet detected (path irrelevant) |
| Container with `battle.net` sibling | Child detected by signal files, not path |

---

## 5. Files Affected

| File | Change |
|------|--------|
| `src/GamingCommander.App/Services/ContainerScanner.cs` | Remove path-based "blizzard" check, use `HasBlizzardSignal(child)` |
| `src/GamingCommander.App/Services/StoreSignalDetector.cs` | Fix `HasBattleNetGameSignal()` to use signal files, add BlizzardBrowser/BlizzardError |
| `tests/GamingCommander.App.Tests/StoreSignalDetectorTests.cs` | New tests for signal-file-based detection |
| `tests/GamingCommander.App.Tests/ContainerScannerTests.cs` | Update BattleNet container tests |

---

## 6. Success Criteria

- [x] ContainerScanner detects BattleNet games by signal files, not path names
- [x] `HasBattleNetGameSignal()` removed (dead code)
- [x] FolderScanner Pass 1b parent propagation removed
- [x] Game at `Q:\random\Diablo III\` with `.build.info` is detected as BattleNet
- [x] All existing tests pass, new tests added
- [x] Build clean
