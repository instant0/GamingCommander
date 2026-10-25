# Task T87: Plan 103 Parity + Plan 123 Residual Text

**Tier:** 1 — Documentation
**Phase:** K — Audit Follow-ups
**Risk:** Minimal
**Status:** completed
**Prerequisites:** None

> Documentation-only. Do not run build/test.

## Objective

Two detection plans still describe gaps that were closed in 2026-09, which misleads the next
detector task.

## What Needs to Change

### 1. `planning/103-detect-py-port-status.md`
- [ ] Replace the executive summary/gap tables: `IsNonGameFolder` has all three layers; the recursive fallback exists with `AppConfig.ScanDepth` (default 3, clamp 1–5); container deep-signal parity is present.
- [ ] Add "Superseded by `docs/detection/07-csharp-parity.md`" near the top.
- [ ] Update the `tools/detect.py` line count to the current value (`wc -l`).

### 2. `planning/123-detection-bugfixes.md`
- [ ] Rewrite the status/revision blocks so only the human review-gate sign-off and doc refresh remain.
- [ ] Mark the 3-level fallback discussion historical (it was a doc error; C# matched/exceeded Python).
- [ ] Change the `IdentificationTier` "NOT implemented" line to `[x]`, citing the shipped typed detection model and `IdentificationTier.cs` — do not cite task IDs.

## Context

- `IdentificationTier.cs`, `DetectionTierDeriver`, `AppConfig.ScanDepth`, `ContainerScanner.IsNonGameFolder`, and `ExecutableDiscovery.FindExecutablesDeep` are the evidence.
- Do not re-port anything; this is status text only.

## Requirements

- [ ] No live "name-only / not ported / capped at 2 / no IdentificationTier" claim remains.
- [ ] Both plans point to the canonical parity/blueprint docs.
- [ ] `detect.py` count is accurate.

## Verification

- [ ] `grep -n "name-only\|not ported\|capped at 2\|no IdentificationTier" planning/103-*.md planning/123-*.md` returns no live gap claim.
- [ ] `wc -l tools/detect.py` matches the number written.
- [ ] No build/test (docs-only).

## Completion Notes

- **Completed:** 2026-10-25
- **What was done:**
  - `planning/103-detect-py-port-status.md`: corrected `tools/detect.py` to 2690 LOC; added "Superseded by `docs/detection/07-csharp-parity.md`"; replaced the stale name-only / not-ported / capped-at-2 / missing-deep-parity claims with the verified current state (`IsNonGameFolder` three layers, `AppConfig.ScanDepth` default 3 clamp 1–5, container deep-signal parity, remaining intentional deep-walk/logger differences).
  - `planning/123-detection-bugfixes.md`: rewrote the status/revision blocks so only human review-gate sign-off and doc refresh remain; marked the 3-level fallback discussion as a historical documentation error; changed the `IdentificationTier` line to `[x]` citing `IdentificationTier.cs`, `DetectionInfo`, `StoreInfo`, and `DetectionTierDeriver`; removed remaining `PlatformMetadata` references in favour of typed records.
- **Verification:**
  - `grep -n "name-only\|not ported\|capped at 2\|no IdentificationTier" planning/103-*.md planning/123-*.md` → no matches.
  - `wc -l tools/detect.py` → `2690`, matching the number written in plan 103.
  - No build/test run (documentation-only).
- **Issues encountered:** None.
