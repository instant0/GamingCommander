# Task T67: .lnk Shortcut Exe Resolution

**Tier:** 3 — Logic/Behavior
**Phase:** H — MVP
**Effort:** ~45 min
**Risk:** Medium
**Status:** Pending
**Prerequisites:** None
**WP:** WP-3 (3.3)

---

## Objective

GOG Galaxy and some standalone installers place `.lnk` shortcuts in the game root instead of (or alongside) the actual `.exe`. The C# scanner detects `.lnk` existence but never parses them, so games with shortcut-only roots get no executable. Port the `.lnk` binary parsing from `detect.py` `_parse_lnk_exe_name` and `_find_exe_via_lnk`.

**Fallback:** If byte-parsing `.lnk` files proves unreliable across Windows versions, ship without `.lnk` support and document the limitation in TECH_DEBT.md. The GOG `.info` parser (T65) covers the majority of GOG cases.

## What Needs to Change

### 1. New file: `src/GamingCommander.App/Services/LnkParser.cs`

**Current state:** Does not exist.

**Actions:**
- [ ] Create `LnkParser` static class in `GamingCommander.App.Services` namespace
- [ ] Add `/// <summary>` XML doc: "Parses Windows .lnk shortcut files to extract target executable names."
- [ ] Implement `TryGetExeName(string lnkPath, out string? exeName)`:
  - Read file as raw bytes
  - Decode as latin-1 (not UTF-8 — `.lnk` files use legacy encoding)
  - Regex extract `.exe` filenames: `r'([A-Za-z0-9_\-\.]+\.exe)'`
  - Filter known DLLs/misleading patterns (`steam_api.dll`, `unins*.exe`)
  - Pick longest candidate (most likely the real game exe)
  - Return true if a valid exe name found
- [ ] Implement `ResolveExeFromLnk(DirectoryInfo gameDir, int maxDepth = 3)`:
  - Find all `.lnk` files in `gameDir` (root only, not recursive)
  - For each, call `TryGetExeName` to extract the target exe name
  - Walk subdirectories up to `maxDepth` levels to find the actual exe file
  - Handle backup renames: `-Penumbra.exe`, `copy of Penumbra.exe`, fuzzy stem matching
  - Prefer exact match over backup/fuzzy match
  - Return the resolved exe path, or null if not found

### 2. `src/GamingCommander.App/Services/FolderScanner.cs` — `AddGameEntry()`

**Current state:** Line ~344-357 runs `ExecutableDiscovery.FindPrimaryExecutable()` which doesn't consult `.lnk` files.

**Actions:**
- [ ] After `ExecutableDiscovery.FindPrimaryExecutable()` returns null (no exe found), check for `.lnk` files:
  ```csharp
  if (exePath is null)
  {
      exePath = LnkParser.ResolveExeFromLnk(subDir);
  }
  ```
- [ ] This only triggers when the primary exe discovery fails — `.lnk` is a fallback, not a primary path
- [ ] If `.lnk` resolution succeeds, continue with normal entry creation (exe path is now set)

## Context

- **Reference:** `detect.py` lines 269-328 (`_parse_lnk_exe_name`, `_find_exe_via_lnk`)
- `.lnk` binary format:
  - Header: fixed bytes, then variable-length string data
  - Target exe name is embedded as a substring in the binary data
  - Not reliably parseable via standard .NET COM interop (requires `IShellLink` which needs Windows COM)
  - The byte-parse + regex approach is simpler and cross-platform-safe for reading
- `detect.py` filters out: `steam_api.dll`, `steam_api64.dll`, `GalaxyClient.exe`, `unins*.exe`
- Backup renames: `-Penumbra.exe` should match `PENUMBRA.EXE` (case-insensitive stem match)
- `maxDepth: 3` matches Python's behavior (searches all subdirs, 3 levels deep)

## Requirements

- [ ] `LnkParser` class created with XML docs
- [ ] `.lnk` binary parsing extracts exe name via latin-1 decode + regex
- [ ] Known DLLs/misleading patterns filtered
- [ ] Exe resolution walks subdirs up to 3 levels
- [ ] Backup renames handled (stem matching)
- [ ] Exact match preferred over fuzzy match
- [ ] `FolderScanner.AddGameEntry()` uses `.lnk` as fallback when primary exe not found
- [ ] Existing scanner tests still pass
- [ ] Graceful fallback: if `.lnk` parsing fails, no crash, just no exe

## Verification

- [ ] `dotnet build` passes (0 errors)
- [ ] `dotnet test` passes (no regressions)
- [ ] Unit test: synthetic `.lnk` binary with embedded exe name → extracted correctly
- [ ] Unit test: temp dir tree with shortcut pointing to nested exe → resolved
- [ ] Unit test: malformed `.lnk` → returns null, no crash

## Completion Notes

- **Completed:**
- **What was done:**
- **Verification:**
- **Issues encountered:**
