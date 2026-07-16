# Plan 95: Bug Fixes, Dead Code Removal, and Helper Extraction

**Date:** 2026-07-16
**Status:** Completed
**Priority:** P0 (bug fixes) → P1 (cleanup/DRY)
**Trigger:** Architectural review of Phase 2 codebase — found bugs, dead code, and duplicate logic.

---

## Context

After completing the Game Detection Overhaul (Phase 1+2), Build Versioning, VFS Cache, and Keyboard Layout bundles, an architectural review found:
- Two bugs (one masked, one invisible binding)
- Dead interfaces/classes/constructors never used in production
- Identical code duplicated across 3-4 files
- Scanner routing logic duplicated in MainWindow instead of using LibraryManager

---

## Changes

### Bundle A: Bug Fixes (P0)

#### A1. WizardViewModel drops `LastSeenVersion`

**File:** `src/GamingCommander.App/ViewModels/WizardViewModel.cs`

**Bug:** `Finish()` and `Cancel()` create a new `AppConfig` using the positional constructor, which defaults `LastSeenVersion` to `null`. This wipes any stored version. The `App.axaml.cs` Closed handler re-stamps the version afterward, masking the bug on the happy path. But if the app crashes between the two saves, the version is permanently lost, causing the wizard to re-trigger on every launch.

**Fix:** In both `Finish()` and `Cancel()`, pass `current.LastSeenVersion` to the new AppConfig:

```csharp
// Finish() — line 129
AppConfig config = new AppConfig(roots, [], current.HiddenFolders,
    IsFirstRun: false, current.LastSeenVersion, EnableOnlineMetadata);

// Cancel() — line 144
AppConfig config = new AppConfig(roots, [], current.HiddenFolders,
    IsFirstRun: false, current.LastSeenVersion, EnableOnlineMetadata);
```

**Files changed:** `WizardViewModel.cs` (2 lines changed)

---

#### A2. `IsFolderSelected` binding references non-existent property

**Bug:** `MainWindow.axaml` line 99 binds `IsVisible="{Binding IsFolderSelected}"` but `ShellViewModel` has no `IsFolderSelected` property. The binding silently fails, making the "Press F4 to edit" hint permanently invisible.

Additionally, `ShellViewModel` has `IsGameSelected` (line 76) which is never bound in AXAML and has wrong semantics (`IsBrowsable` is true for directories, not games).

**Fix:**
1. Remove `IsGameSelected` property from `ShellViewModel.cs` (line 76)
2. Remove `OnPropertyChanged(nameof(IsGameSelected))` from `UpdateDetailsForSelection()` (line 275)
3. Add new property `HasGameSelected`:
   ```csharp
   public bool HasGameSelected => SelectedItem is { Kind: FileSystemEntryKind.File };
   ```
4. Add `OnPropertyChanged(nameof(HasGameSelected))` to `UpdateDetailsForSelection()`
5. Update `MainWindow.axaml` line 99: change `IsFolderSelected` to `HasGameSelected`

**Files changed:** `ShellViewModel.cs`, `MainWindow.axaml`

---

### Bundle B: Dead Code Removal (P1)

#### B1. Remove `ILauncher` interface

**File:** `src/GamingCommander.Core/ILauncher.cs` (delete)

**Rationale:** Zero implementations. Zero references. The actual architecture uses `LibraryManager` → `FolderScanner`/`SteamLibraryScanner`. ADR-008 described this interface but the pragmatic two-tier scanner approach replaced it. (Note: `IGame` stays — it's used by `GameRecord` which is used by `IMigrationPlanner`.)

**Files changed:** Delete `ILauncher.cs`

---

#### B2. Remove `DesignTimeLibraryManager`

**File:** `src/GamingCommander.App/Services/DesignTimeLibraryManager.cs` (delete)

**Rationale:** Zero instantiations. The real `LibraryManager` replaced it entirely.

**Files changed:** Delete `DesignTimeLibraryManager.cs`

---

#### B3. Remove `MainWindow` dead constructors and helpers

**File:** `src/GamingCommander.App/MainWindow.axaml.cs`

**Rationale:** Three members are dead:
- Parameterless `MainWindow()` constructor (line 26-30) — `App.axaml.cs` always uses the parameterized constructor
- `InitializeServices()` method (line 89-113) — only called by the dead parameterless constructor
- `CreateDefaultViewModel()` method (line 116-133) — never called from anywhere

**Fix:** Remove all three. Keep the parameterized constructor and its service creation (lines 51-59) — those are actively used.

**Files changed:** `MainWindow.axaml.cs` (~60 lines removed)

---

#### B4. Remove `LibrarySetupViewModel` unused `scanner` parameter

**Files:** `LibrarySetupViewModel.cs`, `LibrarySetupWindow.axaml.cs`, `MainWindow.axaml.cs`

**Rationale:** `LibrarySetupViewModel` constructor accepts `FolderScanner scanner` (line 21) but never stores or uses it. `ScanAndSaveAsync()` delegates to `_libraryManager.AddRoot()` which handles scanner routing internally. `LibrarySetupWindow` also accepts and passes through this unused parameter.

**Fix:**
1. Remove `scanner` parameter from `LibrarySetupViewModel` constructor
2. Remove `scanner` parameter from `LibrarySetupWindow` constructor
3. Update `MainWindow.OpenLibrarySetupAsync()` to stop passing `_scanner`

**Files changed:** `LibrarySetupViewModel.cs`, `LibrarySetupWindow.axaml.cs`, `MainWindow.axaml.cs`

---

### Bundle C: Helper Extraction (P1)

#### C1. Extract `ComputeId` to shared helper

**Files:** New `Core/Services/GameEntryId.cs`, `FolderScanner.cs`, `SteamLibraryScanner.cs`

**Rationale:** Both scanners have identical `ComputeId(rootPath, folderName)` methods using MD5. If the ID computation ever changes, both files need updating in lockstep.

**Fix:**
1. Create `src/GamingCommander.Core/Services/GameEntryId.cs`:
   ```csharp
   namespace GamingCommander.Core.Services;

   public static class GameEntryId
   {
       public static string Compute(string rootPath, string folderName)
       {
           string combined = $"{rootPath}|{folderName}";
           byte[] hash = System.Security.Cryptography.MD5.HashData(
               System.Text.Encoding.UTF8.GetBytes(combined));
           return System.Convert.ToHexString(hash)[..16].ToLowerInvariant();
       }
   }
   ```
2. Replace `ComputeId()` in `FolderScanner.cs` and `SteamLibraryScanner.cs` with calls to `GameEntryId.Compute()`

**Files changed:** 1 new file, 2 modified files

---

#### C2. Extract `InferType` / `ParseType` to shared helper

**Files:** New `Core/Models/GameSourceParser.cs`, `WizardViewModel.cs`, `LibrarySetupViewModel.cs`

**Rationale:** Both ViewModels have identical `InferType(string path)` and `ParseType(string type)` methods.

**Fix:**
1. Create `src/GamingCommander.Core/Models/GameSourceParser.cs`:
   ```csharp
   namespace GamingCommander.Core.Models;

   public static class GameSourceParser
   {
       public static GameSourceKind InferFromPath(string path) { ... }
       public static GameSourceKind ParseFromString(string type) { ... }
   }
   ```
2. Replace the duplicated methods in both ViewModels with calls to `GameSourceParser`

**Files changed:** 1 new file, 2 modified files

---

### Bundle D: MainWindow DRY Refactoring (P1)

#### D1. Route scanner logic through LibraryManager

**File:** `MainWindow.axaml.cs`, `LibraryManager.cs`

**Rationale:** `RefreshCurrentRootAsync()` (lines 392-398 and 412-414) and `AddRootAsync()` (lines 441-445) duplicate the scanner selection logic that `LibraryManager.SelectScannerAndScan()` already handles. Three places implement the same `if SteamScanner && looksLikeSteam → steam else → folder` routing.

**Fix:**
1. Store `_libraryManager` as a field in `MainWindow` (it's already created in the constructor but not stored)
2. Replace manual scanner routing in `RefreshCurrentRootAsync()` root-level with `_libraryManager.Refresh()`
3. Replace manual scanner routing in `RefreshCurrentRootAsync()` game-level with `_libraryManager` scan + `_viewModel.ApplyRescannedGames()`
4. Simplify `AddRootAsync()` to call `libraryManager.AddRoot(result, detectedType, [])` and let LibraryManager handle scanning

**Files changed:** `MainWindow.axaml.cs`

---

### Bundle E: Documentation (P1)

#### E1. Update `META/CODE_MAP.md`

Fix stale data:
- Line counts for modified files
- Add missing files (SteamLibraryScanner, VdfParser, BlacklistLoader, GameEntryId, GameSourceParser)
- Update keyboard mapping (F4, not T)
- Fix test count
- Remove references to deleted files (ILauncher, DesignTimeLibraryManager)

#### E2. Add note to `planning/90-sdk-upgrade.md`

Add a note that .NET 9 upgrade is lowest priority — working application comes first.

#### E3. Update `META/SESSION/CURRENT.md`

Record session activity at end.

#### E4. Update `META/SESSION/NEXT.md`

Set next task to "UI Polish for Steam Status" (the task deferred by this cleanup pass).

---

## Execution Order

1. Bundle A — Bug fixes (A1, A2)
2. Bundle B — Dead code removal (B1, B2, B3, B4)
3. Bundle C — Helper extraction (C1, C2)
4. Bundle D — MainWindow DRY refactoring (D1)
5. Build and test
6. Bundle E — Documentation (E1, E2, E3, E4)

---

## Risk Assessment

| Bundle | Risk | Rationale |
|--------|------|-----------|
| A1 | Zero | Preserving an existing value instead of dropping it |
| A2 | Low | Binding was already broken; now it works |
| B1-B2 | Zero | No code depends on deleted types |
| B3 | Low | Dead constructors never called |
| B4 | Low | Removing unused parameter |
| C1-C2 | Very low | Pure extraction, no behavior change |
| D1 | Low | Routing through existing LibraryManager methods |
| E | Zero | Documentation only |

---

## Verification

1. `dotnet build` — must produce 0 errors
2. `dotnet test` — all existing tests must pass (17 tests)
3. Manual: verify hint text "Press F4 to edit" appears when a game is selected
