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

Each launcher detector (Steam, Epic, GOG, EA, Ubisoft, Battle.net, Xbox) must be isolated behind `ILauncher` interface.

### Future Metadata Pipeline

```
PCGamingWiki → SteamDB → Steam Store → IGDB → games_db.json
```

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
