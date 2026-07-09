# AGENTS.md

Repository guidance for human contributors and coding agents.

---

## Repository Rules

Read this file before making repository changes.

The purpose of this file is to define stable operating rules. Project progress, current tasks, and changing decisions belong in `planning/`.

---

## Critical Operating Rules

### Direct Implementation

* Use direct repository tools for implementation.
* Do not use external planning systems or hidden task trackers.
* Keep changes visible and reviewable.

### Compact Output

Keep responses concise.

After a successful build and test cycle:

`Build clean, continuing with <next task>`

Do not provide large summaries unless explicitly requested.

### After Context Compaction

After any context reset or compaction, provide only:

1. Current state
2. Operating constraints
3. Next step
4. Confirmation prompt

Do not dump entire plans, file trees, or repository summaries.

---

## Privacy and Repository Boundaries

Only access files inside:

`/home/malware/projects/gamingCommander`

Do not inspect external paths unless explicitly authorized.

Do not disclose:

* local machine paths,
* game library locations,
* generated launcher data,
* private test data.

---

## Project Intent

GamingCommander is a C# Windows-native game management and launcher application.

Goals:

* discover installed games,
* collect technical metadata,
* launch games safely,
* support migration between locations,
* support multiple game platforms.

Primary targets:

* Standalone games
* Steam
* Future support:

  * GOG Galaxy
  * Epic Games Store
  * EA App
  * Ubisoft Connect

---

## Technical Direction

Language:

* C#

Platform:

* Windows

UI:

* Retro Norton Commander-inspired interface.
* Resizable and adaptive.
* Do not assume fixed terminal dimensions.

Source control:

* Git + GitHub

---

## Architecture Principles

1. Keep launcher integrations isolated behind interfaces.
2. Separate:

   * UI
   * domain logic
   * detection
   * migration
   * platform integrations
3. Prefer read-only detection.
4. Treat migration as safety-critical.
5. Avoid launcher-specific assumptions leaking into core models.
6. Prefer explicit dependencies over hidden global state.
7. Prefer immutable data models where practical.

---

## Coding Standards

* Target modern .NET unless a compatibility reason exists.
* Enable nullable reference types.
* Use descriptive names.
* Keep methods small and focused.
* Avoid unsafe shortcuts.
* Avoid duplicated logic.
* Prefer simple, testable components.
* Perform a sanity check after every change:

  * What could break?
  * Are prerequisites validated?
  * Are side effects explicit?

---

## Change Workflow

For each implementation task:

1. Read:

   * `planning/CURRENT.md`
   * `planning/ARCHITECTURE.md`
   * relevant phase document
   * relevant research documents

2. Implement the requested change.

3. Update:

   * `planning/CURRENT.md`
   * relevant planning documents if milestones changed.

4. Refactor:

   * keep files focused,
   * avoid duplicated logic,
   * extract complex responsibilities.

5. Build and test.

---

## Safety Rules

Never modify game platform data without:

* validating paths,
* preserving originals where required,
* logging operations,
* ensuring recovery is possible.

Always distinguish:

* move only,
* move with link/junction,
* metadata repair.

---

## Testing Expectations

Prefer tests for:

* launcher detection,
* manifest parsing,
* metadata normalization,
* migration flows.

Use:

* deterministic tests,
* fixtures,
* temporary directories.

Avoid unnecessary network-dependent tests.

---

## Documentation

Update documentation when adding:

* launcher support,
* migration behavior,
* configuration formats,
* user workflows,
* architectural decisions.

---

## Planning and Project Memory

Before making changes, read documents in this order:

1. `planning/CURRENT.md`
   - Current milestone
   - Active task
   - Session handoff

2. `planning/ARCHITECTURE.md`
   - Stable design decisions
   - Constraints
   - Established patterns

3. Relevant `planning/phase-*.md`
   - Current roadmap item
   - Acceptance criteria

4. Relevant research documents
   - External platform details
   - Technical references

Do not read every planning document unless the current task requires it.
