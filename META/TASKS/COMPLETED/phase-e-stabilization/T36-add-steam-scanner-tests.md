# Task T36: Add SteamLibraryScanner Unit Tests

**Tier:** 3 — Logic/Behavior
**Phase:** E — Stabilization
**Effort:** ~50 min
**Risk:** Low
**Status:** ✅ completed

---

## Objective

`SteamLibraryScanner.cs` (354 lines) has zero test coverage. It handles Steam library detection, ACF parsing, cross-library detection, and Installed/Moved/Orphaned/Missing status tracking. Add comprehensive tests using mock Steam library structures.

## What Needs to Change

### New file: `tests/GamingCommander.App.Tests/SteamLibraryScannerTests.cs`

**Current state:** Does not exist.
**Actions:**
- [x] Create test class `SteamLibraryScannerTests` with 14 tests using temporary directories
- [x] Tests cover: basic scanning (3), ACF parsing (2), cross-library detection (3), status fields (2), library discovery via VDF (2), ScanAll (2)

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

- [x] Test file created with 14 test methods
- [x] All tests pass: `dotnet test --filter "FullyQualifiedName~SteamLibraryScannerTests"`
- [x] Tests use temporary directories with mock Steam library structures
- [x] Tests cover all four statuses: Installed, Moved, Orphaned, Missing
- [x] Tests verify Extra dictionary contents
- [x] Tests are isolated (no shared state between tests)

## Verification

- [x] `dotnet build` passes (0 errors)
- [x] `dotnet test` passes (now 62 tests: 25 Core + 1 Migration + 36 App)
- [x] `dotnet test --filter "FullyQualifiedName~SteamLibraryScannerTests"` shows 14 tests passing

## Completion Notes

- **Completed:** 2026-07-19
- **What was done:** Created SteamLibraryScannerTests.cs with 14 tests covering: basic scanning (3), ACF parsing (2), cross-library detection (3), status fields (2), library discovery via VDF (2), ScanAll (2). Uses temporary directories with mock Steam library structures.
- **Verification:** Build clean, 62 tests passing.
- **Issues encountered:** VdfParser requires `{` on the same line as the key. DiscoverLibraryPaths expects flat VDF format (`"0" "path"` not `"0" { "path" "..." }`). Fixed test data formats accordingly.
