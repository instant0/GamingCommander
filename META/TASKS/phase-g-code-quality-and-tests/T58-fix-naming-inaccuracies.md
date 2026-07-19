# Task T58: Fix Naming Inaccuracies Across Codebase

**Tier:** 2 — Code Quality
**Phase:** G — Code Quality & Tests
**Effort:** ~45 min
**Risk:** Low
**Status:** completed

---

## Objective

Audit found 42 naming issues across the codebase where variable/parameter/property names are too short, abbreviated, or ambiguous. A junior developer or AI agent would struggle to discover what `_db`, `idx`, `gIdx`, `pos`, `dir`, `kvp`, `pcgw`, etc. mean without reading surrounding context.

## What Needs to Change

### Priority 1 — Public API names (breaking changes, highest impact)

**`src/GamingCommander.Core/Models/GameEntry.cs`** (+ cascading updates)
- [ ] Line 16: `bool Override` → `bool IsSourceOverridden` — "Override" alone doesn't say override of what
  - Update: `GamesDatabaseService.cs` (lines 47, 78), `ShellViewModel.cs` (lines 335, 336), `GamesDatabaseServiceTests.cs` (line 188)
- [ ] Line 22: `string CmdlineArgs` → `string CommandLineArguments` — "Cmdline" is abbreviated and inconsistent casing
  - Update: `GamesDatabaseService.cs` (lines 50, 81), `GameSetupWindow.axaml.cs` (line 49)
- [ ] Line 33: `Dictionary<string, string> Extra` → `Dictionary<string, string> PlatformMetadata` — "Extra" is vague
  - Update: `GamesDatabaseService.cs` (lines 54, 85), `ShellViewModel.cs` (lines 287, 288, 294, 319), `ShellPaneItemViewModel.cs` (line 42), `SteamLibraryScannerTests.cs` (18 occurrences)

**`src/GamingCommander.Core/Models/LibraryRoot.cs`**
- [ ] Line 8: `string Path` → `string RootPath` — conflicts with `System.IO.Path`, inconsistent with `GameRoot.RootPath`
  - Update: 0 usages found (only used via DTO mapping)

**`src/GamingCommander.Core/Models/FolderOverride.cs`**
- [ ] Line 10: `GameSourceKind Type` → `GameSourceKind OverrideType` — conflicts with `System.Type`
  - Update: `JsonConfigService.cs` (line 49)

**`src/GamingCommander.Core/Models/FileSystemEntry.cs`**
- [ ] Line 16: `long Size` → `long SizeInBytes` — ambiguous unit
  - Update: 0 usages found

**`src/GamingCommander.Core/Services/GameEntryId.cs`**
- [ ] Line 16: `Compute(...)` → `ComputeId(...)` — "Compute" alone doesn't say what is computed
  - Update: `FolderScanner.cs`, `SteamLibraryScanner.cs` (3 call sites)

**`src/GamingCommander.Core/Models/GameSourceParser.cs`**
- [ ] Line 13: `AvailableTypes` → `SourceDisplayNames` — "Types" is vague, these are display strings
  - Update: `GameSetupWindow.axaml.cs`, `LibrarySetupViewModel.cs`, `WizardViewModel.cs`
- [ ] Line 41: `ParseFromString(string type)` → `ParseFromString(string displayName)` — parameter `type` is generic
  - Update: all callers (search for `ParseFromString`)

**`src/GamingCommander.Core/ILibraryManager.cs` + `IGamesDatabaseService.cs`**
- [ ] `AddRoot(..., games)` → `AddRoot(..., initialGames)` — "games" is vague, unclear if it's seed data or something else
  - Update: all implementors and callers
- [ ] `UpdateGameEntry(string rootPath, GameEntry updated)` → `UpdateGameEntry(string rootPath, GameEntry updatedEntry)` — "updated" is ambiguous
  - Update: all callers

### Priority 2 — Private field/variable names (high readability impact)

**`src/GamingCommander.App/Services/LibraryManager.cs`**
- [ ] Line 19: `_db` → `_databaseService` — inconsistent with `_configService`, `_scanner` which use full names

**`src/GamingCommander.App/Services/GamesDatabaseService.cs`**
- [ ] Lines 129, 159, 172: `int idx` → `int rootIndex` — abbreviation repeated in 3 methods
- [ ] Lines 146, 176: `int gIdx` → `int gameIndex` — abbreviation repeated in 2 methods

**`src/GamingCommander.App/Services/BlacklistLoader.cs`**
- [ ] Line 55: `var dirPatterns` → `var directoryPatterns`
- [ ] Line 56: `var peMetaPatterns` → `var peMetadataPatterns`
- [ ] Line 57: `var pcgwNoise` → `var pcgwTitleNoisePatterns` — "pcgw" alone is not self-documenting

**`src/GamingCommander.App/Services/SteamAcfParser.cs`**
- [ ] Lines 29, 105: `installdir` / `Installdir` → `installDir` / `InstallDir` — inconsistent casing (leaks Valve's ACF format)

**`src/GamingCommander.App/Services/HelpDialogBuilder.cs`**
- [ ] Lines 23-26: `bgColor`, `textColor`, `headerColor`, `keyColor` → `backgroundBrush`, `textBrush`, `headerBrush`, `keyBrush` — these are `IBrush` objects, not `Color` values

**`src/GamingCommander.App/GameSetupWindow.axaml.cs`**
- [ ] Line 13: `_original` → `_originalGame` — doesn't say original *what*
- [ ] Lines 109, 172: `fieldIdx` → `fieldIndex` — abbreviation in parameter names

**`src/GamingCommander.Core/Services/VdfParser.cs`**
- [ ] Line 20: `int idx` → `int lineIndex` — abbreviation
- [ ] Lines 74, 98: `int pos` → `int charPos` — abbreviation in 2 methods

**`src/GamingCommander.UI/ViewModels/ReactiveObject.cs`**
- [ ] Line 24: `ref T field` → `ref T backingField` — "field" clashes with C# field concept

### Priority 3 — Local variable names (moderate readability impact)

**`src/GamingCommander.App/Services/FolderScanner.cs`**
- [ ] Lines 159, 190, 209, 238, 253: `dir` parameter → `gameFolder` — used in 5 separate methods, "dir" is generic

**`src/GamingCommander.App/Services/ExecutableDiscovery.cs`**
- [ ] Line 188: `var lookup` → `var tierLookupFallback` — vague name wraps another parameter

**`src/GamingCommander.App/Services/SteamLibraryScanner.cs`**
- [ ] Lines 70-73: `kvp` → `entry` — abbreviation in foreach loop

## Context

- Public API renames (Priority 1) affect code references but NOT JSON serialization — `GamesDatabaseService` uses separate DTO classes (`GameEntryDto`, `GameRootDto`) with their own property names that map to the domain records
- Private/field renames (Priority 2) are safe local changes
- Local variable renames (Priority 3) are lowest risk
- All renames are pure identifier changes — no logic changes
- Estimated total call sites to update: ~40 (mostly mechanical find-replace)

## Requirements

- [ ] All Priority 1 names updated (including JSON DTO mapping in GamesDatabaseService)
- [ ] All Priority 2 names updated
- [ ] All Priority 3 names updated
- [ ] No behavior change
- [ ] All existing tests still pass

## Verification

- [ ] `dotnet build` passes (0 errors)
- [ ] `dotnet test` passes (99+ tests)
- [ ] `grep -rn "\bidx\b\|bgIdx\b\|_db\b\|\.Override\b\|\.Extra\b\|\.CmdlineArgs\b" src/` — returns 0 hits

## Completion Notes

- **Completed:**
- **What was done:**
- **Verification:**
- **Issues encountered:**
