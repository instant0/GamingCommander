# Task T50: Add LibraryManager Unit Tests

**Tier:** 3 — Logic/Behavior
**Phase:** G — Code Quality & Tests
**Effort:** ~40 min
**Risk:** Low
**Status:** pending

---

## Objective

`LibraryManager` is the central orchestrator wiring `IConfigService`, `IGamesDatabaseService`, `FolderScanner`, and `SteamLibraryScanner` together. It has zero test coverage. Add tests for its routing logic and utility methods.

## What Needs to Change

### New file: `tests/GamingCommander.App.Tests/LibraryManagerTests.cs`

**Current state:** Does not exist.
**Actions:**
- [ ] Create test class with `[Fact]` tests
- [ ] Use temp directories + mock services (or real services with temp paths)
- [ ] Test cases:

**Scanner selection:**
- [ ] `LooksLikeSteamLibrary_WithSteamAppsDir_ReturnsTrue` — dir has `steamapps/common/`
- [ ] `LooksLikeSteamLibrary_WithoutSteamApps_ReturnsFalse` — plain dir
- [ ] `NormalizeLibraryRoot_WithTrailingSlash_RemovesSlash` — `D:\Games\` → `D:\Games`

**Scan routing:**
- [ ] `Scan_SteamLibrary_UsesSteamScanner` — verify Steam games detected
- [ ] `Scan_StandaloneLibrary_UsesFolderScanner` — verify standalone games detected

**Root management:**
- [ ] `AddRoot_PersistsToConfig` — add root → appears in config
- [ ] `RemoveRoot_RemovesFromConfig` — remove root → gone from config

## Context

- `LibraryManager` constructor takes `IConfigService`, `IGamesDatabaseService`, `FolderScanner`, `SteamLibraryScanner`
- `SelectScannerAndScan()` checks `LooksLikeSteamLibrary()` to decide which scanner to use
- `NormalizeLibraryRoot()` strips trailing slashes for consistent path comparison

## Requirements

- [ ] Test file created with 7+ test methods
- [ ] All tests pass: `dotnet test --filter "FullyQualifiedName~LibraryManagerTests"`
- [ ] Tests cover scanner selection, normalization, and root management

## Verification

- [ ] `dotnet build` passes (0 errors)
- [ ] `dotnet test` passes (119+ tests)

## Completion Notes

- **Completed:**
- **What was done:**
- **Verification:**
- **Issues encountered:**
