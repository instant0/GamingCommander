# AGENTS.md

Repository guidance for human contributors and coding agents.

---

## CRITICAL OPERATING RULES

These rules must always be followed, regardless of context state.

### No Plannotator
- Use direct bash/edit/write tool calls only. No LLM-based planning tools.

### Compact Output Only
- After a successful build + test cycle: state "Build clean, continuing with X" — no full summaries, no dumps, no essays.

### Post-Compact Output
After any context compact, output only:
1. **Current state** — brief summary of what was just completed
2. **Operating rules** — key constraints (no Plannotator, privacy, build→compact status)
3. **Next step** — what the plan says to do next
4. **Confirmation prompt** — "Continue with [next step]?"
- Do **not** dump full plan documents, file listings, or detailed status reports.

### Privacy
- Python helper scripts (`tools/`) and real library data are dev-only.
- Never disclose concrete library paths, game names, or Python script outputs back to the Agent.

---

## Project Intent

GamingCommander is a C# Windows-native game management and launcher application with a retro Norton Commander-inspired interface. It is intended to:

- discover games from supported launchers and stand-alone folders,
- display useful technical metadata for each game,
- launch games safely through their owning platform or executable,
- support safe game migration between folders,
- eventually sync metadata from a GitHub-hosted source.

## Current Status

This repository is in early planning/bootstrap stage.

Until implementation exists, prefer creating foundational structure over premature feature code.

## Technical Direction

- Language: C#
- Platform target: Windows
- Source control: Git + GitHub
- UI direction: retro text-mode / terminal-inspired UX with Windows-native packaging and behavior
- UI must be resizable and adaptive to window width/height; emulate the Norton Commander style, not a fixed 80-column DOS constraint
- Initial functional scope: stand-alone games + Steam first
- Follow-up launchers: GOG Galaxy, Epic Games Store, EA App, Ubisoft Connect

## Architecture Principles

1. Keep store integrations isolated behind explicit interfaces.
2. Separate UI, domain logic, detection, and migration logic into distinct projects.
3. Prefer read-only detection logic; mutation paths must be explicit and heavily validated.
4. Treat migration as a safety-critical feature:
   - preflight validation,
   - dry-run support,
   - rollback metadata/logging where feasible,
   - no destructive deletes without a validated replacement path.
5. Avoid launcher-specific assumptions leaking into the core domain model.

## Suggested Solution Layout

When the solution is created, prefer something close to:

```text
src/
  GamingCommander.App/
  GamingCommander.Core/
  GamingCommander.UI/
  GamingCommander.Detection/
  GamingCommander.Launchers.Steam/
  GamingCommander.Launchers.Gog/
  GamingCommander.Launchers.Epic/
  GamingCommander.Launchers.EA/
  GamingCommander.Launchers.Ubisoft/
  GamingCommander.Migration/
tests/
  GamingCommander.Core.Tests/
  GamingCommander.Detection.Tests/
  GamingCommander.Migration.Tests/
tools/
docs/
data/
```

## Coding Rules

- Target modern .NET unless a concrete compatibility constraint requires otherwise.
- Enable nullable reference types.
- Enable implicit usings only if used consistently across the solution.
- Prefer explicit, descriptive names over abbreviations.
- Keep methods small and side effects obvious.
- Do not suppress type or compiler errors with unsafe shortcuts.
- Avoid hidden global state.
- Prefer immutable records/value objects for metadata where practical.

## Workflow — Change Discipline

After every code change set, follow this sequence:

1. **Log intent** — briefly describe what changed and why.
2. **Do code changes** — implement.
3. **Update plan documentation** — reflect changes in `.sisyphus/plans/`.
4. **Refactoring pass** — after every plan update, review the affected files:
   - No file should exceed ~300 lines. Extract logical groups into helper classes or static utility classes.
   - No inline block should be duplicated in the same file. Extract shared logic into a method.
   - No method should do more than one thing. Split when the cognitive load is high.
   - Prefer small, single-responsibility files that an Agent can read and reason about in one shot.
5. **Build + test** — verify clean compile and all tests pass before considering the change done.

## Safety Rules for Game Operations

- Never modify store manifests without:
  - backing up the original file,
  - validating the target path,
  - logging the operation.
- Never create symlinks or junctions silently.
- Always distinguish between:
  - move only,
  - move + symlink/junction,
  - manifest-only repair.
- File operations must be resumable or recoverable where possible.

## Testing Expectations

- Unit test parsing logic for manifests, registry readers, and metadata normalization.
- Integration test migration flows with temp directories and fake manifests.
- Add fixture-based tests for Steam and future launcher metadata.
- Prefer deterministic tests without network access unless explicitly marked.

## Logging & Diagnostics

- All launcher detection failures should be non-fatal and diagnosable.
- Log enough context to debug filesystem/registry issues without leaking sensitive data.
- Migration operations should emit step-by-step logs.

## Documentation Expectations

When adding major features, update or add docs for:

- store detection behavior,
- migration behavior and warnings,
- config format,
- user-visible hotkeys and workflows.

## AI/Automation Guidance

- Read this file before making repo changes.
- Prefer minimal, reviewable diffs.
- Match existing patterns once code exists.
- If the repository is still mostly empty, establish clean defaults instead of speculative abstractions.
- Do not commit secrets, local machine paths, or generated launcher data caches.

## Files Likely To Stay Untracked

- `bin/`, `obj/`
- `.vs/`, `.idea/`, `.vscode/`
- local game scan outputs
- temp migration logs
- machine-specific launcher path snapshots
- AI scratch notes and private transcripts
