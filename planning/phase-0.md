# Phase 0: Foundations & Environment

## Goal

Establish a practical architectural baseline and repository foundation for GamingCommander, with a clean split between Linux-based development work and later Windows-only validation.

## Constraints

- The current automation environment is Linux with .NET 8 SDK available.
- Native Windows build verification is expected to happen manually on Windows.
- Phase 0 should prefer portable scaffolding, documentation, and proof-of-concept code that does not lock the repository into an unverified Windows-only toolchain prematurely.

## Objectives

1. Make the repository ready for implementation.
2. Choose and document the initial UI direction.
3. Establish project boundaries for UI, core logic, detection, and migration.
4. Capture Windows validation as an explicit manual task instead of an implicit assumption.

## Work Breakdown

### 1. Framework Selection

- Compare `Terminal.Gui`, `Consolonia`, and at least one alternative desktop/terminal-hosting strategy.
- Evaluate against:
  - retro Norton Commander look,
  - adaptive resizing and layout reflow for modern window sizes,
  - Windows usability,
  - maturity and maintenance,
  - packaging practicality,
  - keyboard-first UX.
- Record a primary choice and fallback option.

### 2. Repository Foundation

- Pin the SDK with `global.json`.
- Add shared .NET defaults via `Directory.Build.props`.
- Create baseline repository directories:
  - `src/`
  - `tests/`
  - `tools/`
  - `docs/`
  - `data/`
- Add development-environment guidance and a Windows validation checklist.

### 3. Solution and Project Baseline

- Create the first solution structure after the UI direction is selected.
- Preferred initial projects:
  - `GamingCommander.App`
  - `GamingCommander.Core`
  - `GamingCommander.UI`
  - `GamingCommander.Detection`
  - `GamingCommander.Migration`
  - matching test projects for parser and migration logic.

### 4. Technical Research

- Prototype or document:
  - Steam library folder discovery,
  - Steam app manifest parsing,
  - stand-alone executable detection heuristics,
  - Windows registry abstraction,
  - migration preflight and backup strategy.
- Capture follow-up notes for Epic, GOG, EA, and Ubisoft.

### 5. Proof of Concept

- Add a minimal bootstrap once framework choice is made.
- Keep proof-of-concept scope tight:
  - shell window,
  - basic layout skeleton with resize-aware pane behavior,
  - placeholder service wiring,
  - no real launcher mutation yet.

## Deliverables

- [x] Repository guidance and hygiene files
- [x] SDK pinning and shared build defaults
- [x] Repository directory scaffold
- [x] Linux/Windows workflow documentation
- [x] UI framework decision note
- [x] Solution and project scaffold
- [x] Minimal application proof-of-concept
- [x] Windows manual validation pass

## Exit Criteria

Phase 0 is complete when:

- the repository has a stable solution structure,
- the initial UI direction is documented,
- cross-platform-safe parts build in the development environment,
- Windows-specific validation tasks are explicitly documented,
- the project is ready to begin Phase 1 UI and configuration work.
