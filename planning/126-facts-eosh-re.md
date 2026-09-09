# 126 — Facts: EOSH Launcher Reverse Engineering (DISCOVERY, in progress)

Status: DISCOVERY (2026-09-09). Objective: determine how the launcher decides "report/keep/update" vs "delete" for `.item` installs, and whether it can be influenced/bypassed. Target: `P:\Program Files\Epic Games\Launcher\Portal\Binaries\Win64\EpicGamesLauncher.exe` (52,649,912 bytes, UE5 **20.2.9-57799698+++UE5+Release-Distro-5.5**, PE x86-64).

## Binaries

- `EpicGamesLauncher.exe` — main portal (52 MB) — the EOSH reconciliation code lives here
- `EOSSDK-Win64-Shipping.dll` (35 MB) — EOS SDK
- `EpicGamesUpdater.exe` (3.4 MB) — self-update/queryinstallation
- Sections: `.text` @0x400→0x140001000 (size 0x24868cc), `.rdata` @0x2495c00→0x142497000 (0xadef6e), `.data` @0x2f74c00→0x142f76000

## Key strings found (UTF-16, verified in .rdata)

| VMA | String | Role |
|---|---|---|
| 0x142922130 | `EoshInstallationsChanged: Received new revision '%s'` | revision event |
| 0x1429223C0 | `EOSH reconciliation: removing installation not reported by EOSH '%s' (InstallationId %s, AppId %s).` | **the deletion log** |
| 0x142922000-ish | `EOSH reconciliation: removing stale duplicate installation ...` | dup removal |
| 0x142922C00 | `FCommunityPortalAppManagerImpl: Keeping discovered manifest` | the keep path |
| 0x142770708 | `skipeosh` | command-line/config switch |
| 0x1427706E0 | `forcereinstallegu` | command-line switch (parsed with skipeosh) |
| 0x142918260 | `Launcher.EoshEnabled` | config key |
| 0x142918290 | `Launcher.EoshDisabled` | config key |
| 0x142921450 | `forceEoshMcp` | config key |
| 0x142921910 | `LauncherUsingEOSH` | log/config |

Also: `EoshState = Enabled/Disabled/Resetting/...`, `Skipping EOSH login: ...`, `EOSH polling has been disabled`, `eosh_disabled_%s`, `EoshDisabledReason`, `bForceEoshMcp`, `errors.com.epicgames.eosh.cant_parse_response`, `errors.com.epicgames.eosh.not_logged_in`.

## Reconciliation decision code (first mapping)

**Keep-vs-remove helper at `0x141d8c950`** (called from `0x141d8c537` and `0x141da7fb3`):
```
cmp [rdx+0x48], 0 ; je remove
cmp [rdx+0x10], 0 ; je remove
cmp [rdx+0x8],  0 ; je remove
cmp [rdx+0x28], 0 ; je remove
cmp [rdx+0x20], 0 ; je remove
cmp [rdx+0x40], 0 ; je remove
cmp [rdx+0x38], 0 ; je remove
; all non-zero -> jmp 0x141d8d140 (KEEP path)
; else -> log "EOSH reconciliation: removing installation not reported by EOSH" (call 0x142489db0 with format 0x1429223C0)
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

**Meaning:** the keep-vs-remove helper only checks that the record's identity strings are non-empty (namespace, catalogItemId, appName + 2 more). **It does NOT check EOSH/server state at all** — it validates the local record's fields. A record with all 5 populated is kept; one missing any → logged as "not reported by EOSH" and removed.

**IMPORTANT CORRECTION to earlier theory:** this helper is a *record-validity* check, not the "is it in the EOSH reported set" check. The EOSH-reported-set query is the `call [rax+0x2c8]` before the keep-path at `0x141d8c52a` (returns TRUE → keep branch). So there are TWO gates:
1. `call [rax+0x2c8]` = "is this install in the EOSH reported set?" (the real gate)
2. `0x141d8c950` = 5-string record validity (namespace/catId/appName + 2 — sanity check)

### Caller flow (reconciliation, `0x141da7f40` region)

```
call 0x141dadff0  ; build/parse the installation record
call 0x141d8c950  ; validate 5 strings (keep/remove helper)
```

### The EOSH-reported-set query (`call [rax+0x2c8]`)

At `0x141d8c52a` the keep path is taken only when `call [rax+0x2c8]` (a virtual call on the app manager) returns true. **This is the "is reported by EOSH" check** — the one that decides keep vs delete. Next step: find what implements vtable slot `0x2c8` and trace its data source (local `.egi`/`LauncherInstalled.dat`? or EOSH HTTP query?).

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

## Next steps (RE plan)

1. Trace `skipeosh` → which member flag it sets → where that flag gates EOSH state (does `-skipeosh` disable the reconciliation?).
2. Trace `Launcher.EoshEnabled`/`Launcher.EoshDisabled` → where the config is read (ini? registry?) → testable bypass candidate.
3. Map the full reconciliation function (caller of `0x141da7fb3`) — find where it enumerates local `.item`s, where it queries EOSH's reported list (the `GetInstalledApps` call), and the exact branch to keep/remove/update.
4. Identify the "Update offer" condition (what makes the launcher offer Update vs delete) — likely a comparison of local manifest version vs catalog/`manifestUris` build.
5. Verify the 7-field keep condition against real data (which field is missing for our orphaned `.item`s → why they're removed).

## Tools

- `strings -el -n N -t d` for UTF-16 strings with decimal file offsets
- `objdump -D -M intel` for disassembly
- Manual xref scanner (Python): LEA rip-rel + imm64 + pointer-table scans in .text/.data