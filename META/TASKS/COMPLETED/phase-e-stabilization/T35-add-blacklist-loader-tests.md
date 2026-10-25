# Task T35: Add BlacklistLoader Unit Tests

**Tier:** 3 — Logic/Behavior
**Phase:** E — Stabilization
**Effort:** ~30 min
**Risk:** Minimal
**Status:** ✅ completed
**Prerequisites:** T29 (Blacklist tier preservation)

---

## Objective

`BlacklistLoader.cs` (183 lines) has zero test coverage. It loads noise patterns from `data/blacklist.json` and falls back to hardcoded defaults. Add tests covering loading, parsing, tier preservation, and error handling.

## What Needs to Change

### New file: `tests/GamingCommander.App.Tests/BlacklistLoaderTests.cs`

**Current state:** Does not exist.
**Actions:**
- [ ] Create test class `BlacklistLoaderTests` with `[Fact]` and `[Theory]` tests
- [ ] Add test cases:

**Loading:**
- [ ] `Load_WithValidFile_ReturnsNonEmptyPatterns` — Load from data/blacklist.json → patterns not empty
- [ ] `Load_WithMissingFile_ReturnsDefaults` — Load from non-existent path → returns hardcoded defaults
- [ ] `Load_WithEmptyFile_ReturnsDefaults` — Load from empty file → returns defaults
- [ ] `Load_WithCorruptFile_ReturnsDefaults` — Load from invalid JSON → returns defaults

**Pattern verification:**
- [ ] `Load_WithValidFile_ContainsKnownPatterns` — Verify "unins", "setup", "installer" are present
- [ ] `Load_WithValidFile_DirectoryPatternsPresent` — Verify directory patterns loaded

**Tier preservation (after T29):**
- [ ] `Load_WithValidFile_TieredPatternsPopulated` — Verify `TieredExePatterns` is not empty
- [ ] `Load_WithValidFile_TierRangeIsValid` — Verify all tiers are between 1 and 21
- [ ] `Load_WithValidFile_PatternsMatchTiers` — Verify each pattern has a valid tier

**Error handling:**
- [ ] `Load_WithMissingDirectory_ReturnsDefaults` — Load from non-existent directory → no exception, returns defaults
- [ ] `Load_WithReadOnlyFile_ReturnsDefaults` — Load from read-only file → no exception

## Context

- `BlacklistLoader` is used by `LibraryManager` and `MainWindow` to create `FolderScanner` instances
- The JSON file is `data/blacklist.json` at the app's base directory
- Hardcoded defaults are in `FolderScanner.DefaultNoiseExePatterns`
- Tests should use a temporary directory with mock JSON files to avoid depending on real data

## Requirements

- [x] Test file created with 11 test methods
- [x] All tests pass: `dotnet test --filter "FullyQualifiedName~BlacklistLoaderTests"`
- [x] Tests use temporary directories with mock JSON files
- [x] Tests verify both success and error paths
- [x] Tests verify tier preservation

## Verification

- [x] `dotnet build` passes (0 errors)
- [x] `dotnet test` passes (now 48 tests: 25 Core + 1 Migration + 22 App)
- [x] `dotnet test --filter "FullyQualifiedName~BlacklistLoaderTests"` shows 11 tests passing

## Completion Notes

- **Completed:** 2026-07-19
- **What was done:** Created BlacklistLoaderTests.cs with 11 tests covering: loading (4), pattern verification (2), tier preservation (4), error handling (1). Uses temporary directories with mock JSON files.
- **Verification:** Build clean, 48 tests passing.
- **Issues encountered:** None
