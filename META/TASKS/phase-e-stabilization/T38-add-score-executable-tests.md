# Task T38: Add ScoreExecutable Unit Tests

**Tier:** 3 — Logic/Behavior
**Phase:** E — Stabilization
**Effort:** ~30 min
**Risk:** Minimal
**Status:** pending
**Prerequisites:** T30 (ScoreExecutable fixed)

---

## Objective

`ScoreExecutable()` has zero test coverage. It scores executables based on folder-name matching, launcher penalties, and shipping bonuses. Add tests covering all scoring branches.

## What Needs to Change

### New file: `tests/GamingCommander.App.Tests/ExecutableScoringTests.cs`

**Current state:** Does not exist.
**Actions:**
- [ ] Create test class `ExecutableScoringTests` with `[Fact]` and `[Theory]` tests
- [ ] Add test cases:

**Folder-name matching:**
- [ ] `ScoreExecutable_ExactNameMatch_AddsBonus` — "MyGame.exe" in "MyGame/" → high score
- [ ] `ScoreExecutable_SubstringMatch_AddsBonus` — "Game.exe" in "MyGame/" → moderate score
- [ ] `ScoreExecutable_TokenMatch_AddsBonus` — "MyGameLauncher.exe" in "My Game/" → moderate score

**Launcher penalties:**
- [ ] `ScoreExecutable_LauncherPattern_Penalizes` — "launcher.exe" → negative score
- [ ] `ScoreExecutable_InstallerPattern_Penalizes` — "setup.exe" → negative score (after T30)
- [ ] `ScoreExecutable_HighTierPattern_HeavilyPenalizes` — "unins000.exe" → large negative score

**Shipping bonus:**
- [ ] `ScoreExecutable_ShippingBinary_AddsBonus` — "MyGame-Win64-Shipping.exe" → bonus

**File size:**
- [ ] `ScoreExecutable_LargeExe_AddsBonus` — exe > 10MB → bonus
- [ ] `ScoreExecutable_SmallExe_Penalizes` — exe < 100KB → penalty

**Edge cases:**
- [ ] `ScoreExecutable_EmptyFolderName_ReturnsBaseScore` — No folder context → base score
- [ ] `ScoreExecutable_UnknownExe_ReturnsZeroScore` — No matching patterns → zero

**After T30 (noise patterns):**
- [ ] `ScoreExecutable_NoisePattern_TierBasedPenalty` — "patch.exe" → tier-based penalty
- [ ] `ScoreExecutable_StoreBootstrap_LightPenalty` — "epicgameslauncher.exe" → light penalty

## Context

- `ScoreExecutable` is called by `FindPrimaryExecutable` to rank exe candidates
- Scoring factors: folder-name match (+10), launcher penalty (-20 to -30), shipping bonus (+5), file size bonus
- After T30, noise patterns add tier-based penalties (-5 to -30)
- Tests should mock file size or use real temporary files

## Requirements

- [ ] Test file created with 12+ test methods
- [ ] All tests pass: `dotnet test --filter "FullyQualifiedName~ExecutableScoringTests"`
- [ ] Tests cover all scoring branches
- [ ] Tests use temporary directories with mock exe files (or mock file size)

## Verification

- [ ] `dotnet build` passes (0 errors)
- [ ] `dotnet test` passes (now 72+ tests)
- [ ] `dotnet test --filter "FullyQualifiedName~ExecutableScoringTests"` shows all tests passing

## Completion Notes

- **Completed:**
- **What was done:**
- **Verification:**
- **Issues encountered:**
