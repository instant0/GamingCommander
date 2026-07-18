# Task T37: Add Noise-Check Regression Test

**Tier:** 3 — Logic/Behavior
**Phase:** E — Stabilization
**Effort:** ~20 min
**Risk:** Minimal
**Status:** pending
**Prerequisites:** T21 (Noise check consolidation)

---

## Objective

Bug 5 (static vs instance noise check divergence) was fixed in code but has no regression test. Add a test that proves the instance method sees JSON-blacklisted patterns that the old static method missed.

## What Needs to Change

### `tests/GamingCommander.App.Tests/ScannerFilterTests.cs`

**Current state:** 6 tests for scanner filtering, no noise-check regression test
**Actions:**
- [ ] Add new test method:
  ```csharp
  [Fact]
  public void IsNoiseExeName_SeesJsonBlacklistedPatterns()
  {
      // Arrange: Create scanner with JSON blacklist patterns that were
      // previously only visible to the instance method
      var noisePatterns = new List<string>
      {
          "blender", "python", "scummvm", "server", "editor"
      };
      var scanner = new FolderScanner([], noisePatterns, [], []);

      // Act & Assert: Instance method should detect all patterns
      Assert.True(scanner.IsNoiseExeName("blender.exe"));
      Assert.True(scanner.IsNoiseExeName("python3.exe"));
      Assert.True(scanner.IsNoiseExeName("scummvm.exe"));
      Assert.True(scanner.IsNoiseExeName("server.exe"));
      Assert.True(scanner.IsNoiseExeName("editor.exe"));
  }
  ```
- [ ] Add test for `.lnk` shortcut handling:
  ```csharp
  [Fact]
  public void IsNoiseExeName_HandlesLnkShortcuts()
  {
      var noisePatterns = new List<string> { "setup", "installer" };
      var scanner = new FolderScanner([], noisePatterns, [], []);

      // .lnk files should be checked by their target name
      Assert.True(scanner.IsNoiseExeName("setup.lnk"));
      Assert.True(scanner.IsNoiseExeName("installer.lnk"));
  }
  ```
- [ ] Add test for negative case (non-noise should return false):
  ```csharp
  [Fact]
  public void IsNoiseExeName_NonNoiseExe_ReturnsFalse()
  {
      var noisePatterns = new List<string> { "setup", "installer" };
      var scanner = new FolderScanner([], noisePatterns, [], []);

      Assert.False(scanner.IsNoiseExeName("Game.exe"));
      Assert.False(scanner.IsNoiseExeName("MyGame.exe"));
  }
  ```

## Context

- Bug 5 was fixed by making `IsNoiseExePattern` instance and adding `IsNoiseExeName`
- The old static version only saw 25 hardcoded patterns
- The new instance version sees the full JSON blacklist (130+ patterns)
- This test prevents regression — if someone reverts to static, these tests fail

## Requirements

- [ ] At least 3 new test methods added to `ScannerFilterTests.cs`
- [ ] Tests verify instance method sees JSON-blacklisted patterns
- [ ] Tests verify `.lnk` shortcut handling
- [ ] Tests verify negative cases (non-noise exes)
- [ ] All tests pass

## Verification

- [ ] `dotnet build` passes (0 errors)
- [ ] `dotnet test` passes (now 60+ tests)
- [ ] `dotnet test --filter "FullyQualifiedName~ScannerFilterTests"` shows all tests passing

## Completion Notes

- **Completed:**
- **What was done:**
- **Verification:**
- **Issues encountered:**
