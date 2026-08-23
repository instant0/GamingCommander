# META/ARCHITECTURE.md — Stable Architecture

**Nature:** Reference. Append-only. New decisions added at bottom.
**Audience:** Builder, Planner, Reviewer. Read before implementing new features.

---

## Core Design Goals

GamingCommander separates:
- UI presentation
- domain logic
- launcher integrations
- detection
- migration
- persistence

The core application must not depend on specific launcher implementations.

---

## Architecture Decision Records

| ID | Title | Date | Status |
|----|-------|------|--------|
| ADR-001 | Avalonia UI Framework | 2025-01-15 | Accepted |
| ADR-002 | Virtual File System Model | 2025-02-10 | Accepted |
| ADR-003 | Two-Level Game Classification | 2025-03-01 | Accepted |
| ADR-004 | Read-Only Detection | 2025-01-20 | Accepted |
| ADR-005 | Migration Safety | 2025-01-20 | Accepted |
| ADR-006 | Norton Commander UI Direction | 2025-01-15 | Accepted |
| ADR-007 | Game Metadata Normalization | 2025-02-10 | Accepted |
| ADR-008 | Isolated Launcher Integrations | 2025-01-15 | Accepted |
| ADR-009 | Executable Detection Pipeline | 2025-03-15 | Accepted |
| ADR-010 | Local JSON Persistence | 2025-03-01 | Accepted |
| ADR-011 | Cross-Platform-Safe Development | 2025-01-10 | Accepted |

Detailed records: `META/ADR/`

---

## Current Architecture Overview

### Navigation Flow

```
F9
↓
Library Roots
↓
Games from data/games.json
↓
Game Details
```

### Virtual Filesystem Model

The real filesystem is only touched during Setup/Rescan. Normal navigation operates over a virtual filesystem stored in `data/games.json`.

- **Top level** (F9): Lists configured library roots as "drives"
- **Inside a root**: Lists games parsed from `data/games.json` — not filesystem entries
- **No real filesystem browsing during navigation** — `Browse()` reads from the virtual DB

### Solution Structure

```
GamingCommander.sln
├── src/
│   ├── GamingCommander.Core/        Interface definitions + domain models
│   ├── GamingCommander.Detection/   Game discovery abstractions + stub
│   ├── GamingCommander.Migration/   Migration planning abstractions + stub
│   ├── GamingCommander.UI/          ViewModels (Norton Commander shell)
│   └── GamingCommander.App/         Avalonia app entry, windows, services, DI wiring
└── tests/
    ├── GamingCommander.Core.Tests/
    ├── GamingCommander.Detection.Tests/
    ├── GamingCommander.Migration.Tests/
    └── GamingCommander.App.Tests/
```

**Dependency flow:** Core ← Detection ← UI ← App. Migration sits alongside Detection.

### Executable Detection Pipeline

1. Enumerate candidates
2. Apply exclusion scoring (non-game exes: anti-cheat, installers, launchers)
3. Apply positive scoring (folder-name-matching bonus)
4. Rank candidates
5. Return highest score + confidence

### Provider Detection Pipeline

Launcher detection is isolated behind `ILibraryManager`. The `LibraryManager` routes scanning to the appropriate scanner based on folder structure:

- `SteamLibraryScanner` — structural Steam detection (steamapps/common/ is definitive)
- `FolderScanner` — generic folder scanner for Standalone, GOG, EA, Ubisoft, Epic, and other sources

> **Note:** `ILauncher` (proposed in ADR-008) was retired in favor of this two-tier scanner architecture. The isolation intent is preserved — scanner implementations are decoupled from core logic.

### Live metadata pipeline (opt-in)

```
EnableOnlineMetadata + Online chip
  → PCGW (appid.php or OpenSearch → Parse wikitext)
  → Steam Store appdetails if AppID known (ACF or PCGW Availability)
  → data/games_metadata.json only
```

SteamDB, Cargo, Epic GraphQL, IGDB are **not** in the product. Contract: `docs/ONLINE-AND-DATA.md`.

### Future Migration Repair Pipeline

The app does NOT move game files. The user moves files with OS tools. The app only
repairs launcher registration after a relocation is detected.

```
User moves files → Rescan → Detect mismatch → Backup Manifest → Fix Registration → Validate
```

---

## Current Decisions

Recorded here for quick reference. Full context in ADR files.

| Topic | Decision | Rationale |
|-------|----------|-----------|
| Launcher Integration | Launcher logic isolated behind interfaces | Support multiple stores without modifying core |
| Game Metadata | Normalized domain objects (not launcher-specific) | Steam/GOG/Epic expose different data formats |
| Detection | Read-only | Scanning must not modify user data |
| Migration | Treated as safety-critical | Validate, preserve recovery info, avoid destructive actions |
| UI Direction | Norton Commander-inspired, modern resizable Windows app | Adaptive layout, keyboard-friendly, clear navigation |

---

## Current Reality (2026-08-22)

Appended after post-MVP alignment. Does not replace ADRs above.

- **Scanning lives in App**, not the Detection project. `LibraryManager` selects `SteamLibraryScanner` vs `FolderScanner`. `GamingCommander.Detection` is a deprecated stub kept so project references still compile.
- **Migration is not implemented.** `IMigrationPlanner` / `DesignTimeMigrationPlanner` produce dry-run summaries only. Real repair is Plan SyncMove (`planning/04-phase-2-syncmove.md`).
- **Multi-store detection ≠ multi-store clients.** Folder signals + registry fallback classify GOG/Epic/EA/Ubisoft/Battle.net/Rockstar. There is no GOG Galaxy / Epic / EA / Ubisoft API or repair path yet.
- **Epic local manifests shipped** (Plan 109). Online extras: PCGW + Steam Store only (`docs/ONLINE-AND-DATA.md`).
- **Rescan is F5**, async and cancellable. Enter launches. Esc/Backspace goes up. No F9.
- **Command bar** F1–F5, F8, F10 are clickable. F9 removed (redundant with Backspace).

---

## Current Reality (2026-08-22, metadata close)

Appended after Plans 119/120 shipped. Does not replace ADRs above.

- **Two JSON files:** `data/games.json` = VFS/launch; `data/games_metadata.json` = extras only. Lookup never writes DisplayName.
- **HTTP only if** F2 `EnableOnlineMetadata` **and** chip Online. Highlight a row = cache only. F3 = force lookup (picker if several PCGW pages). F5 queues after rescan. F4 does not wait.
- **PCGW first**, then Steam Store `appdetails` if an AppID is known. No SteamDB, Cargo, Epic GraphQL, IGDB.
- **Launch:** `steam://rungameid/{id}` stays a Steam URI (extras unused). Everything else = exe + `CommandLineArguments` + `ExtraLaunchArguments`. Do not invert Steam → raw exe.
- **Unreal scoring:** `Binaries\Win64\*-Win64-Shipping.exe` beats root `game.exe` / launcher stubs.

---

## Current Reality (2026-08-22, filter + scan)

- **F8 / S** filter games across every library (tags including sidecar genre/engine, store label, wildcard). Not Plan 101 category folders.
- **Steam scan** stays cheap: ACF + root exe + a few named subfolders. No EngineDetector listing. Metadata does not require an exe.
- **Publish:** exe + readme + host trio at root; DLLs in `lib/`; defaults in `data/`.
- Genre/engine from PCGW show as **tags** (engine badge type). Not written into `GameEntry.Tags`.
