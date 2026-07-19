# Task T57: Delete Deprecated Detection Project

**Tier:** 5 — Cleanup
**Phase:** G — Code Quality & Tests
**Effort:** ~15 min
**Risk:** Low
**Status:** pending

---

## Objective

`GamingCommander.Detection` project is entirely deprecated — both source files are 3-line comment stubs. The test project `GamingCommander.Detection.Tests` produces zero tests. Remove both.

## What Needs to Change

### Remove projects:
- [ ] Delete `src/GamingCommander.Detection/` directory entirely
- [ ] Delete `tests/GamingCommander.Detection.Tests/` directory entirely

### Update solution:
- [ ] Remove both projects from `GamingCommander.sln`

### Update references:
- [ ] Remove `<ProjectReference Include="..\GamingCommander.Detection\..." />` from `GamingCommander.App.csproj`
- [ ] Remove `<ProjectReference Include="..\GamingCommander.Detection\..." />` from `GamingCommander.UI.csproj` (if not already done in T47)
- [ ] Remove `<ProjectReference Include="..\GamingCommander.Detection.Tests\..." />` from solution (if exists)

### Verify:
- [ ] Build still passes without the Detection projects
- [ ] Test runner doesn't attempt to load Detection.Tests

## Context

- `DesignTimeGameDiscoveryService.cs` and `IGameDiscoveryService.cs` are both commented-out stubs
- Detection.Tests has a single `.cs` file that is also a comment stub
- Both files say "DEPRECATED — will be removed in a future cleanup pass"
- Removing eliminates 2 project references and 2 build targets

## Requirements

- [ ] Detection project and test project deleted
- [ ] Solution file updated
- [ ] All references removed
- [ ] Build and tests still pass

## Verification

- [ ] `dotnet build` passes (0 errors)
- [ ] `dotnet test` passes (99 tests, no Detection.Tests in output)
- [ ] `ls src/GamingCommander.Detection/` — directory does not exist

## Completion Notes

- **Completed:**
- **What was done:**
- **Verification:**
- **Issues encountered:**
