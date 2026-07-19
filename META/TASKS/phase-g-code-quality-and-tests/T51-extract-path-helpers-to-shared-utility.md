# Task T51: Extract Path Helpers to Shared Utility

**Tier:** 4 — Code Quality
**Phase:** G — Code Quality & Tests
**Effort:** ~20 min
**Risk:** Low
**Status:** pending

---

## Objective

`GetConfigPath()` and `GetGamesDbPath()` are duplicated identically in `App.axaml.cs` (lines 187-203) and `MainWindow.axaml.cs` (lines 85-101). Extract to `FileSystemHelper` to eliminate DRY violation.

## What Needs to Change

### `src/GamingCommander.App/Services/FileSystemHelper.cs`
- [ ] Add `GetConfigPath()` static method
- [ ] Add `GetGamesDbPath()` static method
- [ ] Both compute the same paths and create `data/` directory if needed

### `src/GamingCommander.App/App.axaml.cs`
- [ ] Remove local `GetConfigPath()` and `GetGamesDbPath()` methods (lines 187-203)
- [ ] Replace calls with `FileSystemHelper.GetConfigPath()` and `FileSystemHelper.GetGamesDbPath()`

### `src/GamingCommander.App/MainWindow.axaml.cs`
- [ ] Remove local `GetConfigPath()` and `GetGamesDbPath()` methods (lines 85-101)
- [ ] Replace calls with `FileSystemHelper.GetConfigPath()` and `FileSystemHelper.GetGamesDbPath()`

## Context

- Both implementations are identical: they compute `Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", ...)` and call `Directory.CreateDirectory`.
- Having two copies means a change to one but not the other would silently cause the app to use different files.

## Requirements

- [ ] Methods exist in exactly one place (FileSystemHelper)
- [ ] Both callers updated
- [ ] No behavior change

## Verification

- [ ] `dotnet build` passes (0 errors)
- [ ] `dotnet test` passes
- [ ] `grep -c "GetConfigPath" src/` — shows exactly 2 references (1 in FileSystemHelper, 1 call site in App.axaml.cs; MainWindow calls it via FileSystemHelper)

## Completion Notes

- **Completed:**
- **What was done:**
- **Verification:**
- **Issues encountered:**
