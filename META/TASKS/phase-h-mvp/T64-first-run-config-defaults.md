# Task T64: First-Run Config Defaults

**Tier:** 3 — Logic/Behavior
**Phase:** H — MVP
**Effort:** ~20 min
**Risk:** Low
**Status:** Pending
**Prerequisites:** None
**WP:** WP-2

---

## Objective

On first run, `JsonConfigService.Load()` returns `IsFirstRun = false` even when `settings.json` doesn't exist (because `bool` defaults to `false`). The wizard trigger relies on `LastSeenVersion` null-check and `LibraryRoots.Count == 0` instead, which works but is fragile. Fix `IsFirstRun` to be `true` when no config file exists, and add 2 unit tests to verify first-run behavior.

## What Needs to Change

### 1. `src/GamingCommander.App/Services/JsonConfigService.cs` — `Load()` method

**Current state:** `JsonFileHelper.ReadFromFile<ConfigDto>(_configPath, () => new ConfigDto())` returns a default `ConfigDto` with `IsFirstRun = false` when the file doesn't exist. The `loaded?.IsFirstRun ?? false` expression in the `AppConfig` constructor always evaluates to `false` for missing files.

**Actions:**
- [ ] Detect whether the file existed by checking `File.Exists(_configPath)` before the read, or modify `JsonFileHelper.ReadFromFile` to indicate whether the file was found
- [ ] When file doesn't exist, set `IsFirstRun = true` in the returned `AppConfig`
- [ ] Recommended approach: check file existence before read:
  ```csharp
  bool fileExists = File.Exists(_configPath);
  ConfigDto? loaded = JsonFileHelper.ReadFromFile<ConfigDto>(_configPath, () => new ConfigDto());
  // ...
  return new AppConfig(
      // ...
      IsFirstRun: !fileExists,
      // ...
  );
  ```
- [ ] Ensure `Save()` still works correctly (file is created on first save via `JsonFileHelper.WriteToFile`)

### 2. `tests/GamingCommander.App.Tests/JsonConfigServiceTests.cs` (or similar)

**Current state:** No tests for `JsonConfigService`.

**Actions:**
- [ ] Create `JsonConfigServiceTests.cs` (or add to existing test file if appropriate)
- [ ] Test 1 — **Missing file returns IsFirstRun = true:**
  - Create a `JsonConfigService` pointing to a non-existent temp path
  - Call `Load()`
  - Assert `IsFirstRun == true`, `LibraryRoots` is empty
- [ ] Test 2 — **Save then Load returns IsFirstRun = false:**
  - Load from missing path, save with some roots, reload
  - Assert `IsFirstRun == false`, roots are preserved
- [ ] Test 3 — **Missing games.json returns empty database:**
  - Create `GamesDatabaseService` pointing to a non-existent temp path
  - Call `Load()`
  - Assert `Roots` is empty, no exception

## Context

- `JsonFileHelper.ReadFromFile<T>()` already handles missing files gracefully (returns `defaultFactory()` result)
- The issue is specifically that `IsFirstRun` defaults to `false` for `bool` fields
- The wizard is already triggered by `LibraryRoots.Count == 0`, so this bug doesn't block functionality — but it's incorrect and could cause issues if other code checks `IsFirstRun`
- Auto-creation of files on first `Save()` is already working via `JsonFileHelper.WriteToFile` (creates parent dir)
- The blacklist (`data/blacklist.json`) is shipped with the app and doesn't need first-run creation

## Requirements

- [ ] `JsonConfigService.Load()` returns `IsFirstRun = true` when `settings.json` doesn't exist
- [ ] `JsonConfigService.Load()` returns `IsFirstRun = false` after a successful save+reload
- [ ] `GamesDatabaseService.Load()` returns empty `Roots` when `games.json` doesn't exist
- [ ] No exception thrown on first run (missing files)
- [ ] Files are created on first `Save()` (already working)

## Verification

- [ ] `dotnet build` passes (0 errors)
- [ ] `dotnet test` passes with new tests (total count increases by 3)
- [ ] Manual: delete `settings.json`, run app, wizard appears (or at minimum, `IsFirstRun` is true)

## Completion Notes

- **Completed:**
- **What was done:**
- **Verification:**
- **Issues encountered:**
