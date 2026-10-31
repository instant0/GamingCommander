# Task T133: Install Identity by Path, Not Folder Name

**Tier:** 2 — Correctness
**Phase:** M — Store Capabilities
**Risk:** Medium (two scanners + persistence identity keys; each step tested)
**Status:** Done
**Prerequisites:** None. Run before the Steam/Epic inventory readers.

## Objective

Make one rule true everywhere: **a physical install is identified by its full path (and, where a store
provides one, by its store id) — never by its folder name.** Names remain display text and, at most, a
last-resort relocation hint when it is unambiguous.

The goal is accuracy: two different folders never collapse into one entry, and a store record is never
compared against the wrong game because both live in a folder called `Foo`.

## What the investigation found

The defect is not Epic-specific. Six sites reduce a folder to its leaf name or use an ad-hoc path compare:

| # | Site | Current key | Consequence |
|---|---|---|---|
| 1 | `SteamLibraryScanner.CollectAcfMap` | `AcfInfo.Installdir` (leaf name), first-wins across libraries | Two libraries with `steamapps/common/Foo` and different appids: the second game gets the first ACF, so wrong appid/name and often status `Moved` |
| 2 | `SteamLibraryScanner.Scan` lookup | `acfMap[folderName]` | Same collision; the lookup never considers the library path |
| 3 | `EpicLibraryScanner` claimed set | full path **and** `EpicInstallPath.FolderName(...)` | A catalog item at `D:\Games\Foo` also claims a known entry at `E:\Games\Foo`; the second game is suppressed |
| 4 | `GamesDatabaseService.PathsEqual` | local `TrimEnd` + `OrdinalIgnoreCase` | Does not normalize `/` vs `\`; duplicates `PathText`, so a path can be "equal" in one place and not another |
| 5 | `GameIdentity` weak `FolderName` key + `FindRelocated` | leaf name only, any store | A deleted folder plus a same-named game in another store can be adopted as a "relocation"; user data attaches to the wrong game |
| 6 | `EpicLibraryScanner` id fallback | `GameEntryId.ComputeId(manifestsDir, folderName)` when `CatalogItemId` is absent | Two Epic items with no catalog id and the same leaf name get the same persisted id |
| 7 | `GameEntryId.ComputeId` | raw string concatenation | `D:\Games\Foo` vs `D:/Games/Foo` produce different ids, so identity is not stable across formatting |

`EpicInstallPath.Same` and `PathText` are already path-exact; the remaining name keys are leftovers from
before that rule existed. `EpicItemCatalog.DedupPlayable` uses `path + display name` only inside one
catalog when the id is missing; that is not a cross-folder claim and stays as-is (review only).

## The model

Three tiers, in order:

1. **Strong — store id.** `SteamAppId`, `EpicCatalogItemId`, `GogGameId`, EA content id, etc. Used when both
   sides have the id. This is the only key that survives a move.
2. **Strong — full path.** `PathText.NormalizeForCompare` / `Equals` / `IsUnder`. Used to join a store record
   to a disk folder and to dedupe within a scan. Path equality is case-insensitive, separator-insensitive,
   trailing-separator-insensitive, and drive-root-safe.
3. **Weak — folder name.** Only for relocation of an entry that has no store id, only inside the same
   `GameSourceKind`, only when the old path no longer exists, and only when **exactly one** existing entry
   and **exactly one** scanned entry carry that name. Otherwise no match: a new entry is correct.

Names are never dictionary keys, claim keys, or id inputs.

## Generic helpers (one place, no new framework)

Add `src/GamingCommander.Core/Services/InstallIdentity.cs`:

```csharp
public static class InstallIdentity
{
    /// Canonical key for a physical folder. Empty input -> empty string (callers skip it).
    public static string PathKey(string? fullPath) => PathText.NormalizeForCompare(fullPath);

    /// Scope for the weak relocation hint; never used as identity on its own.
    public static string WeakFolderKey(GameSourceKind kind, string? folderName);
}
```

Why only two functions: `PathKey` is the named seam for every identity comparison; `WeakFolderKey` is the
one genuinely new rule. A `RecordKey(kind, storeId, path)` helper is **not** added yet — the scanners do not
need it, and `GameStoreIndex`/`StoreInfo.StoreKey` already own the store-id key. It will be added by the
import/sync work when it has a real consumer.

Why not a generic matcher or index class: there are two live consumers (Steam and Epic scanners) plus the
future import path; a `Dictionary<string,T>` keyed by `PathKey` is already the simplest expression. A
matcher/index abstraction would add indirection and a second path for the same decision. These pure
functions are the shared input→output seam; each caller still decides what to do with a hit.

## Changes, in order

### 1. Helper + tests
- [x] Add `InstallIdentity` with `PathKey` and `WeakFolderKey`.
- [x] Tests: empty/whitespace, `/` vs `\`, trailing separators, drive root, case, UNC is treated as opaque;
      `WeakFolderKey` is store-scoped and case-insensitive in the dictionaries that use it.

### 2. Steam — ACF map by full path
- [x] `CollectAcfMap` keys each ACF by
      `PathKey(Path.Combine(info.LibraryPath, "steamapps", "common", info.Installdir))`.
- [x] Use `TryAdd` (first-wins only for the same full path; duplicate library entries dedupe).
- [x] `Scan` looks up `PathKey(gameDir.FullName)`, not `folderName`.
- [x] The missing-ACF pass iterates `AcfInfo` values and uses `acf.Installdir` for the candidate path and
      status; it must not use the dictionary key.
- [x] Status is `PathText.Equals(acf.LibraryPath, root) ? Installed : Moved` for an exact-path ACF. If no
      exact ACF exists, a **single** ACF in another library whose own folder is gone is a last-resort
      `Moved` hint; two such ACFs are ambiguous and the folder stays `Orphaned`. The name is a weak hint
      here, never the entry identity, and the appid still comes from the ACF.
- [x] Id remains `GameEntryId.ComputeId(acf.LibraryPath, acf.Installdir)` / `(libraryRoot, folderName)`.

### 3. Epic — claim by full path only
- [x] `claimedFolders` adds only `PathKey(installLocation)` and `PathKey(match.FolderPath)`; skip empty.
- [x] Known-entry suppression checks only the path key, plus `EpicItemCatalog.MatchesInstall` (path-exact).
- [x] Remove `EpicInstallPath.FolderName` from the claimed set and lookup. Keep it for display/AppName.
- [x] Id: catalog id when present; else `GameEntryId.ComputeId(catalog.ManifestsDir, installLocation)`;
      if the install path is absent, `ComputeId(item.ItemFilePath, folderName)`. If both are empty (a
      malformed item), fall back to `ComputeId(catalog.ManifestsDir, folderName)` and accept that the item
      has no unique identity — it will be treated as a duplicate and not invented.
- [x] `FolderName` remains display and the AppName fallback only.

### 4. Persistence identity
- [x] `GamesDatabaseService.PathsEqual` is replaced by `PathText.Equals`; delete the local helper.
- [x] `GameEntryId.ComputeId` normalizes both inputs through `InstallIdentity.PathKey` before hashing.
      (Store ids passed as `folderPath` are unaffected in practice; they contain no separators.)
- [x] Document that ids are stable for the same normalized inputs; persisted ids are disposable pre-ship.

### 5. Weak relocation hint (keep the feature, remove the false match)
- [x] In `FindRelocated`, a `FolderName` key matches only when:
      - `existing.GameSource == scanned.GameSource`;
      - the existing folder no longer exists;
      - exactly one existing entry in that anchor and exactly one scanned entry carry that
        `WeakFolderKey` (counts computed once per `SetGamesForLibrary`, not per entry).
- [x] If two entries share the name, no adoption: the scanned entry becomes a new entry. Losing a
      speculative merge is correct; merging two distinct games is not.
- [x] Strong keys (store ids) keep their current behaviour.
- [x] `GameIdentity` continues to store the `FolderName` key; only `FindRelocated` applies the uniqueness
      guard, so no persisted-key schema changes.

### 6. Consistency review (no behaviour change)
- [x] `EpicItemCatalog.DedupPlayable` keeps its composite key; use `PathKey` for the path part so formatting
      does not create two keys.
- [x] `SteamAcfWriter`'s installdir verification uses `StringComparison.Ordinal` for the exact bytes it
      wrote; leave it (it is not identity matching).

## Tests

| Case | Assertion |
|---|---|
| Steam, two libraries, both `common/Foo`, different appids | two entries; each has its own appid; both `Installed`; neither `Moved` |
| Steam, ACF in another library only | status `Moved` (existing behaviour preserved) |
| Steam, two cross-library ACFs both named `Foo`, folder in a third library | ambiguous → `Orphaned`, no appid adopted |
| Epic, item at `D:\Games\Foo`, known entry at `E:\Games\Foo` | known entry not suppressed; two distinct ids |
| Epic, no `CatalogItemId`, two install paths with the same leaf | two distinct ids |
| `GameEntryId` with `/` vs `\`, case, trailing separator | same id |
| `PathsEqual` replacement | `/` vs `\` and case compare equal; different paths do not |
| Weak relocation, one candidate | adopted, `PreviousLocations` preserved |
| Weak relocation, two same-named candidates | not adopted; two entries remain |
| Existing Steam/Epic scanner tests + detection matrix | unchanged |

Tests use temp directories and synthetic manifests; no real library/ProgramData access.

## What this deliberately does not do

- **Do not merge the scanners into one physical pass.** Steam and Epic have out-of-band inventories
  (`libraryfolders.vdf`/ACF, ProgramData `*.item`); the general `FolderScanner` walks added roots for the
  other stores. The duplication being removed is the **identity rule**, not the readers. A unified
  enumerator is a separate refactor with a different risk profile.
- **Do not add a generic matcher/index type.** Two pure helpers are enough; a class hierarchy would
  duplicate the same decision behind indirection.
- **Do not change store signals, health rules, display names, or writes.**
- **Do not use folder names for identity anywhere, including future import/sync code.**

### Known edges left unchanged

- **Steam missing-ACF pass** treats a same-named folder in *any* library as "present", so a different game
  with that name can hide a `Missing` verdict for the ACF's own library. Fixing it needs appid evidence for
  a disk folder, which does not exist without an ACF; recorded as a limitation, not silently guessed.
- **Nested `installdir`** (e.g. `Foo\Bin`) still does not resolve to a direct `common/` child; unchanged.
- The weak name hint is deliberately conservative: ambiguous names are not adopted, at the cost of an
  occasional extra entry. That is the accuracy trade-off.

## Acceptance

- [x] No dictionary in those scanners is keyed by `Installdir` or `FolderName`. The only leaf-name
      comparison left is the documented, uniqueness-guarded `Moved` hint in `FindAcfForFolder`.
- [x] `InstallIdentity` is the only new shared helper; no new package.
- [x] Every test above passes; the full existing suite stays green.
- [x] `dotnet build` 0 errors; `dotnet test` green.
- [x] The connection map's known-gap row for name-keyed identity is removed when this lands.

## Stop rules

- Stop at identity correlation. Do not refactor scanners, change detection order, or touch writes.
- If a change would alter a detection verdict for a **single** same-named folder, it is out of scope;
  this task only separates folders that are currently conflated.
- One step per change: helper, Steam, Epic, persistence, weak key. Each step builds and tests.
- No cache, no background work, no new persisted schema.

## Completion Notes

- **Completed:** 2026-10-31
- **What was done:**
  - Added `Core/Services/InstallIdentity.cs` with `PathKey` and `WeakFolderKey` only (no `RecordKey`).
  - Steam: the ACF map is keyed by the normalized full install path with `TryAdd`; `Scan` looks up the
    scanned folder's full path. The missing-ACF pass iterates `AcfInfo` values and uses `acf.Installdir`.
    A scanned folder with no exact-path ACF resolves `Moved` from a single cross-library ACF whose own
    folder is gone; two such ACFs are ambiguous and yield `Orphaned`, so the existing single-ACF `Moved`
    verdict is unchanged.
  - Epic: claims are full-path keys only (`PathKey`, empty skipped); known-entry suppression uses the path
    key plus `MatchesInstall`. Id is the catalog id, else `ComputeId(manifestsDir, installLocation)`, else
    `ComputeId(item.ItemFilePath, folderName)`, else the documented malformed-item fallback.
  - Persistence: `GamesDatabaseService` uses `PathText.Equals` (local `PathsEqual` deleted).
    `GameEntryId.ComputeId` passes both inputs through `InstallIdentity.PathKey` before hashing; the combined
    string is lower-invarianted so casing cannot fork an id.
  - Weak relocation: store-scoped folder-name counts are computed once per `SetGamesForLibrary`; a
    `FolderName` key adopts only when the store matches, the old folder is gone, and both counts are exactly
    1. Strong keys, `GameIdentity`, and the persisted schema are unchanged.
  - `EpicItemCatalog.DedupPlayable` uses `PathKey` for the no-id path part.
  - Removed the name-keyed identity row from `META/CONNECTION_MAP.md`; recorded `InstallIdentity` and the
    path-keyed map in `META/CODE_MAP.md`.
- **Verification:**
  - `dotnet build GamingCommander.sln -v q --nologo` — 0 errors (3 pre-existing Avalonia AVLN3001 warnings).
  - `dotnet test GamingCommander.sln --no-build -v q --nologo` — 780 passing: Core 235, App 544,
    Migration 1 (baseline 755 + 25 new).
  - New tests cover `InstallIdentity` (empty/whitespace, separators, trailing separator, drive root, case,
    UNC, store-scoped weak key), Steam two libraries with `common/Foo`, Epic same-leaf different folders
    (with and without a catalog id), `GameEntryId` separator/case/trailing-separator stability,
    `GamesDatabaseService` equivalent-path equality, and weak relocation with one candidate, two scanned
    candidates, two existing candidates, and a different store.
- **Issues encountered:**
  - Path-key-only lookup makes the old `Moved` verdict unreachable for a folder whose ACF lives in another
    library, which would have broken the preserved `Moved` test. Added the narrow single-candidate fallback
    described above (name used only as a last-resort, uniqueness-guarded hint; ids stay path-based).
  - `PathKey` preserves case, so `ComputeId` lower-invariants the normalized inputs; the task's required
    "case → same id" test is what forces this.
  - `EpicInstallPath.Normalize`/`FolderName` now have no call sites (the scanner has its own display-only
    `FolderName`); left in place as the shared Epic path helper.
