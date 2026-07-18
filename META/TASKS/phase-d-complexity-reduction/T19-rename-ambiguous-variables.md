# Task T19: Rename Ambiguous Variables Across Codebase

**Tier:** 1 — Documentation
**Phase:** D — Complexity Reduction
**Effort:** ~30 min
**Risk:** Low
**Status:** pending
**Prerequisites:** None

---

## Objective

Several variables use single-letter names, abbreviations, or non-descriptive identifiers that make code harder to read. Rename them to full, descriptive names. This is a pure rename — no logic changes.

## What Needs to Change

### 1. `src/GamingCommander.App/Services/FolderScanner.cs`

**Actions:**
- [ ] Line 604: Rename `p` to `pattern` in `foreach (string p in DefaultNoiseExePatterns)`
- [ ] Line 327: Rename `swPath` to `steamworksPath`
- [ ] Line 330: Rename `svDir` to `steamworksVersionDir`

### 2. `src/GamingCommander.App/Services/SteamLibraryScanner.cs`

**Actions:**
- [ ] Line 170: Rename `p` to `steamPath` in `foreach (string p in _configuredSteamPaths)`

### 3. `src/GamingCommander.App/App.axaml.cs`

**Actions:**
- [ ] Line 206: Rename `a` to `leftVersion` in `CompareVersions(string a, string b)`
- [ ] Line 206: Rename `b` to `rightVersion` in `CompareVersions(string a, string b)`

### 4. `src/GamingCommander.UI/ViewModels/ShellViewModel.cs`

**Actions:**
- [ ] Line 265: Rename `sid` to `steamAppId`
- [ ] Line 266: Rename `eid` to `epicCatalogItemId`

### 5. `src/GamingCommander.App/Services/JsonConfigService.cs`

**Actions:**
- [ ] Line 55: Rename `ov` to `folderOverride` in `foreach (ConfigFolderOverrideDto ov in ...)`

### 6. `src/GamingCommander.App/ViewModels/LibrarySetupViewModel.cs`

**Actions:**
- [ ] Line 68: Rename `rawPath` — already descriptive, no change needed

## Context

- All renames are local variable or parameter renames — no API changes
- Single-letter names like `p` in foreach loops are common but reduce readability
- Abbreviations like `swPath`, `svDir`, `sid`, `eid` require mental expansion
- LINQ lambda parameters (`r =>`, `g =>`) are acceptable in context and left unchanged
- Event handler parameters (`(_, e) =>`) are standard C# convention and left unchanged

## Requirements

- [ ] All single-letter foreach/lambda variables renamed to descriptive names
- [ ] All abbreviated variables renamed to full words
- [ ] No logic changes — only identifier names
- [ ] No public API changes (all renames are private/local)

## Verification

- [ ] `dotnet build` passes (0 errors)
- [ ] `dotnet test` passes (17 tests)
- [ ] `grep -rn "\bfor.* string p " src/` returns 0 hits (no single-letter foreach variables remain)
- [ ] `grep -rn "swPath\|svDir\|\.sid\b\|\.eid\b" src/` returns 0 hits (no abbreviated variables remain)

## Completion Notes

- **Completed:**
- **What was done:**
- **Verification:**
- **Issues encountered:**
