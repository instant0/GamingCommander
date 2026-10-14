# Plan 126 — Epic Manifest Review Pass: The Captain

**Status:** **INVESTIGATION CLOSED** (2026-09-10). The Captain reinstalled/working via Epic. Shadowrun registration attempt failed (EOSH). Findings split into fact files. **Conclusion: no file-based workaround exists — EOSH reported-set membership is server/account-driven, so no `.item`/`.egi`/`.mancpn` can register an app EOSH does not report (see `126-facts-eosh.md` §8j/8o/8r/8t/8u).** Remaining work here is product follow-up only (below).

## Goal
Register an orphan Epic install (`.egstore` present, no ProgramData `.item`) with the launcher, or prove why it cannot be registered.

## Outcome
- **The Captain** (`D:\Games\Epic Games\TheCaptain`): works **only after a manual Epic reinstall** (`DA0CECC9….item`).
- **Shadowrun Dragonfall** (`D:\Games\Epic Games\ShadowrunDragonfall`): every generated file **removed by EOSH** — including the correct-identity variant and the proven-working original file.
- **Root cause of rejection: EOSH install registry does not report the app for this account.** Not a file-content problem.

## The Rule (corrected)
- `AppName` ← `.mancpn` AppName (authoritative). Manifest FString[0] strip Staging is DS-only; FString[0] is a different id for `.mancpn`-era installs.
- `CatalogItemId` ← `.mancpn`/`.ovt`. **Never** a GraphQL store offer id.
- `CatalogNamespace` ← `.mancpn`/`.ovt` (GraphQL corroborates).
- `DisplayName` ← GraphQL title. `LaunchExecutable` ← manifest FString[2]. `InstallationGuid` ← manifest filename.
- Tolerated at write time (Epic rewrites): `AppCategories` public-or-not, `OwnershipToken`, `BuildLabel`, `StagingLocation` `\bps`/`/bps`, `ManifestLocation` mixed/backslash, drive case.
- Identification `.item` is accepted (title appears, Update offered) **without** `EoshRevision`/`CompleteManifestPath`; Epic stamps them **during Update**.
- Survival gate = EOSH registration. Unowned/unreported apps are deleted via `EOSH reconciliation`.

## Follow-ups

1. **`EpicItemWriter.cs:82-83`** — currently `app = appFromManifest ?? folder`, which ignores the `.mancpn`/`.ovt` AppName it already reads into `localApp` (line 47). **Correct precedence:** `.mancpn` AppName → `.manifest` strip-Staging (only correct for old `.ovt`-era installs like Death Stranding) → `.ovt` `sub` → folder leaf. `.mancpn` is authoritative; the manifest FString[0] strip-Staging rule is **Death-Stranding-specific**, not general. Add a unit test asserting `.mancpn` wins over a differing manifest FString[0].
2. **`docs/research/epic_item_format.md`** — correct the generalized claim "AppName = `.manifest` strip Staging" (lines ~12, ~63 Required Fields table, ~258). It is DS-specific. Also soften the optimistic "accepted / survives" wording to match Plan 126: as of launcher 20.2.9, an identification-only `.item` for an app EOSH does not report is removed within ~300 ms (not merely "after the next patch").
3. **`EpicRepairDialog` + `MainWindow.PlatformRepair` copy** — set expectations: writing an identification `.item` only helps an install EOSH already reports; for an unreported game the launcher deletes it. Update Plan 121's orphan-write description accordingly.
4. No `.item` can register an app absent from the account's EOSH registry (server/account-driven; see `126-facts-eosh.md` §8j/8o/8r/8t/8u).

## Fact files
- [126-facts-identity.md](126-facts-identity.md) — identity trio, sources, comparison tables, `.mancpn` redundancy.
- [126-facts-eosh.md](126-facts-eosh.md) — EOSH gate: log evidence, cache, EoshRevision stamps, catcache.bin.
- [126-facts-shadowrun.md](126-facts-shadowrun.md) — Shadowrun full recovery record + original-vs-generated diff.