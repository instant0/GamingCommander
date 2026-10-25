# Task T102: O9 — Unify Title Edition Stripping

**Tier:** 3 — Logic/Behavior
**Phase:** L — Detection Follow-ups
**Risk:** Medium
**Status:** completed
**Prerequisites:** None

## Objective

`FileSystemHelper.NormalizeDisplayName` and `TitleText.StripEdition` keep separate edition lists,
so a title can normalize one way for display and another for matching. Extract one shared
edition-token source and align both while preserving their distinct contracts.

## What Needs to Change

### 1. Shared edition source
- [ ] Inventory the two current sources:
  - `FileSystemHelper.NormalizeDisplayName` (`src/GamingCommander.App/Services/FileSystemHelper.cs`): Remastered, Definitive Edition, Enhanced Edition, Ultimate Edition, Special Edition, GOTY, generic "Edition", **substring** replacement.
  - `TitleText.StripEdition` (`src/GamingCommander.Core/Services/TitleText.cs`): Reloaded, Definitive, Complete, Enhanced, Ultimate, Deluxe, Gold, Anniversary, Game of the Year, GOTY, **anchored tail regex** (`\s*[-:]\s*...(\s+Edition)?$`).
- [ ] Add one `EditionTokens` source in `GamingCommander.Core/Services` holding the vocabulary and the two caller sets (`DisplayTokens`, `TailTokens`). This is the single source; do **not** flatten the two matching algorithms.
- [ ] Preserve current behavior: display keeps its substring set, the tail keeps its anchored set. The win is one place to add/rename an edition token.

### 2. Wire both callers
- [ ] `FileSystemHelper.NormalizeDisplayName` reads `EditionTokens.DisplayTokens` instead of inline literals.
- [ ] `TitleText.EditionTail` builds its regex from `EditionTokens.TailTokens` (escape + longest-first ordering).
- [ ] Do not change `TitleSource` semantics or any persisted field.

### 3. Tests
- [ ] `TitleTextTests`: each tail token is stripped at a `-`/`:` end; a mid-title token is **not** stripped by `StripEdition`; idempotence.
- [ ] Display tests: each `DisplayTokens` entry is removed from a folder name; existing behavior unchanged.
- [ ] A pin test asserts the two caller sets are deliberate (adding a token is a one-place change).
- [ ] Existing display/scanner title tests stay green.
- [ ] Add a regression test proving a store/title-detected title still resolves (`TitleSource` contract).

## Context

- `docs/detection/09-blueprint.md` §14, item **O9** (P1).
- Detection behavior must stay stable; this is a consolidation, not a new heuristic.
- Python parity: no `tools/detect.py` change expected; run the matrix to prove it.

## Requirements

- [ ] One `EditionTokens` source exists; both callers read from it.
- [ ] Matching semantics are unchanged (display substring vs anchored tail).
- [ ] No persisted schema or `TitleSource` change.
- [ ] Title/scanner tests added or extended.

## Verification

- [ ] `dotnet build GamingCommander.sln` — 0 warnings / 0 errors.
- [ ] `dotnet test GamingCommander.sln` — all pass (≥607).
- [ ] `python3 tools/validate_probe_matrix.py` — ALL SCENARIOS PASS (22/22).
- [ ] `grep -rn "Remastered\|Reloaded" src/` shows only `EditionTokens` (plus tests).

## Completion Notes

- **Completed:** 2026-10-25
- **What was done:** Added `EditionTokens` in `GamingCommander.Core/Services` with the two deliberate
  sets: `DisplayTokens` (specific editions before generic "Edition") and `TailTokens` (search-only
  vocabulary). `FileSystemHelper.NormalizeDisplayName` now iterates `DisplayTokens` with ordinal
  substring removal; `TitleText.EditionTail` builds its anchored `-`/`:` tail regex from escaped
  `TailTokens` sorted longest-first. Matching behavior is unchanged. Added tests for every tail token
  at `-`/`:`, mid-title non-stripping, idempotence, every display token, pinned caller sets, and store
  `LookupName`/`SearchQueries` regression coverage. Updated blueprint §14 O9 row and note.
- **Verification:** `dotnet build GamingCommander.sln` — 0 errors (3 pre-existing Avalonia AVLN3001
  warnings in untouched AXAML; no new warnings). `dotnet test GamingCommander.sln` — 641 passed,
  0 failed (Core 194, App 446, Migration 1). `validate_probe_matrix.py` — ALL SCENARIOS PASS (22/22).
  `grep -rn "Remastered\|Reloaded" src/` — only `EditionTokens.cs`.
- **Issues encountered:** None affecting behavior. The three build warnings are pre-existing Avalonia
  XAML loader warnings unrelated to this change.
