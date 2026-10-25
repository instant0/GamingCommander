# Task T85: Refresh TECH_DEBT and IDEAS

**Tier:** 1 — Documentation
**Phase:** K — Audit Follow-ups
**Risk:** Minimal
**Status:** completed
**Prerequisites:** None

> Documentation-only. Do not run build/test.

## Objective

`META/BACKLOG/TECH_DEBT.md` was last verified 2026-08-22 and now lists fixed items while missing
the detector backlog. `META/BACKLOG/IDEAS.md` contains shipped/dead ideas without annotation.

## What Needs to Change

### 1. `META/BACKLOG/TECH_DEBT.md`
- [ ] Update the "last verified" date to 2026-10-25.
- [ ] Close **Bug 12** (ComboBox `MinWidth` fixed) and **per-tag colour** (shipped `TagColorService`/`ITagColorProvider`).
- [ ] Add a **Detector review backlog** section pointing at `docs/detection/09-blueprint.md` §14: O1/O2 residual, O9, SteamEmu, Epic engine (P2), play-time, `Relations.Kind`, collections UI.
- [ ] Replace `PlatformMetadata` references with `StoreInfo`/typed records.
- [ ] Keep the EA caveat but mark it **blocked on a real install**.

### 2. `META/BACKLOG/IDEAS.md`
- [ ] Annotate shipped/dead/superseded ideas (no deletion): local game DB/merge, launch counter, Epic manifest patching (dead end), F9 hint.
- [ ] Update `ExeCandidateSelector` references to `DetectionInfo.CandidateExes`.
- [ ] Update hardcoded-list references to `DetectionNoise`.

## Context

- Findings: `planning/128-doc-and-task-audit.md` §1.4.
- Append-only spirit: annotate history, do not prune it.

## Requirements

- [ ] No entry claims a fixed/shipped item is open.
- [ ] Every open item traces to `docs/detection/09-blueprint.md` §14 or `CURRENT.md`.
- [ ] No removed type names remain in live guidance.

## Verification

- [ ] `grep -n "Bug 12\|PlatformMetadata" META/BACKLOG/TECH_DEBT.md` shows no open fixed item / removed type.
- [ ] `grep -n "DetectionNoise\|CandidateExes" META/BACKLOG/IDEAS.md`.
- [ ] No build/test (docs-only).

## Completion Notes

- **Completed:** 2026-10-25
- **What was done:**
  - `META/BACKLOG/TECH_DEBT.md`: set last-verified to 2026-10-25; closed Bug 12 (`MinWidth = 148`); marked per-tag colour shipped (`TagColorService`, `ITagColorProvider`, `tag_colors.json`); replaced the removed `PlatformMetadata` reference with typed `StoreInfo` records; kept the EA caveat and marked it blocked on a real install; added a **Detector Review Backlog** section (O1/O2 residual, O9, SteamEmu, Epic engine, deep 4-level walk, `DetectionLogger`, play-time, `Relations.Kind`, collections UI) pointing at `docs/detection/09-blueprint.md` §14.
  - `META/BACKLOG/IDEAS.md`: annotated shipped/dead/superseded items without deleting history (ExeCandidateSelector shipped, KODI/F8 stale, quick search shipped as type-to-search, Epic manifest patching dead end, local game DB/merge superseded, launch counter partial, F9 hint dead); updated `ExeCandidateSelector` to `DetectionInfo.CandidateExes`; updated hardcoded-list references to `DetectionNoise`.
- **Verification:**
  - `grep -n "Bug 12\|PlatformMetadata" META/BACKLOG/TECH_DEBT.md` → only the header date line and the Bug 12 heading; no open Bug 12 status and no removed type.
  - `grep -n "DetectionNoise\|CandidateExes" META/BACKLOG/IDEAS.md` → matches on the shipped candidate-exe idea and the detection-signals idea.
  - No build/test run (documentation-only).
- **Issues encountered:** None.
