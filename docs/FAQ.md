# GamingCommander — FAQ

**Nature:** User questions and answers. Grounded in shipped behavior (0.4.x).
**Audience:** End users. The essentials (install, first run, rescan, uninstall) are mirrored in `GamingCommander.Readme.txt`, which ships with the exe; this file carries the full detail.
**Updated:** 2026-08-23

If code and this file disagree, fix the code or this file — do not leave both.
Related: [`ONLINE-AND-DATA.md`](ONLINE-AND-DATA.md) (network/file contract), [`../GamingCommander.Readme.txt`](../GamingCommander.Readme.txt) (access guide).

---

## Installation

**Q: How do I install GamingCommander?**
There is no installer, no MSIX, no setup program. Installation is: unpack the ZIP into a new folder. That's it. Uninstallation is: delete that folder.

**Q: Where should it live?**
Any folder your user account can **write to** — GamingCommander keeps all of its data (`data/settings.json`, `games.json`, cache, log) next to the exe, inside its own folder. Do **not** put it in `C:\Program Files` unless you enjoy permission errors; use something like `C:\Tools\GamingCommander` or `D:\GamingCommander`. It creates no shortcuts, services, drivers, or registry entries anywhere else.

## Requirements

| | |
|--|--|
| OS | Windows 10 (1607+) or Windows 11, x64. Windows Server on x64 generally works but is not a test target. |
| Architecture | x64 only. There is no native ARM64 build (an ARM PC runs it under Windows' x64 emulation — untested). |
| .NET runtime | The released ZIP is **self-contained**: the .NET 8 runtime ships inside the `lib/` folder. You do not need to install anything. (A build produced with `--self-contained false`, as shown in the GitHub README for development, instead needs the .NET 8 Desktop Runtime.) |
| Admin rights | Not required. GamingCommander never requests elevation. |
| Internet | Optional, off by default. Only used for online metadata (PCGW + Steam Store) if you enable it in F2. |

## First-run setup

On first launch (and after an upgrade — see *Data migration*) a setup dialog opens:

1. Click **Add folder** and pick a folder that contains games.
   - For Steam, pick the **library root** — the folder that *contains* `steamapps\`, e.g. `D:\SteamLibrary`, not `D:\SteamLibrary\steamapps\common`.
   - For everything else, pick the parent folder whose subfolders are the games (e.g. `D:\GOG Games`).
2. Each root gets a platform default (auto-guessed from the path; you can change it per game later with F4).
3. The folder is scanned immediately (background thread; the dialog shows progress). A root with **0 games found is rejected** and not added.
4. Nesting is checked: a root may not sit inside another configured root (either direction). You'll get a clear message if it does.
5. Close the dialog. The main window lists your roots; Enter drills in, Enter on a game launches it.

**What happens if I select a very large directory?**
Scanning reads only one level of subfolders looking for per-store signal files, then does a bounded executable search inside candidate folders (see *Scanning behavior*). Cost grows with the number of top-level folders, not total disk size. Pointing it at `C:\` or `C:\Program Files` will take noticeably longer and mostly finds nothing useful — prefer dedicated game folders. The scan runs off the UI thread; press F5 again to cancel.

## Scanning behavior

**How deep does scanning go?**

- **FolderScanner (everything except Steam libraries):** examines the immediate child folders of your root. Inside a detected game it searches executables at the game root, direct children, `Binaries\Win64|Win32|WinGDK|Steam\`, and recursively at most **2 levels deep**. It does not walk entire game trees.
- **Steam roots:** driven by `steamapps\*.acf` manifests plus `libraryfolders.vdf` for additional libraries — no blind tree walk at all.
- **Epic Manifests root:** reads `*.item` files from the ProgramData manifests folder.

**What is excluded?**
Hidden-folder names from settings, known noise directories (redist, installers, docs, licenses, store-launcher folders like `Epic Games`/`Origin`/`Battle.net`, backup copies), nested Steam trees (`steamapps\common` structures or a folder literally named `steam`) unless added explicitly as a Steam root, and executables matching the noise blacklist in `data/blacklist.json` (installers, updaters, anti-cheat setup, editors, tools…).

**How are false-positive .exe files told apart from real games?**
Candidate exes are scored: filename tokens shared with the folder name score high, `-Win64-Shipping.exe` scores highest, generic names (`game.exe`) score well; launchers/updaters/uninstallers/org-copies score negative; file size is a minor factor. When several plausible candidates remain, the entry stores them and the details pane says *"Multiple EXE files detected — press F4 to choose"*; your choice sticks and survives rescans. Store manifests (GOG `.info`, Epic `.item`, EA logs…) override guessing entirely when present.

## Performance

There are no official benchmark numbers. Qualitatively: a normal root (tens of folders) scans in seconds; hundreds of game folders stay manageable because per-folder work is bounded; the practical slow case is pointing the scanner at huge non-game trees. Rescans run sequentially per root, asynchronously, cancellable. With thousands of games the catalogue itself is a plain JSON list — loading is fast, but expect the initial scan and the optional online-metadata queue (one game at a time, rate-limited) to take a while.

## Rescanning

Rescans are **manual**. There is no filesystem watcher and no automatic background scan.

- **F5** rescans the current root — or all roots when you're at the root level. Press F5 again to cancel.
- **F2** has a per-root Rescan button.
- Adding a root scans it once.
- Startup does **not** rescan; it loads `games.json` from last time.

So: newly installed or moved games appear after the next F5, not by themselves.

## Duplicate games

The same game found under two different roots appears twice — entries are identified by root path + folder name, and there is deliberately no cross-root deduplication (the nesting rules prevent the common overlap cases). Within Steam, the ACF cross-reference across all discovered Steam libraries means a game moved between libraries updates its status (Installed/Moved) instead of duplicating.

## Uninstall / removal of a game

Removing a game from disk does **not** remove it from the catalogue automatically — a rescan keeps entries it can't find anymore (by design: drives can be temporarily unavailable). What you'll see:

- **Steam:** status becomes *Missing* (files gone from every known library) or *Orphaned*, depending on what remains.
- **Everything else:** the entry stays until you remove it — open **F4** on the game and delete the entry, or remove/re-add the root in F2.

## Portable / moved installation

Because all state lives in `data/` next to the exe, moving the **whole GamingCommander folder** (exe + `data/` + `lib/`) to a new location is safe: nothing is registered elsewhere, no absolute self-paths are stored. Your library roots are absolute paths to your *games*, so they keep working. If you move only the exe without `data/`, the app starts fresh with an empty configuration next to the new location — copy `data/` along to avoid that.

## Data migration / upgrades

Upgrading: unzip the new version into the existing folder (keep `data/`). On first start after a version increase the setup dialog re-opens (showing your existing roots — just close it) and the new version is recorded in `settings.json`. Game and setting files are plain JSON read tolerantly; unknown fields are ignored. A **corrupt or unreadable JSON file silently resets to defaults** (empty catalogue/config) rather than crashing — keep backups (below).

## Backup / recovery

Yes — copying `data/` is a complete backup of your configuration, catalogue, tags, overrides, and metadata cache. Restore by closing the app and copying the folder back. Nothing outside `data/` needs backing up (the shipped defaults `blacklist.json`/`tag_colors.json` are restored from embedded copies if missing/corrupt).

## Error handling

- **Malformed/incomplete launcher manifests** (ACF, `.item`, GOG `.info`, EA logs): the parser returns "nothing usable" and detection falls back to the next source — folder name, exe discovery, registry keys. A broken manifest degrades gracefully; it doesn't crash the scan.
- **Locked files:** reads that fail are treated as "not there"; scanning continues.
- **GamingCommander's own files:** unreadable/corrupt JSON → defaults (see *Data migration*).

## Permissions failures

Filesystem access problems during scanning are swallowed silently: a denied folder simply yields no games. At add-time this surfaces as the root being rejected with "0 games found". There is currently **no explicit "access denied" dialog** — if a folder you know contains games comes back empty, suspect NTFS permissions and run F5 after fixing access.

## Unusual game installations

- **Network shares / mapped drives:** work as ordinary paths while reachable. UNC paths (`\\server\share\…`) can be roots, and wiki config/save links pointing at UNC are displayed but never clickable.
- **External / removable drives:** fine while mounted. An unavailable drive is skipped on rescan; its catalogue entries are retained until you remove them.
- **Junctions/symlinks:** followed like normal directories during scanning.
- **Nonstandard launcher locations:** detection is based on files *inside* the game folder, not on install location, so it is location-independent; EA/Ubisoft/GOG/Rockstar additionally have HKLM registry fallback for signal-less installs.

## Launching behavior

Enter resolves the launch target: Steam games with a run URI open `steam://rungameid/{id}` (Steam must be running; it owns overlay/cloud and extra PCGW flags are **not** passed). Everything else starts `ExecutablePath` with combined arguments, with the **working directory set to the exe's folder** automatically. Launches use shell execution, so a game that requests admin rights triggers the normal Windows UAC prompt.

If the exe was moved or deleted, Windows refuses the start and the status bar shows `Launch failed: …`; if no target is known at all you get `No launch target for …`. Fix either with F4 (pick the correct exe). Launching never touches game files.

## Command-line arguments

Sources, in order of authority: store manifests (e.g. GOG `.info` launch args) → resolved `.lnk` shortcuts → your manual edits. PCGamingWiki contributes a **catalog of known flags** shown in F4 as checkboxes; ticking them adds them to `ExtraLaunchArguments`. Everything is visible and editable in **F4** (command line + extras), and the details pane displays the composed line. At launch, `CommandLineArguments` and `ExtraLaunchArguments` are combined; for Steam URI launches arguments are intentionally ignored.

## Online metadata failure

With lookup enabled, a single connectivity probe (HEAD to pcgamingwiki.com, 15 s timeout) decides the session: **any** response = Online; none = Offline, sticky for the whole run. Later PCGW/Steam failures (timeout, 5xx, connection error) also flip the chip to red Offline and stop the queue. Effects on you: the details pane just shows less (offline facts already cached remain), and **a failed fetch never wipes or replaces an existing sidecar row**, nor can a mismatched wiki page overwrite a good one (identity guard). Retry any time with F3 once you're back online. With the F2 checkbox off, nothing ever contacts the network.

## Caching

Online extras live in `data/games_metadata.json`, one row per game with a `LastUpdated` timestamp. A row younger than **60 days** is considered fresh and is not refetched (triggers that would refresh: F3 force lookup, F5 rescan queue, F4 background, launch). Older rows are **kept forever** until a successful fetch overwrites them — nothing expires destructively. To force a refresh for one game: select it and press F3.

## Deletion / reset

To reset GamingCommander to a clean state: exit the app and delete the JSON files in `data/` (or the whole `data/` folder — it is recreated on next start). Deleting `games.json` clears the catalogue; `settings.json` clears roots/preferences and re-runs first-run; `games_metadata.json` clears the extras cache. Deleting the entire installation folder is the nuclear option and removes everything.

## Uninstall

Uninstall = delete the GamingCommander folder. That removes the program, settings, catalogue, cache, and logs in one step — the app created nothing outside its folder (no registry writes, no services, no Start Menu entries). One exception to remember: if you used **Write Steam ACF** (user-started `appmanifest_{id}.acf` in your Steam library), those files live in *your* Steam library and are not removed by uninstalling GamingCommander.

## Keyboard / UI reference

Also available in-app under **F1**.

| Key | Action |
|-----|--------|
| Up / Down | Move selection |
| Enter | Launch game / drill into folder (double-click works too) |
| Esc / Backspace | Go up one level (also clears the active filter) |
| F1 | Help — this list |
| F2 | Library Setup — add/remove/rescan roots, Enable online metadata |
| F3 | Fetch online extras (Steam AppID → one PCGW page; otherwise pick if several match) |
| F4 | Configure game — name, type, choose exe, argument checkboxes, delete entry |
| F5 | Rescan current root or all roots (press again to cancel) |
| F8 / S | Filter by tag, store label, or wildcard; Backspace/Clear/`..` restores the list |
| F10 | Quit |

There is no F9; Esc/Backspace handles going up.

## Troubleshooting

Startup diagnostics go to `data/startup.log` (disable with environment variable `GC_STARTUP_LOGGING=0`).

| Symptom | Check |
|---------|-------|
| App doesn't start / blank window | `data/startup.log` — last lines name the failing step |
| Root added but no games found | Right root level? (Steam: folder containing `steamapps\`.) Permissions readable? Nested-inside-another-root rejection message in F2? |
| Game listed but won't launch | Status bar message; F4 → verify exe path; Steam game → is Steam running? |
| Wrong exe chosen | Details pane "Multiple EXE…" hint → F4 → pick the real game exe |
| Wrong display name / type | F4 → rename, or change the source type (your override survives rescans) |
| Details pane empty, chip yellow | Online metadata disabled — enable in F2 |
| Chip red Offline | No route to pcgamingwiki.com at startup, or a later request failed; fix connectivity, restart or rely on cache; F3 retries per game when green |
| Filter seems stuck | Backspace, Esc, Clear button, or `..` |
| Games vanished from list | Did `data/games.json` get deleted/corrupt (resets silently)? Restore from backup |
| Old games still listed after uninstalling them | Expected — remove via F4 or rescan won't drop them (see *Uninstall/removal*) |

## Known limitations

Things the scanner **intentionally** does not do:

- No automatic change detection — no watcher, rescans are manual (F5).
- No cross-root deduplication; same game under two roots lists twice.
- Nested Steam libraries are skipped by the general scanner and must be added as their own Steam root.
- Store **clients** are not integrated: no downloads, updates, cloud, or friend lists — detection and launching only. Full GOG/Epic/EA/Ubisoft client APIs are out of scope.
- No migration/manifest-repair yet (Phase 2.1 SyncMove is planned, not shipped); "Moved" Steam games are reported, not fixed.
- Xbox titles have the thinnest metadata (signal-file detection only).
- Games that consist solely of blacklisted-pattern exes (or no exe at all) are not detected; heavily renamed/modded setups may need F4 correction.
- x64 Windows only; no installer, no auto-update.

## Contributing / development

Requirements: .NET 8 SDK. From Linux or Windows:

```bash
dotnet build
dotnet test
dotnet list GamingCommander.sln package --vulnerable --include-transitive   # must report none vulnerable
```

Windows Release publish (framework-dependent; see `publish.sh` for the self-contained release flow):

```bash
dotnet publish src/GamingCommander.App/GamingCommander.App.csproj -c Release -r win-x64 --self-contained false -o ./publish
```

Linux covers build/tests; native UI, registry reading, real launcher installs, and packaging need a Windows machine. See [`CONTRIBUTING.md`](../CONTRIBUTING.md) and [`AGENTS.md`](../AGENTS.md) for repository conventions. Bug reports: open a GitHub issue with your `data/startup.log` excerpt, the affected game folder *structure* (names, not contents), and what you expected vs. happened — never paste personal paths you don't want public.
