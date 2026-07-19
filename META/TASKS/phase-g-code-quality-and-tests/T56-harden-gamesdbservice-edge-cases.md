# Task T56: Harden GamesDatabaseService Edge Cases

**Tier:** 4 — Code Quality
**Phase:** G — Code Quality & Tests
**Effort:** ~15 min
**Risk:** Minimal
**Status:** pending

---

## Objective

`GamesDatabaseService` methods silently return when given non-existent root paths or game IDs. Add explicit tests for these "return early" paths to document the behavior.

## What Needs to Change

### `tests/GamingCommander.App.Tests/GamesDatabaseServiceTests.cs`
- [ ] Add test: `UpdateGameEntry_NonExistentRoot_NoOp` — update on non-existent root doesn't throw
- [ ] Add test: `UpdateGameEntry_NonExistentGame_NoOp` — update with unknown game ID doesn't throw
- [ ] Add test: `DeleteGameEntry_NonExentricRoot_NoOp` — delete on non-existent root doesn't throw
- [ ] Add test: `RetagGame_NonExistentRoot_NoOp` — retag on non-existent root doesn't throw
- [ ] Add test: `RetagGame_NonExistentGame_NoOp` — retag with unknown game ID doesn't throw
- [ ] Add test: `RescanRoot_NonExistentRoot_NoOp` — rescan on non-existent root doesn't throw

## Context

- All mutation methods (`UpdateGameEntry`, `DeleteGameEntry`, `RetagGame`, `RescanRoot`) call `FindIndex` and return early if < 0
- This behavior is correct but untested — a refactor could accidentally add throws or exceptions
- Tests document the contract: "no-op on invalid input, never throws"

## Requirements

- [ ] 6 new tests added
- [ ] All tests pass
- [ ] Tests verify no exception is thrown AND database state is unchanged

## Verification

- [ ] `dotnet build` passes (0 errors)
- [ ] `dotnet test --filter "FullyQualifiedName~GamesDatabaseServiceTests"` — all 22 tests pass

## Completion Notes

- **Completed:**
- **What was done:**
- **Verification:**
- **Issues encountered:**
