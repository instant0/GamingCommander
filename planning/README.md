# Planning Documents

This folder contains implementation plans following the FILE NAMING CONTRACT (`nnn-short-name.md`; legacy 0–99 phase slots use two digits).

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
| **`131-security-and-release-integrity.md`** | Release gate, contract accuracy, persistence and repair-write reviews, supply-chain hygiene | **PROPOSED** (2026-10-29) — security-review follow-ups; Phase N, split into core/persistence/repair/Epic-client reviews |
| **`130-duplicate-feature-consolidation.md`** | Shared path rule (implemented) plus the open store-identity, locator, reader, writer, policy and stat decisions | **PARTIAL** (2026-10-29) — `PathText` landed; the remaining decisions are taken with their consumers |
| **`132-persistence-write-integrity.md`** | One JSON store, four-state read result, `.bak`/`.corrupt` recovery, app-root-only directory creation | **PROPOSED** (2026-10-29) — contract settled; implementation is the safe-local-state-writes work |
| **`133-repair-write-safety.md`** | Steam creates a missing ACF (create-only), Epic modifies an existing `.item` (refuses to create); staged generate → copy → length check, backup for replace | **DECIDED** (2026-10-29) — implementation is the guarded-repair-writes work |
| **`135-io-call-audit.md`** | Inventory of redundant registry and filesystem calls, split by read/write, with the removals made | **APPLIED** (2026-10-31) — 23 redundant probes removed; audit result recorded |
| **`129-store-capabilities-and-launcher-discovery.md`** | Store capability matrix + launcher-driven inventory import proposal | **PROPOSED** (2026-10-25) — matrix published; implementation split into Phase M |
| **`128-doc-and-task-audit.md`** | Documentation/task-backlog audit: corrections + small-task backlog | **COMPLETE** (2026-10-25) — hygiene wave + O9 done; SteamEmu decided |
| **`126-epic-manifest-review-pass.md`** + `126-facts-*.md` | Epic orphan `.item` registration review + EOSH gate reverse-engineering (DISCOVERY) | **INVESTIGATION CLOSED** (2026-09-10) — no file-based registration path; EOSH reporting is server/account-driven (`.egi` plant falsified). Product follow-ups (EpicItemWriter `.mancpn` AppName; `epic_item_format.md` correction) **DEFERRED**. Optional RE tail remains in `126-facts-eosh-re.md`. |
| **`123-detection-bugfixes.md`** | Detection tightening: confidence tiers (Locked/Secure), Steam locked, Epic manifest Secure, Missing-Manifest fix, safe-first | **SUBSTANTIALLY COMPLETE** (verified 2026-10-25) — Python phases 0–4 done; **C# port shipped 2026-09-07** (`docs/detection/07-csharp-parity.md`, 10 parity tests); confidence tiers shipped and wired; E6a (`"tool"`) and fallback-depth (`AppConfig.ScanDepth`) closed. Only final documentation refresh + human review-gate sign-off remain. Scope: `d:\games`; P-layout = §8.3 separate plan |
| **`127-persistence-v2.md`** | Typed game records + detection provenance (tier/signals), identity, file ownership, tracking/customization; A/B/C proposals | **IN PROGRESS** (2026-10-14) — A1–A6 + B7–B13 done (composed `GameEntry`; bag retired; tier/provenance; schema versioning; identity reconciliation; sidecar ownership; tracking/install/user-state; launch profiles; relations; collections; typed tags); C deferred |
| **`125-stabilization-refactor.md`** | Code architecture refactor: split oversized files, dedup composition/projection, settle GameEntryId contract | **COMPLETE** (2026-09-09) — all phases + final stage; 564 tests green; Phase 3 dropped (N/A). Predecessor: `124` COMPLETE |
| `122-type-to-search.md` | Silent type-to-search: 3+ chars filters all roots by name + tags (frees S/T keys) | **COMPLETE** (2026-08-23) |
| **`04-phase-2-syncmove.md`** | SyncMove — repair store registration after user moves files | PLANNED (Phase 2.1) |
| `102-tags-metadata-display.md` | Tags + local engine | Phases 1+2+4 ✅; Phase 3 → **119/120 COMPLETE** |
| `103-detect-py-port-status.md` | C# vs detect.py parity gaps | **SUPERSEDED / REFERENCE** — use `docs/detection/07-csharp-parity.md` and `docs/detection/09-blueprint.md`; retained as historical parity audit |
| `104-detect-py-module-split.md` | Split detect.py into modules | PLANNED (P3) |
| `101-top-level-modes-and-filter.md` | Mode switcher + flatten + category folders | PLANNED. **F8/S filter shipped** (tags/stores/wildcard; not this plan’s full UI) |
| `04-phase-2-metadata-lookup.md` | Historical Phase 2.2 (Cargo/SteamDB/IGDB) | **SUPERSEDED** by `119-metadata-sidecar.md` + `120-pcgw-identity-and-details.md` |
| `04-phase-2.md` | Steam & standalone baseline | **Baseline COMPLETE**; leftover = SyncMove |
| `97-multi-theme-system.md` | Runtime theme switching | PLANNED |
| `111-logging-toggle-readme-metadata.md` | Logging toggle / readme metadata | **PARTIAL** (2026-10-25) — Task 1 open; Tasks 2–3 shipped |
| `110-user-tags-source-tagging.md` | User tags + override protection | **PARTIAL** — Phases 1–5 shipped; future phases remain |

---

## Recently completed (post-MVP)

| Document | Description | Status |
| :--- | :--- | :--- |
| `124-anchor-library-game-db.md` | Anchor-based Library + Game DB split (`libraries.json` + flat `games.json`) | **COMPLETE** (2026-09-08) — `Library`, `ILibrariesService`, `LibrariesDatabaseService`; `GameRoot`/`LibraryRoot` removed — [completion record](../META/COMPLETED/plan-124-anchor-library-game-db.md) |
| `121-epic-manifest-vfs-investigation.md` | Epic catalog VFS (F2 Add Epic Games Store) + Missing/Orphaned + orphan `.item` write | **COMPLETE** (orphan `.item` write limit now proven by 126 — see plan) |
| `120-pcgw-identity-and-details.md` | PCGW details sidecar + F3/F4 extras | **COMPLETE** |
| `119-metadata-sidecar.md` | `games_metadata.json` + Steam Store / PCGW | **COMPLETE** |
| `117-left-pane-layout.md` | Left pane: badge left, tags as subtitle, path right | **COMPLETE** |
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
| `05-phase-3-category-browse.md` | Category browse | **SUPERSEDED/STALE** (2026-10-25) — do not implement; design retained as historical reference |
| `06-phase-4.md` | Advanced polish stub | **Archived stub** — split into concrete plans |
| `90-sdk-upgrade.md` | .NET 8 → .NET 9 | **Deferred indefinitely** |

---

## Task trackers (META)

| Path | Notes |
| :--- | :--- |
| `META/TASKS/COMPLETED/phase-h-mvp/STATUS.md` | MVP — complete (archived) |
| `META/TASKS/phase-i-post-mvp/STATUS.md` | Post-MVP mode/filter tasks |
| `META/TASKS/phase-j-followups/STATUS.md` | Deferred follow-ups; detection tier complete; SyncMove deferred |
| `META/TASKS/phase-k-audit-followups/STATUS.md` | Docs/state hygiene — complete |
| `META/TASKS/phase-l-detection-followups/STATUS.md` | Steam vs SteamEmu complete; later detector work |
| `META/TASKS/phase-m-store-capabilities/STATUS.md` | Store matrix, launcher discovery, inventory import, health |

---

## Naming convention

```
nnn-short-name.md          — FILE NAMING CONTRACT
  Legacy 0–99 slots use two digits — 00-overview.md, 01-phase-0.md … 06-…,
  04-phase-2-<feature>.md, 90–99-*.md
  100+                     — Post-phase numbered plans
```
