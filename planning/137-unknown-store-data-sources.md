# 137 — Unknown Store Data Sources: Decisions

**Status:** applied (2026-10-31).
**Scope:** EA, Ubisoft, Battle.net, Arc, and Rockstar verification. Steam and Epic are out of scope: Steam is
resolved from files (`steamapps`, ACF, vdf); Epic from the known ProgramData manifests directory.
**Inputs:** `docs/research/launcher_discovery.md`, `ea_format.md`, `ubisoft_format.md`, `blizzard_format.md`,
`rockstar_format.md`, `docs/detection/10-store-capabilities.md`, `StoreSignalDetector`,
`RegistryFallbackDetector`, `FolderScanner`, `TECH_DEBT.md` Bug 24.

Evidence classes: **verified** (in shipped code or a confirmed fixture), **documented** (research note, not
confirmed against a complete install), **assumed** (no artifact). No path is treated as verified because it
appears in a plan.

---

## 1. Summary

| Store | Existing detection (keep) | Candidate data source | Decision | Safety | Task effect |
|---|---|---|---|---|---|
| **EA** | `__Installer/`, `Touchup.exe`, `ActivationUI.exe`; per-game registry keys; `InstallLog.txt` metadata | EA Desktop install key; `installerdata.xml` per game; encrypted `IS`; legacy `.mfst` | **Detection-only.** No locator (launcher path adds no inventory). No reader (no reliable global inventory; `installerdata.xml` needs a complete-install fixture). | `Unsupported` for inventory | Cancel EA locator and EA reader |
| **Ubisoft** | `uplay_install.*`, loader DLLs, `uplay_download/`, `_UPP*.exe`; `UbisoftReadmeParser` | `Ubisoft\Launcher\Installs\{gameId}\InstallDir` registry; `uplay_install.state`; encrypted manifest | **Registry inventory reader only.** No launcher locator; no manifest/state parser yet. | `NeedsVerification` | Keep the Ubisoft registry reader; cancel the Ubisoft half of the locator task |
| **Battle.net** | `.battle.net/`, `.build.info`, game-root `.product.db`/`.patch.result` | Agent `%ProgramData%\Battle.net\Agent\product.db` (SQLite); `Battle.net.config` XML | **Detection-only.** No SQLite/WinRT dependency; no locator/reader. | `Unsupported` for inventory | Cancel Battle.net locator and reader |
| **Arc** | `arc\` launcher folder; `arc.exe` is noise (TECH_DEBT Bug 24) | none identified | **Not a store.** No `GameSourceKind`, no locator/reader. Keep as launcher noise. | `Unsupported` | No task |
| **Rockstar** | `title.rgl`, `common.rpf`, registry per-game keys | `HKCU/HKLM Rockstar Games\Launcher\InstallFolder`; `%LOCALAPPDATA%\Rockstar Games\Launcher\Settings.json` | **Locator + registry reader.** Settings.json stays unparsed until a fixture confirms it. | `NeedsVerification` for Settings; registry safe | Keep the Rockstar locator and registry reader |

---

## 2. EA

**Existing detection (keep).** `StoreSignalDetector` marks `__Installer/`, `Touchup.exe`, `ActivationUI.exe`;
`RegistryFallbackDetector` reads per-game EA keys; `EaInstallLogParser` reads `__Installer/InstallLog.txt`.
These are the classification baseline.

**Candidates.**

- EA Desktop install key: `HKLM\SOFTWARE\Electronic Arts\EA Desktop\InstallDir` — **documented**, and it only
  finds the launcher. It does not tell us which games are installed, so it adds nothing to a preview whose
  purpose is “what can we read?”.
- `__Installer/installerdata.xml` — **documented from a staged install only**. Fields observed:
  `contentID`, `gameVersion`, `gameTitle`, `runtime/launcher/filePath`. Complete-install naming and presence
  are unconfirmed.
- EA App `IS` — **encrypted**; unsupported.
- Legacy Origin `.mfst` + registry — **documented**, but no complete modern source and no fixture.

**Decision.** Detection-only.

- No locator: the launcher install path is not an inventory.
- No reader: there is no confirmed global inventory; a per-game `installerdata.xml` reader would add metadata
  but needs a complete-install fixture and is not required for the product goal.
- The preview row is `Unsupported` (“detection-only; no readable inventory in this build”).
- If a complete EA install is later inspected, a separate decision can add `installerdata.xml` metadata; it
  is not a reader that discovers installed games.

**What would change it:** a complete EA install with `installerdata.xml` plus a documented global record;
or a usable Origin `.mfst` set. Neither exists in the repository.

---

## 3. Ubisoft

**Existing detection (keep).** `uplay_install.manifest`/`.state`, `uplay_r*_loader*.dll`, `uplay_download/`,
`*_UPP*.exe`; `UbisoftReadmeParser` for title; registry per-game keys.

**Candidate inventory.** `HKLM\SOFTWARE\WOW6432Node\Ubisoft\Launcher\Installs\{gameId}\InstallDir` is already
enumerated by `RegistryFallbackDetector` for classification. Reading all such keys as an inventory is a
small step: it yields game id + install path, even when the folder is missing. It is plain registry data,
no decryption.

**Decision.**

- **Implement a minimal Ubisoft registry locator** that reports the `Installs` key as the inventory source.
  This is not a launcher executable locator; it advertises the registry inventory the preview and reader
  need.
- **Implement the registry inventory reader**, marked `NeedsVerification`: the shape is
  documented and already used by the classifier, but there is no complete-install fixture.
- Do not parse `uplay_install.manifest` (encrypted) or `uplay_install.state` yet; `state` may become a
  metadata source after a fixture.

**Tests:** mock registry with one/multiple ids, missing folder, unknown id shape; `Missing` state; no
manifest parsing.

---

## 4. Battle.net

**Existing detection (keep).** `.battle.net/`, `.build.info`, game-root `.product.db`, `.patch.result`;
product code from `.build.info`.

**Candidates.**

- Agent `%ProgramData%\Battle.net\Agent\product.db` — **documented SQLite** (`product` table with code,
  platform, installed, install_path, game_uid). Reading it requires a SQLite dependency and schema
  verification against a real Agent; not justified for classification.
- `%LOCALAPPDATA%\Battle.net\Battle.net.config` — **documented XML**, but shape/paths unconfirmed.
- Launcher install key — **documented**, but locates only the launcher, not games.

**Decision.** Detection-only.

- No locator/reader; no SQLite or XML dependency; no guess at the Agent DB.
- The preview row is `Unsupported` (“detection-only; Agent inventory not read”).
- Cancel the Battle.net locator and reader.
- If a no-dependency source is confirmed later (e.g. a stable `Battle.net.config` shape), it becomes its own
  decision.

---

## 5. Arc

**Evidence.** `TECH_DEBT.md` Bug 24: `d:\games\arc\arc.exe` was classified as a Standalone game; `"arc"` was
added to `NoiseSubDirNames`. The folder is a launcher/store directory, not a game.

**No game-level signal is known.** There is no `GameSourceKind`, no marker that identifies an *Arc-installed
game* (as opposed to the launcher), and no documented inventory.

**Decision.**

- **Arc is not a store in this build.** Do not add an enum value, locator, reader, or launcher row.
- Keep `arc`/`arc.exe` as launcher noise.
- If Arc support is requested later, it starts with a game-level signal (a marker inside an installed game
  folder) and a data source, not a name blacklist.

---

## 6. Rockstar (verification only)

**Existing detection (keep).** `title.rgl` (definitive), `common.rpf` (RAGE), registry per-game keys.

**Candidates.** `HKCU/HKLM Rockstar Games\Launcher\InstallFolder` (documented, locator only);
`%LOCALAPPDATA%\Rockstar Games\Launcher\Settings.json` (documented JSON with `installFolder`,
`gameLibraryFolders`, per-game paths); per-game registry keys (already used).

**Decision.**

- Keep the Rockstar locator with `NeedsVerification` for the registry/Settings marker.
- Keep the Rockstar registry inventory reader; it is plain registry data and already used by the classifier.
- **Do not parse `Settings.json` yet.** It is documented but no fixture confirms the exact shape; a JSON
  reader can be added as a separate metadata/inventory decision once a fixture exists.
- `title.rgl` is a detection/identity marker, not an inventory.

---

## 7. Task effects

| Task | Decision |
|---|---|
| EA/Ubisoft locator task | **EA cancelled; Ubisoft reinstated** as a registry-inventory locator for the preview. |
| EA inventory reader | **Cancel.** No confirmed readable inventory; EA stays detection-only. |
| Battle.net locator | **Cancel.** No readable inventory; no SQLite dependency. |
| Battle.net reader | **Cancel.** Same. |
| Ubisoft registry reader | **Ready**: registry inventory, `NeedsVerification`, no manifest/state parsing. |
| Rockstar locator + Xbox | **Ready**; Rockstar registry/Settings marker, Xbox unsupported. |
| Rockstar registry reader | **Ready**; registry only, Settings deferred. |
| GOG locator | Unchanged; GOG file signals are known; Galaxy path is a hint. |
| Preview/policy | Unchanged; EA/Battle.net rows show `Unsupported` via no locator. |

## 8. Open questions (recorded, not guessed)

- EA: does a complete install keep `__Installer/installerdata.xml` without the `_DiP_Staged` suffix, and is
  there a global record? No complete install in the repository.
- Ubisoft: confirm the exact `Installs\{gameId}` value shape and whether every installed game has an entry.
- Battle.net: is `Battle.net.config` stable enough to read without SQLite? No fixture.
- Rockstar: exact `Settings.json` shape; no fixture.
- Arc: is there a game-level marker at all? No evidence.

## 9. Non-goals

- No decryption, SQLite, WinRT/package API, or new dependency.
- No new `GameSourceKind` for Arc.
- No replacement of the existing file signals; discovery is additive and optional.
- No code in this document.
