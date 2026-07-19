# Task T49: Add GameSourceParser and JsonConfigService Tests

**Tier:** 3 — Logic/Behavior
**Phase:** G — Code Quality & Tests
**Effort:** ~30 min
**Risk:** Minimal
**Status:** pending

---

## Objective

Two Core/App classes have zero test coverage: `GameSourceParser` (string↔enum conversion) and `JsonConfigService` (settings persistence).

## What Needs to Change

### New file: `tests/GamingCommander.Core.Tests/GameSourceParserTests.cs`
- [ ] Create test class with `[Fact]` and `[Theory]` tests
- [ ] Test cases:
  - `ParseFromString_AllKnownStrings_ReturnsCorrectEnum` — "Steam"→Steam, "Standalone"→Standalone, etc.
  - `ParseFromString_UnknownString_ReturnsUnknown` — "RandomThing"→Unknown
  - `ParseFromString_CaseInsensitive_ReturnsCorrectEnum` — "steam"→Steam, "STEAM"→Steam
  - `InferFromPath_SteamPath_ReturnsSteam` — "D:\SteamLibrary\..."→Steam
  - `InferFromPath_GogPath_ReturnsGog` — "GOG Games\..."→Gog
  - `InferFromPath_UnknownPath_ReturnsUnknown` — "D:\RandomFolder"→Unknown
  - `AvailableTypes_ReturnsAllEnumValues` — returns all GameSourceKind values

### New file: `tests/GamingCommander.App.Tests/JsonConfigServiceTests.cs`
- [ ] Create test class mirroring `GamesDatabaseServiceTests` pattern (temp directory, IDisposable)
- [ ] Test cases:
  - `Load_WithNoFile_ReturnsDefaultConfig` — first run → defaults
  - `Load_WithValidFile_ReturnsPersistedConfig` — save then load → same values
  - `Save_CreatesFile_OnDisk` — save → file exists
  - `Save_WithCorruptFile_OverwritesCorrupt` — corrupt → save succeeds
  - `Load_CachesResult` — load twice → same reference
  - `UpdateConfig_PersistsChanges` — modify and save → changes reflected

## Context

- `GameSourceParser` in Core is a pure utility class, trivial to test
- `JsonConfigService` implements `IConfigService`, loads/saves `settings.json`
- Both have simple APIs with no external dependencies beyond filesystem

## Requirements

- [ ] Two new test files created
- [ ] GameSourceParserTests: 7+ test methods
- [ ] JsonConfigServiceTests: 6+ test methods
- [ ] All tests pass

## Verification

- [ ] `dotnet build` passes (0 errors)
- [ ] `dotnet test` passes (112+ tests)

## Completion Notes

- **Completed:**
- **What was done:**
- **Verification:**
- **Issues encountered:**
