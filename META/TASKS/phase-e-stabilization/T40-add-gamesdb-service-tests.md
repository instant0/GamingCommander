# Task T40: Add GamesDatabaseService Unit Tests

**Tier:** 3 — Logic/Behavior
**Phase:** E — Stabilization
**Effort:** ~40 min
**Risk:** Low
**Status:** ✅ completed
**Prerequisites:** T17 (JsonFileHelper extracted)

---

## Objective

`GamesDatabaseService.cs` (235 lines) has zero test coverage. It handles JSON persistence, in-memory caching, and CRUD operations for game entries. Add tests covering all operations.

## What Needs to Change

### New file: `tests/GamingCommander.App.Tests/GamesDatabaseServiceTests.cs`

**Current state:** Does not exist.
**Actions:**
- [ ] Create test class `GamesDatabaseServiceTests` with `[Fact]` and `[Theory]` tests
- [ ] Create helper method `CreateTestService()` that uses a temporary directory
- [ ] Add test cases:

**Load/Save:**
- [ ] `Load_WithNoFile_ReturnsEmptyDatabase` — First run → empty Roots list
- [ ] `Load_WithValidFile_ReturnsPersistedData` — Save then load → same data
- [ ] `Save_CreatesFile_OnDisk` — Save → file exists at expected path
- [ ] `Save_WithCorruptFile_OverwritesCorrupt` — Corrupt file → save succeeds, overwrites

**CRUD operations:**
- [ ] `AddRoot_AddsToDatabase` — Add root with games → appears in Load() result
- [ ] `RemoveRoot_RemovesFromDatabase` — Add then remove root → gone from Load()
- [ ] `GetGamesForRoot_ReturnsCorrectEntries` — Add 3 games to root → returns 3
- [ ] `GetGamesForRoot_UnknownRoot_ReturnsEmpty` — Non-existent root → empty list
- [ ] `UpdateGameEntry_UpdatesFields` — Add game, update name → name changed
- [ ] `DeleteGameEntry_RemovesEntry` — Add 2 games, delete 1 → only 1 remains
- [ ] `RetagGame_ChangesSourceKind` — Add game, retag → source changed

**Caching:**
- [ ] `Load_CachesResult` — Load twice → second load returns same reference
- [ ] `Save_UpdatesCache` — Save → subsequent Load reflects changes without re-reading file
- [ ] `Refresh_ClearsCache` — Load, Refresh, Load → fresh data from disk

**Edge cases:**
- [ ] `RescanRoot_ReplacesAllGames` — Add 5 games, rescan with 2 → only 2 remain
- [ ] `MultipleRoots_IndependentCRUD` — Add games to 2 roots → operations don't cross roots

## Context

- `GamesDatabaseService` implements `IGamesDatabaseService` interface
- Uses JSON file at configurable path (tests use temp directory)
- In-memory cache prevents repeated disk reads
- DTO mapping between domain records and JSON format

## Requirements

- [x] Test file created with 16 test methods
- [x] All tests pass: `dotnet test --filter "FullyQualifiedName~GamesDatabaseServiceTests"`
- [x] Tests use temporary directories (no dependency on real data/)
- [x] Tests verify both success and edge cases
- [x] Tests verify caching behavior

## Verification

- [x] `dotnet build` passes (0 errors)
- [x] `dotnet test` passes (now 99 tests: 33 Core + 1 Migration + 65 App)
- [x] `dotnet test --filter "FullyQualifiedName~GamesDatabaseServiceTests"` shows 16 tests passing

## Completion Notes

- **Completed:** 2026-07-19
- **What was done:** Created GamesDatabaseServiceTests.cs with 16 tests covering Load/Save (empty, valid, corrupt), CRUD (add, remove, update, delete, retag), caching (same reference, updated cache), and edge cases (rescan replaces, multi-root isolation).
- **Verification:** Build clean, 99 tests passing.
- **Issues encountered:** Save_UpdatesCache initial assertion was wrong — AddRoot creates a new GamesDatabase instance so `Assert.Same` failed. Fixed to `Assert.NotSame` with correct behavior validation.
