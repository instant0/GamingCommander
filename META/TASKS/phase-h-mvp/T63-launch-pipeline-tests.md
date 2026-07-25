# Task T63: Launch Pipeline Unit Tests

**Tier:** 3 — Tests
**Phase:** H — MVP
**Effort:** ~30 min
**Risk:** Low
**Status:** Pending
**Prerequisites:** T61, T62
**WP:** WP-1

---

## Objective

The launch pipeline fix (T61+T62) has no test coverage. Add targeted unit tests to verify: (a) Steam URI is resolved as `LaunchTarget` instead of the exe path, (b) non-URI arguments are carried through the VM, (c) empty targets don't throw. These tests run against `ShellViewModel` and the launch resolution logic without requiring a running UI.

## What Needs to Change

### 1. New file: `tests/GamingCommander.App.Tests/LaunchResolutionTests.cs`

**Current state:** Does not exist.

**Actions:**
- [ ] Create test class `LaunchResolutionTests` in `GamingCommander.App.Tests`
- [ ] Test 1 — **Steam URI preferred over exe path:**
  - Create a `GameEntry` with `ExecutablePath = "D:\\Steam\\common\\Game\\game.exe"`, `CommandLineArguments = "steam://rungameid/12345"`, `GameSource = GameSourceKind.Steam`
  - Verify the mapping logic produces `LaunchTarget = "steam://rungameid/12345"` (not the exe path)
  - Verify `CommandLineArguments = "steam://rungameid/12345"`
- [ ] Test 2 — **Standalone args carried through:**
  - Create a `GameEntry` with `ExecutablePath = "D:\\GOG\\SCUMMVM\\scummvm.exe"`, `CommandLineArguments = "-p \"C:\\Games\\Monkey Island\""`
  - Verify `LaunchTarget = "D:\\GOG\\SCUMMVM\\scummvm.exe"` (the exe path)
  - Verify `CommandLineArguments = "-p \"C:\\Games\\Monkey Island\""`
- [ ] Test 3 — **Empty target (no exe, no URI):**
  - Create a `GameEntry` with `ExecutablePath = ""`, `CommandLineArguments = ""`
  - Verify `LaunchTarget` is empty (no crash, no exception)
- [ ] Test 4 — **Steam game without AppId (exe-only fallback):**
  - Create a `GameEntry` with `ExecutablePath = "D:\\Steam\\common\\Game\\game.exe"`, `CommandLineArguments = ""`, `GameSource = GameSourceKind.Steam`
  - Verify `LaunchTarget = "D:\\Steam\\common\\Game\\game.exe"` (exe path, not URI)
- [ ] Test 5 — **Non-steam URI (e.g. epic://) preferred over exe:**
  - Create a `GameEntry` with `ExecutablePath = "D:\\Epic\\Game\\game.exe"`, `CommandLineArguments = "epic://launch/123"`
  - Verify `LaunchTarget = "epic://launch/123"` (URI preferred)
- [ ] Add test infrastructure: mock `ILibraryManager` or use in-memory `GamesDatabaseService` with pre-populated entries

## Context

- The existing test infrastructure uses in-memory databases (`GamesDatabaseServiceTests` pattern)
- `MockDataIntegrationTests` already demonstrates mock filesystem scanning — follow that pattern
- Tests should verify the ViewModel mapping, not the actual `Process.Start` call
- The `SteamLibraryScanner` and `FolderScanner` already have good test coverage — no need to re-test scanning
- The actual `Process.Start` call cannot be unit-tested (requires Windows + real processes), but the mapping logic can

## Requirements

- [ ] 5 unit tests covering all resolution branches
- [ ] Tests run without UI (no Avalonia dependency)
- [ ] Tests use in-memory or mock data (no filesystem)
- [ ] All tests pass: `dotnet test`
- [ ] `dotnet build` passes (0 errors)

## Verification

- [ ] `dotnet build` passes (0 errors)
- [ ] `dotnet test` passes with new tests (total count increases by 5)
- [ ] `grep -rn "LaunchResolutionTests" tests/` confirms test file exists
- [ ] All 5 tests green

## Completion Notes

- **Completed:**
- **What was done:**
- **Verification:**
- **Issues encountered:**
