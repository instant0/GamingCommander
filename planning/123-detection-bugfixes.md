# Plan 123: Detection Tightening — confidence tiers, safe-identifications-first

**Created:** 2026-08-29
**Priority:** P1
**Status:** PLANNED — analysis-first, Python-first; C# changes only after validated experiments
**Source:** User live-smoke-test on `d:\games` (9 reported failures) + code trace
**Revision:** 2026-09-06 — strategy update: explicit confidence tiers (`Locked`/`Secure`/`Candidate`/`Unknown`), Steam locked + ACF deferred to a follow-up, Epic manifest treated as Secure, safe identifications resolved before generic tightening. Method, symptoms (S1–S9), and experiments (E1–E8) unchanged from the 2026-08-29 revision.

---

## 0. Strategy

Tighten identification in a fixed order so that what already works stays working, and so every
non-authoritative decision is explainable. The order of work is **safe-first**:

1. **Lock the already-correct paths** (Steam structural path; see §0.1).
2. **Declare and validate Secure paths** (Epic manifest; see §0.2).
3. **Fix Missing Epic Manifest identifications** (the known gap; see §0.2).
4. **Tighten the generic scanner** (folders, containers, executables — the main false-positive sources; Phases 1–4).
5. **PE-scan + cloud lookup** as the *last* fallback for obscure titles only (E7, last).

### 0.1 Confidence tiers

Every entry carries an identification tier. Tiers are **derived from existing signals**, not a new
independent field unless one is needed; the natural carrier is `PlatformMetadata` (e.g.
`IdentificationTier` / `TitleSource` / `EpicStatus` / `SteamAppId`).

| Tier | Meaning | Example signals |
|------|---------|-----------------|
| **Locked** | Authoritative from a unique, unambiguous signal; automation must not rewrite without explicit user action | Steam `steamapps/common/<folder>`; Epic global `.item` matched to `InstallLocation` |
| **Secure** | Strong local signal, high confidence; title/exe may still be enriched but only with stronger evidence | Epic local `.item`; `.mancpn`; other store markers (GOG `.info`, EA install log, Ubisoft manifest) |
| **Candidate** | Plausible but unverified; eligible for PE/cloud enrichment | Root exe, engine layout, container child with signals |
| **Unknown / review** | Ambiguous; hidden by default or surfaced for manual configuration | No signal, generic/no exe, conflicting signals |

Rules that bind the tiers:

- A **Locked** entry is never silently rewritten by a rescan or a lookup. Only an explicit user
  edit (already tracked via `UserOverrides`) may change it.
- A **Secure** entry keeps its store source and identifiers; only a higher-tier source may upgrade
  its title.
- A **Candidate** may be enriched, but enrichment is guarded (see Phase 2 experiments) and recorded
  via `TitleSource`.
- **Unknown** entries are not emitted as confident titles; they go to review.

### 0.2 Locked and Secure paths

**Steam — LOCKED and working.** `steamapps/common/<GameFolder>` is a unique structural path. Steam
detection is considered complete and must not be touched by this plan. Steam ACF issues
(mismatch, missing, moved, cross-library) are a **separate follow-up** — see §8.1.

**Epic manifest — SECURE when the files are present.** The manifest scan (global `.item`
cross-reference, local `.item`, `.mancpn`) is authoritative when it finds files at the expected
locations:

- Global `.item` matched to `InstallLocation` → **Locked** source + title.
- Local `.item` → **Secure** source + identifiers; strong title candidate.
- `.mancpn` only → **Secure** source + catalog identifiers; title not locked.
- `.egstore`/`.egsstore` only → **Secure** "Epic installation" signal; title/exe unresolved.

**Missing Epic Manifest — the next fix.** The known gap: an Epic folder exists (`.egstore`) but has
no matching ProgramData `.item` and no local `.item`/`.mancpn`. This is the "Orphaned" case already
modeled in Plan 121. The fix must identify these orphans and recover identity from `.mancpn` scraps,
binary `.manifest` (`tools/decode_manifest.py`), or a store lookup — never inventing catalog UUIDs.
Reference: `planning/121-epic-manifest-vfs-investigation.md` (I1–I8) and `docs/research/epic_item_format.md`.

### 0.3 Analysis path

The single ordered decision chain. Each rule states what it finds, what it filters out, and where it
sends the folder next. This is the canonical path both scanners must follow (and what `--probe`
must render).

```text
1. User override?            → preserve; stop (never overwrite)
2. Steam structural path?    → LOCKED Steam (source + location); stop generic detection
   (ACF metadata = follow-up, not this path)
3. Epic manifest scan?       → files found at expected location = SECURE/LOCKED Epic
   → no files = Orphaned Epic → identity recovery (0.2), then continue
4. Other store signal?       → SECURE source; classify, then continue to exe/title
5. Registry fallback?        → SECURE source (EA/Ubisoft/GOG/Rockstar)
6. Non-game folder filter?   → reject data/platform/tool/redist/launcher folders
7. Container analysis?       → if no self-signal: check children for game signals
   → recurse only into children with real evidence; reject data-only children
8. Executable discovery?     → reject installers/tools/copies/cracks/redist
   → score remaining candidates; record WHY each was kept/rejected
9. Fallback/engine layout?   → root exe, .lnk, Unreal/UE layout (Candidate)
10. Title selection?         → store/manifest title → guarded PE description
    → exe stem → normalized display name → folder name (folder is evidence, never authority)
11. Unknown?                 → review, no confident title
12. PE-scan + cloud lookup?  → LAST resort, obscure titles only (E7)
```

## 1. Problem statement

Live testing of `d:\games` reported 9 failures. They are treated as **symptoms to analyze**, not confirmed root causes:

| # | Symptom | Suspected area (unverified until Phase 1/2) |
|---|---------|---------------------------------------------|
| S1 | A folder containing 17 game subfolders produced zero entries | Container/launcher-tree handling; store typing |
| S2 | `arc-install\neverwinter_en\neverwinter.exe` not detected | Directory-pattern matching; nesting depth; store typing |
| S3 | ELEX-style game becomes "system" (exe lives in a subfolder) | Container decision when exe-bearing child exists |
| S4 | Penumbra exe only in `redist` never found | Exe discovery fallback paths |
| S5 | mmxl keeps folder acronym as title ("Might and Magic X Legacy.exe") | PE-title enrichment guard |
| S6 | Lookups search folder name only (pigs, mmxl, endless, monkeyisland, neverwinter, jag2) | Identity query pipeline |
| S7 | Picked PCGW title never applied to DisplayName | F3 pick persistence |
| S8 | `dungeonoftheendless.exe` lists as "Endless"; huge wrong lookup list | Query source + exact-match key handling |
| S9 | `ja2.exe` not titled "Jagged Alliance 2" | PE-title guard + identity pipeline |

**Product requirement (user):** GamingCommander must work on **any** system. The FolderScanner, exe scanner, PE scanner, and store scanner must be correct generically. No hardcoded drive paths, no hardcoded store-folder names (e.g. `epic games`) as detection rules, no speculative removal of filters we intentionally added.

The most critical false-positive sources (from the user's last test) are **exe detection** and
**folder-name-as-title**. Both are addressed by the analysis path (§0.3) and Phases 1–4 below, in
that priority order.

## 2. Method: analysis-first, Python-first

Order of work is strict:

1. **Realign** `tools/detect.py` with the C# gamescan code — find what each has that the other lacks (both directions).
2. **Measure** both scanners against a scenario corpus (fixtures + the sample EXE list the user copied to `testdata/samples/d-games.txt`).
3. **Experiment** in Python only: test theories and scoring changes against the corpus.
4. **Port** only the changes that passed corpus acceptance — to C# with unit tests.

No behavior change (Python or C#) before the Phase 1 divergence report exists.

## 3. Phases

### Phase 0 — Lock & secure the already-correct paths (declaration + validation, no speculative change)

1. **Steam:** confirm `steamapps/common/<folder>` is the sole Steam signal and that no generic
   detection runs on Steam roots. No code change expected — this is a **declaration**; record any
   gap found as backlog. ACF work is explicitly out of scope (see §8.1).
2. **Epic:** validate the manifest tiers in §0.2 against fixtures (valid global `.item`, valid local
   `.item`, `.mancpn` only, `.egstore` only, missing/incomplete/DLC/path-mismatch). Map each to
   Locked/Secure/Orphaned. Confirm no Epic entry with a matched manifest can be demoted by generic
   scoring.
3. **Missing Epic Manifest:** enumerate the orphan cases (Epic folder, no ProgramData `.item`, no
   local `.item`/`.mancpn`) and define the recovery order (`.mancpn` scraps → binary `.manifest` via
   `tools/decode_manifest.py` → store lookup; never invent UUIDs). This is the direct follow-on to
   Plan 121 I4/I5/I7.

### Phase 1 — Realignment & baseline (analysis only; zero behavior change)

1. **Parity audit (both directions).** Compare `tools/detect.py` (`scan_directory`/`_scan`, `_is_non_game_folder`, `_find_game_executables`, `_pick_primary_executable`, `_read_pe_metadata`, `_build_name_candidates`, `_pcgw_lookup`, `_match_markers`) against C# (`FolderScanner`, `ContainerScanner`, `ExecutableDiscovery`, `StoreSignalDetector`, `FallbackSignalDetector`, `FolderScanner` PE enrichment, `TitleText`, `PcgwTitleFilter`). Produce a divergence table: C#-only features, Python-only features, and confirmed drifts. Update `planning/103-detect-py-port-status.md` with the result.
2. **Probe diagnostics.** Add `--probe <folder>` to `detect.py`: per-folder decision chain (signals checked and results, container decisions with reasons, exe candidates + scores, PE title captured, name-candidate list), rendered against the §0.3 analysis path.
3. **Scenario corpus.**
   - Task: `testdata/samples/d-games.txt` — the user-provided sample **EXE file list** from the live test library. **Do not read/eyeball it now**; it is harness input for scripted comparisons (builder writes the harness; data stays a fixture). Since it is an exe list, we do **not** need to scan every exe on disk — the list itself generates the scenario patterns (see task 3a).
   - Complement with existing `testdata/mock/` trees + new fixture trees derived from corpus findings.
3a. **Sample-list structure analysis (scripted, from the list alone).** Parse `d-games.txt` and emit a structural profile:
   - Per-path exe counts and stores: how many games per container folder, nesting-depth histogram (root-level, one-level-below, two-level-below).
   - Exe-in-subfolder patterns: exes under `redist`/`system`/`bin`/`Binaries`/platform dirs — which folders need selective visits versus which resolve from path+exe patterns alone.
   - Exe-stem naming patterns: acronym vs full-name stems, roman-numeral stems, `_en`/locale suffixes, multi-exe-per-folder families.
   - Noise ratio: how many listed exes match existing blacklist tiers (measure, don't assume) — folders whose only exes are noise need no deep visit.
   - Output becomes the scenario pattern catalog feeding E1–E8 experiments.
4. **Selective folder visits (analysis only).** Use the profile from 3a to decide which folders need physical probing (PE metadata reads, store-signal checks) to validate scenarios — not "scan every exe", and no behavior change to the scanners yet.
5. **Baseline runs.** Run Python and C# scanners over the corpus; emit a divergence report: missed entries, extra (false-positive) entries, wrong primary-exe picks, wrong titles, wrong store types — per case S1–S9 and overall.
6. **Findings doc + gate.** Write the divergence report to `META/BACKLOG/` or a planning annex. Stop: no fixes designed until this report is reviewed.

### Phase 2 — Scenario experiments (Python only; each gated on acceptance)

Each experiment states a hypothesis, measures before/after against the corpus (recall gained, false positives introduced), and is only accepted if it improves or holds both metrics. Ordered **safe-first**: folder/exe false-positive reduction before identity/cloud.

- **E4 — Engine-subfolder parent decision (S3).** When a child has the only exe and that exe scores strongly against the **parent** folder name, promote the parent, not the child. Scoring-driven, name-agnostic; validate against engine-layout games in the corpus (no hardcoded `system`/`bin` names).
- **E1 — Organizer-container generalization (S1/S2).** Hypothesis: any folder with no self-signals but ≥N game-signal children is an organizer; recurse into children — name-agnostic, no store-name lists. Measure which currently-skipped folder shapes the generic signal path would correctly recover, and what noise/filters are actually blocking them (measure before proposing any filter change).
- **E2 — Store typing via child signals + global manifests (S1/S2).** Type children by their own signals (`.item`/`.mancpn` cross-ref to ProgramData for Epic, ACF for Steam, `.build.info` for BattleNet, etc.). No path/name-based store guessing.
- **E3 — Nesting depth (S2).** One-level-below publisher/launcher trees: test recursion depth and signal propagation rules against corpus; verify the `install` substring pattern's actual impact before touching it.
- **E5 — Fallback exe admission (S4).** Test admitting exes in redist-like subfolders when nothing else exists, with installer-noise filtering intact; measure false-positive risk on corpus.
- **E6 — PE-title guard relaxation (S5/S9).** Test relaxing the folder-token-share guard for acronym/short-folder cases; measure wrong-title risk on corpus (pe_metadata_blacklist + generic-label checks stay).
- **E8 — Title persistence (S7).** Write-policy experiment: explicit pick always writes DisplayName; confident auto-resolve (single clean page / exact-stem match) writes too. Verify rescan merge preserves the title.
- **E7 — Identity query pipeline (S6/S8/S9) — LAST.** Ordered candidates: PE title → exe stem (extension stripped) → exe stem with separators → display name → folder name; first clean PCGW result set wins; exact-match on exe stem. Spot-check queries against PCGW (rate-limited) on corpus cases.

### Phase 3 — C# port (accepted experiments only)

5. Port accepted E1–E8 into C#:
   - `FolderScanner.cs`, `ContainerScanner.cs`, `ExecutableDiscovery.cs` — structural container logic, depth/signal rules, fallback admission, PE-title guard.
   - `FileSystemHelper.cs` + `data/blacklist.json` — only pattern changes proven necessary by E1/E3/E5/E6 measurements.
   - Persist PE FileDescription in `PlatformMetadata` at scan (single source of truth for F3 + auto queue).
   - Identity hints threaded through `MetadataService`/`PcgwLookup`; `MainWindow` F3 builds pipeline queries and writes picked titles via `UpdateGameEntry` + `TitleSource` marker.
   - `TitleText` / `PcgwTitleFilter` — exe-stem variants and exact-match key fix.
   - Confidence-tier marking (§0.1) on emitted entries, derived from the signals that produced them.

### Phase 4 — Tests & validation

6. `testdata/mock/` fixtures generalized from accepted experiments (not only the 9 reported shapes), plus Epic-manifest tier fixtures from Phase 0.
7. Unit tests per accepted experiment, including negative cases (no new false positives) and tier assertions (Locked/Secure/Candidate/Unknown).
8. Python + C# both green on the corpus; C# suite green (539 + new, 0 regressions).
9. `planning/103-detect-py-port-status.md` updated with final parity.

## 4. Principles (binding)

- **No hardcoded detection rules** for drive paths, store folder names, or publisher folder names. Detection is signal-, structure-, and scoring-driven so it works on any system.
- **No speculative filter removal.** Every removal/relaxation requires measured corpus evidence from Phase 2 (E1/E3/E5/E6).
- **Scanner components must be generic and correct**: folder scanner, exe scanner, PE scanner, store scanner, container scanner, identity pipeline — each evaluated independently on the corpus.
- **Sample data is harness input.** `testdata/samples/d-games.txt` is consumed by scripts, not read ad hoc.
- **Steam is LOCKED and out of scope.** `SteamLibraryScanner` is untouched; ACF work is a separate follow-up (§8.1).
- **Folder name is evidence, never authority.** No folder name may become a confident title on its own; titles come from store/manifest/PE/exe, with folder only as a fallback label.
- **PE-scan + cloud lookup are the last resort** for obscure titles, never the first response to a local detection error.

## 5. Files affected

| File | Change |
|------|--------|
| `tools/detect.py` | `--probe` mode; parity realignment; experiments E1–E8 (Python only until accepted) |
| `testdata/samples/d-games.txt` | Sample EXE list fixture (harness input; already copied by user — no manual reads) |
| `tools/` or `scripts/` | Sample-list structure analyzer (path/exe patterns, nesting histogram, noise ratio from the list alone) |
| `scripts/` or `tools/` | Corpus comparison harness (Python vs C# divergence report) |
| `planning/103-detect-py-port-status.md` | Parity divergence table refresh (Phase 1 + final) |
| `src/GamingCommander.App/Services/FolderScanner.cs` | Accepted container/PE/identity changes; persist PE description; tier marking |
| `src/GamingCommander.App/Services/ContainerScanner.cs` | Accepted structural container changes |
| `src/GamingCommander.App/Services/ExecutableDiscovery.cs` | Accepted fallback/scoring changes |
| `src/GamingCommander.App/Services/FileSystemHelper.cs` | Only corpus-validated pattern changes |
| `src/GamingCommander.App/Services/Metadata/PcgwLookup.cs`, `MetadataService.cs` | Identity hints |
| `src/GamingCommander.App/MainWindow.axaml.cs` | F3 pipeline + title write via `UpdateGameEntry` |
| `src/GamingCommander.Core/Services/TitleText.cs`, `PcgwTitleFilter.cs` | Query variants + exact-match fix |
| `data/blacklist.json` | Only corpus-validated pattern changes |
| `testdata/mock/`, `tests/…` | Fixtures + unit tests per accepted experiments + Epic tier fixtures |
| `src/GamingCommander.App/Services/EpicManifestParser.cs`, `EpicLibraryScanner.cs` | Missing-manifest recovery (Phase 0.3); tier mapping |

## 6. Risks

| Risk | Mitigation |
|------|-----------|
| Drift between Python and C# so large that realignment stalls | Cap realignment to the functions under experiment; document remainder as backlog |
| Corpus is one library, not "any system" | Corpus combines d-games sample EXE list + `testdata/mock` + scenario fixtures; every rule must be signal-/structure-based to generalize |
| Experimental change improves recall but adds false positives | Acceptance gate: recall may rise only if false positives do not rise; negative tests mandatory |
| Windows-only stores (ProgramData `.item`, registry) untestable on Linux CI | Fixture `.item`/`.mancpn`/ACF files driving the same code paths |
| F3 title writes could be seen as breaking the sidecar contract | One explicit, documented exception: `UpdateGameEntry` only, identity-guarded, `TitleSource` marked; `games_metadata.json` stays extras-only |
| Tier marking rewrites a Locked entry on rescan | Locked entries are never rewritten without `UserOverrides`; tier assertions in tests |
| Missing-manifest recovery invents catalog UUIDs | Never synthesize UUIDs; only `.mancpn`/binary-manifest/store-lookup proven values (Plan 121 I5/I7) |

## 7. Success criteria

- [ ] Phase 0: Steam confirmed Locked and untouched; Epic manifest tiers validated against fixtures
- [ ] Phase 0: Missing-Epic-Manifest recovery order defined (`.mancpn` → binary `.manifest` → store lookup; no invented UUIDs)
- [ ] Phase 1 divergence report exists (Python vs C#, both directions) and is reviewed before any behavior change
- [ ] Probe mode + corpus harness run over `testdata/mock` and the d-games sample list, rendered against the §0.3 analysis path
- [ ] Sample-list structure profile generated from `d-games.txt` alone (nesting histogram, exe-in-subfolder patterns, acronym stems, blacklist-noise ratio) and consumed by E1–E8
- [ ] Selective-visit list produced from the profile (no "scan every exe", no scanner behavior change)
- [ ] Each Phase 2 experiment recorded with before/after corpus metrics (recall, false positives) and acceptance decision
- [ ] No hardcoded path/store-name detection rules introduced anywhere
- [ ] No blacklist/filter change without corpus evidence
- [ ] Every entry carries a tier (Locked/Secure/Candidate/Unknown) derived from its signals; no folder name becomes a confident title alone
- [ ] Accepted experiments show: neverwinter-style single-child nesting, engine-subfolder games titled as the game (not the subfolder), redist-only exes resolved, acronym PE titles applied (mmxl/ja2), exe-driven PCGW identity (aamfp/ja2/dungeonoftheendless), F3 picks persist through rescan
- [ ] C# port matches Python outcomes on all corpus fixtures
- [ ] Full C# suite green (539 + new, 0 regressions); `planning/103` parity notes updated

## 8. Follow-ups (separate plans, not this one)

### 8.1 Steam ACF repair (secondary, later)

Steam path detection is Locked and correct. ACF issues — mismatched/missing/moved ACFs, cross-library
`appmanifest_*.acf` resolution, orphaned `common/` folders — are a simple, self-contained process and
should get their own plan once detection tightening lands. Reference: `planning/94-game-detection-overhaul.md`
(SteamLibraryScanner) and `docs/` Steam notes.

### 8.2 Epic catalog deep-dive (if Missing-Manifest work grows)

Plan 121 (`planning/121-epic-manifest-vfs-investigation.md`) already models Installed/Missing/Orphaned.
If Phase 0.3 reveals more than a contained fix, promote it to a dedicated plan rather than expanding
this one.
