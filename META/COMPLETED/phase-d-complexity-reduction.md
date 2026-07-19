# Phase D: Complexity Reduction — Complete

**Completed:** 2026-07-19
**Tasks:** T16–T30 (15 tasks: 10 completed, 3 skipped, 2 merged)
**Effort:** ~9 hours estimated, completed across multiple sessions

---

## Summary

Reduced mental complexity across the codebase by extracting shared utilities, eliminating duplication, improving naming, adding documentation, and proactively splitting files.

## Task Breakdown

### Layer 1 — Shared Utilities (T16–T18)
- **T16:** Extracted `FileSystemHelper.cs` — shared `GetDirectoriesSafe`, `GetFilesSafe`, `GetLastWriteTimeSafe`, `NormalizeDisplayName`
- **T17:** Extracted `JsonFileHelper.cs` — shared JSON read/write with parameterized options; integrated into 3 services
- **T18:** Extracted `GameSourceParser.AvailableTypes` — single definition in Core; removed from 4 App files

### Layer 2 — Naming & Docs (T19–T22)
- **T19:** Renamed ambiguous variables across 6 files (`p`→`pattern`, `a/b`→`versions`, `sid/eid`→`steamAppId/epicCatalogItemId`, etc.)
- **T20:** Added XML docs to ~20 public members across 8 files
- **T21:** Deleted dead `IsNoiseExePattern`; renamed `IsNonGameExe`→`IsNoiseExeByPath`; added XML docs to noise-check methods
- **T22:** Unified `NormalizeDisplayName` in FileSystemHelper; removed from FolderScanner + SteamLibraryScanner

### Layer 3 — Proactive Splits (T23–T29)
- **T23:** Extracted `StoreSignalDetector.cs` — `DetectType` + 10 signal methods from FolderScanner
- **T24:** Extracted `ExecutableDiscovery.cs` — `FindExecutablesDeep`, `ScoreExecutable`, `FindPrimaryExecutable`, `FindLauncherExecutable`, `ExeNameMatchesFolderName`, `FindEpicManifest` from FolderScanner
- **T25:** Extracted `SteamAcfParser.cs` — `ParseAcfFile`, `DiscoverLibraryPaths`, `NormalizePath`, `AcfInfo` record from SteamLibraryScanner
- **T27:** Extracted `HelpDialogBuilder.cs` — `ShowHelpAsync(Window)` from MainWindow (107 lines)
- **T26:** Skipped — overengineered (two 10-case switch statements are clear as-is)
- **T28:** Skipped — high risk, low value (ShellViewModel under 500-line limit, XAML binding changes error-prone)
- **T29:** Skipped — trivial (LibraryManager.NormalizeLibraryRoot already exists, duplicate check is 1 line)

### Layer 4 — Pattern Extraction (T30)
- **T30:** Merged into T17 (JsonFileHelper extraction)

## Key Outcomes

- **8 new files extracted:** FileSystemHelper, JsonFileHelper, StoreSignalDetector, ExecutableDiscovery, SteamAcfParser, HelpDialogBuilder, BlacklistData, GameRoot/GamesDatabase model splits
- **Duplicated code eliminated:** Shared utilities, unified methods, single-source constants
- **Documentation improved:** XML docs on ~20 public members, clear variable names
- **No behavior changes:** All refactors were structural, preserving existing functionality

## Files Changed

| File | Change |
|------|--------|
| `FileSystemHelper.cs` | New — shared filesystem utilities |
| `JsonFileHelper.cs` | New — shared JSON I/O |
| `StoreSignalDetector.cs` | New — extracted from FolderScanner |
| `ExecutableDiscovery.cs` | New — extracted from FolderScanner |
| `SteamAcfParser.cs` | New — extracted from SteamLibraryScanner |
| `HelpDialogBuilder.cs` | New — extracted from MainWindow |
| `BlacklistData.cs` | New — extracted from BlacklistLoader |
| `GameRoot.cs` | New — split from GameEntry.cs |
| `GamesDatabase.cs` | New — split from GameEntry.cs |
| `GameRecord.cs` | New — split from GameEntry.cs |
| `FolderScanner.cs` | Reduced — extracted methods removed |
| `SteamLibraryScanner.cs` | Reduced — extracted methods removed |
| `MainWindow.axaml.cs` | Reduced — HelpDialogBuilder extracted |
