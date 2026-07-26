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
| **Orphaned** | "Orphaned" (red) | "Orphaned -- game folder has no ACF registration" |
| **Missing** | "Missing" (red) | "Missing -- ACF exists but game files not found" |
| **Moved** | "Moved" (yellow) | "Moved -- ACF expects: D:\..." |

**User feedback:** "Orphaned" is confusing — users don't know what it means or what to do. The distinction between Orphaned and Missing is unclear. No actionable guidance is provided.

---

## 1. Proposed Status Messages

### Orphaned (game folder exists, no ACF anywhere)

**Current:** `"Orphaned -- game folder has no ACF registration"`

**Proposed:**
```
Status: Orphaned (red)
Detail: "No Steam manifest found for this game folder.
         The folder '{folderName}' exists in {libraryRoot} but is not registered with Steam.
         
         Possible causes:
         • Game was installed outside Steam (manual copy, backup restore)
         • Steam manifest was deleted or moved
         • Game was removed from Steam library but files remain
         
         To fix: Right-click in Steam → Properties → Local Files → Verify integrity"
```

**What data is available:**
- `folderName` — the orphaned game folder name
- `libraryRoot` — the Steam library where the folder lives
- No ACF data (by definition — no ACF exists)

### Missing (ACF exists, no game folder)

**Current:** `"Missing -- ACF exists but game files not found"`

**Proposed:**
```
Status: Missing (red)
Detail: "Steam manifest exists but game files are missing.
         ACF '{acfName}' (AppID: {appId}) at {acfPath} expects files at {expectedPath}, 
         but the game folder was not found in any library.
         
         Possible causes:
         • Game was uninstalled but the manifest was not cleaned up
         • Game files were moved or deleted manually
         • Game is installed in a library that is not configured
         
         To fix: Re-install from Steam, or delete the orphaned ACF"
```

**What data is available:**
- `acfName` — from ACF metadata
- `appId` — from ACF metadata
- `acfPath` — full path to the `.acf` file
- `expectedPath` — where the ACF expects the game to be (not currently stored, but computable)

### Moved (ACF in library A, game folder in library B)

**Current:** `"Moved -- ACF expects: D:\..."`

**Proposed:**
```
Status: Moved (yellow)
Detail: "Game files found in a different library than expected.
         ACF '{acfName}' (AppID: {appId}) is in {acfLibraryPath} but 
         the game folder is in {actualLibraryPath}.
         
         This usually happens when you move a game between Steam libraries.
         The ACF was not updated to reflect the new location.
         
         To fix: Re-install from Steam, or move the game folder back to {expectedPath}"
```

---

## 2. Data Availability

All needed data is already collected during scan but not all is stored in `PlatformMetadata`:

| Data Point | Currently Stored | Needs Adding |
|------------|-----------------|--------------|
| `SteamStatus` | ✅ All statuses | — |
| `SteamAppId` | ✅ Installed/Moved/Missing | — |
| `AcfLibraryPath` | ✅ Installed/Moved/Missing | — |
| `AcfExpectedPath` | ✅ Moved only | Add to Missing |
| `AcfFilePath` | ✅ Missing only | Add to Installed/Moved |
| `FolderName` | ❌ Not stored | Add (available at scan time) |
| `LibraryRoot` | ❌ Not stored for Orphaned | Add (the `rootPath` parameter) |
| `AcfName` | ❌ Not stored | Add (from ACF metadata) |

---

## 3. Code Changes

### SteamLibraryScanner.cs

**Add to `PlatformMetadata` in `CreateEntry()`:**
```csharp
entry.PlatformMetadata["FolderName"] = folderName;
entry.PlatformMetadata["AcfName"] = acf.Name;
```

**Add to `CreateOrphanedEntry()`:**
```csharp
entry.PlatformMetadata["FolderName"] = folderName;
entry.PlatformMetadata["LibraryRoot"] = rootPath;
```

**Add to `CreateMissingAcfEntry()`:**
```csharp
entry.PlatformMetadata["AcfExpectedPath"] = Path.Combine(
    acf.LibraryPath, "steamapps", "common", acf.Installdir);
entry.PlatformMetadata["FolderName"] = acf.Installdir;
```

### ShellViewModel.cs — LoadGamesForRoot()

**Replace status detail logic with richer messages:**

```csharp
case "Orphaned":
    string folderName = game.PlatformMetadata.GetValueOrDefault("FolderName", "");
    string libRoot = game.PlatformMetadata.GetValueOrDefault("LibraryRoot", "");
    platformStatusDetail = $"No Steam manifest found for '{folderName}' in {libRoot}. " +
        "Game folder is not registered with Steam. " +
        "To fix: Right-click in Steam → Properties → Local Files → Verify integrity of game files.";
    break;

case "Missing":
    string acfName = game.PlatformMetadata.GetValueOrDefault("AcfName", "");
    string acfPath = game.PlatformMetadata.GetValueOrDefault("AcfFilePath", "");
    string expectedPath = game.PlatformMetadata.GetValueOrDefault("AcfExpectedPath", "");
    platformStatusDetail = $"Steam manifest '{acfName}' at {acfPath} expects files at {expectedPath}, " +
        "but the game folder was not found in any library. " +
        "To fix: Re-install from Steam, or delete the orphaned manifest.";
    break;

case "Moved":
    // Keep existing logic but enrich
    string movedExpected = game.PlatformMetadata.GetValueOrDefault("AcfExpectedPath", "");
    platformStatusDetail = $"Game files found in a different library than expected. " +
        $"ACF expects: {movedExpected}. " +
        "To fix: Re-install from Steam, or move the game folder back.";
    break;
```

---

## 4. Files Changed

| File | Change |
|------|--------|
| `SteamLibraryScanner.cs` | Add `FolderName`, `AcfName`, `LibraryRoot`, `AcfExpectedPath` to PlatformMetadata |
| `ShellViewModel.cs` | Replace status detail strings with actionable guidance |

---

## 5. Tests

- `SteamLibraryScannerTests.cs`: Verify new metadata keys are present for all 4 statuses
- `ShellViewModelTests.cs`: Verify status detail text includes actionable guidance

---

## 6. Success Criteria

- [ ] Orphaned status shows: what it means, why it happened, how to fix
- [ ] Missing status shows: which ACF, where it is, what's missing, how to fix
- [ ] Moved status shows: where ACF is, where files are, how to fix
- [ ] All status details include the game name/ID for clarity
- [ ] Build clean, all tests pass

---

## 7. Future: Repair/Move/Generate Manifest

The user mentioned "repair/move/generate manifest" as proposed solutions. These are Phase 3 features:
- **Repair:** Re-scan a specific game folder and update its metadata
- **Move:** Relocate a game folder and update the ACF to match
- **Generate Manifest:** Create a minimal ACF for an orphaned game folder (requires knowing the Steam AppID)

These are out of scope for this plan but should be added to the backlog.
