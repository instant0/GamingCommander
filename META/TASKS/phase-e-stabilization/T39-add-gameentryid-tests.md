# Task T39: Add GameEntryId Unit Tests

**Tier:** 3 — Logic/Behavior
**Phase:** E — Stabilization
**Effort:** ~20 min
**Risk:** Minimal
**Status:** pending

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

- [ ] Test file created with 8 test methods
- [ ] All tests pass: `dotnet test --filter "FullyQualifiedName~GameEntryIdTests"`
- [ ] Tests verify determinism, uniqueness, format, and edge cases

## Verification

- [ ] `dotnet build` passes (0 errors)
- [ ] `dotnet test` passes (now 80+ tests)
- [ ] `dotnet test --filter "FullyQualifiedName~GameEntryIdTests"` shows all tests passing

## Completion Notes

- **Completed:**
- **What was done:**
- **Verification:**
- **Issues encountered:**
