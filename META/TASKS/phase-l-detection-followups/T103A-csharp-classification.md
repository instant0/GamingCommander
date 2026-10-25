# Task T103A: C# — Steam Library Membership Classification

**Tier:** 3 — Logic/Behavior
**Phase:** L — Detection Follow-ups
**Risk:** Medium
**Status:** completed
**Prerequisites:** Task T103A depends on Task T103

## Objective

Make Steam-library location authoritative in the C# classification path, with tests that prove it.
No Python or docs changes in this segment.

## What Needs to Change

### 1. New helper `src/GamingCommander.App/Services/SteamLibraryMembership.cs`
- [x] `internal static bool IsUnderSteamLibrary(string path)`.
- [x] Structural rule: normalize `\` to `/` (do **not** call `Path.GetFullPath` on Windows-style strings), trim trailing separators, and return true when the path is under the segment pair `steamapps/common` (case-insensitive).
- [x] Returns true for the `common` folder itself and for any descendant.
- [x] Returns false for `steamapps/commonfiles/…` (segment match, not substring).
- [x] Optional overload `IsUnderSteamLibrary(string path, IEnumerable<string> libraryRoots)` that checks `<root>/steamapps/common` prefixes; use it where known roots are available.

### 2. `StoreSignalDetector`
- [x] Change `DetectType(DirectoryInfo subDir)` to `DetectType(DirectoryInfo subDir, bool isInSteamLibrary = false)`.
- [x] At the top: `if (isInSteamLibrary) return GameSourceKind.Steam;` — before every store signal, including the SteamEmu checks.
- [x] Keep the old call sites compiling via the default `false` (existing tests unchanged).

### 3. `FolderScanner`
- [x] At the `StoreSignalDetector.DetectType` call (around line 149), pass `SteamLibraryMembership.IsUnderSteamLibrary(subDir.FullName)`.
- [x] Do not change `IsNestedSteamTree` behavior.

### 4. `ContainerScanner`
- [x] Where a `SteamEmu` child is promoted from a deep signal, use `GameSourceKind.Steam` when `SteamLibraryMembership.IsUnderSteamLibrary(child.FullName)` is true.
- [x] Do not change other promotion paths.

### 5. Tests
- [x] `SteamLibraryMembershipTests`: under `steamapps/common`, the `common` folder itself, case-insensitivity, `commonfiles` negative, unrelated path negative, backslash and slash inputs.
- [x] `StoreSignalDetectorTests`: same folder with `steam_api64.dll` → `DetectType(dir, true)` is Steam, `DetectType(dir, false)` is SteamEmu.
- [x] `FolderScanner` test: temp root `…/steamapps/common`, game child with `GameA.exe` + `steam_api64.dll` and **no ACF** → source `Steam`.
- [x] Container test only if the promotion path is reachable with a fixture.

## Context

- `FolderScanner.cs:149` is the call site.
- `ContainerScanner` promotion lives around the `summary.HasSteamEmuDeep` branch.
- Windows paths are opaque; the helper must not assume the host OS.

## Requirements

- [x] Library membership does not depend on an ACF or a DLL.
- [x] Outside a library, the SteamEmu behavior is unchanged.
- [x] Only Steam classification changes.

## Verification

- [x] `dotnet build GamingCommander.sln` — 0 warnings / 0 errors.
- [x] `dotnet test GamingCommander.sln` — all pass, new tests included.
- [x] Targeted test: library child + DLL + no ACF → `Steam`; non-library child + DLL → `SteamEmu`.

## Completion Notes

- **Completed:** 2026-10-25
- **What was done:** Added `SteamLibraryMembership` (segment-based, separator-normalized, case-insensitive, with known-root overload). `StoreSignalDetector.DetectType` now takes `isInSteamLibrary = false` and returns `Steam` first. `FolderScanner` passes membership at its DetectType call site. `ContainerScanner` promotes deep SteamEmu children as `Steam` when under a Steam library (and passes membership to its store classification). Added membership, detector, and FolderScanner tests; updated two existing mock-scan tests whose Steam-library expectations changed.
- **Verification:** `dotnet build GamingCommander.sln` — 0 errors; 3 pre-existing Avalonia AVLN3001 XAML warnings (unrelated files, not touched). `dotnet test GamingCommander.sln` — 659 passed, 0 failed (App 464, Core 194, Migration 1; Detection project has no tests).
- **Issues encountered:** None. The 3 build warnings are baseline Avalonia XAML warnings outside this change.
