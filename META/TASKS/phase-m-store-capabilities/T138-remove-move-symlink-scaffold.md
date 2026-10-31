# Task T138: Remove the Move/Symlink Migration Scaffold

**Tier:** 5 — Removal
**Phase:** M — Store Capabilities
**Risk:** Low
**Status:** completed (2026-10-31)
**Prerequisites:** None

---

## Objective

The app never moves game files and never creates symlinks/junctions. It only repairs store
registration. The earlier `GamingCommander.Migration` scaffold (`MoveOnly`, `MoveAndLink`,
`RequiresLinkCreation`, dry-run planner) modelled a feature the product explicitly rejects and
duplicated the real repair path (`RepairPlan` / `RepairWriteGuard`). Remove it so the codebase
and docs describe only registration repair.

## What Needs to Change

### 1. Delete the Migration project and test project

**Files:**
- `src/GamingCommander.Migration/` (`IMigrationPlanner.cs`, `DesignTimeMigrationPlanner.cs`, csproj)
- `tests/GamingCommander.Migration.Tests/` (test, globals, csproj)

### 2. Delete the move/symlink models

**Files:**
- `src/GamingCommander.Core/Models/MigrationMode.cs`
- `src/GamingCommander.Core/Models/MigrationPlanSummary.cs`

### 3. Remove composition and project wiring

- `src/GamingCommander.App/App.axaml.cs`: remove `using GamingCommander.Migration;` and the
  `DesignTimeMigrationPlanner` construction.
- `src/GamingCommander.App/GamingCommander.App.csproj`: remove the project reference.
- `GamingCommander.sln`: remove both project entries, configurations, and nesting entries.

### 4. Correct the written record

- Replace the move/symlink ADR with registration-repair safety and rename the file to
  `META/ADR/005-registration-repair-safety.md`.
- Update `META/ARCHITECTURE.md`, `META/CODE_MAP.md`, `META/ROADMAP.md`, `README.md`,
  `CONTRIBUTING.md`, `docs/FAQ.md`, `META/SESSION/CURRENT.md`, and `META/SESSION/NEXT.md` where
  they described file moves or the removed project.

## Context

- `AGENTS.md` and ADR-005 already state the app does not move game files.
- The live repair path is `SteamAcfWriter`, `EpicItemWriter`, and `RepairWriteGuard`; the removed
  planner was never consumed by the UI.
- Historical completed task docs and archived plans are left as records.

## Requirements

- [x] No `GamingCommander.Migration` project, reference, or solution entry remains.
- [x] No `MigrationMode` / `MigrationPlanSummary` / `IMigrationPlanner` / `DesignTimeMigrationPlanner` in source.
- [x] The solution builds with the remaining projects.
- [x] Live docs no longer describe moving files or symlinks as a product feature.
- [x] The repair path is the only registration-write path.

## Verification

- [x] `dotnet build GamingCommander.sln` — 0 errors.
- [x] `dotnet test GamingCommander.sln` — 561 tests (Core 157, App 404), green.
- [x] `dotnet sln GamingCommander.sln list` shows 7 projects, no Migration.
- [x] `grep` finds no source reference to the removed types/project.

## Completion Notes

- **Completed:** 2026-10-31
- **What was done:** Deleted the Migration project and test project, the two Core models, the App
  project reference and composition code, and both solution entries. Rewrote and renamed ADR-005
  to registration-repair safety. Updated the live architecture, code map, roadmap, README,
  CONTRIBUTING, FAQ, and session state to describe registration repair instead of file migration.
- **Verification:** Build 0 errors; 561 tests green (Core 157, App 404); solution lists 7
  projects. Only the five pre-existing warnings remain (three `AVLN3001`, two `CS8604`).
- **Issues encountered:** `AGENTS.md` and `META/RULES.md` are human-owned and still contain
  "migration" wording; they were not edited. Historical completed task docs still reference the
  removed project and are left as records.
