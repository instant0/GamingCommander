# Plan 100 — MVP Next Steps (Minimum Viable Working Product)

**Status:** COMPLETE — all work packages delivered  
**Audience:** OpenCode / BigConda ZEN / any Builder agent  
**Priority:** P0 product completeness (not more code-quality churn)  
**Estimated effort:** ~2–3 focused sessions  
**Depends on:** Phase 2 detection + UI already present

---

## 0. State of the World (read once)

### What already works (do not rebuild)

| Area | Status |
|------|--------|
| Dual-pane NC UI, wizard, F2 library setup, F4 game edit | Working |
| VFS over `games.json` | Working |
| Steam scan: ACF + libraryfolders.vdf, Installed/Moved/Orphaned/Missing | Working |
| Standalone multi-store signals (GOG/EA/Ubi/Epic/Blizzard/Xbox/Rockstar/SteamEmu) | Working (C#) |
| Exe scoring + JSON blacklist tiers | Working |
| Theme centralization, keyboard F-keys | Working |
| Tests | ~99 passing |
| Python research tool `tools/detect.py` | Advanced reference (~1829 LOC); **not** the product |

### What is *not* MVP yet

1. **Launch is incomplete / wrong for Steam** — critical bug  
2. **Detection parity: Python `detect.py` > C# scanners** for several real cases  
3. **Clean first-run persistence** (default config/db beside exe)  
4. **Steam SyncMove repair** (optional stretch; not required for “launch games”)  
5. Online metadata, multi-theme, GOG/Epic full clients — **out of MVP**

### Doc / plan health

| Doc | Health |
|-----|--------|
| `META/SESSION/CURRENT.md` / `NEXT.md` | Accurate but **over-focused on Phase G quality** |
| `META/ROADMAP.md` | Phase 2 ACTIVE; migration still open |
| `planning/04-phase-2.md` | Stale checkboxes (Steam/standalone already done) |
| `planning/04-phase-2-syncmove.md` | Good design; post-MVP or stretch |
| `planning/04-phase-2-metadata-lookup.md` | Research-ready; **not MVP** |
| Phase G tasks T48–T57 | Useful later; **do not block MVP** |
| `tools/detect.py` | Gold reference; needs module split later (not MVP) |
| Deprecated: `detect_folder.py`, `list_standalone_games.py` | Keep until C# parity proven |

### Critical product bug (fix first)

```
SteamLibraryScanner stores:  CommandLineArguments = "steam://rungameid/{appid}"
ShellViewModel sets:         LaunchTarget = game.ExecutablePath   // WRONG for Steam URI
MainWindow launches:         Process.Start(LaunchTarget)          // never sees steam://
CommandLineArguments:        never passed to Process.Start
LauncherPath:                never used
```

**Result:** Steam “launch” may start a raw `.exe` (or nothing) instead of the Steam client protocol. Standalone games never get stored launch args (e.g. GOG SCUMMVM).

---

## 1. MVP Definition (acceptance criteria)

A Windows user can:

1. **Install / run** the published app (or `dotnet run`) on Windows.  
2. **First run:** wizard appears → add at least one Steam library root and one standalone games folder.  
3. **Scan:** games appear under each root with correct source labels; Steam statuses color correctly.  
4. **Browse:** dual-pane VFS navigation (F9 roots, Enter drill-in, Esc/Back up).  
5. **Launch Steam game:** Enter/F5 uses `steam://rungameid/{appid}` when AppId is known.  
6. **Launch standalone:** Enter/F5 starts primary `.exe` with working directory = exe folder; optional args applied.  
7. **Edit game (F4):** change display name / source / exe path / args → persisted in `games.json`.  
8. **Rescan (F6 / library setup):** refreshes DB without wiping user overrides where designed.  
9. **No crash** on missing folders, empty roots, or games with no exe (status message only).  
10. **`dotnet build` + `dotnet test`** green on Linux CI; Windows smoke via `docs/windows-validation-checklist.md` (UI/launch/detection sections).

**Explicitly out of MVP**

- SyncMove / ACF patching  
- PCGamingWiki / online metadata  
- F3 metadata view, F8 categories, S search  
- Multi-theme runtime switch  
- Full GOG/Epic/EA/Ubi *client* integration  
- `detect.py` module split  
- Phase G T48–T57 polish  
- .NET 9 upgrade  

---

## 2. Execution Protocol (every agent)

```
1. Read AGENTS.md → META/RULES.md → META/SESSION/CURRENT.md → THIS FILE
2. Work ONE work package (WP) at a time
3. Small, reviewable diffs; no drive-by refactors
4. After each WP: dotnet build && dotnet test
5. Update META/SESSION/CURRENT.md (what done / blockers)
6. Mark WP checkboxes here
7. Do NOT start Phase G / themes / metadata until MVP gate passes
```

**Rules**

- Prefer C# product path (`src/GamingCommander.App/Services/*`). Python tools are reference only.  
- Port logic from `tools/detect.py` when C# is weaker — do not call Python from the app.  
- Silent `catch { }` is OK only for pure probes; user-facing operations must set `StatusText` or log.  
- Privacy: do not hardcode user library paths in tests or docs.  

---

## 3. Work Packages (implement in order)

### WP-0 — Session re-aim (5 min, docs only)

- [ ] Overwrite `META/SESSION/NEXT.md` to point at this plan as the only active track.  
- [ ] Note in `META/SESSION/CURRENT.md`: **MVP track active; Phase G deferred.**  
- [ ] Optionally add one line to `planning/README.md` Active Plans table for `100-mvp-next-steps.md`.

**Done when:** next agent cannot confuse “write more tests” with “ship MVP.”

---

### WP-1 — Fix launch pipeline (P0, ~1–2 h)

**Files (expected):**

- `src/GamingCommander.UI/ViewModels/ShellViewModel.cs` — `LoadGamesForRoot`  
- `src/GamingCommander.App/MainWindow.axaml.cs` — `LaunchSelectedGameAsync`  
- `src/GamingCommander.App/Services/SteamLibraryScanner.cs` — how URI is stored (optional cleanup)  
- Tests under `tests/GamingCommander.App.Tests/` (new or extended)

**Requirements**

1. **Resolve LaunchTarget** when building list items:

   | Priority | Condition | LaunchTarget |
   |----------|-----------|--------------|
   | 1 | `CommandLineArguments` starts with `steam://` | that URI |
   | 2 | non-empty `LauncherPath` and user/policy prefers launcher | `LauncherPath` (MVP: only if ExecutablePath empty) |
   | 3 | else | `ExecutablePath` |

2. **Pass args** for non-URI launches:

   ```csharp
   // Pseudocode
   if (target is steam:// or other URI)
     Process.Start(UseShellExecute=true, FileName=target);
   else
     Process.Start(
       FileName = target,
       Arguments = game.CommandLineArguments,  // if not a URI
       WorkingDirectory = Path.GetDirectoryName(target),
       UseShellExecute = true);
   ```

3. Shell list items must carry enough data: either keep looking up `GameEntry` by `GameId` on launch, or add `CommandLineArguments` / `LaunchKind` to `ShellPaneItemViewModel`. **Prefer lookup by GameId** from `ILibraryManager` / DB to avoid model sprawl.

4. **Missing / no-exe games:** do not call Process.Start; status: `"No launch target for {title}"`.

5. **Steam without AppId** but with exe: fall back to exe path.

**Tests**

- [ ] Unit/helper test: resolve target prefers `steam://` over exe.  
- [ ] Unit test: non-URI includes args.  
- [ ] Unit test: empty target → no throw.

**Done when:** Steam games launch via protocol; standalone via exe+args; tests green.

---

### WP-2 — First-run & persistence hygiene (P0, ~45 min)

**Files:**

- `JsonConfigService` / `GamesDatabaseService` / `App.axaml.cs`  
- Tech debt: “Default settings/games files not created alongside exe”

**Requirements**

1. On first run, ensure `data/` under BaseDirectory exists.  
2. If `settings.json` / `games.json` missing, create valid empty defaults (same schema as Load already expects).  
3. Blacklist still loads from shipped `data/blacklist.json` (copy on publish already expected).  
4. Status bar messages stay user-actionable (`Press F2…`).

**Tests**

- [ ] Load on missing files creates defaults and does not throw.  
- [ ] Second Load returns same empty roots.

**Done when:** clean publish folder + first launch works without manual file seeding.

---

### WP-3 — C# detection parity (critical gaps only) (P0, ~2–4 h)

**Source of truth for behavior:** `tools/detect.py` + `planning/99-detection-hardening.md`  
**Product code:** `FolderScanner`, `ExecutableDiscovery`, `StoreSignalDetector`, `SteamLibraryScanner`

Port **only** what breaks real libraries. Order:

| # | Gap | C# target | detect.py reference |
|---|-----|-----------|---------------------|
| 3.1 | GOG `goggame-*.info` → display name + `playTasks` exe + args | `FolderScanner` / small `GogInfoParser` | `_extract_gog_metadata` |
| 3.2 | UE-aware exe paths (`*/Binaries/Win64`, Win32, Steam) | `ExecutableDiscovery` | UE-aware discovery |
| 3.3 | `.lnk` → target exe name (best-effort, no COM required if byte parse works) | `ExecutableDiscovery` | `_parse_lnk_exe_name` |
| 3.4 | Container recursion: pure standalone children under publisher folders not dropped | `FolderScanner` container pass | container=`True` path |
| 3.5 | Prefer launcher vs game exe only when clearly a launcher (keep scoring; document) | `ExecutableDiscovery.ScoreExecutable` | scoring table in plan 99 |

**Do not** in this WP:

- FFXIV multi-folder merge (unless trivial once 3.4 works)  
- PE FileDescription scoring (nice-to-have)  
- Python refactor  

**Tests (fixtures under temp dirs, no real game paths)**

- [ ] GOG: fake `goggame-123.info` with playTasks → ExecutablePath + CommandLineArguments set.  
- [ ] UE layout: only `Game/Binaries/Win64/Game.exe` → selected.  
- [ ] Container: `Publisher/GameA/` with store signal → entry for GameA, not only Publisher.  
- [ ] Existing scanner tests still pass.

**Done when:** mock + synthetic fixtures cover 3.1–3.4; build/test green.

---

### WP-4 — Launch UX polish (P1, ~1 h)

**Files:** `MainWindow.axaml` / `.cs`, command bar, `HelpDialogBuilder`

**Requirements**

1. Command bar buttons that already map to keys (F1, F2, F4, F5, F6, F9, F10) should be **clickable** (`IsHitTestVisible=true` + same handlers as keys). Decorative-only is tech debt.  
2. Help text matches actual behavior (no “F5 Launch” if wiring differs).  
3. After launch failure, keep selection; show error in status bar (already mostly true).  
4. Optional: confirm when launching “Missing” Steam games (status only is fine for MVP).

**Done when:** mouse + keyboard both launch and open setup; help is accurate.

---

### WP-5 — Windows smoke gate (P0 validation, human + agent)

On a Windows machine with real or mock libraries:

```
[ ] App starts, wizard or root list appears
[ ] Add Steam root → games list populates
[ ] Add standalone root → multi-store games appear
[ ] Enter/F5 launches Steam game (Steam client reacts)
[ ] Enter/F5 launches standalone exe
[ ] F4 edit exe/args → relaunch uses new values
[ ] F6 rescan after folder change
[ ] Missing/Moved Steam entries show red/yellow
[ ] No unhandled exceptions in status/startup.log
```

Record results in `META/SESSION/CURRENT.md` (no private paths).

**Done when:** checklist above is checked; agent marks MVP **READY**.

---

### WP-6 — Stretch only if WP-1…5 done (optional)

1. **Steam SyncMove dry-run** for Moved games — implement *plan only* dry-run + backup path from `planning/04-phase-2-syncmove.md` (Steam ACF only). No Epic/GOG repair.  
2. **Cross-library Steam dedup** in `ScanAll` if duplicates appear in UI.  
3. Replace silent catches in scanners with debug log (T52) — quality, not feature.

Stop if any stretch risks MVP stability.

---

## 4. Suggested task file layout (optional)

If the agent uses `META/TASKS/`, create:

```
META/TASKS/phase-h-mvp/
  T61-fix-launch-pipeline.md      # WP-1
  T62-first-run-defaults.md        # WP-2
  T63-detection-parity-gog-ue.md   # WP-3.1–3.2
  T64-detection-parity-lnk-container.md  # WP-3.3–3.4
  T65-command-bar-clickable.md     # WP-4
  T66-windows-smoke-gate.md        # WP-5
```

Each task file: objective, files, acceptance, test commands — follow `META/TASKS/TEMPLATE/task-template.md`.

---

## 5. Build / test commands

```bash
dotnet build
dotnet test --no-build   # or: dotnet test
```

Windows publish smoke (when validating):

```bash
dotnet publish src/GamingCommander.App/GamingCommander.App.csproj -c Release -r win-x64 --self-contained false -o ./publish
```

Python reference only (never required for product build):

```bash
python tools/detect.py /path/to/games --log detect.log
```

---

## 6. Stop conditions / escalation

| Situation | Action |
|-----------|--------|
| Launch works but one exotic game mis-detects | Log as TECH_DEBT; do not block MVP |
| Need COM for .lnk and byte parse fails | Ship without .lnk; document fallback |
| SyncMove scope creep | Defer to Phase 2.1 plan |
| Test count pressure | Prefer 5 targeted tests over Phase G bulk |
| Unsure product behavior | Ask user; do not invent multi-launcher clients |

---

## 7. After MVP (ordered backlog)

1. Phase G remaining tests/quality (T48–T57) — harden what you shipped  
2. Steam SyncMove real repair (backup + ACF path fix)  
3. Port remaining detect.py edges; then split `detect.py` modules  
4. PCGamingWiki metadata (`planning/04-phase-2-metadata-lookup.md`)  
5. Category browse / search  
6. Themes, .NET 9  

---

## 8. One-page agent checklist

```
[x] WP-0  Session points at this plan
[x] WP-1  LaunchTarget + args + steam://   ← DONE (T61, T62)
[x] WP-2  Default settings/games.json      ← DONE (T64)
[x] WP-3  GOG info, UE paths, lnk, containers ← DONE (T65, T66, T67, T68, T68C)
[x] WP-4  Clickable F-keys / accurate help ← DONE (T69, T71)
[x] WP-5  Windows smoke gate              ← DONE (T70, T75, T76, T77)
[x] Update CURRENT.md: MVP READY          ← DONE
[ ] Only then: stretch or Phase G
```

---

**Planner note:** Recent sessions optimized internal structure (Phases D–G). That was valuable, but the product gap is **launch correctness + detection parity + clean first run**. This plan deliberately starves non-MVP work until the acceptance criteria in §1 pass.
