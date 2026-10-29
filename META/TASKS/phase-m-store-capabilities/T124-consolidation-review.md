# Task T124: Duplicate-Capability Review and Consolidation Plan

**Tier:** 1 — Documentation / Architecture
**Phase:** M — Store Capabilities
**Risk:** Low (analysis only)
**Status:** completed (D1 implemented at human direction; D2–D7 deferred)
**Prerequisites:** Task T124 depends on Task T120 and Task T123

> Human direction overrode the documentation-only rule for D1: the path rule was implemented and the
> duplicate normalisers removed. D2–D7 remain decisions to be taken with their consumers.
> Original constraint, still true for D2–D7: do not redesign, do not add an interface without a caller.

## Objective

Produce **decisions**, not a refactor: name the shared contract for each capability that T117, T118,
and T121 would otherwise each re-implement. The deliverable is one plan document with a decision per
item, the evidence for it, the consumer, the migration order, and the test.

## Why this is a decision gate

T117 (launcher discovery), T118 (inventory import), and T121 (launch sync) all need the same five
things: path comparison, store identity, launcher location, manifest reading, and the decision to
write. Each already exists somewhere in the codebase, in one or more variants. If the consumers are
built first, each picks whichever helper is nearest and the duplication doubles — and the variants
disagree in ways that only show up as detection bugs.

The point of this task is to make that choice once, in writing, before the consumers exist.

## Blocking decisions (the only thing this task must resolve)

| # | Capability | Existing code | Planned consumer | Decision required |
|---|---|---|---|---|
| D1 | Path text | `SteamAcfParser.NormalizePath`, `RegistryFallbackDetector.NormalizePath`, `EpicInstallPath.Normalize`, `SteamLibraryMembership.Normalize` (private), `LibraryManager.NormalizeLibraryRoot` | locators, readers, index | one canonical form: separators, trailing separator, case rules, drive-letter, UNC |
| D2 | Store identity | `GameStoreIndex.StoreKey`, `GameIdentity`/`IdentityKey`, `GameSourceParser` | import, sync, dedup | one resolver: `GameSourceKind` + store id → key, and what happens when the id is missing |
| D3 | Launcher location | `SteamInstallPathLocator`, `RegistryFallbackDetector` registry probing | `ILauncherLocator` (planned) | extend the existing locator, or define the interface and make it the only entry |
| D4 | Inventory read | `SteamLibraryScanner`, `EpicLibraryScanner` | `IStoreInventoryReader` (planned) | does the interface wrap the scanners or replace them; who owns the manifest-first rule |
| D5 | Scan/sync write | `LibraryManager.RescanLibrary`, `SelectScannerAndScan` | `StoreSyncService` (planned) | one writer of scan results; the other becomes a caller |
| D6 | Policy gate | `IStorePolicyProvider` (`CanImport`/`CanSync`) | T117/T118/T121/T122 | it is the only gate; no direct per-store checks |
| D7 | Stat/fingerprint | `FileSystemHelper.GetLastWriteTimeSafe` | `StoreManifestFingerprint` (planned) | one stat helper; define what "changed" means (mtime, size, both) |

Each decision is recorded even if the answer is "keep the existing code and document it". A decision to
do nothing is a valid outcome and must be written down as one.

**Constraint for D1 (write it into the plan):** NTFS is case-insensitive by default, so the *filesystem*
never cares about case. String comparison still does: paths arrive from the registry (`\`), from ACF
files, from Epic JSON (`/`), and from the user's folder picker, and the same folder can arrive with
different casing. So:

- Normalise separators and trailing separators once, then compare with `OrdinalIgnoreCase`
  (never culture-aware — a culture-aware compare changes behaviour under a Turkish locale).
- Never lowercase the value you store, display, or use as a key; fold case only at comparison time.
  `EpicInstallPath.Normalize` lowercases, which is fine for a comparison helper but must not leak into
  persisted paths.
- Per-directory case sensitivity does exist (`fsutil file setCaseSensitiveInfo`, used by WSL), so a
  comparison must not *assume* the filesystem folded case — it folds case because the strings may
  disagree, not because the disk does.
- Do not uppercase a path you keep. `ToUpper()` is culture-sensitive (`install` → `İNSTALL` under a
  Turkish locale), allocates, and destroys the casing Windows preserved for display and for the real
  path on disk. Compare with `StringComparison.OrdinalIgnoreCase` and use
  `StringComparer.OrdinalIgnoreCase` for dictionaries. A separate derived compare key is allowed only
  if it is never displayed, stored as the path, or passed to the shell.

## Deferred duplicates (record one line each, do not analyse)

These are real duplications, but nothing in T117/T118/T121 depends on them, and they have a home in
`docs/detection/09-blueprint.md` §11. They stay there:

- **O1/O2** noise exe and directory lists (9 and 5 declarations) — backlog.
- **O3** Epic `.egstore` detection in four places — backlog.
- **O4/O5/O6** Ubisoft/Rockstar/Blizzard signal checks — backlog.
- **O7** UE layout and platform tokens — backlog.
- **O10** cheap-exe finders — backlog, and note the repair writer is a consumer (T124C).

Also record the store-priority disagreement (`StoreSignalDetector` GOG→EA vs `RegistryFallbackDetector`
EA→Ubi) as a known behavioural inconsistency; it is not a consolidation decision, it is a bug to fix
where the signal order is defined.

## Evidence corrections (found while scoping this task)

Record these in the plan; they supersede the stale text in the blueprint:

- **O9 is closed.** `EditionTokens` (Core) is consumed by `FileSystemHelper`, so the edition lists are
  aligned. The blueprint still lists O9 as open.
- **O8's list is wrong in the blueprint.** `EpicManifestParser.NormalizePath` no longer exists.
  `GameStoreIndex` has no `NormalizePath`. The live variants are the five in D1, plus
  `SteamLibraryMembership.Normalize`, which is private and therefore cannot be reused as-is.
- **Dead-code line is stale.** The blueprint says `EpicStoreSearch` was removed; the file exists and is
  compiled. Its disposition belongs to T124D, and the wording correction belongs to that task.
- **Two concrete divergences to cite as evidence, not as hypotheticals:**
  - `SteamAcfParser.NormalizePath` does not convert `/` to `\`, while `RegistryFallbackDetector` does.
    An ACF path written with forward slashes and a registry path for the same folder compare unequal.
  - `LibraryManager.IsChildOf` is a raw `StartsWith(OrdinalIgnoreCase)` on untrimmed-of-`/` strings, so
    `C:/Games/X` is not recognised as under `C:\Games`.
- **Fuzzy identity heuristic in `EpicInstallPath.Same`.** When normalised paths differ it falls back to
  comparing the last folder name (length ≥ 3) and returns true. Two different games in different
  libraries that share a folder name are treated as the same game. This is a D2 input, not a path fix.

## Deliverable

`planning/130-duplicate-feature-consolidation.md`:

- [ ] **Decisions:** one entry per D1–D7: the decision, `file:line` evidence, the consumer, the
      migration order, and the test that proves the migration kept behaviour.
- [ ] **Do-not-consolidate list** with reasons (display-string edition matching vs filename tail;
      registry fallback vs manifest inventory are different sources; parent-bound child names are an
      intentional set).
- [ ] **Reuse checklist** for future store work: find the shared helper first; a new reader implements
      the shared interface; no new path normaliser, store-key parser, or stat helper.
- [ ] Link to the connection map (T124A) instead of restating wiring.
- [ ] Link to the three companion plans (persistence, repair writes, Epic client) instead of
      duplicating them.
- [ ] No task IDs in the plan document.
- [ ] Link it from `docs/detection/09-blueprint.md` §11 as the decided outcomes for D1–D7.

## Stop rules

- **No `src/` change.** If a decision seems obvious, write the migration and its test; do not apply it.
- **No interface without a named consumer.** A proposed abstraction that T117/T118/T121 would not call
  is out of scope.
- **No silent behaviour change.** If two variants disagree today, merging them is a behaviour decision:
  state which behaviour wins and what test covers it. "They look the same" is not evidence.
- **Stop at D1–D7.** The deferred list is recorded, not resolved.
- **The blueprint is the inventory of record.** Plan 130 records decisions; it does not paste a second
  copy of the overlap table.

## Context

- Overlap inventory: `docs/detection/09-blueprint.md` §11 (O1–O10), plus the corrections above.
- Store design: `planning/129-store-capabilities-and-launcher-discovery.md`.
- Built foundations: `DetectionNoise`, `EditionTokens` (Core), `GameStoreIndex` (Core),
  `IStorePolicyProvider` (Core), `GameIdentity`/`IdentityKey` (Core).
- Companion reviews: T124B (persistence), T124C (repair writes), T124D (Epic online client).
- Must complete before T117/T118 so their interfaces adopt the decided contracts.

## Requirements

- [ ] Every D1–D7 item has a decision, evidence, consumer, migration order, and test.
- [ ] Every variant named in D1/D2 exists at the cited location (verify with `grep`, the blueprint is
      stale in two places).
- [ ] The "do not consolidate" list is present.
- [ ] The reuse checklist is present.
- [ ] Deferred duplicates are recorded, not analysed.
- [ ] No code, no behaviour change, no build/test.

## Verification

- [ ] `test -f planning/130-duplicate-feature-consolidation.md`.
- [ ] Each decision cites `file:line` or a planning document.
- [ ] A "do not consolidate" section exists.
- [ ] The reuse checklist is present.
- [ ] The three delegated areas are linked, not duplicated.
- [ ] `git status --short src/` is empty (the task changed no source).
- [ ] No build/test (docs-only).

## Completion Notes

- **Completed:** 2026-10-29
- **What was done:** D1 decided and implemented. Added `PathText` (Core) as the single path rule and
  removed the duplicate normalisers (`SteamAcfParser.NormalizePath`, `RegistryFallbackDetector.NormalizePath`,
  `EpicInstallPath.Normalize`/folder-name fallback, `SteamLibraryMembership`'s private `Normalize`,
  `LibraryManager.IsChildOf`, `WindowsExplorer.IsUnder`). Real paths are only trimmed, never rewritten;
  comparison is `OrdinalIgnoreCase` on a derived form. `planning/130-duplicate-feature-consolidation.md`
  records the rule, the behaviour changes, the D2–D7 open list, the do-not-consolidate list, and the
  reuse checklist.
- **Verification:** `dotnet build GamingCommander.sln` — 0 errors. `dotnet test GamingCommander.sln` —
  Migration 1, Core 219, App 488, all passing. 19 of the Core tests are the new `PathTextTests`.
- **Issues encountered:** A first pass used the comparison form as a real path inside
  `SteamLibraryScanner`/`SteamInstallPathLocator`, which broke 17 Steam tests on the Linux test host.
  Fixed by splitting `TrimTrailingSeparators` (real paths) from `NormalizeForCompare` (comparison only).
  This distinction is now the first rule in the reuse checklist.

