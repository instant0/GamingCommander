# Task T48: Add StoreSignalDetector Unit Tests

**Tier:** 3 — Logic/Behavior
**Phase:** G — Code Quality & Tests
**Effort:** ~45 min
**Risk:** Low
**Status:** pending

---

## Objective

`StoreSignalDetector` has 10 detection signal methods that are core to game detection but have zero unit tests. Add tests verifying each signal detects its expected files/directories.

## What Needs to Change

### New file: `tests/GamingCommander.App.Tests/StoreSignalDetectorTests.cs`

**Current state:** Does not exist.
**Actions:**
- [ ] Create test class `StoreSignalDetectorTests` with `[Fact]` tests
- [ ] Each test creates a temp directory with signal-specific files, calls the detector, verifies result
- [ ] Test cases:

**GOG signals:**
- [ ] `DetectType_GogIcoFile_ReturnsGog` — root has `gog.ico`
- [ ] `DetectType_GogInfoFile_ReturnsGog` — root has `goggame-*.info`

**EA signals:**
- [ ] `DetectType_EaInstaller_ReturnsEaApp` — root has `__Installer/` dir
- [ ] `DetectType_EaTouchup_ReturnsEaApp` — root has `touchup.exe`
- [ ] `DetectType_EaActivationui_ReturnsEaApp` — root has `ActivationUI.exe`

**Ubisoft signals:**
- [ ] `DetectType_UbisoftConnect_ReturnsUbisoftConnect` — root has `data.cfg` + `Ubisoft Connect` dir
- [ ] `DetectType_UbisoftEmulator_ReturnsSteamEmu` — root has `UbiStats.dll`

**Epic signals:**
- [ ] `DetectType_EpicCatalog_ReturnsEpic` — root has `.item` manifest file

**Blizzard signals:**
- [ ] `DetectType_Battlenet_ReturnsBattleNet` — root has `BlizzardAgent.exe`

**Xbox signals:**
- [ ] `DetectType_XboxGamePass_ReturnsXbox` — root has `Microsoft Gaming` dir

**Rockstar signals:**
- [ ] `DetectType_Rockstar_ReturnsRockstar` — root has `Rockstar Games` dir

**Steam signals:**
- [ ] `DetectType_SteamEmu_ReturnsSteamEmu` — root has `steam_emu.ini`
- [ ] `DetectType_SteamAppId_ReturnsSteam` — root has `steam_appid.txt`

**No signal:**
- [ ] `DetectType_UnknownFolder_ReturnsUnknown` — empty root → Unknown

## Context

- `StoreSignalDetector.DetectType(DirectoryInfo root)` returns `GameSourceKind`
- Each signal checks for specific files/directories in the game root
- Currently only tested indirectly through `FolderScanner` integration tests

## Requirements

- [ ] Test file created with 13+ test methods
- [ ] All tests pass: `dotnet test --filter "FullyQualifiedName~StoreSignalDetectorTests"`
- [ ] Tests cover all 10 signal paths + Unknown fallback
- [ ] Tests use temporary directories with real files

## Verification

- [ ] `dotnet build` passes (0 errors)
- [ ] `dotnet test` passes (112+ tests)
- [ ] `dotnet test --filter "FullyQualifiedName~StoreSignalDetectorTests"` shows all tests passing

## Completion Notes

- **Completed:**
- **What was done:**
- **Verification:**
- **Issues encountered:**
