# AGENTS.md — Repository Entry Point

**Read this file before making any repository changes.**

This file defines the operating system for autonomous AI agents. Project progress, current tasks, and changing decisions belong in `META/` and `PLANNING/`.

---

## Critical Operating Rules

### Direct Implementation
- Use direct repository tools for implementation.
- Do not use external planning systems or hidden task trackers.
- Keep changes visible and reviewable.

### Compact Output
Keep responses concise. After successful build and test:
```
Build clean, continuing with <next task>
```
Do not provide large summaries unless explicitly requested.

### After Context Compaction
After any context reset or compaction, provide only:
1. Current state
2. Operating constraints
3. Next step
4. Confirmation prompt

Do not dump entire plans, file trees, or repository summaries.

### Privacy
Only access files inside this repository. Do not disclose local machine paths, game library locations, generated launcher data, or private test data.

---

## Project Intent

GamingCommander is a C# Windows-native Norton Commander-style game management and launcher. It discovers installed games, collects metadata, launches games safely, and supports migration. Primary targets: Standalone games, Steam. Future: GOG, Epic, EA App, Ubisoft Connect.

---

## Agent Roles

| Role | Responsibility | Trigger |
|------|---------------|---------|
| **Planner** | Define what to build next. Create/update plans. Set NEXT.md. | Phase start, milestone complete, priority change |
| **Builder** | Implement code. Update session state. Create ADRs. | NEXT.md contains actionable task |
| **Reviewer** | Review code against architecture and rules. Log tech debt. | Builder completes implementation |
| **Researcher** | Investigate external platforms. Create research docs. | New platform support needed |
| **Librarian** | Audit documentation. Fix drift. Maintain CODE_MAP. | Periodic or drift detected |

---

## Reading Order for New Sessions

Every agent follows this protocol. Stop reading once you have enough context.

```
 1. AGENTS.md                     ─── Mandatory. Always.
 2. META/RULES.md                 ─── Mandatory. Once per model.
 3. META/SESSION/CURRENT.md       ─── Mandatory. Always.
 4. META/SESSION/NEXT.md          ─── If implementing.
 5. META/CODE_MAP.md              ─── If implementing (skip if same session).
 6. META/ARCHITECTURE.md          ─── If implementing new feature.
 7. PLANNING/<relevant-plan>.md   ─── If implementing.
 8. DOCS/RESEARCH/<relevant>.md   ─── If implementing new platform.
```

---

## Document Lifecycle (Quick Reference)

| Document | Nature | Updated By | When |
|----------|--------|-----------|------|
| AGENTS.md | Permanent | Human | Rarely |
| META/RULES.md | Permanent | Human | Rarely |
| META/ARCHITECTURE.md | Append-only | Planner/Builder | New decision |
| META/CODE_MAP.md | Reference | Builder | Code structure change |
| META/ROADMAP.md | Reference | Planner | Milestone complete |
| META/ADR/ADR-*.md | Append-only | Planner/Builder | New decision |
| META/COMPLETED/*.md | Append-only | Planner/Builder | Milestone done |
| META/BACKLOG/IDEAS.md | Append-only | Anyone | When inspired |
| META/BACKLOG/TECH_DEBT.md | Mutable | Builder/Reviewer | Bug/issue found |
| META/SESSION/CURRENT.md | **Overwrite** | Builder | End of session |
| META/SESSION/NEXT.md | **Overwrite** | Planner/Builder | Start/end session |
| PLANNING/*.md | Mutable | Planner | Planning phase |
| DOCS/RESEARCH/*.md | Read-only | Researcher | Once, on creation |

---

## Change Workflow

For each implementation task:

1. Read: AGENTS.md → META/SESSION/CURRENT.md → META/SESSION/NEXT.md → relevant PLANNING doc
2. Implement the requested change.
3. Update META/SESSION/CURRENT.md at end of session.
4. Build and test.

Detailed rules: `META/RULES.md`
