# 126 — Facts: EOSH Launcher Reverse Engineering (DISCOVERY, in progress)

Status: DISCOVERY (2026-09-09). Objective: determine how the launcher decides "report/keep/update" vs "delete" for `.item` installs, and whether it can be influenced/bypassed. Target: `P:\Program Files\Epic Games\Launcher\Portal\Binaries\Win64\EpicGamesLauncher.exe` (52,649,912 bytes, UE5 **20.2.9-57799698+++UE5+Release-Distro-5.5**, PE x86-64).

Latest (2026-09-10, fresh session): **CORRECTED the keep/remove gate model.** The `vtable[0xd0]` result is NOT a gate — both empty and non-empty FString outcomes flow into the removal log + removal call; the string only formats the log message. The only KEEP paths in phase-2 are (1) the `[obj+0xec]` skip flag (via `vtable[0x8]`) and (2) **`vtable[0x48]` on the global singleton `0x14316fed0`** (a Meyers-static class instance) returning TRUE. The 7-field hash lookup is a **duplicate detector** (hash-match → "removing stale duplicate … still installed elsewhere" → also removal). Reconcile fn `0x141da6250` has NO direct call sites — it is invoked via a delegate/event (EoshInstallationsChanged pattern). EOSH `get-installed-apps-response` JSON schema strings fully mapped.

Previous "Latest" (2026-09-09): found the real removal path (reconcile fn `0x141da6250`; removal block `0x141da9b1c` → `0x141db0ab0`), the log-record xref trick (string−0x28), and the C++ invariants (`EoshInstallation.ArtifactId == Manifest->GetBinaryId().GetArtifactId()`).

## Binaries

- `EpicGamesLauncher.exe` — main portal (52 MB) — the EOSH reconciliation code lives here
- `EOSSDK-Win64-Shipping.dll` (35 MB) — EOS SDK
- `EpicGamesUpdater.exe` (3.4 MB) — self-update/queryinstallation
- Sections: `.text` @0x400→0x140001000 (size 0x24868cc), `.rdata` @0x2495c00→0x142497000 (0xadef6e), `.data` @0x2f74c00→0x142f76000

## Key strings found (UTF-16, verified in .rdata)

| VMA | String | Role |
|---|---|---|
| 0x142922130 | `EoshInstallationsChanged: Received new revision '%s'` | revision event (record @0x142922108, line 4428, xref 0x141da633c) |
| 0x1429223C0 | `EOSH reconciliation: removing installation not reported by EOSH '%s' (InstallationId %s, AppId %s).` | **the deletion log** (record @0x142922398, line 4743, xref 0x141da9b47) |
| 0x1429222A0 | `EOSH reconciliation: removing stale duplicate installation '%s' (InstallationId %s; AppId %s still installed elsewhere).` | **dup-removal log** (record @0x142922278, xref 0x141da95ee — hash-MATCH path) |
| 0x142922C00 | `FCommunityPortalAppManagerImpl: Keeping discovered manifest` | the keep path (record @0x142922BE0, line 4980, xref 0x141ddadf8) |
| 0x14291E5D0 | `D:\build\++Distro\Sync\Distro\Source\Programs\EpicGamesLauncher\Application\Private\CommunityPortalAppManager.cpp` | source file (in every log record) |
| 0x14276A7F8 | `%s:%s:%s` | AppId builder (ns:itemId:appName), used at 0x141da94f6 |
| 0x1424A8794 | (empty string) | default empty `%s` in log/compare calls |
| 0x142770708 | `skipeosh` | command-line/config switch (xrefs 0x14122fc8d) |
| 0x1427706E0 | `forcereinstallegu` | command-line switch (parsed with skipeosh) |
| 0x142918260 | `Launcher.EoshEnabled` | config key (xref 0x141d29a7c) |
| 0x142918290 | `Launcher.EoshDisabled` | config key (xref 0x141d296cb) |
| 0x142921450 | `forceEoshMcp` | config key (xref 0x141dbd91d) |
| 0x142921910 | `LauncherUsingEOSH` | log/config (xrefs 0x141dd8e66, 0x141dd9004) |
| 0x1429225B8/5D0/5F0 | `Error.bSucceeded()`, `!NewRevision.IsEmpty()`, `EoshInstallation.ArtifactId == Manifest->GetBinaryId().GetArtifactId()`, `Manifest.IsValid()` | **the C++ invariants EOSH reconciliation enforces** (ensure texts) |
| — | `GameId.IsValidId()` (via 0x1429239B8 region) | helper 0x141d8c950's ensure text, line 7258 |

Also: `EoshState = Enabled/Disabled/Resetting/...`, `Skipping EOSH login: ...`, `EOSH polling has been disabled`, `eosh_disabled_%s`, `EoshDisabledReason`, `bForceEoshMcp`, `errors.com.epicgames.eosh.cant_parse_response`, `errors.com.epicgames.eosh.not_logged_in`.

### Log-message record layout (.rdata, 0x20 bytes before each string)

| off | content |
|---|---|
| +0x00 | fmt string ptr |
| +0x08 | source file ptr (0x14291E5D0) |
| +0x10 | `verbosity<<32 \| line` (e.g. 0x500001287 = verbosity 5, line 4743) |
| +0x18 | category object ptr (0x1431AB97C/77/86/5E0) |

**Xref-scan tip: scan for LEA→(string_VMA − 0x28), not LEA→string_VMA.**

## Reconciliation decision code (first mapping)

**Keep-vs-remove helper at `0x141d8c950`** (called from `0x141d8c537` and `0x141da7fb3`):
```
cmp [rdx+0x48], 0 ; je FAIL(ensure: GameId.IsValidId() @ CommunityPortalAppManager.cpp:7258)
cmp [rdx+0x10], 0 ; je FAIL
cmp [rdx+0x8],  0 ; je FAIL
cmp [rdx+0x28], 0 ; je FAIL
cmp [rdx+0x20], 0 ; je FAIL
cmp [rdx+0x40], 0 ; je FAIL
cmp [rdx+0x38], 0 ; je FAIL
; all non-zero -> jmp 0x141d8d140 (KEEP path)
; FAIL -> ensure-trap via 0x142489db0  (NOT the EOSH removal log — that lives in reconcile fn @0x141da9b47)
```

### The 7 required fields = 5 FStrings (struct layout confirmed by the `.item` JSON parser at `0x140d89330`)

The record (`rdx`) is a struct of 5 `FString`s (16 bytes each: data ptr at +0, count at +8), so the 7 checked DWORDs are:

| Offset | FString member | Checked via | JSON key (parser) |
|---|---|---|---|
| +0x00 | **CatalogNamespace** | count at +0x08 | `namespace` → `[r13+0x00]` (xref `0x140d8935b`, write `0x140d89375`) |
| +0x10 | **CatalogItemId** | data at +0x10 | `catalogItemId` → `[r13+0x10]` (xref `0x140d893af`, write `0x140d893ca`) |
| +0x20 | **AppName** | data +0x20, count +0x28 | `appName` → `[r13+0x20]` (xref `0x140d89404`, write `0x140d8941f`) |
| +0x30 | 4th string | count at +0x38 | (likely InstallationGuid or DisplayName) |
| +0x40 | 5th string | data +0x40, count +0x48 | (likely ManifestLocation or InstallLocation) |

**Meaning:** the keep-vs-remove helper only checks that the record's identity strings are non-empty (namespace, catalogItemId, appName + 2 more). **It does NOT check EOSH/server state at all** — it validates the local record's shape. (Note: this helper is a record-validity pre-check with an ensure-trap; the actual keep/remove decision is made by the reconcile gates described below.)

**IMPORTANT CORRECTION to earlier theory (2026-09-10):** the earlier claim that the reconcile gate = "7-field hash lookup (keep at 0x141da9544) + `vtable[0xd0]` non-empty" is **superseded**. Verified: the hash lookup's match path REMOVES (stale-duplicate log), and `vtable[0xd0]` is log-only. The REAL gates in reconcile phase-2:
1. `[obj+0xec] != 0` skip-flag (via `vtable[0x8]` at `0x141da93ab`) → KEEP
2. **`vtable[0x48]` on global `0x14316fed0`** (= EOSH-reported-set membership) → KEEP
3. `0x141d8c950` = 5-string record validity (ns/catId/appName + 2 — sanity ensure, trap if empty)

### Caller flow (reconciliation, `0x141da7f40` region)

```
call 0x141dadff0  ; build/parse the installation record
call 0x141d8c950  ; validate 5 strings (keep/remove helper)
```

### The EOSH-reported-set query (the REAL gate) — CORRECTED 2026-09-10

**The earlier `vtable[0xd0]`-as-gate reading is WRONG.** Verified in this session: both outcomes of the `cmp [rax+0x8],0` after `call [vtable+0xd0]` at `0x141da9532` flow into the removal log + removal call:
- empty FString (`je 0x141da9b1c`): `lea rsi,[empty]` → falls into `0x141da9b23`
- non-empty FString (`0x141da953c`): `mov rsi,[rax]` → `jmp 0x141da9b23`
- `0x141da9b23` = the removal log (`record 0x142922398`) + `call 0x141db0ab0` (REMOVE)

The `vtable[0xd0]` FString is used ONLY to populate the log's InstallId field. **It is not a keep gate.**

The genuine gate structure inside reconcile `0x141da6250` phase 2 (loop over local installs, `[rbp-0x50]` FCountedString list, 16-byte `{ptr,count}` elements):

1. `vtable[0x8]` call at `0x141da93ab` → `[obj+0xec] != 0` → `jne 0x141da9f78` = **KEEP** (skip-flag check)
2. **`vtable[0x48]` on the global singleton `0x14316fed0`** at `0x141da942b` (this = `[rip+0x13c6aac]`, arg rdx = 5-FString record `[rbp+0x10..0x58]` from `vtable[0xc0]` at `0x141da9377`) → `test al,al; jne 0x141da9f78` = **KEEP** (EOSH-reported-set membership)
3. Otherwise → **REMOVE** (two flavors below)

The 7-field hash lookup at `0x141da9493` (table `[rbp+0x7a0]`, stride 0x68, bucket compare at 0x141da9448) is a **duplicate detector among the reconciled set, not a keep gate**:
- hash-match path `0x141da9544` → logs `record 0x142922278` ("removing stale duplicate installation '%s' … still installed elsewhere") at `0x141da95ee` → also REMOVES
- hash-miss path `0x141da94e4` → builds AppId `%s:%s:%s` (0x14276A7F8) → `vtable[0xd0]` (log-only) → `0x141da9b1c`/`0x141da9b23` → logs `record 0x142922398` ("removing installation not reported by EOSH") → REMOVE

Removal call signature at `0x141da9b84`: `rcx=r14` (manager, from `0x141da94da: mov r14,[rbp-0x70]`), `rdx=1` (`dl,0x1`), `r8=lea [rbp+0x10]` (the 5-FString key), `r9=vtable[0xb8]` result → `call 0x141db0ab0`.

**Implication:** for TR/DS (kept) vs orphans (removed), the deciding factor is `vtable[0x48]` on the global `0x14316fed0` — i.e. membership in the EOSH-reported set held by that singleton. If the singleton is populated server-side (from `get-installed-apps-response`), no local `.item`/manifest content can satisfy it — consistent with the observed server call before deletion and the reinstall-only recovery path.

**Config/command-line parsing** (`0x14122fc60` region): reads `forcereinstallegu` then `skipeosh`; `xor sil,1` inverts skipeosh → a flag used downstream. `skipeosh` is a real switch in the binary.

**EOSH enable/disable config keys**: `Launcher.EoshEnabled` / `Launcher.EoshDisabled` referenced at `0x141d29a7c` / `0x141d296cb` (in the EOSH state module `0x141d29000`-ish). `forceEoshMcp` at `0x141dbd91d`. `LauncherUsingEOSH` at `0x141dd8e66`/`0x141dd9004`.

## EOSH config in ini files

`Portal/SysFiles/Engine.ini`:
```
[OnlineSubsystemMcp.EoshMcp]
bUpdatesConnectionStatus=false
...
[EOSH]
BootstrapUserBasePercentage=0.01
BootstrapUserBasePercentage2=0.01
BootstrapUserBasePercentage3=1.00
BootstrapMinimumVersion=4.0.0
McpUserBaseFraction5=1.0
ResolveDependencies=true
EoshInstallErrorEnsureFraction=0.2
```
`Portal/Config/DefaultPortalRegions.ini`:
```
[Portal.OnlineSubsystemMcp.EoshMcp]
Domain=localhost:'port
Protocol=http
ServiceName=
```
`Portal/Config/DefaultEngine.ini`:
```
; Never route loopback EOSH calls through a system proxy
HttpNoProxy=localhost,127.0.0.1
```

## CRITICAL TOOLING FINDING — how log-string references work (2026-09-09, fresh session)

**Direct xref scans for the log strings FAILED** (`LEA [rip+disp]` → string VMA → no hits, and `movabs` → none). Reason found: **the code does NOT reference the string address itself.** It references a **log-message record** located **0x28 bytes before** the string text:

```
record { fmt* ; file* ; verbosity|line ; category* }   (4 × 8 bytes = 0x20, record is the LEA target)
string text ... 0x28 bytes after record start (record+0x28 points BACK? no)
```

Actual layout at the record:
| offset | content |
|---|---|
| +0x00 | pointer to the format string (0x1429223C0 etc.) |
| +0x08 | pointer to source file (0x14291E5D0 = `D:\build\++Distro\Sync\Distro\Source\Programs\EpicGamesLauncher\Application\Private\CommunityPortalAppManager.cpp`) |
| +0x10 | `verbosity<<32 | line` = `0x500001287` → verbosity 5 (Log/Display-ish), **line 4743** (REMOVING) |
| +0x18 | log-category object (0x1431AB97C / 0x1431AB977 / 0x1431AB986) |

So the **code xrefs that matter** (found by scanning for LEA → record VMA, i.e. string_VMA − 0x28):

| log | record VMA | fmt string | line | code xref |
|---|---|---|---|---|
| `EOSH reconciliation: removing installation not reported by EOSH ...` | 0x142922398 | 0x1429223C0 | 4743 | **0x141da9b47** |
| `FCommunityPortalAppManagerImpl: Keeping discovered manifest` | 0x142922BE0 | 0x142922C00 | 4980 | **0x141ddadf8** |
| `EoshInstallationsChanged: Received new revision '%s'` | 0x142922108 | 0x142922130 | 4428 | **0x141da633c** |

**Future xref scans must target `string_VMA − 0x28` (the record), not the string.** Also `0x1424a8794` is the canonical **empty string** (used as default `%s` when an FString is empty) — very common in this code.

## The REAL removal path (reconcile function) — corrected from earlier session

Previous doc wrongly placed the removal log inside helper `0x141d8c950`. **Corrected:** the removal log is in the **reconcile function starting at `0x141da6250`** (prologue: `40 55 53 41 55 48 8d ac 24 f0 f6 ff ff 48 81 ec 10 0a 00 00`).

### Where each piece lives (reconcile function `0x141da6250`, spans ≥ 0x4400 bytes, many sub-blocks)

| addr | role |
|---|---|
| 0x141da6250 | **function start** (arg1=`this`/ctx, arg2=FString revision, arg3=array, arg4=array) |
| 0x141da633c | NEWREV log (`Received new revision '%s'`) — logs when revision string non-empty |
| 0x141da63d3+ | **phase-1 loop** over EOSH installation records (stride 0x240, array in r9) — the EOSH-reported list |
| 0x141da8c1d | hash-table init at `[rbp+0x7a0]` (0x68-stride; count at +0x7d4, spare bucket 0xffffffff at +0x7d0) |
| 0x141da8ee4 | **phase-2 list init**: `[rbp-0x50]` = manager (r12, from `0x141da8040: mov r12,[rbp-0x70]`); elements appended at `0x141da92af`–`0x141da92e5` as `{ptr,count}` (16 bytes) |
| 0x141da8f19–0x141da9304 | phase-2 list build (iterates local installs) |
| 0x141da9370 | **phase-2 loop start**: `mov rcx,[r12]; call [vtable+0xc0]` → 5-FString record → `[rbp+0x10..0x58]` |
| 0x141da93ab | `vtable[0x8]` → `[obj+0xec]` skip-flag: non-zero → **KEEP** (`0x141da9f78`) |
| 0x141da93ed | null-check of global `0x14316fed0` (ensure if null) |
| 0x141da942b | **`vtable[0x48]` on global `0x14316fed0`** → `test al,al; jne 0x141da9f78` = **KEEP** (THE EOSH membership gate) |
| 0x141da9493–0x141da94c1 | **hash-map lookup comparing 7 fields** (record vs entries of a 0x68-byte table) — duplicate detector |
| 0x141da9544 | hash-MATCH path → logs dup-removal `record 0x142922278` → REMOVE |
| 0x141da952c | `vtable[0xd0]` — **log-only** (both outcomes → removal log) |
| 0x141da94e4 | hash-miss path → build AppId `%s:%s:%s` (0x14276A7F8) → `vtable[0xd0]` → removal log `record 0x142922398` |
| 0x141da9b1c–0x141da9b8f | **the REMOVAL block**: logs the removing message, then `call 0x141db0ab0` |
| 0x141da9f78 | **KEEP/loop-continue**: `add r12,0x10` (next 16-byte element) → `cmp r12,[rbp-0x28]; jne 0x141da9370` |

### The 7-field hash lookup (0x141da9493) — DUPLICATE DETECTOR, not the keep test

Compares `rdx` (table base, entries 0x68 stride) fields against register-loaded values:

| field cmp | meaning |
|---|---|
| `[rcx+0x48]` vs ebx | a string length |
| `[rcx+0x10]` vs r15d | a string length |
| `[rcx+0x00]` vs r11 | a string data pointer |
| `[rcx+0x28]` vs r14d | a string length |
| `[rcx+0x18]` vs r10 | a string data pointer |
| `[rcx+0x40]` vs esi | a string length |
| `[rcx+0x30]` vs r9 | a string data pointer |

**CORRECTED (2026-09-10):** All 7 equal → `je 0x141da9544` → logs `record 0x142922278` = **"removing stale duplicate installation '%s' (InstallationId %s; AppId %s still installed elsewhere)"** → REMOVE. A hash match means the same 7-field identity was already reconciled (kept) in this pass, so this second occurrence is a duplicate → removed. A hash miss → continues to the "not reported by EOSH" removal. **Neither keeps the install.** The only keep paths are `[obj+0xec]` flag and `vtable[0x48]` on global `0x14316fed0`.

### The removal block (0x141da9b1c–0x141da9b8f)

```
0x141da9b1c: lea rsi,[empty]             ; vtable[0xd0] returned empty → rsi = ""
0x141da9b23: ; (non-empty path lands here via mov rsi,[rax])
             mov rcx,[r12]; call [vtable+0x30]    ; get InstallLocation/path string → r8
             lea rdx, REMOVING_record(0x142922398); lea rcx, logcategory(0x1431ab5e0)
             call 0x1402b8da0            ; emit "EOSH reconciliation: removing installation not reported by EOSH '%s' (InstallationId %s, AppId %s)."
             mov rcx,[r12]; call [vtable+0xb8]    ; get args for removal
0x141da9b84: mov r14,[rbp-0x70]          ; r14 = manager
             lea r8,[rbp+0x10]           ; r8 = 5-FString key
             mov dl,1                    ; rdx = confirm-remove
             call 0x141db0ab0            ; ← THE ACTUAL REMOVAL ROUTINE (rcx=r14)
```

### The removal routine `0x141db0ab0` (called from 0x141da9b84 — and 0x141d908fd, 0x141db5724)

Full function (0x141db0ab0, prologue `48 89 5c 24 18 ... 55 56 57 41 54 55 56 57 ...`). Deletes the install: known xrefs:
- `0x141da9b84` = the reconcile removal block (dl=1 = confirmed remove)
- `0x141d908fd`, `0x141db5724` = other callers (update/dup-removal paths probably)

### The keep/remove helper `0x141d8c950` — CORRECTED semantics

| behavior | description |
|---|---|
| checks 7 DWORDs: `[rdx+0x48, 0x10, 0x8, 0x28, 0x20, 0x40, 0x38]` all non-zero | record-validity (5 FStrings: data+count mixed) |
| all non-zero | `jmp 0x141d8d140` = KEEP processing |
| any zero | **logs `GameId.IsValidId()` ensure failure** at `CommunityPortalAppManager.cpp:7258` (fmt 0x1429239B8 region, line 0x1c4e=7258) — **NOT the EOSH removal message** |

So the helper validates the record shape (5 FStrings = the identity triple + 2 more). The EOSH-membership test is the `vtable[0x48]` call on global `0x14316fed0` in the reconcile function (corrected 2026-09-10).

## Reconcile invocation — no direct call sites (2026-09-10)

A full `E8` xref scan of `0x141da6250` found **no direct callers**. Reconcile is thus invoked via a **delegate/event** — consistent with the documented `EoshInstallationsChanged` event pattern (received revision log at `0x141da633c`). This matches the observed "removal ~300ms after `.item` load": the event fires after the server response is parsed, driving the reconcile pass.

## Global singleton `0x14316fed0` — the EOSH membership registry (2026-09-10)

- Content: **class instance pointer** (Meyers function-local-static pattern: `movups [0x14316fed0],xmm0` copies `{ptr,count}`, `mov [0x14316fed0],rcx` / `[0x14316fed8],rax` — an `FCountedString`-style or shared-ref pair).
- Written at `0x141404267` (thread-safe-init guard pattern via `0x142493ad0`/`0x142489db0` ensure); comparison at `0x141404158` (`cmp rax,[rbx]` static-vs-instance).
- Read at: `0x141a04f52` (shared-ptr copy), `0x141db72a6` (`vtable[0x28]` call), `0x141da941d` (**the reconcile gate, `vtable[0x48]`**), `0x141dee704` (`vtable[0x68]` call).
- `vtable[0x48]` = membership/`Contains`-style bool-returning method (arg rdx = 5-FString record).
- **Data-source question (OPEN):** whether this singleton's set is populated from the EOSH server `get-installed-apps-response` or from a local cache. If server-side (expected), local files cannot forge membership — matches the proven server-call-before-deletion behavior.

## C++ validation conditions revealed by ensure-strings (0x1429225B8 / 0x1429225D0 / 0x1429225F0)

Reconcile-function ensures (= debug checks that trap if violated) at lines ~0x1146/0x114b show the **actual invariants EOSH-reconciliation enforces**:

```
Error.bSucceeded()                                    (0x1429225B8 region)
!NewRevision.IsEmpty()
EoshInstallation.ArtifactId == Manifest->GetBinaryId().GetArtifactId()
Manifest.IsValid()
```

**KEY:** `EoshInstallation.ArtifactId == Manifest->GetBinaryId().GetArtifactId()` — EOSH's reported install must match the **manifest's** binary-id ArtifactId. This is the identity cross-check. `EoshInstallation` is the EOSH-side record; `Manifest` is the local `.manifest`/`.item`. This is why our hand-placed `.item`s fail: EOSH has no `EoshInstallation` record (server-side), so nothing matches the local manifest — the reconcile removes the orphan.

Source file of all this: `CommunityPortalAppManager.cpp` (0x14291E5D0). Class name visible in strings: `FCommunityPortalAppManagerImpl`.

## JSON parser (`.item` → native record) at `0x140d89330` region

Confirmed earlier session mapping (parser writes into 5-FString struct):
| JSON key | VMA | struct offset |
|---|---|---|
| `namespace` | 0x1426D3358 | +0x00 (write at 0x140d89375) |
| `catalogItemId` | 0x1426D3370 | +0x10 (write at 0x140d893ca) |
| `appName` | 0x1426D3390 | +0x20 (write at 0x140d8941f) |
| (+2 more fields → +0x30, +0x40) | — | — |

## AppId format string

`%s:%s:%s` at **0x14276a7f8** — used at 0x141da94f6 to build the `AppId ns:itemId:appName` shown in removal logs.

## EOSH server response JSON schema (`get-installed-apps-response`) — 2026-09-10

UTF-16 schema strings in `.rdata` (top level → installation record fields):

| VMA | key |
|---|---|
| 0x142794388 | `get-installed-apps` |
| 0x1427943B0 | `get-installed-apps-response` |
| 0x1426D4268 | `installations` |
| 0x1426D42A8 | `label` |
| 0x1426D40D4 | `installationId` |
| 0x1426D40A0 | `revision` (also `EoshRevision` @0x14276C2D0) |
| 0x1426D40E4 | `state` |
| 0x1426D40EC | `dir` |
| 0x1426D40F0 | `metaDir` |
| 0x1426D40F8 | `manifestPath` |
| 0x1426D4108 | `pendingManifestPath` |
| 0x1426D411C | `platform` |
| 0x1426D4128 | `sandboxId` |
| 0x1426D4134 | `itemId` |
| 0x1426D40AC | `aliases` |
| 0x1426D40B4 | `entries` |
| 0x1426D4028 | `launchCommand` |
| 0x1426D4038 | `launchExe` |
| 0x1426D4044 | `prereqSHA1Hash` |
| 0x1426D4054 | `buildSize` |
| 0x1426D4078 | `dependencies` |
| 0x1426D413C | `pendingTags` |
| 0x1426D4148 | `stagedVersion` |
| 0x1426D4218 | `manifestData` |
| 0x1426D4228 | `pendingManifestData` |
| 0x1426D423C | `dependencyConfig` |

Phase-1 records (stride 0x240) are parsed from this response shape; phase-2 uses the 5-FString records (namespace/catalogItemId/appName + 2) built from local installs. Duplicate log: `EoshInstallationsChanged: duplicate installation for %s at '%s' - keeping %s, dropping %s` @0x1429221C0.

## Log category / verbosity gate

- Category globals: 0x1431ab5e0 (main log), also 0x1431AB97C/0x1431AB977/0x1431AB986 referenced by the records.
- Gate pattern: `cmp BYTE PTR [rip+X], 0x5; jb skip_log` before `call 0x1402b8da0` (the log emitter, xrefs at 0x141da634a, 0x141da9b55, 0x141ddadf8-adjacent...). Verbosity in record = 5.
- `0x142489db0` = **ensure-trap** emitter (used by helper, not normal logs).

## Next steps (RE plan)

1. **Identify the class of global `0x14316fed0`** (EOSH membership registry) — find which object/manager creates and populates it. Xref scan for writes/reads of the singleton and the ensure-call at `0x1413e5381` → trace what feeds `vtable[0x48]`'s set. **This is THE question for TR/DS vs orphans:** if the set is server-fed, membership is unforgeable locally.
2. **Map phase-1 → hash-table relationship in reconcile** — confirm the 0x68-stride table at `[rbp+0x7a0]` is built from phase-1 EOSH records (the "already reconciled/kept" set) vs the local list; determines what the "stale duplicate" removal actually compares against.
3. **Trace `0x141dcab60` (called at 0x141da8032)** and the record-building at `0x141dadff0` — how the reconcile enumerates local `.item` files into the 5-FString records.
4. **Identify class implementing `vtable[0xc0]` (5-FString getter) and `vtable[0xd0]` (InstallId getter)** in phase-2 local-install records — confirms what fields a local install must expose to be checked.
5. Trace `Launcher.EoshEnabled`/`Launcher.EoshDisabled` → where the config is read (ini? registry?) → testable bypass candidate.
6. **Confirm `vtable[0x48]` semantics**: is it `Contains(record)` against the server-reported set, or a query against something else (e.g. `FindByAppId`)? Resolves whether TR/DS were kept because they were in the EOSH server set.

## Tools

- `strings -el -n N -t d` for UTF-16 strings with decimal file offsets
- `objdump -D -M intel` for disassembly
- Manual xref scanner (Python): LEA rip-rel + imm64 + pointer-table scans in .text/.data