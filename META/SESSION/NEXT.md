# META/SESSION/NEXT.md — Next Action

**Nature:** Scratch. **Overwritten** every session handoff.
**Audience:** Builder. Read before implementing.

---

## ACTIVE TRACK: MVP (Minimum Viable Working Product)

**Canonical plan:** [`planning/100-mvp-next-steps.md`](../../planning/100-mvp-next-steps.md)
**Task tracker:** [`META/TASKS/phase-h-mvp/STATUS.md`](../../META/TASKS/phase-h-mvp/STATUS.md)

**Do this next (in order):**

| WP | Task | Priority |
|----|------|----------|
| **WP-1** | **Fix launch pipeline** — T61 ✅, T62 ✅, T63 cancelled | **P0 COMPLETE** |
| **WP-2** | **First-run defaults** — T64 ✅ | **P0 COMPLETE** |
| **WP-3** | **C# detection parity** — T65 ✅, T66 ✅, T67 ✅, T68 ✅ | **P0 COMPLETE** |
| WP-4 | Launch UX polish — T69 | P1 |
| WP-5 | Windows smoke gate — T70 | P0 gate |

**Deferred (do not start until MVP gate):**

- Phase G T48–T57 (tests/quality polish)
- Multi-theme (`97-multi-theme-system.md`)
- PCGamingWiki metadata (`04-phase-2-metadata-lookup.md`)
- SyncMove full repair (`04-phase-2-syncmove.md`) — stretch only after WP-5
- `detect.py` module split
- .NET 9 upgrade

---

## Critical bug (WP-1)

```
Steam stores:  CommandLineArguments = "steam://rungameid/{appid}"
UI sets:       LaunchTarget = ExecutablePath   // wrong
Launch uses:   Process.Start(LaunchTarget)     // never steam://, never args
```

Fix in `ShellViewModel.LoadGamesForRoot` + `MainWindow.LaunchSelectedGameAsync` (prefer resolve by GameId from DB).

---

## Agent protocol

1. Read `AGENTS.md` → `META/RULES.md` → `CURRENT.md` → **this file** → `planning/100-mvp-next-steps.md`
2. Implement one WP at a time
3. `dotnet build && dotnet test` after each WP
4. Update `META/SESSION/CURRENT.md`
5. Check off WP boxes in plan 100

---

## Recently completed (context only)

- Phases D–G partial: complexity reduction, stabilization, 99 tests, theme extraction, VFS display, detect.py unified, detection hardening (Python)
- T58–T60 naming/docs/noise consolidation done
- T61+T62: Launch pipeline fixed — Steam URI resolution, args passthrough, status bar with args
- T63: Cancelled (trivial logic, not worth Avalonia test infrastructure)
- T64: First-run config defaults fixed — `IsFirstRun` now true when settings.json missing; 3 new tests (102 total)
- T65: GOG .info parser — title/exe/args extraction, DLC filtering, relative path resolution, FolderScanner integration; 10 new tests (112 total)
- T68: Container recursion & organization detection — UE3 fast path, all UE platforms, org folder detection, non-game filtering; 13 new tests (120 total)
- T65b/T71 planned — title/exe selection dialogs, F5 removal
- Phase G T48–T57 still pending — **not MVP blockers**
