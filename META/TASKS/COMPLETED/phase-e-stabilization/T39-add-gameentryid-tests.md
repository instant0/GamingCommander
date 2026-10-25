# Task T39: Add GameEntryId Unit Tests

**Tier:** 3 — Logic/Behavior
**Phase:** E — Stabilization
**Effort:** ~20 min
**Risk:** Minimal
**Status:** ✅ completed

---

## Objective

`GameEntryId.cs` (a small helper) has zero test coverage. It generates deterministic MD5-based IDs for game entries. Add tests verifying determinism, uniqueness, and format.

## What Needs to Change

### New file: `tests/GamingCommander.Core.Tests/GameEntryIdTests.cs`

**Current state:** Does not exist.
**Actions:**
- [ ] Create test class `GameEntryIdTests` with `[Fact]` tests
- [ ] Add test cases:

**Determinism:**
- [ ] `Compute_SameInputs_ReturnsSameId` — Same folder + root → same ID
- [ ] `Compute_CalledTwice_ReturnsSameId` — Verify determinism across calls

**Uniqueness:**
- [ ] `Compute_DifferentFolders_ReturnsDifferentIds` — Different folder names → different IDs
- [ ] `Compute_DifferentRoots_ReturnsDifferentIds` — Same folder, different roots → different IDs

**Format:**
- [ ] `Compute_Returns16CharHexString` — ID is 16 characters, all hex
- [ ] `Compute_ReturnsLowercaseHex` — ID is lowercase hex

**Edge cases:**
- [ ] `Compute_EmptyFolderName_HandledGracefully` — Empty string → no exception, valid ID
- [ ] `Compute_SpecialCharacters_HandledGracefully` — Path with spaces/special chars → valid ID

## Context

- `GameEntryId.Compute(folderName, rootPath)` returns a deterministic 16-char hex string
- Used by `FolderScanner` and `SteamLibraryScanner` to generate unique game entry IDs
- Based on MD5 hash of the concatenated folder name and root path

## Requirements

- [x] Test file created with 8 test methods
- [x] All tests pass: `dotnet test --filter "FullyQualifiedName~GameEntryIdTests"`
- [x] Tests verify determinism, uniqueness, format, and edge cases

## Verification

- [x] `dotnet build` passes (0 errors)
- [x] `dotnet test` passes (now 83 tests: 33 Core + 1 Migration + 49 App)
- [x] `dotnet test --filter "FullyQualifiedName~GameEntryIdTests"` shows 8 tests passing

## Completion Notes

- **Completed:** 2026-07-19
- **What was done:** Created GameEntryIdTests.cs with 8 tests covering determinism (same inputs → same ID), uniqueness (different folders/roots → different IDs), format (16-char lowercase hex), and edge cases (empty folder name, special characters).
- **Verification:** Build clean, 83 tests passing.
- **Issues encountered:** None.
