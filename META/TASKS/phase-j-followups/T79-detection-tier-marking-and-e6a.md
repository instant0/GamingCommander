# Task T79: Plan 123 Closeout — Confidence-Tier Marking + E6a

**Tier:** 3 — Logic/Behavior
**Phase:** J — Post-126 Follow-ups
**Effort:** ~90 min
**Risk:** Medium
**Status:** Complete (2026-10-14)
**Prerequisites:** None

> **Progress (2026-10-14):**
> - **E6a DONE** — `"tool"` added to `data/blacklist.json` `tier_10_dev_editor_tools`.
> - **Recursive fallback depth DONE** — `AppConfig.ScanDepth` (default 3, clamp 1–5) threaded through
>   `FolderScanner` → `ExecutableDiscovery.FindPrimaryExecutable` → `FindExecutablesDeep`; tests added.
>   The old "3-level fallback not ported" gap was a doc error — C# already matched/exceeded Python.
> - **Confidence-tier marking DONE** — `IdentificationTier` enum, `DetectionInfo` record, `StoreInfo`
>   record, `DetectionTierDeriver`, wired into `FolderScanner`, `SteamLibraryScanner` (all 3 paths),
>   `EpicLibraryScanner` (both paths), persisted in `games.json` DTO, tested (14 new tests). See Plan 127 A3.
> - **Remaining (process only):** run the amended review gate over the final Python scanner; refresh
>   `planning/103` notes.

---

## Objective

Plan 123's C# detection port is **already shipped** (2026-09-07). This task closes the genuine
residuals: the confidence-tier marking promised in §0.1 (currently no `IdentificationTier` exists in
code), the E6a bare `"tool"` blacklist penalty, and the final doc/gate refresh. **Do not re-port
E1/E4/E5/E6/E7/E8.**

## What Needs to Change

### 1. Confidence-tier marking (§0.1)
**File:** `src/GamingCommander.Core/Models/PlatformMetadataKeys.cs`
**Actions:**
- [ ] Add `public const string IdentificationTier = "IdentificationTier";` (persisted value — never rename).

**New file:** `src/GamingCommander.App/Services/IdentificationTier.cs`
**Content:** a small static helper `Derive(GameSourceKind source, bool hasAuthoritativeManifest, bool hasStoreSignal, bool hasTitleEvidence)` returning `"Locked" | "Secure" | "Candidate" | "Unknown"`:
- Steam structural path → `Locked`.
- Epic global `.item` matched to InstallLocation → `Locked`.
- Epic local `.item`/`.mancpn`, other store marker, registry fallback → `Secure`.
- Root exe / engine layout / container child with evidence → `Candidate`.
- No signal → `Unknown`.

**Wiring:**
- [ ] `FolderScanner` (`StoreMetadataEnricher` / `AddGameEntry`) writes `IdentificationTier` from the signals that produced the entry.
- [ ] `SteamLibraryScanner` writes `IdentificationTier = "Locked"`.
- [ ] `EpicLibraryScanner` writes `Locked` (global `.item`) / `Secure` (local).
- [ ] Tier is additive metadata only — it must **not** overwrite a `UserOverrides`-pinned field or a `Locked` title on rescan.

### 2. E6a — bare `"tool"` penalty
**File:** `data/blacklist.json`
**Current state:** `"tool"` is absent from `tier_10_dev_editor_tools` (present only in C# hardcoded default lists).
**Actions:**
- [ ] Add `"tool"` to `tier_10_dev_editor_tools` in `data/blacklist.json`.
- [ ] Confirm no corpus regression (the exact-folder-stem `+15` bonus still masks the S-G case, as documented).

### 3. Review gate + doc refresh
**Actions:**
- [ ] Record the amended review-gate outcome (final Python scanner as reference) in `planning/123-detection-bugfixes.md` §7.
- [ ] Refresh `planning/103-detect-py-port-status.md` final-parity note (point to `docs/detection/07` as the porting reference).
- [ ] Mark the remaining 3-level generic subdir fallback as a **decision** (port vs accept-as-LOW) in `docs/detection/07` §3.

## Context

- `docs/detection/07-csharp-parity.md` §2 = "ALL PORTED"; §3 lists the only remaining gaps.
- `docs/detection/04-containers.md` §6 records the dropped single-child rule.
- §0.1 specifies the tier carrier is `PlatformMetadata` (not a new independent model field).
- Constraint: constants values are pinned by contract tests — `IdentificationTier` value strings are persisted, so add a pin test.

## Requirements

- [ ] `IdentificationTier` key + helper added; persisted and read back.
- [ ] Tier is derived from existing signals (no new detection pass).
- [ ] Locked entries are not silently rewritten on rescan.
- [ ] `"tool"` present in `tier_10_dev_editor_tools`.
- [ ] No detection behavior change beyond the above.
- [ ] Plan 123 checkboxes/status consistent with reality.

## Verification

- [ ] `dotnet build` passes (0 errors)
- [ ] `dotnet test` passes; new tier tests green (Locked/Secure/Candidate/Unknown per fixture)
- [ ] Contract pin test for `IdentificationTier` value strings
- [ ] `grep -c '"tool"' data/blacklist.json` ≥ 1 in tier 10

## Completion Notes

- **Completed:** 2026-10-14
- **What was done:** E6a (`"tool"` in `blacklist.json` tier 10); configurable `AppConfig.ScanDepth` (default 3) threaded into the exe fallback; confidence-tier marking via `IdentificationTier` + `DetectionInfo` + `StoreInfo` + `DetectionTierDeriver`, wired into the generic, Steam, and Epic scanners and persisted in `games.json`.
- **Verification:** `dotnet build` 0 errors; `dotnet test` **581 pass / 0 fail** (+14 tests: deriver + config + depth + DTO round-trip).
- **Issues encountered:** the "3-level fallback" gap was a documentation error, not a code gap.
- **Remaining (process):** amended review gate over the final Python scanner; `planning/103` note refresh.
