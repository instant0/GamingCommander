# Plan 126 — Epic Manifest Review Pass: The Captain

**Status:** DISCOVERY DONE (2026-09-09). The Captain reinstalled/working via Epic. Shadowrun registration attempt failed (EOSH). Findings split into fact files.

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
1. `EpicItemWriter.cs:82` — prefer `.mancpn` AppName over manifest FString.
2. `docs/research/epic_item_format.md` — "AppName = manifest strip Staging" is DS-specific.
3. No `.item` can register an app absent from the account's EOSH registry.

## Fact files
- [126-facts-identity.md](126-facts-identity.md) — identity trio, sources, comparison tables, `.mancpn` redundancy.
- [126-facts-eosh.md](126-facts-eosh.md) — EOSH gate: log evidence, cache, EoshRevision stamps, catcache.bin.
- [126-facts-shadowrun.md](126-facts-shadowrun.md) — Shadowrun full recovery record + original-vs-generated diff.