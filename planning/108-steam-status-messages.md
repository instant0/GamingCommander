# Plan 108 — Steam Status Message Improvements

**Status:** DRAFT — awaiting approval  
**Audience:** Builder  
**Priority:** P2 (UX improvement)  
**Effort:** ~2–3 hours  
**Depends on:** None  

---

## 0. Problem Statement

The current Steam status messages are technically accurate but not actionable:

| Status | Current Display | Current Detail |
|--------|----------------|----------------|
| **Orphaned** | "Orphaned" (red) | "Orphaned — game folder has no ACF registration" |
| **Missing** | "Missing" (red) | "Missing — ACF exists but game files not found" |
| **Moved** | "Moved" (yellow) | "Moved — ACF expects: D:\..." |

**User feedback:** "Orphaned" is confusing — users don't know what it means or what to do. The distinction between Orphaned and Missing is unclear. No actionable guidance is provided.

**Key insight:** Most users have multiple Steam libraries (e.g., `D:\SteamLibrary`, `E:\SteamLibrary`). When a game is "Moved", the fix is trivial: **move the ACF file** to the correct library's `steamapps` folder. Steam then finds the files automatically. Our VFS database already knows where the game files actually are.

---

## 1. Steam Multi-Library Context

### How Steam Libraries Work

Steam supports multiple library folders:
```
D:\SteamLibrary\steamapps\
  ├─ appmanifest_292030.acf    ← The Witcher 3 ACF (expects installdir here)
  └─ common\
      └─ The Witcher 3\        ← Game files

E:\SteamLibrary\steamapps\
  └─ common\
      └─ The Witcher 3\        ← Game was moved here (ACF still on D:)
```

### How Moved Detection Works

In `SteamLibraryScanner.Scan()`:
1. ACFs are collected from ALL configured libraries
2. For each game folder in `libraryRoot/steamapps/common/`:
   - If ACF found and `acf.LibraryPath == libraryRoot` → **Installed**
   - If ACF found but `acf.LibraryPath != libraryRoot` → **Moved**
   - If no ACF found → **Orphaned**

### The Simple Fix for Moved

For Moved games, the fix is:
```
Move: D:\SteamLibrary\steamapps\appmanifest_292030.acf
  To: E:\SteamLibrary\steamapps\appmanifest_292030.acf
```

After the move, Steam reads the ACF from E:, finds the game folder in E:\steamapps\common\, and the game registers correctly.

### What Data We Have

For a Moved game, `PlatformMetadata` contains:
| Key | Value | Source |
|-----|-------|--------|
| `AcfLibraryPath` | `D:\SteamLibrary` | Where ACF currently lives |
| `AcfExpectedPath` | `D:\SteamLibrary\steamapps\common\The Witcher 3` | Where ACF expects files |
| `AcfFilePath` | `D:\SteamLibrary\steamapps\appmanifest_292030.acf` | Full ACF path |
| `FolderName` | `The Witcher 3` | Game folder name |
| `SteamAppId` | `292030` | Steam App ID |

The game's actual location is the `libraryRoot` parameter (where we found the game folder during scan). We can compute:
- **Source ACF path:** `acfFilePath` (already stored)
- **Target ACF path:** `{actualLibraryRoot}\steamapps\{acfFileName}`

---

## 2. Proposed Status Messages

Concise, actionable messages (2-3 lines) for the details panel.

### Installed (ACF and game in same library)

**No message needed** — status is "Installed", detail is empty.

### Orphaned (game folder exists, no ACF anywhere)

**Current:** `"Orphaned — game folder has no ACF registration"`

**Proposed:**
```
Orphaned — no Steam manifest for '{folderName}'.
This folder exists in {libraryRoot} but is not registered with Steam.
To fix: Use ACF Generate to create a manifest, or re-install via Steam.
```

### Missing (ACF exists, no game folder in any library)

**Current:** `"Missing — ACF exists but game files not found"`

**Proposed:**
```
Missing — Steam manifest at {acfPath} expects files at {expectedPath}.
Game folder not found in any configured library.
Possible: game is in an unconfigured library, or was uninstalled.
To fix: Add the correct Steam library, or delete the orphaned ACF.
```

**Note for Missing:** The game files might exist in a library that isn't configured in GamingCommander. The user should check their Steam library folders or add the missing library via F2.

### Moved (ACF in library A, game folder in library B)

**Current:** `"Moved — ACF expects: D:\..."`

**Proposed:**
```
Moved — game files found in {actualLibrary} instead of {acfLibrary}.
ACF at: {acfFilePath}
To fix: Move the ACF file to {actualLibrary}\steamapps\ and restart Steam.
```

**The fix is a simple file move:**
```
From: {acfFilePath}
  To: {actualLibrary}\steamapps\{acfFileName}
```

After moving, Steam reads the ACF from the correct library and the game registers automatically.

---

## 3. Data Availability

All needed data is already collected during scan but not all is stored in `PlatformMetadata`:

| Data Point | Currently Stored | Needs Adding |
|------------|-----------------|--------------|
| `SteamStatus` | ✅ All statuses | — |
| `SteamAppId` | ✅ All statuses | — |
| `AcfLibraryPath` | ✅ Installed/Moved/Missing | — |
| `AcfExpectedPath` | ✅ Moved only | Add to Missing |
| `AcfFilePath` | ✅ Missing only | Add to Installed/Moved |
| `FolderName` | ❌ Not in PlatformMetadata | Add (available at scan time) |
| `LibraryRoot` | ❌ Not stored for Orphaned | Add (the `rootPath` parameter) |
| `ActualLibraryRoot` | ❌ Not stored | Add to Moved (the `libraryRoot` where game was found) |
| `DisplayName` | ✅ Already on GameEntry | Use directly |

### Key Addition for Moved: `ActualLibraryRoot`

For Moved games, we need to store the actual library where the game folder was found. This is the `libraryRoot` parameter passed to `CreateEntry()`. Combined with `AcfLibraryPath` (where the ACF currently is), we can compute both the source and target ACF paths.

```csharp
// In CreateEntry(), for Moved games:
if (status == "Moved")
{
    extra["AcfExpectedPath"] = Path.Combine(acf.LibraryPath, "steamapps", "common", folderName);
    extra["ActualLibraryRoot"] = libraryRoot;  // Where game folder actually is
    extra["AcfFilePath"] = acf.AcfFilePath;    // Full path to ACF file
}
```

---

## 4. Code Changes

### SteamLibraryScanner.cs

**Add to `CreateEntry()` (all statuses):**
```csharp
extra["FolderName"] = folderName;
```

**Add to `CreateEntry()` (Moved only):**
```csharp
extra["ActualLibraryRoot"] = libraryRoot;
extra["AcfFilePath"] = acf.AcfFilePath;
```

**Add to `CreateOrphanedEntry()`:**
```csharp
["FolderName"] = folderName,
["LibraryRoot"] = libraryRoot,
```

**Add to `CreateMissingAcfEntry()`:**
```csharp
["FolderName"] = acf.Installdir,
["AcfExpectedPath"] = Path.Combine(
    acf.LibraryPath, "steamapps", "common", acf.Installdir),
```

### ShellViewModel.cs — LoadGamesForRoot()

**Replace status detail logic with richer messages:**

```csharp
"Orphaned" => FormatOrphanedDetail(game),
"Missing" => FormatMissingDetail(game),
"Moved" => FormatMovedDetail(game),
```

**New helper methods:**
```csharp
private static string FormatOrphanedDetail(GameEntry game)
{
    string folder = game.PlatformMetadata.GetValueOrDefault("FolderName", game.FolderName);
    string libRoot = game.PlatformMetadata.GetValueOrDefault("LibraryRoot", "unknown library");
    return $"Orphaned — no Steam manifest for '{folder}'. " +
           $"This folder exists in {libRoot} but is not registered with Steam. " +
           "To fix: Use ACF Generate to create a manifest, or re-install via Steam.";
}

private static string FormatMissingDetail(GameEntry game)
{
    string acfPath = game.PlatformMetadata.GetValueOrDefault("AcfFilePath", "");
    string expected = game.PlatformMetadata.GetValueOrDefault("AcfExpectedPath", "");
    return $"Missing — Steam manifest at {acfPath} expects files at {expected}. " +
           "Game folder not found in any configured library. " +
           "Possible: game is in an unconfigured library, or was uninstalled. " +
           "To fix: Add the correct Steam library via F2, or delete the orphaned ACF.";
}

private static string FormatMovedDetail(GameEntry game)
{
    string acfPath = game.PlatformMetadata.GetValueOrDefault("AcfFilePath", "unknown");
    string acfLib = game.PlatformMetadata.GetValueOrDefault("AcfLibraryPath", "unknown");
    string actualLib = game.PlatformMetadata.GetValueOrDefault("ActualLibraryRoot", "unknown");
    string folder = game.PlatformMetadata.GetValueOrDefault("FolderName", game.FolderName);
    string acfFileName = Path.GetFileName(acfPath);
    string targetPath = Path.Combine(actualLib, "steamapps", acfFileName);
    return $"Moved — game '{folder}' found in {actualLib} but ACF is in {acfLib}. " +
           $"To fix: Move ACF to {targetPath} and restart Steam.";
}
```

---

## 5. Files Changed

| File | Change |
|------|--------|
| `SteamLibraryScanner.cs` | Add `FolderName` to all statuses; add `LibraryRoot` to Orphaned; add `AcfExpectedPath` to Missing; add `ActualLibraryRoot` + `AcfFilePath` to Moved |
| `ShellViewModel.cs` | Replace 3 status detail strings with `FormatXxxDetail()` helper methods |

---

## 6. Tests

- `SteamLibraryScannerTests.cs`: Verify new metadata keys (`FolderName`, `LibraryRoot`, `AcfExpectedPath`, `ActualLibraryRoot`, `AcfFilePath`) are present for all 4 statuses
- `ShellViewModelTests.cs`: Verify status detail text includes actionable guidance and correct paths

---

## 7. Success Criteria

- [ ] Orphaned status shows: what it means, why it happened, how to fix
- [ ] Missing status shows: which ACF, where it expects files, that game might be in unconfigured library
- [ ] Moved status shows: where ACF is, where game is, exact target path for ACF move
- [ ] All status details are concise (2-3 lines max)
- [ ] Moved fix is a simple file move — no reinstallation needed
- [ ] Build clean, all tests pass

---

## 8. Future: ACF Re-Linking (Core Feature)

The status messages reference ACF Move and ACF Generate as fix actions. These are core features of GamingCommander — the application's purpose includes re-linking installed games to their game stores:

- **ACF Move:** Move the ACF file from one library's `steamapps/` to another. For Moved games, this is the primary fix — Steam then finds the files automatically. No content is moved, just the 1KB manifest file.
- **ACF Generate:** Create a minimal ACF for an orphaned game folder. Requires knowing the Steam AppID (can be discovered via Steam API or user input). Registers the game with Steam so it appears in the library.
- **ACF Edit:** Modify ACF fields (name, AppID, installdir, state flags). Useful for fixing corrupted manifests or updating metadata.

### Why ACF Move Is Simple

Unlike moving game files (which can be hundreds of GB), moving an ACF is instant:
- ACF files are ~1KB JSON-like text files
- They live in `{library}\steamapps\appmanifest_{appid}.acf`
- Moving one from `D:\steamapps\` to `E:\steamapps\` takes milliseconds
- Steam re-reads the ACF on next launch and finds the game in the new library

### Multi-Library Scenario

```
User has 3 Steam libraries:
  D:\SteamLibrary (200GB, mostly full)
  E:\SteamLibrary (500GB, lots of space)
  F:\SteamLibrary (1TB, backup)

User moves "The Witcher 3" from D: to E: via Windows Explorer.
Steam still has ACF on D: → game shows as "Moved"

Fix: Move ACF from D:\steamapps\appmanifest_292030.acf
         To: E:\steamapps\appmanifest_292030.acf

Steam now finds ACF on E:, reads installdir, finds game folder on E:. Done.
```

---

**Last updated:** 2026-07-26  
**Related:** `docs/GAME-DETECTION-LOGIC.md` (Steam Library System section), `planning/110-user-tags-source-tagging.md`
