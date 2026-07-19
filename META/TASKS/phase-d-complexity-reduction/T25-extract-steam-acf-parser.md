# Task T25: Extract SteamAcfParser from SteamLibraryScanner

**Tier:** 2 — Code Structure
**Phase:** D — Complexity Reduction
**Effort:** ~35 min
**Risk:** Low
**Status:** ✅ completed (updated per evaluation)

---

## Completion Notes

- **Completed:** 2026-07-19
- **What was done:** Created `SteamAcfParser.cs` with `ParseAcfFile`, `DiscoverLibraryPaths`, `NormalizePath`, and moved existing `AcfInfo` record (kept original field names). Updated SteamLibraryScanner to call `SteamAcfParser.ParseAcfFile()`, `SteamAcfParser.DiscoverLibraryPaths()`, `SteamAcfParser.NormalizePath()`. Removed `RequiredAcfFields`, `ParseAcfFile`, `DiscoverLibraryPaths`, `AcfInfo` record, `NormalizePath` from SteamLibraryScanner.
- **Verification:** Build clean (0 errors), 17 tests passing.
- **Issues encountered:** Had to keep `using GamingCommander.Core.Services` for `GameEntryId` (not just VdfParser).

---

## Evaluation Corrections (2026-07-19)

1. **`AcfInfo` record already exists** at SteamLibraryScanner.cs line 436. Do NOT create a new `SteamAcfInfo` — move the existing `AcfInfo` record to `SteamAcfParser.cs` and make it `internal`.
2. **Field naming:** Use actual field names from code: `LibraryPath`, `AcfFilePath`, `AppId`, `Name`, `Installdir`, `StateFlags`, `LastUpdated`, `SizeOnDisk`, `BuildId` (not the task's proposed names).
3. **`VdfParser` using moves:** Both `ParseAcfFile` and `DiscoverLibraryPaths` use `VdfParser` — the `using GamingCommander.Core.Services` moves from SteamLibraryScanner to SteamAcfParser.
**Prerequisites:** None

---

## Objective

`SteamLibraryScanner.cs` (472 lines) mixes two concerns: ACF file parsing (reading and extracting fields from .acf files) and library orchestration (cross-referencing across libraries, detecting statuses). Extract the ACF parsing logic to a dedicated class. This is a proactive split — ACF parsing will grow as more fields are needed for metadata enrichment.

## What Needs to Change

### 1. New file: `src/GamingCommander.App/Services/SteamAcfParser.cs`

**Current state:** Does not exist.
**Actions:**
- [ ] Create `SteamAcfParser.cs` with namespace `GamingCommander.App.Services`
- [ ] Add `/// <summary>` to class: "Parses Steam ACF (App Manifest) files and libraryfolders.vdf. Provides structured access to game metadata stored in VDF format."
- [ ] Move the following from `SteamLibraryScanner.cs`:
  - `RequiredAcfFields` static field (line 17) → `SteamAcfParser.RequiredAcfFields`
  - `ParseAcfFile(string acfPath, string libraryPath)` (lines 252-278) → `SteamAcfParser.ParseAcfFile(string acfPath, string libraryPath)`
  - `DiscoverLibraryPaths(string libraryRootPath)` (lines 184-218) → `SteamAcfParser.DiscoverLibraryPaths(string libraryRootPath)`
  - `ParseLibraryFoldersVdf(string vdfPath)` internal helper if it exists → `SteamAcfParser`
- [ ] Create a record for ACF data:
  ```csharp
  /// <summary>
  /// Parsed metadata from a Steam ACF (appmanifest) file.
  /// </summary>
  public sealed record SteamAcfInfo(
      string AppId,
      string Name,
      string InstallDir,
      int StateFlags,
      long LastUpdated,
      long SizeOnDisk,
      string BuildId,
      string LibraryPath,
      string AcfPath);
  ```
- [ ] Update `ParseAcfFile` to return `SteamAcfInfo?` instead of building a dictionary
- [ ] All methods become `internal static` (no state dependencies)
- [ ] Add `/// <summary>` XML docs to all methods and the record

### 2. `src/GamingCommander.App/Services/SteamLibraryScanner.cs`

**Current state:** Lines 17, 184-218, 252-278 contain ACF parsing logic
**Actions:**
- [ ] Delete `RequiredAcfFields` (line 17)
- [ ] Delete `ParseAcfFile` (lines 252-278)
- [ ] Delete `DiscoverLibraryPaths` (lines 184-218)
- [ ] Update `CollectAcfMap` to call `SteamAcfParser.ParseAcfFile()` and `SteamAcfParser.DiscoverLibraryPaths()`
- [ ] Update `DiscoverAllSteamPaths` to call `SteamAcfParser.DiscoverLibraryPaths()`
- [ ] Remove `using GamingCommander.Core.Services;` if VdfParser was the only reason (now in SteamAcfParser)
- [ ] Reduce SteamLibraryScanner from ~472 to ~280 lines

## Context

- ACF parsing is a self-contained concern — it reads VDF files and extracts structured data
- The scanner's main job is orchestration: cross-referencing ACFs across libraries, detecting statuses
- Future growth: more ACF fields will be needed for metadata (launcher args, DLC info, cloud save paths)
- `SteamAcfInfo` record replaces the anonymous dictionary currently returned by `ParseAcfFile`
- `DiscoverLibraryPaths` is called by both `Scan()` and `ScanAll()` — it's a pure utility

## Requirements

- [ ] `SteamAcfParser.cs` created with `SteamAcfInfo` record, `ParseAcfFile`, `DiscoverLibraryPaths`
- [ ] All members have `/// <summary>` XML docs
- [ ] `SteamLibraryScanner.cs` no longer contains ACF parsing logic
- [ ] `SteamLibraryScanner` calls `SteamAcfParser.*` for all ACF operations
- [ ] No behavior change — same parsing, same results
- [ ] `SteamAcfParser` class is `internal static`

## Verification

- [ ] `dotnet build` passes (0 errors)
- [ ] `dotnet test` passes (17 tests)
- [ ] `grep -c "ParseAcfFile\|DiscoverLibraryPaths\|RequiredAcfFields" src/GamingCommander.App/Services/SteamLibraryScanner.cs` returns 0 (all moved)
- [ ] `grep -c "SteamAcfParser" src/GamingCommander.App/Services/SteamLibraryScanner.cs` returns 2+ (call sites)
- [ ] `wc -l src/GamingCommander.App/Services/SteamLibraryScanner.cs` shows < 300 lines

## Completion Notes

- **Completed:**
- **What was done:**
- **Verification:**
- **Issues encountered:**
