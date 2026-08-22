# Planning Documents

This folder contains implementation plans following the FILE NAMING CONTRACT (`nn-short-name.md`).

**Project memory, session state, roadmap, and architecture live in `META/`.**

| Doc | Purpose |
|-----|---------|
| `META/SESSION/CURRENT.md` | Current project state |
| `META/SESSION/NEXT.md` | Next concrete action |
| `META/ROADMAP.md` | High-level roadmap |
| `META/ARCHITECTURE.md` | Stable architecture |
| `META/BACKLOG/TECH_DEBT.md` | Known issues |

---

## Execute next (product)

| Document | Description | Status |
| :--- | :--- | :--- |
| **`117-left-pane-layout.md`** | Left pane: badge left, tags as subtitle, path right | **DRAFT — recommended next product work** |
| `04-phase-2-syncmove.md` | SyncMove — repair store registration after user moves files | PLANNED (Phase 2.1) |
| `102-tags-metadata-display.md` | Tags + metadata system | Phase 1+4 ✅; Phases 2–3 pending |
| `103-detect-py-port-status.md` | C# vs detect.py parity gaps | REFERENCE / gaps remain |
| `104-detect-py-module-split.md` | Split detect.py into modules | PLANNED (P3) |
| `101-top-level-modes-and-filter.md` | Category browse / search (F8, S) | PLANNED (P3) |
| `04-phase-2-metadata-lookup.md` | F4 metadata lookup (PCGW etc.) | PLANNED |
| `04-phase-2.md` | Steam & standalone baseline | **Baseline COMPLETE**; leftover = SyncMove |
| `97-multi-theme-system.md` | Runtime theme switching | PLANNED |
| `111-logging-toggle-readme-metadata.md` | Logging toggle / readme metadata | PLANNED |
| `110-user-tags-source-tagging.md` | User tags + override protection | Partial (tags model shipped) |

---

## Recently completed (post-MVP)

| Document | Description | Status |
| :--- | :--- | :--- |
| — | Bugs 10 / 13a / 13b / 16 (Steam internals, nested Steam, embedded blacklist) | **COMPLETE** (no numbered plan) |
| `118-doc-audit-refactor.md` | Governance drift: TECH_DEBT, session, indexes, archives | **COMPLETE** |
| `116-battlenet-detection-enhancement.md` | BattleNet signal-file-only detection | COMPLETE |
| `115-ea-ubisoft-registry-fallback.md` | Registry fallback EA/Ubisoft/GOG/Rockstar | COMPLETE |
| `114-detection-bugfixes.md` | Live-testing bugs B23–B33 | COMPLETE |
| `113-async-background-scanning.md` | F5 async scan + cancel | COMPLETE |
| `112-scan-perf-naming-ubisoft.md` | Blacklist DTO, PE names, Ubisoft signals | COMPLETE |
| `109-epic-manifest-enrichment.md` | Epic .item/.mancpn + global cross-ref | COMPLETE (API deferred) |
| `108-steam-status-messages.md` | Orphaned/Missing/Moved guidance | COMPLETE |
| `106-unified-setup-screen.md` | Wizard + F2 → LibrarySetupWindow | COMPLETE |
| `105-f6-crash-fix-and-f5-rescan.md` | Rescan crash fixes; F6→F5 | COMPLETE |
| `107-battle-net-detection-fix.md` | BattleNet noise-filter regression | COMPLETE (superseded by 116) |
| `100-mvp-next-steps.md` | MVP ship plan | **COMPLETE** |

---

## Completed — earlier phases

| Document | Description | Status |
| :--- | :--- | :--- |
| `00-overview.md` | Historical overview | Superseded by `META/ROADMAP.md` |
| `01-phase-0.md` | Phase 0 foundations | COMPLETE |
| `02-phase-1.md` | Phase 1.0 core UI | COMPLETE |
| `03-phase-1-ui-polish.md` | Phase 1.1 | COMPLETE |
| `03-phase-1-research.md` | Phase 1.2 research | COMPLETE |
| `10-phase-d-complexity-reduction.md` | Complexity reduction | COMPLETE |
| `11-phase-e-stabilization.md` | Stabilization + tests | COMPLETE |
| `91-user-blacklist-editor.md` | User blacklist editor proposal | PLANNED / backlog |
| `92-keyboard-layout-proposal.md` | Keyboard layout | COMPLETE (implemented) |
| `93-in-memory-cache-and-wizard-versioning.md` | VFS cache + wizard versioning | COMPLETE |
| `94-game-detection-overhaul.md` | Detection rewrite | COMPLETE |
| `95-bugfix-and-cleanup.md` | Bugfix / cleanup | COMPLETE |
| `96-vfs-display-enhancements.md` | Missing/cross-library/list color | COMPLETE |
| `98-unified-detection-tool.md` | tools/detect.py unified | COMPLETE |
| `99-detection-hardening.md` | Detection hardening (Python+) | Partial / COMPLETE core |
| `99-stabilization.md` | Phase 1.1a | COMPLETE |
| `998-readme-update-v1.md` | Readme rewrite | COMPLETE |

---

## Stubs / deferred / superseded

| Document | Description | Status |
| :--- | :--- | :--- |
| `05-phase-3.md` | Multi-launcher stub | **Archived stub** — use research docs + Plans 109/115 |
| `05-phase-3-category-browse.md` | Category browse | PLANNED — see also Plan 101 |
| `06-phase-4.md` | Advanced polish stub | **Archived stub** — split into concrete plans |
| `90-sdk-upgrade.md` | .NET 8 → .NET 9 | **Deferred indefinitely** |

---

## Task trackers (META)

| Path | Notes |
| :--- | :--- |
| `META/TASKS/phase-h-mvp/STATUS.md` | MVP T61–T77 — complete |
| `META/TASKS/phase-i-post-mvp/STATUS.md` | Post-MVP mode/filter tasks |

---

## Naming convention

```
00-overview.md             — Project overview (historical)
01-phase-0.md … 06-…       — Phase plans
04-phase-2-<feature>.md    — Phase 2 features
90–99-*.md                 — Cross-cutting / tooling
100+                       — Post-phase numbered plans
nnn-short-name.md          — FILE NAMING CONTRACT
```
