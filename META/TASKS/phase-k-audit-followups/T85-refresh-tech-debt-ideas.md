# Task T85: Refresh TECH_DEBT and IDEAS

**Tier:** 1 — Documentation
**Phase:** K — Audit Follow-ups
**Risk:** Minimal
**Status:** pending
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

- **Completed:**
- **What was done:**
- **Verification:**
- **Issues encountered:**
