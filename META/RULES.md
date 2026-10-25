# META/RULES.md — Permanent Agent Operating Rules

**Nature:** Permanent. Never auto-modified by agents.
**Audience:** All agents. Read once per model session.

---

## Coding Standards

- Target modern .NET unless a compatibility reason exists.
- Enable nullable reference types.
- Use descriptive names.
- Keep methods small and focused.
- Avoid unsafe shortcuts.
- Avoid duplicated logic.
- Prefer simple, testable components.
- Perform a sanity check after every change:

  * What could break?
  * Are prerequisites validated?
  * Are side effects explicit?

## Development Principles

- **Think Simple** — Avoid clever one-liners that obscure intent. Simple code is debuggable code.
- **Smart** — Choose the right abstraction level. Not too simple (copy-paste), not too complex (premature abstraction).
- **Modularized** — One function = one job. If you can't name it clearly, it does too much.
- **Re-use** — Extract shared logic into functions. Don't duplicate scan logic across tiers.
- **Avoid Duplicate Code** — If the same pattern appears twice, it should be a function. DRY.
- **Avoid Massive Source Files** — If a file exceeds ~500 lines, consider splitting.
- **Name Functions Correctly** — Names should describe what, not how. `_find_exe_in_subdirs` is clear. `_scan` is too vague.
- **Add Comments** — Explain WHY, not WHAT. "Why do we check for launcher?" is useful. "This loops through files" is not.
- **Avoid Overengineering** — Don't build frameworks for problems that don't exist yet.
- **Evaluate Two Perspectives** — Before implementing, ask: "What's the simple way? What's the robust way?" Then choose based on actual needs.
- **Discuss with User** — When unsure, ask. Don't guess at requirements.
- **Plan Before Edit** — Read the code, understand the flow, write the change in your head, then edit.
- **Create Before Delete** — Always have a working version before removing the old one.
- **Small, Targeted Edits** — No major rewrites without a clear plan. Document why as changes are made.

## Change Workflow

For each implementation task:

1. Read: `AGENTS.md` → `META/SESSION/CURRENT.md` → `META/SESSION/NEXT.md` → relevant planning doc
2. Implement the requested change.
3. Update: `META/SESSION/CURRENT.md` at end of session.
4. Refactor: keep files focused, avoid duplicated logic, extract complex responsibilities.
5. Build and test.

**Documentation-only sessions:** When the session is documentation-only (planning, TECH_DEBT, IDEAS, README, docs, AGENTS, META files, planning files), do NOT run `dotnet build` or `dotnet test`. Documentation has no code to verify. Do not waste tokens confirming documentation correctness with a build.

## Safety Rules

Never modify game platform data without:

- validating paths,
- preserving originals where required,
- logging operations,
- ensuring recovery is possible.

Always distinguish:

- move only,
- move with link/junction,
- metadata repair.

## Privacy Rules

- Only access files inside `/home/malware/projects/gamingCommander`.
- Do not inspect external paths unless explicitly authorized.
- Do not disclose local machine paths, game library locations, generated launcher data, or private test data.

## Testing Expectations

Prefer tests for:

- launcher detection,
- manifest parsing,
- metadata normalization,
- migration flows.

Use:

- deterministic tests,
- fixtures,
- temporary directories.

Avoid unnecessary network-dependent tests.

## Output Conventions

- Keep responses concise.
- After successful build and test: `Build clean, continuing with <next task>`
- Do not provide large summaries unless explicitly requested.

## Documentation Hygiene

- `META/SESSION/CURRENT.md` is a **slim state tracker**, not a session journal: current state, active-queue pointer, blockers, verification baseline, updated date. No feature narratives, task history, or per-change logs.
- Task descriptions live only in `META/TASKS/**`. `META/SESSION/NEXT.md` is the ordered work queue; each phase `STATUS.md` is its tracker. Those are the only places task IDs belong.
- **Do not reference task IDs, or "task X did Y", in planning docs, product docs, architecture, roadmap, code map, backlog, completed records, or CURRENT.md.** Describe the subject and its current state; the task doc is the provenance.
- Completed tasks are removed from active `STATUS.md` trackers; the task doc remains the record. Never renumber or reorder the remaining rows.

## After Context Compaction

After any context reset or compaction, provide only:

1. Current state
2. Operating constraints
3. Next step
4. Confirmation prompt

Do not dump entire plans, file trees, or repository summaries.
