# Task T36: Add SteamLibraryScanner Unit Tests

**Tier:** 3 — Logic/Behavior
**Phase:** E — Stabilization
**Effort:** ~50 min
**Risk:** Low
**Status:** pending

---

## Objective

`SteamLibraryScanner.cs` (472 lines) has zero test coverage. It handles Steam library detection, ACF parsing, cross-library detection, and Installed/Moved/Orphaned/Missing status tracking. Add comprehensive tests using mock Steam library structures.

## What Needs to Change

### New file: `tests/GamingCommander.App.Tests/SteamLibraryScannerTests.cs`

**Current state:** Does not exist.
**Actions:**
- [ ] Create test class `SteamLibraryScannerTests` with `[Fact]` and `[Theory]` tests
- [ ] Create helper method `CreateMockSteamLibrary(string basePath, ...)` to build mock directory structures
- [ ] Add test cases:

**Basic scanning:**
- [ ] `Scan_WithValidLibrary_ReturnsInstalledGames` — Mock steamapps/common with ACF → Installed status
- [ ] `Scan_WithNoCommon_Folder_ReturnsEmpty` — No common/ directory → empty list
- [ ] `Scan_WithNonExistentPath_ReturnsEmpty` — Non-existent path → empty list

**ACF parsing:**
- [ ] `Scan_WithValidAcf_ParsesAllFields` — Verify appid, name, installdir, StateFlags extracted
- [ ] `Scan_WithMalformedAcf_SkipsEntry` — Corrupt ACF → no exception, entry skipped
- [ ] `Scan_WithMissingRequiredField_SkipsEntry` — ACF missing "name" → entry skipped

**Cross-library detection:**
- [ ] `Scan_WithMovedGame_DetectsMovedStatus` — Game folder in library A, ACF in library B → "Moved"
- [ ] `Scan_WithOrphanedGame_DetectsOrphanedStatus` — Game folder with no ACF → "Orphaned"
- [ ] `Scan_WithMissingGame_DetectsMissingStatus` — ACF with no matching common/ folder → "Missing"

**Status field:**
- [ ] `Scan_InstalledGame_HasSteamStatusExtra` — Verify Extra["SteamStatus"] = "Installed"
- [ ] `Scan_MovedGame_HasAcfExpectedPath` — Verify Extra["AcfExpectedPath"] is set
- [ ] `Scan_MissingGame_HasSteamStatusExtra` — Verify Extra["SteamStatus"] = "Missing"

**Library discovery:**
- [ ] `DiscoverLibraryPaths_WithValidVdf_ReturnsPaths` — libraryfolders.vdf with paths → parsed correctly
- [ ] `DiscoverLibraryPaths_WithMissingVdf_ReturnsEmpty` — No vdf file → empty list

**Edge cases:**
- [ ] `ScanAll_WithMultipleLibraries_CrossReferencesAllAcf` — 2 libraries, ACFs split → correct status
- [ ] `Scan_WithDuplicatePaths_Deduplicates` — Same path twice → only scanned once

## Context

- `SteamLibraryScanner` is used by `LibraryManager` for Steam root types
- Mock data needs: directory structure (steamapps/common/, steamapps/*.acf), VDF files, multiple library paths
- Use `System.IO.Abstractions` or temporary directories for mock filesystem
- The scanner uses `VdfParser` from Core — integration tested implicitly

## Requirements

- [ ] Test file created with 15+ test methods
- [ ] All tests pass: `dotnet test --filter "FullyQualifiedName~SteamLibraryScannerTests"`
- [ ] Tests use temporary directories with mock Steam library structures
- [ ] Tests cover all four statuses: Installed, Moved, Orphaned, Missing
- [ ] Tests verify Extra dictionary contents
- [ ] Tests are isolated (no shared state between tests)

## Verification

- [ ] `dotnet build` passes (0 errors)
- [ ] `dotnet test` passes (now 57+ tests)
- [ ] `dotnet test --filter "FullyQualifiedName~SteamLibraryScannerTests"` shows all tests passing

## Completion Notes

- **Completed:**
- **What was done:**
- **Verification:**
- **Issues encountered:**
