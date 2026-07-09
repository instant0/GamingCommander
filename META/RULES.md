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

## Change Workflow

For each implementation task:

1. Read: `AGENTS.md` → `META/SESSION/CURRENT.md` → `META/SESSION/NEXT.md` → relevant planning doc
2. Implement the requested change.
3. Update: `META/SESSION/CURRENT.md` at end of session.
4. Refactor: keep files focused, avoid duplicated logic, extract complex responsibilities.
5. Build and test.

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

## After Context Compaction

After any context reset or compaction, provide only:

1. Current state
2. Operating constraints
3. Next step
4. Confirmation prompt

Do not dump entire plans, file trees, or repository summaries.
