# Task T51: Extract Path Helpers to Shared Utility

**Tier:** 4 — Code Quality
**Phase:** G — Code Quality & Tests
**Effort:** ~20 min
**Risk:** Low
**Status:** completed

---

## Objective

`GetConfigPath()` and `GetGamesDbPath()` were duplicated identically in `App.axaml.cs` and `MainWindow.axaml.cs`. They were extracted to the shared `AppDataPaths` helper to eliminate the DRY violation. (`AppDataPaths` is the shipped helper — not `FileSystemHelper`.)

## What Needs to Change

### `src/GamingCommander.App/AppDataPaths.cs`
- [x] Add `GetConfigPath()` static method
- [x] Add `GetGamesDbPath()` static method
- [x] Both compute the same paths and create `data/` directory if needed

### `src/GamingCommander.App/App.axaml.cs`
- [x] Remove local `GetConfigPath()` and `GetGamesDbPath()` methods
- [x] Replace calls with `AppDataPaths.GetConfigPath()` and `AppDataPaths.GetGamesDbPath()`

### `src/GamingCommander.App/MainWindow.axaml.cs`
- [x] Remove local `GetConfigPath()` and `GetGamesDbPath()` methods
- [x] Replace calls with `AppDataPaths.GetConfigPath()` and `AppDataPaths.GetGamesDbPath()`

## Context

- Both implementations were identical: they compute `Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", ...)` and call `Directory.CreateDirectory`.
- `GetDirectoriesSafe` and `GetLastWriteTimeSafe` remain in `FileSystemHelper` — they are filesystem-read helpers, not app-data path helpers.

## Requirements

- [x] Methods exist in exactly one place (`AppDataPaths`)
- [x] Both callers updated
- [x] No behavior change

## Verification

- [x] `dotnet build` passes (0 errors)
- [x] `dotnet test` passes
- [x] `grep -rn "GetConfigPath" src/` shows `AppDataPaths` ownership plus call sites, not a duplicate helper

## Completion Notes

- **Completed:** 2026-09-09
- **What was done:** The shipped helper is `AppDataPaths` at `src/GamingCommander.App/AppDataPaths.cs`; it owns `GetConfigPath`, `GetGamesDbPath`, `GetLibrariesPath`, and `GetMetadataDbPath`. `GetDirectoriesSafe`/`GetLastWriteTimeSafe` remain in `FileSystemHelper` for filesystem reads.
- **Verification:** `AppDataPaths.cs` exists with the path getters; `FileSystemHelper.cs` retains `GetDirectoriesSafe`/`GetLastWriteTimeSafe`; call sites use `AppDataPaths`.
- **Issues encountered:** The original task targeted `FileSystemHelper`, which was the wrong home for app-data paths.
