# Task T52: Add Debug Logging to Empty Catch Blocks

**Tier:** 4 — Code Quality
**Phase:** G — Code Quality & Tests
**Effort:** ~20 min
**Risk:** Minimal
**Status:** pending

---

## Objective

13 empty `catch { }` blocks silently swallow exceptions across 4 files. Add `System.Diagnostics.Debug.WriteLine()` calls so failures are visible in debug output without affecting production behavior.

## What Needs to Change

### `src/GamingCommander.App/Services/FolderScanner.cs` (5 empty catches)
- [ ] Line ~186: Add `Debug.WriteLine($"[FolderScanner] Store signal scan failed: {ex.Message}");`
- [ ] Line ~205: Add `Debug.WriteLine($"[FolderScanner] Unreal layout scan failed: {ex.Message}");`
- [ ] Line ~234: Add `Debug.WriteLine($"[FolderScanner] Executable discovery failed: {ex.Message}");`
- [ ] Line ~249: Add `Debug.WriteLine($"[FolderScanner] .lnk scan failed: {ex.Message}");`
- [ ] Line ~259: Add `Debug.WriteLine($"[FolderScanner] Epic manifest scan failed: {ex.Message}");`

### `src/GamingCommander.App/Services/ExecutableDiscovery.cs` (3 empty catches)
- [ ] Line ~71: Add `Debug.WriteLine($"[ExecutableDiscovery] Directory enumeration failed: {ex.Message}");`
- [ ] Line ~150: Add `Debug.WriteLine($"[ExecutableDiscovery] File size check failed: {ex.Message}");`
- [ ] Line ~265: Add `Debug.WriteLine($"[ExecutableDiscovery] Manifest listing failed: {ex.Message}");`

### `src/GamingCommander.App/Services/StoreSignalDetector.cs` (3 empty catches)
- [ ] Line ~95: Add `Debug.WriteLine($"[StoreSignalDetector] INI read failed: {ex.Message}");`
- [ ] Line ~99: Add `Debug.WriteLine($"[StoreSignalDetector] GOG signal scan failed: {ex.Message}");`
- [ ] Line ~118: Add `Debug.WriteLine($"[StoreSignalDetector] Ubisoft signal scan failed: {ex.Message}");`

### `src/GamingCommander.App/Services/SteamLibraryScanner.cs` (2 empty catches)
- [ ] Line ~203: Add `Debug.WriteLine($"[SteamLibraryScanner] ACF parse failed: {ex.Message}");`
- [ ] Line ~339: Add `Debug.WriteLine($"[SteamLibraryScanner] Executable search failed: {ex.Message}");`

## Context

- All 13 catches are in filesystem-scanning/probing code where best-effort behavior is intentional
- `Debug.WriteLine` only outputs in Debug builds, so production is unaffected
- Enables debugging when scans silently fail on permission issues or corrupted files

## Requirements

- [ ] All 13 empty catches have `Debug.WriteLine` calls
- [ ] Exception variable named `ex` in each catch
- [ ] Log messages include class name and descriptive context

## Verification

- [ ] `dotnet build` passes (0 errors)
- [ ] `dotnet test` passes
- [ ] `grep -c "catch { }" src/` — returns 0 (all catches now have bodies)

## Completion Notes

- **Completed:**
- **What was done:**
- **Verification:**
- **Issues encountered:**
