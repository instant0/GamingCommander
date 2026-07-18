# Task T40: Add GamesDatabaseService Unit Tests

**Tier:** 3 — Logic/Behavior
**Phase:** E — Stabilization
**Effort:** ~40 min
**Risk:** Low
**Status:** pending
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

- [ ] Test file created with 15+ test methods
- [ ] All tests pass: `dotnet test --filter "FullyQualifiedName~GamesDatabaseServiceTests"`
- [ ] Tests use temporary directories (no dependency on real data/)
- [ ] Tests verify both success and edge cases
- [ ] Tests verify caching behavior

## Verification

- [ ] `dotnet build` passes (0 errors)
- [ ] `dotnet test` passes (now 95+ tests)
- [ ] `dotnet test --filter "FullyQualifiedName~GamesDatabaseServiceTests"` shows all tests passing

## Completion Notes

- **Completed:**
- **What was done:**
- **Verification:**
- **Issues encountered:**
