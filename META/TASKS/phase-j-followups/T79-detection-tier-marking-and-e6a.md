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
> - **Review gate CLOSED (agent, 2026-10-14; human sign-off pending).** The `planning/103` note
>   refresh is tracked in `T87`; no code work remains.

---

## Objective

Plan 123's C# detection port is **already shipped** (2026-09-07). This task closes the genuine
residuals: the confidence-tier marking promised in §0.1 (currently no `IdentificationTier` exists in
code), the E6a bare `"tool"` blacklist penalty, and the final doc/gate refresh. **Do not re-port
E1/E4/E5/E6/E7/E8.**

## What Needs to Change

### 1. Confidence-tier marking (§0.1) — shipped
**Shipped files:**
- `src/GamingCommander.Core/Models/IdentificationTier.cs` — enum (`Locked`, `Secure`, `Candidate`, `Unknown`).
- `src/GamingCommander.Core/Models/DetectionInfo.cs` — record carrying the tier and title provenance.
- `src/GamingCommander.App/Services/DetectionTierDeriver.cs` — derives `DetectionInfo` from signals the scanners already produced.

**Actions:**
- [x] `IdentificationTier`/`DetectionInfo` replace the old key-based metadata carrier.
- [x] `DetectionTierDeriver.Derive(...)` returns `Locked` / `Secure` / `Candidate` / `Unknown`:
  - Steam structural path → `Locked`.
  - Epic global `.item` matched to InstallLocation → `Locked`.
  - Epic local `.item`/`.mancpn`, other store marker, registry fallback → `Secure`.
  - Root exe / engine layout / container child with evidence → `Candidate`.
  - No signal → `Unknown`.
- [x] Wired into `FolderScanner`, `SteamLibraryScanner` (all 3 paths), and `EpicLibraryScanner` (both paths).
- [x] Persisted as `Detection` in the `games.json` DTO; additive metadata only — does not overwrite user overrides or pinned titles on rescan.

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
- §0.1 proposed a key-based tier carrier; the shipped design uses the dedicated `DetectionInfo` record on `GameEntry` instead.
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
- **Review gate:** CLOSED (agent, 2026-10-14; human sign-off pending). `planning/103` refresh tracked in `T87`.
