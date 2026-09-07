# Plan 123: Detection Tightening — confidence tiers, safe-identifications-first

**Created:** 2026-08-29
**Priority:** P1
**Status:** Phase 0 COMPLETE (2026-09-06). Phase 1 paper spec DONE; Phase 2 (probe, matrix 17/17, selective visits, baseline) DONE; E4/E5 + collection fixes applied in Python. **Next: review gate (amended 2026-09-07: reviews the final Python scanner as reference), then E1/E2/E3 experiments, then C# port.** Analysis-first, Python-first; C# changes only after validated experiments. **Scope (2026-09-07): this plan covers ONLY the messy standalone `d:\games` corpus. The organized "Program Files (x86)" layout (`full-p.txt`) is a SEPARATE scenario — see §8.3. Fix this plan fully first, then tackle the P-layout.**
**Source:** User live-smoke-test on `d:\games` (9 reported failures) + code trace
**Revision:** 2026-09-06 (v2) — inserted an explicit thinking phase **before** any detection code: we document the detection/exclusion model and classify the real corpus on paper first, *then* implement diagnostics. Confidence tiers (`Locked`/`Secure`/`Candidate`/`Unknown`), Steam locked + ACF deferred, Epic manifest Secure all unchanged. Symptoms (S1–S9) and experiments (E1–E8) unchanged; phases renumbered to make room for the thinking phase.
**Revision:** 2026-09-07 (v3) — post-implementation correction pass: E1 amended (single-child-not-a-collection + proof-counter/re-scan; Steam ACF out of E2 scope), E3 rewritten (depth premise overturned by non-noise-filtered corpus analysis — no WALK_MAX_DEPTH increase), Phase 4 reordered (proven Python fixes ported first), gate rescheduled to review the final Python scanner as reference.
**Revision:** 2026-09-07 (v4) — second corpus added (`testdata/samples/full-p.txt` = `P:\Program Files (x86)` listing, 18,106 lines). Declared a **separate scenario** from the messy standalone `d:\games` corpus: organized store-fixture layout. New §8.3 "Program Files (x86) known-fixture layout" plan. This plan (123) is scoped to `d:\games` only; P-layout issues are NOT mixed in.

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

### 0.3 Analysis path (collect → decide → register/escalate)

Detection is **not a single pass** — it is a staged loop: collect evidence → decide → either
**register** the entry (confident) or **escalate** to a deeper evidence phase. A folder is only
registered once the current phase has enough evidence to fix its identity; otherwise it escalates to
the next collection phase. This is the shape both scanners and `--probe` follow.

```text
PHASE A — Structural identity (cheapest, most authoritative first)
1. User override?            → preserve; stop (never overwrite)
2. Steam structural path?    → LOCKED Steam (source + location); REGISTER; stop generic
   (ACF metadata = follow-up, not this path)
3. Epic manifest scan?       → files at expected location = SECURE/LOCKED Epic; REGISTER
   → no files = Orphaned Epic → identity recovery (0.2), then continue

PHASE B — Store / source classification (identity of source, not title)
4. Other store signal?       → SECURE source (probe folder AND immediate children —
   corpus: ~48 of ~90 store signals sit one level below the game folder); classify, then continue
5. Registry fallback?        → SECURE source (EA/Ubisoft/GOG/Rockstar)

PHASE C — Container decision (structure, not names)
6. Container analysis?       → FIRST (probe finding 2026-09-06): a container has no self-signals
   by definition, so the non-game filter must NOT run before this — it would reject every
   container (S1/S2). Check children for game signals (children typed by their OWN signals);
   recurse only into children with real evidence; NEVER promote the parent (S3).
7. Non-game folder filter?   → runs only for non-container folders:
   reject data/platform/tool/redist/launcher folders (file-type + name analysis)

PHASE D — Executable collection & selection
8. Executable discovery?     → collect root + subfolder exes; reject installers/tools/copies/cracks
   → record WHY each was kept/rejected (E5 target: redist-subfolder fallback for penumbra, S4)
9. Executable selection?     → score remaining candidates; pick primary; record score factors
10. Fallback/engine layout?  → root exe, .lnk, Unreal/UE layout (Candidate)

PHASE E — Title resolution
11. Title selection?         → store/manifest title → guarded PE description
    → exe stem → normalized display name → folder name (folder is evidence, never authority)

PHASE F — Enrichment (last resort, obscure titles only)
12. PE-scan + cloud lookup?  → E7, only for unresolved/ambiguous titles
```

**Registration vs escalation:** a folder that hits Phase A–B (Locked/Secure source) is registered
immediately. A Candidate (Phase C/D) is registered with a provisional tier and enriched in Phase F.
A folder that fails all phases becomes **Unknown/review** (registered but not titled) — never dropped
silently.

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

**Scope boundary (2026-09-07):** this plan targets the messy standalone `d:\games` corpus only. The
organized "Program Files (x86)" store-fixture layout (`full-p.txt`) is a DIFFERENT scenario and is
deliberately excluded — it is planned separately in §8.3 (known store-client fixtures, like Steam's
locked `steamapps/common` path). Do not introduce P-layout handling into this plan's experiments.

The most critical false-positive sources (from the user's last test) are **exe detection** and
**folder-name-as-title**. Both are addressed by the analysis path (§0.3) and Phases 1–4 below, in
that priority order.

## 2. Method: think-on-paper first, then code

Order of work is strict. The first substantive act is **not** writing code — it is writing down
the detection/exclusion model *and* proofing it against the real corpus structure, before any
production or tooling code changes:

1. **Specify** the full detection decision model + exclusion taxonomy on paper (§2.1, §2.2).
2. **Classify the corpus** against that model — produce a scenario catalog with expected outcomes per scenario (§2.3). No behavior change.
3. **Record the probe/decision-model contract** so diagnostics render the model exactly (§2.4).
4. **Realign** `tools/detect.py` with the C# gamescan code — find what each has that the other lacks (both directions) [done, T3 → `planning/103`].
5. **Measure** both scanners against the scenario corpus (fixtures + sample EXE list).
6. **Experiment** in Python only: test theories against the scenario catalog.
7. **Port** only changes that passed corpus acceptance — to C# with unit tests.

No detection behavior change (Python **or** C#) before the paper specification and corpus
classification are reviewed at the Phase 2 gate.

### 2.1 Detection decision model (the spec)

The §0.3 analysis path is restated as a **decision table** — the single source of truth both
scanners (and the future `--probe`) must follow. Each stage lists the evidence it reads, what it
accepts, what it rejects/filters, and where it sends the folder next.

| Stage | Evidence | Accepts | Rejects / defers | Outcome → next |
|-------|----------|---------|------------------|----------------|
| 1. User override | `UserOverrides` | Any prior user edit | — | Preserve + stop |
| 2. Steam structural | `steamapps/common/<folder>` | Unique Steam location | — | **Locked** · stop generic |
| 3. Epic manifest | `.item` (global/local), `.mancpn`, `.egstore` | Manifest-matched source + ids | hollow/incomplete/DLC items | **Locked/Secure** or Orphaned → recovery |
| 4. Other store marker | GOG/EA/Ubi/BattleNet/Xbox/Rockstar/SteamEmu markers at folder **or immediate child (depth ≤ 2)** | Authoritative store signal | launcher-only folders, filename guesses | **Secure** source → exe/title |
| 5. Registry fallback | EA/Ubi/GOG/Rockstar per-game keys | Matching key | — | **Secure** source |
| 6. Container analysis | children's own signals (**never parent typing**) | children with real game evidence | data-only / store-launcher children; **parent not promoted** | Recurse into children / skip |
| 7. Non-game filter | folder name + **composed contents** (non-container folders only) | — | data/redist/tool/launcher/backup folders | Reject folder; no entry |
| 8. EXE discovery | exe name, path, size, PE | plausible launch candidates (root + subfolders) | installers/tools/copies/cracks/redist | Candidate list, each kept/rejected **with reason** |
| 9. EXE selection | candidate scores | highest-confidence candidate | ambiguous ties | Primary EXE + score |
| 10. Fallback layout | root exe / `.lnk` / engine layout | clear root launch signal | — | **Candidate** |
| 11. Title selection | manifest → PE → exe stem → folder | authoritative or guarded title | generic/publisher PE labels | Title + `TitleSource` |
| 12. Enrichment | PE fields + PCGW | obscure unresolved titles only | — | Enrich (last resort) |

**Corpus-driven corrections (Phase 1, 2026-09-06):**

- **Stage 4 signal depth:** store markers must be probed at the game folder **and one level deeper**
  — corpus shows ~48 of ~90 store signals sit one level below the game folder.
- **Stage 6 container rule:** children are typed by their **own** signals; a store-typed child never
  promotes its parent (9 of 21 deeper-only shapes are containers; promoting the parent is exactly
  the S3 failure).
- **Container-before-filter (probe finding 2026-09-06):** container analysis (6) must run **before**
  the non-game filter (7). A container has no self-signals by definition; running the filter first
  rejects every container (S1/S2 — verified by `--probe` on `PublisherCollection`: stage 6 fires
  before stage 7, matching Python's real `scan_directory`).
- **Stages 8/9:** the corpus shows 1 folder (penumbra, S4) where the only real exe lives in `redist/`;
  redist/tool exes otherwise co-exist with a real root exe. Candidate admission stays conservative,
  **except** E5 must test redist-subfolder fallback admission for the penumbra shape (admit
  `PENUMBRA.EXE` from `redist/` when no root exe exists, rejecting `super_secret.exe`/`-Penumbra.exe`).
  The 3-level subdir search gap (Python has it, C# does not) also needs experiment coverage (E3/E5).

Distinctions that must hold in the spec:

- **Exclusion** (stage 5, 7) *removes* only clearly non-game content — it is not scoring.
- **Scoring** (stage 8) *ranks* uncertain candidates — it never proves identity alone.
- **Title** (stage 10) never upgrades a **folder name** into proof of identity.

### 2.2 Exclusion taxonomy

Classify every thing we skip into exactly one bucket, with the evidence each bucket requires.
No bucket may be expanded/deleted without corpus evidence (Phase 3 experiments):

| Bucket | Required evidence | Boolean-exclude or score-penalize |
|--------|-------------------|-----------------------------------|
| Non-game root folders | known launcher/tool dirs, config dirs | exclude |
| Data-only folders | no non-noise exe + no meaningful file (file-type analysis) | exclude |
| Redistributables/installers | `redist`/`vcredist`/`directx`/setup-family exes | exclude (deep exe search) + penalize (candidate) |
| Launchers / tools / editors | `launcher`/`updater`/`tool`/`editor`/`configtool` patterns | penalize, rarely exclude |
| Backup / copy / crack | `copy`/`-copy`/`crack`/orgi patterns | penalize strongly |
| Platform dirs | `Win64`/`Win32`/`x64`/`x86`/`Binaries`/`Bin` | exclude as *folders*, never as games |
| Ambiguous | everything else | **Candidate** (do not pre-judge) |

**Corpus check (Phase 1, corrected 2026-09-06):** the noise-dir concern is real but bounded — 78 of
720 exes have a redist/install-like ancestor, and only **1 folder** (penumbra) has its only real exe
in `redist/`. The "Redistributables/installers" bucket is a **penalize, not hard-exclude** for the
fallback path: E5 must test admitting a redist-only real exe when no root exe exists, while still
rejecting installers/`super_secret.exe`/`-Penumbra.exe` backup. No other bucket is justified for
change by this corpus.

### 2.3 Corpus classification (scenario catalog)

From `testdata/samples/d-games.txt` + `testdata/mock/` + `testdata/samples/full-d.txt`, every observed
structure is assigned to a scenario with an **expected** outcome. This is produced **on paper** before
any scanner change. Counts shown are from the Phase 1 inventory of full-d.txt (2026-09-06):

| Scenario | Shape | Expected outcome | Corpus count |
|----------|-------|------------------|--------------|
| S-A valid standalone | folder with root exe | Standalone Candidate/Secure | 82 (direct root exe) |
| S-B store game | folder with real store signal | correct store, Locked/Secure as applicable | 17 (store + exe) |
| S-C container/organizer | folder with N game children | recurse into children | ~9 containers (Blizzard, Origin, Epic Games, SSI, …) |
| S-D deep-nested game | exe 2–3 levels down (`game/`, `run/`, `app/`, scummvm) | game entry, not folder-as-title | ~12–15 |
| S-E engine-subfolder game | exe under `system`/`win32`/`bin` (ELEX, Deadlight, TrappedDead) | parent titled as game, child not promoted | **4–5 confirmed** (ELEX, elexII, Deadlight, trapped, …) |
| S-F redist-only | only redist/support exes | not a game | **1 confirmed (penumbra)** — S4 IS real; **corrected model (probe 2026-09-06): game IS recoverable → Candidate, primary `PENUMBRA.EXE`** (E5 target) |
| S-G multi-exe | several competing exes | single best primary exe | 95 of 103 — **probe DIVERG 2026-09-06: bare `"tool"` not penalized (Python+C#) → tie broken by exact-stem bonus (scoring gap, see E6a below)** |
| S-H acronym title | acronym folder + PE title available | PE title applied, not acronym | mmxl, ja2 (S5/S9) |
| S-I no signal | no marker, no exe | Unknown/review | 3 of 106 |
| S-J false game folder | non-game name/data-only folder | excluded | 0 observed (no noise-only folders) |

The scenario catalog *is* the "on paper" proofing: each §1 symptom (S1–S9) is mapped to a scenario,
and a pass/fail expectation is written before any experiment runs.

**Symptom → scenario mapping (Phase 1, corpus-grounded):**

| Symptom | Scenario | Corpus evidence |
|---------|----------|-----------------|
| S1 container with 17 game subfolders → 0 entries | S-C | 9 container parents; children store-typed |
| S2 `arc-install\neverwinter_en\…` not detected | S-C/S-D | ARC-INSTALL is a container; neverwinter is a child. **FIXED by exact-match terminating rule (2026-09-07, DONE):** `Neverwinter_en\Neverwinter.exe` stem ≡ folder → game folder, primary exe; nested `Neverwinter\Live\x64\GameClient.exe` (rel-4) + double-nested duplicate never promoted (excluded from working set). Match checked BEFORE deep processing. Siblings at the same level are separate entities (catalog signal) |
| S3 ELEX-style game becomes "system" | S-E | **confirmed**: ELEX (`system/ELEX.exe`), elexII, Deadlight (`win32/LOTDGame.exe`), trapped (`bin/TrappedDead.exe`) — parent must win |
| S4 Penumbra exe only in `redist` | S-F | **confirmed (corrected)**: penumbra has only `PENUMBRA.EXE`/`-Penumbra.exe` in `redist/` — E5 must test redist fallback |
| S5 mmxl keeps acronym | S-H | mmxl has `Might and Magic X Legacy.exe` at root — PE title available, guard blocked it |
| S6 lookups use folder name only | S-H/S-D | acronym + deep-nested folders |
| S7 F3 pick not persisted | S-H | write-path bug, not detection |
| S8 `dungeonoftheendless` lists as "Endless" | S-H | query-source bug |
| S9 `ja2.exe` not titled | S-H | `ja2.exe` at `jag2/` root (not `jag2UB/`); PE title should apply |

**Corrected S4 note (2026-09-06):** the corpus **does** contain a redist-only-real-exe folder —
`penumbra` (only `PENUMBRA.EXE`/`-Penumbra.exe` in `redist/`; root has only `oalinst` installer).
The earlier "S4 refuted" claim used a parent-dir-only noise check that masked it. Corrected method:
**exe-name noise + parent-dir noise both checked.** E5 must measure redist-subfolder fallback
admission for this exact shape (admit `PENUMBRA.EXE` from `redist/` when no root exe exists, while
still rejecting `super_secret.exe`, installers, and `-Penumbra.exe` backup).

### 2.4 Probe / decision-model contract

`--probe <folder>` (implemented in Phase 2, **after** this spec is reviewed) must render the §2.1
decision chain for one folder, faithfully and completely. Its output schema is derived from the
model — the tool does not invent its own structure. This section is the binding contract for the
implementation.

#### 2.4.1 Invocation

```
python tools/detect.py --probe <folder>
```

- Takes exactly one folder path. No scan, no recursion beyond the folder being probed (container
  children are *listed*, not recursively scanned, unless the folder itself is the probe target).
- Returns a single JSON document (plus a human-readable table when stdout is a TTY).
- **Additive only:** `--probe` must not change `scan_directory` behavior in any way.

#### 2.4.2 Output schema (top level)

```jsonc
{
  "folder": "<relative-or-normalized label, never the raw absolute path in docs>",
  "root": "<probe root path, used only for reproducibility>",
  "tier": "Locked | Secure | Candidate | Unknown",   // final outcome from §0.1
  "store": "Steam | Epic | GOG | EA | Ubisoft | BattleNet | Xbox | Rockstar | SteamEmu | Standalone | null",
  "title_source": "UserOverride | Manifest | StoreSignal | PeDescription | ExeStem | FolderName | null",
  "display_name": "<resolved title, or null>",
  "primary_exe": "<resolved relative exe path, or null>",
  "chain": [ /* one entry per §0.3 phase/stage that executed, in order */ ],
  "exe_candidates": [ /* admission + scoring detail, see 2.4.5 */ ],
  "signals": { /* store/registry/fallback probes, see 2.4.3 */ },
  "notes": [ "<diagnostic strings, e.g. reasons for skips>" ]
}
```

**Stage numbering (corrected 2026-09-06):** chain stages follow §0.3 **phase order** — 1 User
override, 2 Steam, 3 Epic, 4 Other store, 5 Registry, **6 Container (before filter)**, 7 Non-game
filter, 8 Exe discovery, 9 Exe selection, 10 Fallback/engine, 11 Title, 12 Enrichment. A container
(6 hit) ends the chain immediately — the parent is never an entry; children are handled recursively.

#### 2.4.3 `signals` object — every probe result

One key per §0.3 stage probe that ran. Each value is a list of `{ signal, found, evidence }` records
(e.g. `goggame*.info`, `.egstore/`, `steam_appid.txt`, registry key, `.lnk`). The probe records
**what was checked and what was found**, including negative results, so the decision is fully
auditable.

#### 2.4.4 `chain` array — the decision trail

One element per stage that executed, in §0.3 order:

```jsonc
{
  "stage": 3,                      // §0.3 stage number
  "rule": "Other store marker",
  "hit": false,                    // did this stage produce its outcome?
  "accepted": null,                // what it accepted (store name / entry / exe / title)
  "rejected": ["<folder|exe|title rejected>"],
  "deferred": ["<folder|exe|title sent to next stage>"],
  "reason": "<short human-readable explanation>",
  "next": 4                        // next stage number, or null for stop
}
```

The chain must be **complete and ordered**: every stage that ran appears exactly once in order;
every stage that short-circuited (override / Locked) ends the chain with `next: null`.

#### 2.4.5 `exe_candidates` — admission + scoring detail

For every executable candidate considered at stages 7/8:

```jsonc
{
  "exe": "<relative path from probe folder>",
  "admitted": true,                // false = rejected outright (installer/tool/crack/redist)
  "admission_reason": "noise_tier_1 | forbidden_launch | setup_guard | accepted",
  "score": 123,                    // raw score (null if not admitted)
  "score_factors": [               // each +/− contribution with reason
    { "delta": 40, "why": "exact folder-name match (ADR-012)" },
    { "delta": -25, "why": "blacklist tier_10_dev_editor_tools" }
  ],
  "pe": {                          // only if PE metadata read
    "description": "…",
    "product_name": "…",
    "internal_name": "…",
    "product_year": 2024
  },
  "selected": false                // true for the primary exe
}
```

Every candidate is present — kept **and** rejected — with its reason. "Selected" must match
`primary_exe`.

#### 2.4.6 Tier derivation (must match §0.1)

| Tier | Probe must show |
|------|-----------------|
| Locked | Steam structural path hit, or Epic global `.item` matched to InstallLocation |
| Secure | Epic local `.item`/`.mancpn`, or other store marker at folder/child, or registry match |
| Candidate | standalone fallback (root exe / `.lnk` / engine layout) |
| Unknown | no signal, generic/no exe, conflicting signals |

#### 2.4.7 Validation (Phase 2)

- Run `--probe` on one fixture per scenario (S-A..S-J) from `testdata/mock/` + `testdata/samples/full-d.txt`
  and assert: the rendered `tier` equals the §2.3 expected outcome, and the chain is complete/ordered.
- The same fixtures feed the Python vs C# divergence run (§2.3 scenario catalog).
- Probe output never leaks absolute paths into logs/docs (labels normalized per 2.4.2).

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

#### Phase 0 findings (2026-09-06) — declaration complete, no code change

**T1 — Steam declaration (CONFIRMED).** Evidence:
- `LibraryManager.SelectScannerAndScan` routes to `SteamLibraryScanner` when `LooksLikeSteamLibrary`
  (structural `steamapps/common/` exists) **or** `configuredType == Steam` (explicit override).
- `SteamLibraryScanner` treats every folder under `steamapps/common/` as a game (structural);
  ACF metadata is authoritative (name, AppID, `steam://rungameid/{id}` launch).
- `FolderScanner.IsNestedSteamTree` (Bug 13b) excludes nested Steam client/library trees from
  generic scan — no generic detection ever runs on Steam roots.
- `SteamLibraryScanner` is constructed unconditionally in `App.axaml.cs` / `MainWindow.axaml.cs`
  from configured Steam roots — never null in the real wiring path.
- **Gap found: none.** Steam is LOCKED as declared in §0.2. ACF work remains out of scope (§8.1).

**T2 — Missing Epic Manifest spec (DEFINED).** Orphan cases (Epic folder with `.egstore`/`.egsstore`,
no matching ProgramData `.item` — see `EpicOrphanDiscovery.Find`):

| Case | Local evidence present | What can be recovered |
|------|------------------------|------------------------|
| O1 | `.mancpn` (ns, itemId, app, guid) | Full catalog ids → identification `.item` write (proven: launcher accepted written `.item`) |
| O2 | `.ovt` JWT only (Death Stranding case; `.mancpn` deleted) | namespace + catalogItemId from `ent[0]` → `.item` write |
| O3 | binary `.manifest` only (via `EpicBinaryManifest.TryRead`) | AppName + LaunchExe only — no catalog ids; display name + exe resolution, **no** `.item` write |
| O4 | none readable | Cannot recover; Unknown/review; no invented UUIDs |

Recovery order (never invent UUIDs — Plan 121 I7):
1. Local `.mancpn` → write `.item` (no HTTP).
2. `.ovt` JWT parse → write `.item` (no HTTP).
3. Binary `.manifest` header → AppName + LaunchExe only (identification-only row).
4. Store GraphQL `searchStore` — **tools only today** (`tools/epic_search.py`,
   `tools/generate_epic_item.py`); re-probe before any C# port (Plan 121 I5 leftover #1);
   online-gated; never used to invent catalog ids.

No C# writer change until this spec is consumed by Phase 2/3 work. Reference files:
`EpicItemWriter.cs` (`TryWrite`, `TryReadMancpn`, `TryReadOvt`, `TryParseOvt`),
`EpicBinaryManifest.cs` (`TryRead`), `EpicOrphanDiscovery.cs`, `EpicItemCatalog.cs`.

### Phase 1 — Paper specification & corpus analysis (the thinking phase; **no detection code change**)

Work on paper + scripts that only *read* the corpus. Nothing here changes scanner behavior.

1. **Parity audit (both directions).** [DONE — T3] `planning/103-detect-py-port-status.md` refreshed
   2026-09-06 with the divergence table (C#-only, Python-only, confirmed drifts). Reviewed as the
   gate input.
2. **Specify the decision model** (§2.1): restate the §0.3 analysis path as the decision table —
   evidence in, accept, reject/defer, outcome → next. This is the canonical reference for both
   scanners and for `--probe`.
3. **Specify the exclusion taxonomy** (§2.2): one bucket per skip type, with required evidence and
   exclude-vs-penalize. No bucket changes without corpus evidence (Phase 3).
4. **Corpus inventory (scripted read-only, from the list alone).** Parse `testdata/samples/d-games.txt`
   — **never eyeballed**; aggregate-only, no paths published:
   - Per-path exe counts and stores; nesting-depth histogram (root / 1-below / 2-below).
   - Exe-in-subfolder patterns (`redist`/`system`/`bin`/`Binaries`/platform dirs).
   - Exe-stem naming patterns (acronym vs full, roman numerals, `_en` locale, multi-exe families).
   - Noise ratio: how many listed exes already match blacklist tiers (measure, don't assume).
5. **Classify into the scenario catalog** (§2.3): assign observed structures to scenarios S-A..S-J
   with expected outcomes; map each §1 symptom (S1–S9) to a scenario. This is the "on paper"
   proofing deliverable.
6. **Record the probe contract** (§2.4): `--probe` output schema derived from §2.1.
7. **Gate: review.** Present the decision model + taxonomy + scenario catalog for review. **No
   detection behavior change until reviewed.**

#### Phase 1 corpus findings (2026-09-06) — full-d.txt inventory (aggregate only)

**Source:** `/mnt/r/full-d.txt` — full filtered file listing, 12,193 file lines (no dir lines).
Extensions: txt 6712, dll 4398, exe 720, db 195, manifest 82, info 34 (GOG), mancpn 26 (Epic),
lnk 22, acf 1. Superset of `d-games.txt` (719 exe lines). **No paths are reproduced here.**

**Structure (3rd-level game folders under one root):**

| Measure | Value |
|---------|-------|
| 3rd-level folders total | 106 |
| …with exe or store signal | 103 |
| …exe only / store only / both | 86 / 0 / 17 |
| …without any signal (non-game) | 3 |
| games with >1 exe | 95 of 103 |

**Exe placement (relative to game folder):**

| Depth | Count |
|-------|-------|
| 1 (directly in game folder) | 280 of 720 |
| 2 | 165 |
| 3 | 94 |
| 4+ | ~180 |

**Critical finding — only ~39% of exes sit directly in their game folder.** 61% are deeper. But the
deeper-only games split into two distinct shapes:

- **~15 real deep-nested games** (e.g. game subfolder `game/`, `run/`, `app/`, scummvm, cze,
  worldgenerator) — the S2/S3 targets (neverwinter, qfg5, DarkSouls3, Divine Divinity, Beneath a
  Steel Sky, …).
- **~9 publisher/launcher containers** (`Blizzard`, `Origin`, `Epic Games`, `EpicLauncher`,
  `Rockstar Games`, `SquareEnix`, `SSI`, `Stardock`, `Ubi`, `ARC-INSTALL`) — **not games**. Their
  exes sit under store-typed children. This confirms the container problem (S1) is the dominant
  false-game source, and that **container typing must not promote the parent** (S3).
- **5 folders with only non-plausible exes** (noise-named or noise-dir): penumbra (S4, redist),
  ELEX/elexII/Deadlight/trapped (S3, `system`/`win32`/`bin`). Separated by corrected
  exe-name+parent-dir noise analysis.

**Store-signal placement:** GOG `.info` / Epic `.mancpn` / `.egstore` / Steam `.acf` live at depth
2–4 relative to the game folder (13 direct, 16 at depth 2, 48 at depth 3, 12 at depth 4). **Store
signals mostly do NOT sit directly in the game folder** — they sit one level deeper, which is why
folder-scope signal checks miss them.

**Immediate parent dirs of exes (top 25):** `redist` 21, `win64` 20, `__installer` 18, `tools` 16,
`system` 13, `launcher` 12, `game` 11, `crashsender` 11, `bin` 11, `boot` 11, … — the noise-dir
problem is real but bounded (78 exes have a redist/install-like ancestor of 720).

**Approach verdict (answers the review question):**

- The §0.3 analysis path order is **correct in principle** and matches the corpus: store/structural
  identity first (17 store+exe games, 0 store-only), then container typing, then exe selection.
- **Two corrections the corpus forces:**
  1. **Container children must be typed by their own signals, never the parent** — 9 of the 21
     deeper-only shapes are containers, and promoting the parent is exactly S3.
  2. **Store signals must be searched one level deep, not just at game-folder root** — 48 of ~90
     store signals sit at depth 3 (one below the game folder). Root-only signal checks would miss
     them (this is the Epic `.mancpn`/GOG `.info` gap).
- **No exclusion bucket needs changing** from this inventory — no folder had a *single* exe in a
  noise subdir, and no blacklist change is justified by file placement alone. **However, S4 is
  CONFIRMED (corrected 2026-09-06):** `penumbra`'s only non-installer exe (`PENUMBRA.EXE`,
  `-Penumbra.exe`) lives in `redist/` — 1 of 103 folders. The earlier "S4 refuted" claim used a
  parent-dir-only noise test and missed `oalinst` (an installer at root) masking the real pattern.
  Corrected method: exe-name noise + parent-dir noise both checked. S4 therefore stays an **E5
  experiment target** (fallback exe admission into redist-like subfolders when no root exe exists),
  not a refuted symptom.
- **The full-d.txt listing is REQUIRED input** for the scenario catalog; the 719-line exe-only
  sample cannot answer structure questions. It stays a harness fixture — never read ad hoc.

#### Phase 1 findings — decisions adopted (2026-09-06)

1. **Source input = `/mnt/r/full-d.txt`** (superset of `d-games.txt`). Copy as harness fixture
   `testdata/samples/full-d.txt` (aggregate-only; no paths in docs).
2. **Container typing rule confirmed:** children are typed by their own signals; a parent whose
   children are store-typed is a container and must **not** be promoted (S3 fix direction).
3. **Store signal depth rule confirmed:** store-signal probes must check the game folder **and its
   immediate children** (depth ≤ 2), matching the corpus's depth-3 signal placement.
4. **No blacklist change** is justified by this inventory, **but S4 is CONFIRMED, not refuted
   (corrected):** penumbra's only real exe (`PENUMBRA.EXE`) is in `redist/`. The earlier "S4
   refuted" claim used parent-dir-only noise analysis and missed `oalinst` at root masking it.
   E5 must test redist-subfolder fallback admission for this exact shape (see §2.3).
5. **Scenario catalog now corpus-grounded:** S-A..S-J mapping below uses actual counts from this
   inventory.

#### Phase 2 probe findings (2026-09-06) — §2.4.7 validation matrix

`tools/validate_probe_matrix.py` runs `--probe` on one fixture per scenario. **Result: 9 PASS +
1 DIVERG.** Findings that corrected the model:

1. **Container-before-filter (S1/S2):** container analysis must run before the non-game filter —
   a container has no self-signals by definition, so filtering first rejects every container.
   Verified: `PublisherCollection` → stage 6 (container) fires before stage 7. §0.3/§2.1 corrected.
2. **Platform-child rule (S3/E4):** platform/build subdirs (`system`, `bin`, `win64`, `Binaries`,
   …) belong to the **parent** game and must not count as container children, nor cause the
   parent to be rejected by the non-game filter. Verified: `Elex` → Candidate with
   `system/ELEX.exe` primary, parent treated as the game (not "system").
3. **S-F / S4 recovery confirmed (E5):** `Penumbra` (only real exe in `redist/`) now resolves to
   **Candidate with primary `redist/PENUMBRA.EXE`** — `super_secret.exe` and `-Penumbra.exe`
   correctly not selected. The corrected model admits the redist fallback; E5 must port this
   into `ExecutableDiscovery`/`FolderScanner`.
4. **S-G scoring gap (E6a, not E6):** `MultiRunnerTool.exe` ties `MultiRunner.exe` because **bare
   `"tool"` is not penalized** in either Python `_TOOL_NAMES` or C# `tier_10_dev_editor_tools`.
   The tie is now broken deterministically by the exact-folder-stem `+15` bonus (so `MultiRunner.exe`
   wins), but the missing penalty remains a latent scoring gap. **E6a candidate:** add `"tool"` to the
   penalty tier in both scanners. (Note: this is separate from **E6** — the PE-title guard relaxation
   for acronym titles S5/S9.)

### Phase 2 — Diagnostics & baseline (tooling only; no production behavior change)

1. **Probe diagnostics** (after spec review): implement `--probe <folder>` in `tools/detect.py`
   **exactly per §2.4 contract** (schema, chain ordering, tier derivation, path normalization).
   Additive; `scan_directory` behavior unchanged. Validate against one fixture per scenario S-A..S-J
   (§2.4.7). **DONE 2026-09-06** — implementation complete and verified; exposed a real ordering
   flaw (container analysis must precede the non-game filter; §2.1 corrected).
2. **Selective folder visits.** Use the Phase 1 inventory to choose which corpus folders need
   physical probing (PE reads, store-signal checks) — not "scan every exe". **DONE 2026-09-06:**
   `tools/selective_visits.py` emits 14 curated visits covering S1–S6/S8/S9 (symptom-driven,
   one or two folders each) → `testdata/samples/selective-visits.json`. Executed on a Windows
   machine (PE reads/manifest contents); S7 (F3 pick persistence) is app-level, not detection.
3. **Baseline runs.** Run Python and C# scanners over the scenario corpus; emit a divergence report:
   missed entries, false positives, wrong primary-exe picks, wrong titles, wrong store types — per
   scenario (S-A..S-J) and per §1 symptom (S1–S9). **DONE 2026-09-06 (Python side):**
   `tools/baseline_compare.py` → `testdata/samples/baseline-report.json`. Python production
   `scan_directory` vs corrected model (`--probe`) over the 10 scenario fixtures:

   | Scenario | Production | Corrected | Divergence |
   |----------|-----------|-----------|------------|
   | S-A standalone | GameAlpha | Candidate | — |
   | S-B Epic | EpicGameGamma | Secure | — |
   | S-C container | SubGame* (children) | Unknown parent | — (children ARE the entries) |
   | S-D deep-nested | Neverwinter/neverwinter_en | Candidate | — |
   | S-E engine-subfolder | **Elex/system** | Candidate | **WRONG FOLDER — child `system` promoted (S3)** |
   | S-F redist-only | **Penumbra/redist** | Candidate | **WRONG FOLDER — child `redist` promoted (S4)** |
   | S-G multi-exe | MultiRunner | Candidate | — (but both pick MultiRunnerTool.exe — E6, see §2.3) |
   | S-H acronym | Mmxl | Candidate | — |
   | S-I no signal | — | Unknown | — |
   | S-J false game | — | Unknown | — |

   **Summary: 2 wrong-folder promotions (S3, S4), 0 missed games, 0 false positives.** The two
   divergences are exactly the E4/E5 experiment targets: production promotes the platform/build
   child (`system/`, `redist/`) instead of the parent game. Deep-nested (S2) and container
   recursion (S1) already work in Python. C# side runs in Phase 4 (Python-first method).

   **E4/E5 PRODUCTION FIXES APPLIED 2026-09-06 (Python first):**
   - `_PARENT_BOUND_CHILD_DIRS` added: platform/build/redist children (`system`, `bin`, `win64`,
     `Binaries`, `redist`, …) belong to the **parent** game — they no longer trigger the container
     check and their exes are collected as parent candidates.
   - Container check skips parent-bound children; after it, if parent-bound exes exist and the
     folder is not a container, the **parent is promoted** (`parent_bound_child_exe` Tier 2 path)
     with the exes scored against the parent folder name.
   - `_pick_best_root_exe` now analyzes the **base filename** (so `redist/PENUMBRA.EXE` matches
     tokens against `PENUMBRA.EXE`) and penalizes leading-dash backups (`-Penumbra.exe`, `-15`).
   - Probe `_probe_exe_candidates` gained the exact-folder-stem `+15` bonus to match production
     scoring (S-G tie is now deterministic: `MultiRunner.exe` beats `MultiRunnerTool.exe`).
   - **Deterministic tie-break (generic, no hardcoded paths):** all three scorers
     (`_pick_best_root_exe`, `_pick_primary_executable`, `_probe_exe_candidates`) now sort by
     `(-score, base_name)` instead of relying on Python's stable sort over `os.scandir`
     order. A score tie now resolves to the alphabetically-first candidate reproducibly
     across machines/scans (verified: `['Zzz.exe','Aaa.exe']` and reversed input both → `Aaa.exe`).
     User-pick fallback (candidate list + `UserOverrides`) remains the UI surface for genuine ties.
   - **Collection folder with stray root files (E1 finding, user scenario 2026-09-06):** the
     store-collection detection was gated on `not has_files_at_root` — a collection folder
     containing launcher residue (`EpicGamesLauncher.url`, `readme.txt`) was misread as a single
     "Unknown" game and its children were missed. Removed the gate: the collection signal is now
     **game-shaped children** (child containing a deeper exe, e.g. `epicgames/snuffbox/binaries/
     snuffbox.exe`), regardless of stray root files. Verified: `epicgames/` with stray files →
     both children found; clean 200-game collection (93ms); all existing fixtures unchanged.
     This is exactly the "once the first deep finding proves it's a collection, the parent is a
     collection" rule from the user's model — the parent is never an entry; children are the games.
   - **Deep-nested collection games (E1 finding, user model 2026-09-06):** a collection whose games
     have DEEP exes (`monsterhunter/win64/binaries/monsterhunter.exe`) was MISSED — the container
     data-only check looked only at direct children and skipped `monsterhunter` as "data-only"; the
     deep container check then wrongly promoted `win64`. Fixed: (a) the container data-only check now
     searches deep exes via `_find_exe_in_subdirs`; (b) the deep container check skips
     parent-bound children (`win64/`, `bin/`, …); (c) parent-bound exe collection now reaches 2 levels
     (`win64/binaries/`). Result: `monsterhunter` → `win64/binaries/monsterhunter.exe` (parent wins).
     Regression fixture `CollectionDeepNest/` + baseline check added.
   - **Depth analysis (CORRECTED 2026-09-07, user asked for evidence):** an earlier claim of "games
     13 levels deep" was WRONG — those were noise/emulator/installer paths (Cemu, fceux, steambackup,
     vcredist, `resize 66%.exe`, CrashReportClient). Real (non-noise) game exe depth relative to the
     game folder: 43 at depth 1, 28 at d2, 10 at d3, 14 at d4, 4 at d5, 2 at d6, 1 at d8 (Cemu
     emulator — not a game). 81/102 real exes at depth 1-4. A depth-6 container walk was added based
     on the wrong claim and was REVERTED — it would surface ~20 tools for ~4 real deep games. The
     genuine deep cases (UE `*-Shipping.exe`, Neverwinter `GameClient.exe` rel 5-6) are found via the
     existing UE fast-path and container recursion; Neverwinter's correct entry is the rel-2
     `Neverwinter.exe` (already handled), not the nested duplicate `GameClient.exe`.
   - **Proof-based collection model (user model, 2026-09-06):** "1 proof / 2 proof" — the first
     game-shaped finding under a folder tentatively marks it a collection; a second confirms it
     (`epiccollection` is a collection, not a game); subsequent children are handled with the
     simplified "each child is a game-folder base" rule. The **path-divergence** observation (many
     sibling game folders diverging from one parent, e.g. `game1/game2/game3`) is an aggregate
     signal reinforcing collection status. Current implementation discovers this per-folder via
     game-shaped children; the divergence/aggregate signal is noted as an E1 optimization candidate
     (avoid re-parsing the collection parent once confirmed) but is not yet an explicit counter.
   - **Result: baseline now 10/10 clean (0 wrong-folder, 0 missed, 0 false positives) + collection
     regression PASS; validation matrix 10/10 PASS.** User-pick fallback remains for genuinely
     ambiguous installs (candidate list + `UserOverrides`).

### Phase 3 — Scenario experiments (Python only; each gated on acceptance)

Each experiment states a hypothesis, measures before/after against the scenario catalog (recall
gained, false positives introduced), and is only accepted if it improves or holds both metrics.
Ordered **safe-first**: folder/exe false-positive reduction before identity/cloud.

**Status 2026-09-07:** E4 and E5 are **DONE in Python** (applied as production fixes with
regression coverage). The **exact-match terminating rule** (E1, user model 2026-09-07) is **DONE in
Python**: Tier 1.5 in `scan_directory` + probe stage 13; working-set pruning + catalog/sibling-entity
signal verified via `ArcInstall/` fixture (S-D2). **P3c-2 (single-child rule + proof-counter) DROPPED
2026-09-07** — corpus (full-d + full-e) shows production already handles every real single-child
chain (Ashen, SquareEnix, Stardock, qfg5). Replaced by three corpus-grounded fixes, all DONE:
**GAP A** whole-name match in terminating rule (`Diablo III`↔`Diablo III.exe`, `Dead Space 3`↔
`deadspace3.exe`, 7 real cases) with backup-exclusion guard (`-Penumbra.exe` never matches);
**GAP B** deep container check now reuses `_find_exe_in_subdirs` (UE-wrapped `Ashen/Binaries/Win64/`
recognized — was missed); **GAP C** `PublisherWrapper/` fixture (real SquareEnix/Stardock/qfg5/COD
shapes) added to matrix + baseline. Matrix **20/20 PASS**; baseline **12 scenarios, 0 missed / 0
wrong-folder / 0 FP**. Remaining E1: none (P3c-2 dropped). E3's premise was corrected (see E3 below).

- **E4 — Engine-subfolder parent decision (S3).** When a child has the only exe and that exe scores strongly against the **parent** folder name, promote the parent, not the child. Scoring-driven, name-agnostic; validate against engine-layout games in the corpus (no hardcoded `system`/`bin` names).
- **E1 — Organizer-container generalization (S1/S2).** Hypothesis: any folder with no self-signals but ≥N game-signal children is an organizer; recurse into children — name-agnostic, no store-name lists. Measure which currently-skipped folder shapes the generic signal path would correctly recover, and what noise/filters are actually blocking them (measure before proposing any filter change). **Amended 2026-09-07 (user model):**
  - **Exact-match terminating rule (the Neverwinter fix, refined 2026-09-07):** if an exe at the folder root has a stem that matches the folder name, then **folder name = game folder, that exe = game exe, DO NOT process deeper.** This is a *terminating signal*, not just a +15 scoring bonus. Concretely: `Neverwinter_en\Neverwinter.exe` → `neverwinter` stem ≡ `neverwinter_en` token → folder is the game, `Neverwinter.exe` is the primary; `Neverwinter_en\Neverwinter\Live\x64\GameClient.exe` (rel-4) and the double-nested duplicate are **never promoted**. The match check MUST run **before** any deep-exe processing: when 3+ candidate exes exist at various subfolder depths, evaluate signals **and** the `foldername ≡ gamename` match first; if it hits, skip deeper candidates outright.
  - **Working-set pruning (2026-09-07):** the terminating rule shrinks the working set the client sees — matched folder + matched exe are the ONLY candidates; deeper exes (nested `GameClient.exe`) are excluded before collection. Pipeline principle: large data → classify/categorize quickly → dedicated per-scenario processing → filter → understand hierarchy → iterate → process. Do NOT run full scoring on every entry.
  - **Catalog / sibling-entity signal (2026-09-07):** a terminating match identifies not just the game folder + exe, but the *level* at which the match fired. Siblings at that level (`othergame_en` beside `neverwinter_en`) are separate entities — possibly games, but NOT this game. This is the container/catalog recognition (like the existing collection logic): parent = catalog, matched children = distinct game entries. **IMPLEMENTED in Python (Tier 1.5 / probe stage 13); fixture `ArcInstall/` + S-D2 matrix + baseline entries added.**
  - **Whole-name match + backup guard (2026-09-07):** the match accepts TOKEN (`neverwinter`≡`neverwinter_en`) AND WHOLE-NAME normalized forms (`Diablo III`↔`Diablo III.exe`, `Dead Space 3`↔`deadspace3.exe` — 7 real corpus cases). Leading-dash/copy-of backups (`-Penumbra.exe`, `copy of X.exe`, numbered `10 org X.exe`) NEVER satisfy the terminating rule (E5 guard preserved).
  - **Single-child folders are NOT collections.** A folder with exactly ONE child that contains a deep game exe is a *game with deep nesting*, not a collection. The current code treats single-child chains as containers and can promote a deep subfolder (e.g. Neverwinter `Live` instead of `Neverwinter_en`). E1 must test: a single-child chain collapses to the game folder.
  - **Proof-counter / re-scan mechanism.** Implement the "1 proof / 2 proof" model: the first game-shaped finding under a folder tentatively marks it a collection (proof 1); a second confirms it (proof 2); once confirmed, previously-skipped children are re-scanned with the simplified "each child is a game-folder base" rule.
  - **Path-divergence / aggregate signal.** Many sibling game folders diverging from one parent (`game1/game2/game3`) is a collection signal. Optimization candidate: avoid re-parsing a confirmed collection parent for each child.
  - **Depth budget note:** a collection level consumes recursion depth, but the corrected depth analysis (2026-09-07) shows real game exes are at rel 1-4; collection recognition only needs the shallow game-shaped-child signal. No depth increase.
- **E2 — Store typing via child signals + global manifests (S1/S2).** Type children by their own signals (`.item`/`.mancpn` cross-ref to ProgramData for Epic, `.build.info`/`.product.db` for BattleNet). No path/name-based store guessing. **Amended 2026-09-07: Steam ACF is OUT OF SCOPE** — Steam is a separate locked detection mechanism (`steamapps/common/` + ACF); Steam children never appear in the generic scanner (SteamLibraryScanner handles them). E2 covers only non-Steam store typing (Epic, BattleNet, and child-of-container manifest cross-ref).
- **E3 — Nesting depth (S2).** One-level-below publisher/launcher trees: test recursion depth and signal propagation rules against corpus; verify the `install` substring pattern's actual impact before touching it. **Rewritten 2026-09-07 (depth analysis corrected):**
  - **Premise overturned.** Real (non-noise) game exes sit at rel 1-4 (43/28/10/14 folders); deeper paths are noise/emulators/backups (Cemu, fceux, steambackup, vcredist, `resize 66%.exe`, CrashReportClient). The deepest real games are UE `*-Shipping.exe` (rel 5) and Neverwinter `GameClient.exe` (rel 5-6, a duplicate-install artifact — the real entry is rel-2 `Neverwinter.exe`).
  - **Do NOT raise WALK_MAX_DEPTH or add deep-walk container logic.** A depth-6 container walk was tried and REVERTED (would surface ~20 tools for ~4 real deep games).
  - **E3 scope now:** (a) validate depth-1-4 coverage is complete for the scenario catalog; (b) confirm the UE-shipping fast-path handles rel-5 `*-Shipping.exe` cases; (c) verify the `install` substring pattern's impact; (d) ensure Neverwinter resolves to `Neverwinter.exe` (rel 2), not the nested `GameClient.exe` duplicate.
- **E5 — Fallback exe admission (S4).** Test admitting exes in redist-like subfolders when nothing else exists, with installer-noise filtering intact; measure false-positive risk on corpus.
- **E6 — PE-title guard relaxation (S5/S9).** Test relaxing the folder-token-share guard for acronym/short-folder cases; measure wrong-title risk on corpus (pe_metadata_blacklist + generic-label checks stay). **Note: this is C# title-selection logic (`FolderScanner` `SharesNameToken` guard) — the Python scanner has no equivalent guard (Python title resolution is ExeStem/manifest only; see 2026-09-06 probe finding). Implementation requires PE metadata, i.e. the Windows selective-visit data (P2.2).**
- **E6a — bare `"tool"` scoring penalty (S-G).** Distinct from E6. Python `_TOOL_NAMES` and C# `tier_10_dev_editor_tools` both lack bare `"tool"`; currently masked by the exact-folder-stem `+15` bonus (deterministic pick). Add `"tool"` to the penalty tier in both scanners once corpus evidence justifies it (no false-positive regression).
- **E8 — Title persistence (S7).** Write-policy experiment: explicit pick always writes DisplayName; confident auto-resolve (single clean page / exact-stem match) writes too. Verify rescan merge preserves the title.
- **E7 — Identity query pipeline (S6/S8/S9) — LAST.** Ordered candidates: PE title → exe stem (extension stripped) → exe stem with separators → display name → folder name; first clean PCGW result set wins; exact-match on exe stem. Spot-check queries against PCGW (rate-limited) on corpus cases.

### Phase 4 — C# port (accepted experiments only)

Port order (2026-09-07): **proven Python production fixes first**, then remaining accepted
experiments.

5. Port the **already-accepted Python fixes** (highest value, corpus-verified):
   - Parent-bound children rule (E4): platform/build/redist children belong to the parent game; container check skips them; parent promoted with their exes.
   - Redist fallback admission (E5): admit a redist-only real exe as primary when no root exe exists (penumbra shape).
   - Deterministic tie-break (`-score, base_name`) in all scorers.
   - Collection rules: stray root files do not break collection detection; deep-nested collection games resolve to the parent (never the platform child); single-child folders are games, not collections.
6. Then port remaining accepted E1/E2/E3/E6/E7/E8 experiments:
   - `FolderScanner.cs`, `ContainerScanner.cs`, `ExecutableDiscovery.cs` — structural container logic, depth/signal rules, fallback admission, PE-title guard.
   - `FileSystemHelper.cs` + `data/blacklist.json` — only pattern changes proven necessary by E1/E3/E5/E6 measurements.
   - Persist PE FileDescription in `PlatformMetadata` at scan (single source of truth for F3 + auto queue).
   - Identity hints threaded through `MetadataService`/`PcgwLookup`; `MainWindow` F3 builds pipeline queries and writes picked titles via `UpdateGameEntry` + `TitleSource` marker.
   - `TitleText` / `PcgwTitleFilter` — exe-stem variants and exact-match key fix.
   - Confidence-tier marking (§0.1) on emitted entries, derived from the signals that produced them.

### Phase 5 — Tests & validation

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
| `tools/validate_probe_matrix.py` | Validation matrix (now **18 scenarios** incl. S-D2 exact-match terminating) |
| `testdata/samples/d-games.txt` | Sample EXE list fixture (harness input; already copied by user — no manual reads) |
| `testdata/samples/full-d.txt` | Full filtered file listing fixture (12,193 lines; harness input — aggregate-only, no paths in docs) |
| `testdata/samples/full-p.txt` | SECOND corpus: `P:\Program Files (x86)` listing (18,106 lines, added 2026-09-07). **Separate scenario (§8.3)** — stored for future fixture-path plan, NOT consumed by this plan's experiments |
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

- [x] Phase 0: Steam confirmed Locked and untouched (T1)
- [x] Phase 0: Missing-Epic-Manifest recovery order defined — `.mancpn` → binary `.manifest` → store lookup; no invented UUIDs (T2)
- [x] Phase 1: Parity divergence report exists — `planning/103-detect-py-port-status.md` refreshed 2026-09-06 (T3)
- [ ] Phase 1: Decision model documented as the §2.1 decision table (evidence → accept → reject → next) — **DONE 2026-09-06 (corpus-corrected: signal depth ≤ 2, container parent non-promotion)**
- [ ] Phase 1: Exclusion taxonomy documented (§2.2) with exclude-vs-penalize per bucket; no bucket change without corpus evidence — **DONE 2026-09-06 (no bucket justified for change by corpus)**
- [x] Phase 1: Corpus inventory produced from full-d.txt (nesting histogram, exe-in-subfolder, acronym stems, blacklist-noise ratio) — aggregate only, no paths published — **DONE 2026-09-06**
- [x] Phase 1: Scenario catalog (S-A..S-J) with expected outcomes; each §1 symptom mapped to a scenario — **DONE 2026-09-06 (S4 CONFIRMED — penumbra redist-only; S-F count 1)**
- [x] Phase 1: Probe contract recorded (§2.4) — full JSON schema, chain ordering, tier derivation, validation plan — **DONE 2026-09-06**
- [ ] **Gate: decision model + taxonomy + scenario catalog reviewed; no detection behavior change before review** — **Amended 2026-09-07: the gate now reviews the FINAL Python scanner as the reference** (the paper model PLUS the 7 applied production fixes: E4/E5, deterministic tie-break, store-coverage, collection×2, depth correction). No further behavior change until this is reviewed.
- [x] Phase 2: `--probe <folder>` renders the §0.3 decision path (rule → evidence → kept/rejected with reason → next → tier) — **DONE 2026-09-06**, verified on Steam/Epic/standalone/container/penumbra/ELEX fixtures
- [x] Phase 2: Probe validation per §2.4.7 (one fixture per scenario S-A..S-J asserting tier == expected) — **DONE 2026-09-06**: `tools/validate_probe_matrix.py` → **17/17 PASS** after E4/E5 + deterministic tie-break + **store-coverage fixtures added for all 9 corpus store types (GOG, EA, Ubisoft, Blizzard, SteamEmu, Xbox, Rockstar)** — regression guard so detection fixes cannot silently break store typing. (S-G latent scoring gap recorded as **E6a**: bare `"tool"` not penalized in Python `_TOOL_NAMES` **or** C# `tier_10_dev_editor_tools` — currently masked by the exact-stem bonus; separate from E6 PE-title guard.)
- [x] Phase 2: Selective-visit list produced from the inventory (no "scan every exe", no scanner behavior change) — **DONE 2026-09-06**: `tools/selective_visits.py` → 14 symptom-driven visits, JSON at `testdata/samples/selective-visits.json`
- [x] Phase 2: Baseline runs (Python vs C#) over the scenario corpus emit divergence per scenario and per symptom — **DONE 2026-09-06 (Python side, now 10/10 clean)**: `tools/baseline_compare.py`; E4/E5 production fixes applied in Python → 0 wrong-folder promotions (Elex→`system/ELEX.exe`, Penumbra→`redist/PENUMBRA.EXE`), 0 missed, 0 false positives. Report: `testdata/samples/baseline-report.json`. C# side deferred to Phase 4 (Python-first)
- [x] Phase 3 (partial): E4/E5 implemented in Python production scanner (parent-bound children; base-name scoring; dash-backup penalty); validation matrix now 10/10 PASS — **DONE 2026-09-06**
- [x] Phase 3 (partial): Collection fixes applied in Python — stray root files no longer break collection detection; deep-nested collection games resolve to the parent; regression fixtures `CollectionWithStray/` + `CollectionDeepNest/` + baseline checks — **DONE 2026-09-06/07**
- [x] Phase 3 (partial): **Exact-match terminating rule (E1)** implemented in Python — Tier 1.5 in `scan_directory`, probe stage 13, working-set pruning (nested `GameClient.exe` excluded), catalog/sibling-entity signal; fixture `ArcInstall/`; matrix **18/18 PASS**; baseline **11 scenarios, 0 missed / 0 wrong-folder / 0 FP** — **DONE 2026-09-07**
- [ ] Phase 3: Single-child-not-a-collection rule + proof-counter/re-scan mechanism (E1) — **PENDING (amended 2026-09-07)**
- [ ] Phase 3: Depth-1-4 coverage validation + UE-shipping rel-5 fast-path confirmation; NO WALK_MAX_DEPTH increase (E3, rewritten 2026-09-07)
- [ ] Phase 3: Each experiment recorded with before/after corpus metrics (recall, false positives) and acceptance decision
- [ ] No hardcoded path/store-name detection rules introduced anywhere
- [ ] No blacklist/filter change without corpus evidence
- [ ] Every entry carries a tier (Locked/Secure/Candidate/Unknown) derived from its signals; no folder name becomes a confident title alone
- [ ] Accepted experiments show: neverwinter-style single-child nesting (**resolved by the exact-match terminating rule: `Neverwinter_en\Neverwinter.exe` → game folder + primary, nested `GameClient.exe` never promoted**), engine-subfolder games titled as the game (not the subfolder), redist-only exes resolved, acronym PE titles applied (mmxl/ja2), exe-driven PCGW identity (aamfp/ja2/dungeonoftheendless), F3 picks persist through rescan
- [ ] C# port matches Python outcomes on all corpus fixtures
- [ ] Full C# suite green (539 + new, 0 regressions); `planning/103` parity notes updated

## 8. Follow-ups (separate plans, not this one)

### 8.1 Steam ACF repair (secondary, later)

Steam path detection is Locked and correct. ACF issues — mismatched/missing/moved ACFs, cross-library
`appmanifest_*.acf` resolution, orphaned `common/` folders — are a simple, self-contained process and
should get their own plan once detection tightening lands. Reference: `planning/94-game-detection-overhaul.md`
(SteamLibraryScanner) and `docs/` Steam notes. **Update 2026-09-07:** the §8.3 Program Files plan
adds `libraryfolders.vcf` parsing as the master Steam library index — cross-library ACF resolution
and orphaned-`common/` reconciliation become a single-file pass, so §8.1 is subsumed by §8.3 rather
than a separate effort.

### 8.2 Epic catalog deep-dive (if Missing-Manifest work grows)

Plan 121 (`planning/121-epic-manifest-vfs-investigation.md`) already models Installed/Missing/Orphaned.
If Phase 0.3 reveals more than a contained fix, promote it to a dedicated plan rather than expanding
this one.

### 8.3 Program Files (x86) known-fixture layout — SEPARATE SCENARIO (planned 2026-09-07)

**Do NOT fold into this plan.** The messy standalone `d:\games` corpus (Plan 123) is a DIFFERENT
scenario from the organized "Program Files (x86)" layout. Fix Plan 123 fully first, then tackle this
as its own plan.

**Corpus:** `testdata/samples/full-p.txt` (18,106-line listing of `P:\Program Files (x86)`, copied
2026-09-07; aggregate analysis only — no raw paths in docs).

**Key insight (user, 2026-09-07):** when the user points the scanner at a "Program Files" folder
(rather than a dedicated games folder), the **store-client folders are KNOWN FIXTURES** — same idea
as Steam's locked `steamapps/common/<GameFolder>` structural path. The store clients install games
under fixed, well-known sub-paths, so the fixture path itself is a strong signal:

| Fixture root (under scan root) | Games land at | Observed in full-p.txt |
|--------------------------------|---------------|------------------------|
| `GOG Galaxy\` | `GOG Galaxy\Games\<Game>\` (each has `goggame-<id>.info` = Secure marker) | 13 games: Arx Fatalis, Baldurs Gate 3, Cyberpunk 2077, Deus Ex Mankind Divided, Fallout 4 GOTY, God's Trigger, Gothic 1 Remake, Metro Exodus, Monkey Island 2 SE, System Shock Remake, The Witcher 3, beneath_a_steel_sky, gothic_2_gold_edition |
| `Ubisoft\` | `Ubisoft\Ubisoft Game Launcher\games\<Game>\` | 2 games: Immortals Fenyx Rising, Tom Clancy's The Division 2 |
| `Steam\` | **`Steam\steamapps\libraryfolders.vcf` → ALL Steam libraries on ALL disks** (see below); games at `<library>\steamapps\common\<Game>\` | already LOCKED (Steam scanner); note `Steamworks Shared` = known non-game (redist-only) |
| `EA\` | `EA Games\<Game>\` / `Origin Games\<Game>\` (registry-driven) | folder empty in this snapshot |
| Everything else | **not games** | Microsoft SDKs, Microsoft Visual Studio, Windows Kits, uTorrent = pure tooling/noise |

**Steam library bootstrap — `libraryfolders.vcf` (user insight, 2026-09-07):**
`P:\Program Files (x86)\Steam\steamapps\libraryfolders.vcf` is a VDF file that enumerates **every**
Steam library folder on **every** disk (each `"path"` entry = a library root containing
`steamapps\common\<Game>\` + `appmanifest_<appid>.acf`). From this **single file** the scanner can
populate the complete Steam library set — all drives, all libraries — without scanning each disk
root or guessing locations. This is the *master index* for Steam:
- Read `libraryfolders.vcf` → one entry per library root (path + library id).
- For each library root: enumerate `<library>\steamapps\common\*` (Locked games) + cross-ref
  `appmanifest_*.acf` (appid → folder mapping, install state).
- Note: `full-p.txt` is a filtered listing (dll/txt/exe/manifest/info/acf/lnk) so `.vcf` files are
  absent from the corpus text, but the file exists on any real Steam install.
- This also resolves the deferred §8.1 Steam ACF cross-library work: with `libraryfolders.vcf` as the
  index, "orphaned `common/` folders" and "mismatched/moved ACFs" become a single-file reconciliation
  pass instead of a guess.

**Steam registry bootstrap — how we FIND the Steam install (user, 2026-09-07):**
The only registry signal needed is the Steam **install path**:
- `HKLM\SOFTWARE\Valve\Steam` → `InstallPath` = `P:\Program Files (x86)\Steam`
  (verified in `/mnt/r/hklmvalve.reg`; the HKCU `valve.reg` `SteamPath`/`SteamExe` are equivalent
  but the HKLM `InstallPath` is the canonical one — **ignore all `Apps\*` subkeys**, they are
  irrelevant to path discovery).
- Chain: `InstallPath` → `<InstallPath>\steamapps\libraryfolders.vdf` → all library roots.
- This is a **known-fixture lookup** (like `EpicManifestPaths.DefaultManifestsDir`), NOT a
  detection-scoring rule — it only locates the Steam client, then the existing locked
  `SteamLibraryScanner` takes over.

**`libraryfolders.vdf` structure — PATH LOCATOR ONLY (parsed from `/mnt/r/libraryfolders.vdf`, 2026-09-07):**
```
"libraryfolders"
{
    "0" { "path" "P:\\Program Files (x86)\\Steam"  "apps" { "10110" "..." ... } }   ← 7 apps
    "1" { "path" "E:\\SteamLibrary"                 "apps" { "730" "..." ... } }    ← 65 apps
    "2" { "path" "D:\\SteamLibrary"                 "apps" { "2320" "..." ... } }   ← 63 apps
}
```
**Authority rules (user, 2026-09-07):**
- `libraryfolders.vdf` answers ONE question: **WHERE are the Steam libraries?** It is a *path
  locator* — it does NOT say what a library contains.
- The catalogue structure is identical for every library regardless of which key:
  `<library>\steamapps\appmanifest_*.acf` + `<library>\steamapps\common\<GameFolder>\`.
- **ACF + folder scan is the ONLY authority on content.** `libraryfolders.vdf` never overrides,
  augments, or contradicts ACF+folder findings. The `apps` blocks inside the vdf are
  **informational only (appid → size-on-disk) and are ignored** — the ACF scan is authoritative.
- Consequence: the bootstrap produces ONLY a list of library paths. Every library path is then
  scanned with the existing `SteamLibraryScanner.Scan(root)` (ACF cross-ref + `common/`
  enumeration), which already implements the correct authority model.

**Virtual catalog goal (user, 2026-09-07):** the launcher shows ONE catalog per source type —
**"Standalone"**, **"Epic"**, **"Steam"** — NOT one catalog per physical library root
(`Steam-1`, `Steam-2`, …). Multiple Steam library paths all feed the single "Steam" catalog:
- The game database already records which physical root each game belongs to (`LibraryRoot`); the
  catalog grouping is by `GameSourceKind`, not by root path.
- So "add all Steam libraries" adds N roots internally, but the UI presents them under ONE "Steam"
  node. The root paths remain metadata (per-game), never a catalog axis.
- Implication for `SteamLibraryScanner`: it already scans ALL configured paths and cross-refs ACFs
  (`ScanAll`); the catalog is the natural single grouping. No per-root UI entries.

**ACF cross-library remediation (user, 2026-09-07):** with all library paths + all ACF paths known
(`<lib>\steamapps\appmanifest_*.acf`), cross-library mismatches become detectable and plan-able:
- Example: game folder on `D:\SteamLibrary\steamapps\common\<Game>\` but its `appmanifest_<id>.acf`
  lives on `E:\SteamLibrary\steamapps\` (Moved status today).
- The bootstrap knows every ACF path, so we can report the mismatch and plan a **remediation
  action** (e.g., move the ACF from E to D, or vice versa) rather than just displaying "Moved".
- This is a plan-time/UX proposal: detect → propose remedy → user-confirmed action. It does NOT
  change detection authority (ACF+folder still decides content); it adds a repair pathway on top.

**First-launch / F4 offer-to-add UX (planned, mirrors Epic):**
`LibrarySetupViewModel` already offers `AddEpicCatalogAsync` (parse ProgramData manifests, add as a
root). Add the Steam equivalent — with TWO affordances:
- **Offer "Add all Steam libraries" (fresh setup).** `CanAddSteamLibraries` — true when the
  registry `InstallPath` resolves and `libraryfolders.vdf` yields ≥1 library path. `AddSteamLibrariesAsync`
  parses the vdf → install path + all other library roots → adds EACH **not already configured**
  as a `GameSourceKind.Steam` root, then scans each with `SteamLibraryScanner` (ACF+folder).
  Mirrors `AddEpicCatalogAsync` (add entry → `ScanAndSaveAsync` → remove on failure → refresh
  `CanAdd*`). On a fresh setup no root is configured, so all library paths are added.
- **Offer "Rescan Steam libraries" (existing setup).** When ≥1 Steam root is ALREADY configured,
  the same bootstrap detects paths in the vdf that are **not yet added** (user added a library in
  Steam) and paths that are configured but **no longer in the vdf** (user removed a library) —
  offer to add/remove accordingly. This is a *reconciliation*, not a re-authority: the vdf still
  only tells us WHERE; ACF+folder scan decides what each (new) library contains.
- Surfaced on first-run onboarding AND the F4 Library Setup dialog, next to the existing Epic offer.
- Registry access via the existing `IRegistryReader` abstraction (mock-able, no hardcoded paths in
  tests); Windows-only production path already gated by `OperatingSystem.IsWindows()`.
- **Test plan:** mock `IRegistryReader` + a fixture `libraryfolders.vdf` mirroring the real 3-root
  shape (install-as-`"0"` + 2 external libs). Assert: (a) fresh setup → all 3 roots added;
  (b) a root already configured is NOT double-added; (c) a vdf path absent from config is offered
  by "rescan"; (d) a configured root absent from the vdf is offered for removal; (e) `apps` blocks
  are ignored (a library with `apps` but no `common/` yields no games).

**Why fixtures are legitimate here (distinct from "hardcoded game paths"):** the fixture is a
**store-client layout contract**, not a per-game rule. `GOG Galaxy\Games\<X>` means "X is a GOG game"
for ANY X — exactly like Steam's `steamapps/common/<X>`. The child count/signals do not need to be
re-scored from scratch; the store knows its own layout. `goggame-*.info` and Ubisoft manifests make
them **Secure** under the existing tier model (§0.1).

**Plan shape (new plan, after 123 completes):**
1. Recognize store-client fixture roots under a user-pointed root (GOG Galaxy, Ubisoft, EA, Steam).
2. **Steam bootstrap first (highest value):** read `HKLM\SOFTWARE\Valve\Steam\InstallPath` (registry)
   → `<InstallPath>\steamapps\libraryfolders.vdf` → all library roots → enumerate
   `steamapps\common\*` per library (Locked) + ACF cross-ref; reconciles §8.1.
3. **Offer-to-add UX (F4 + first-run):** `CanAddSteamLibraries` / `AddSteamLibrariesAsync` in
   `LibrarySetupViewModel`, mirroring the existing Epic manifests offer.
4. Each remaining fixture's known games sub-path yields Locked/Secure game entries directly (GOG
   `Games\`, Ubisoft `...\games\`).
5. Non-fixture top-level folders (Microsoft SDKs, Visual Studio, Windows Kits, uTorrent) are
   excluded as non-game tooling — the generic scanner must not attempt deep-dive scoring inside them.
6. Registry-driven stores (EA/Origin/Ubisoft install dirs) cross-ref registry fixtures
   (`testdata/mock/registry/`) for game locations outside the fixture path.
7. Reuse the probe/validation matrix approach from this plan's Phase 2.

**Fixture type vs. generic scanner:** the generic signal-scoring path stays for the messy standalone
case (this plan); the fixture path is a structural fast-path that fires only when the user points at
a known store-client root layout. Both feed the same tier model.
