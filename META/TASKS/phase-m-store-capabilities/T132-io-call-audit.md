# Task T132: Registry and Filesystem Call Audit — Read Once, Write Once

**Tier:** 2 — Code Quality / Reliability
**Phase:** M — Store Capabilities
**Risk:** Medium (touches scan paths; each change must be independently revertable)
**Status:** pending
**Prerequisites:** Task T124A (the map names the call sites), Task T129 and Task T130 (they move the
write paths this audit would otherwise churn)

## Objective

Remove every registry and filesystem call that does not produce information or change state.

The rule is not "fewer calls". It is: **a read takes its answer from the operation already being
performed, and a write performs the state change and nothing else.** No probe exists to make the next call
look safe.

## Reads and writes are not the same rule

| | Read | Write |
|---|---|---|
| Shape | one open/enumeration, use what it returned | one mutating call, handle its failure |
| Pre-check | never | only where a contract requires refusal (see below) |
| Post-check | never | one verification, only where the contract defines one |
| Failure reporting | the exception/return of the read itself | the exception of the write |
| Why | a probe returns less than the read and hides the real error (access denied looks like "missing") | a probe adds a race and a call, and the write reports the same condition anyway |

Allowed write preconditions, and only these:

- **Steam ACF:** target must be absent (create-only contract, `planning/133`).
- **Epic `.item`:** target must exist and parse, same installation (modify-only contract, `planning/133`).
- **Any directory creation:** inside the application data root only (`planning/132`).
- **Repair/journal:** verify the bytes you just wrote parse (one read-back), because that is the contract.

Everything else — `File.Exists` before a read, `Directory.Exists` before `CreateDirectory`, `Exists`
before `Delete`, a second stat after an enumeration, a reopen of a key to confirm the value — is extra.

## Registry

The application writes **zero** registry values. If the audit finds a `SetValue`, `CreateSubKey`, or
`DeleteSubKey`, stop: that is a contract violation, not an optimisation.

| # | Pattern | Why it is redundant | Keep when |
|---|---|---|---|
| R1 | `OpenBaseKey(hive, view)` inside a loop or per value | one base key opens the hive once | never |
| R2 | `key.OpenSubKey(path)` then `key.OpenSubKey(path)` again for a second value | the first `RegistryKey` can read every value | never |
| R3 | `OpenSubKey(path) is not null` as an existence check, then open again to read | the open *is* the check | never |
| R4 | `GetSubKeyNames()` then `OpenSubKey(name) is not null` per name | the names come from an open key; opening each is required only to read values | when values are needed (unavoidable) — but not for existence |
| R5 | `GetValueNames()` then `GetValue(name)` per name | read known value names directly | when the value names are genuinely unknown |
| R6 | `GetValue(name)` then `GetValue(name, default)` to test presence | one call with a default distinguishes absent from present | never |
| R7 | enumerating the same key per game during one scan | read the key once per scan, reuse the map | when a game can write the key (it cannot) |
| R8 | reading both 32-bit and 64-bit views for a store | **deliberate** — store installers differ by view | never remove without store-specific evidence |
| R9 | a `RegistryKey` not wrapped in `using` | a leaked handle forces the runtime to keep the hive open | never |

## Filesystem reads

| # | Pattern | Why it is redundant | Keep when |
|---|---|---|---|
| P1 | `File.Exists(x)` then `ReadAllText`/`OpenRead`/`ReadFromFile` | two opens; the read throws `FileNotFoundException` | never |
| P2 | `Directory.Exists(x)` then `GetFiles`/`GetDirectories`/`Enumerate*` | enumeration returns empty for a missing directory | when an unreadable directory must be distinguished from an empty one (and then report the difference) |
| P3 | `new FileInfo(path)` for a path just returned by an enumeration | the entry was already stat'ed | when the instance is stale (a later phase of a long scan) |
| P4 | `GetLastWriteTime*` / length for a `FileInfo` already in hand | same stat twice | when the path came from a string, not an enumeration |
| P5 | `Directory.GetDirectories` + `Directory.GetFiles` on the same directory | two enumerations where one `EnumerateFileSystemEntries` gives both | when only one kind is needed |
| P6 | the same `Directory.Exists(samePath)` inside a per-game/per-file loop | one answer reused for hundreds of iterations | when the directory can appear or disappear during the loop |
| P7 | reading the same file twice (once for a signal, once for the parse) | one read gives both | when the encodings differ or the file is rewritten between reads |
| P8 | a recursive walk when a shallow probe answers the question | directory-tree traversal multiplies calls | when the layout genuinely requires the walk |
| P9 | `Path.GetFullPath` / `Path.Combine` rebuilt per item for a scan-constant value | no syscall, but allocation per item | when the base changes per item |

## Filesystem writes

| # | Pattern | Why it is redundant | Keep when |
|---|---|---|---|
| W1 | `Directory.Exists(x)` then `Directory.CreateDirectory(x)` | `CreateDirectory` is idempotent | never |
| W2 | `File.Exists(x)` then `File.Delete(x)` | `File.Delete` does not throw when the file is absent | never |
| W3 | `File.Exists(x)` then `WriteAllText`/`Copy` | the write reports the failure; the check adds a race | **Steam create-only** (target absent) and **Epic modify-only** (target present) preconditions, and nowhere else |
| W4 | probe → act → probe (stat after `Copy` to confirm) | the length check is the contract; a second stat adds nothing | when the contract verification is a parse read-back, not a stat |
| W5 | `Directory.CreateDirectory` for a store folder | the launcher owns that folder | never — refuse instead (`planning/133`) |
| W6 | `File.Exists` before a restore-from-embedded-resource | this is the one legitimate "do not overwrite user edits" guard | `BlacklistLoader` and any equivalent restore |
| W7 | a write path with two mutating calls (temp + rename, copy + patch) | one target, one call; staging lives in `data/` | never, unless the contract says otherwise |

## Known seed cases (verify before changing)

| Site | Pattern | Expected verdict |
|---|---|---|
| `AppDataPaths.cs:16-17` | W1 | redundant; `CreateDirectory` alone |
| `JsonFileHelper.cs:64-68` | W1 | redundant; becomes the `AppPaths` guard from `planning/132` |
| `BlacklistLoader.cs:128-133` | W6/P2 | **deliberate** — the restore must never overwrite user edits |
| `SteamAcfWriter.cs:73` | W3 | **deliberate** — create-only is the contract |
| `EpicItemWriter.cs:18` | P2 | keep — it is the user-facing "folder missing" message |
| `WindowsRegistryReader.cs:40-56` | R5 (`GetValueNames` then `GetValue` per name) | keep — the value names are genuinely unknown; it reads every value of one key |
| `WindowsRegistryReader.cs:17-25,63-72` | R1/R2/R3/R9 | clean — one key per call, `using`, no reopen |

The table is a starting point, not the inventory. The inventory is the deliverable.

## Deliberate non-goals

- No filesystem or registry abstraction (`IFileSystem`, `IRegistry` wrapper). Wrapping calls hides them; it
  does not remove them.
- No caching layer. Cache only a value already computed in the same method, for the same path or key.
- No change to what is detected. Fewer or more games is a regression.
- No concurrency, prefetch, or benchmarking harness.
- No change to the repair or persistence preconditions (`planning/132`, `planning/133`).
- No host-OS-specific path logic.

## Method

1. Inventory with grep, per pattern, over `src/` (exclude `bin/`, `obj/`).
2. For each hit, read the surrounding method and record: store, intent (read/write), pattern, verdict
   (`redundant` / `deliberate`), reason, proposed change.
3. Fix one pattern per change, reads first (P1, P2, R2, R3), then writes (W1, W2).
4. Re-run the greps; every remaining hit must have a recorded reason.
5. Write the inventory into `planning/135-io-call-audit.md`.

Greps (from the repository root):

```
grep -rn "Directory.Exists" src --include=*.cs
grep -rn -A2 -B2 "Directory.CreateDirectory" src --include=*.cs
grep -rn -A3 "File.Exists" src --include=*.cs
grep -rn "new FileInfo\|GetLastWriteTime\|GetLastWriteTimeUtc" src --include=*.cs
grep -rn "Registry\.\|OpenBaseKey\|OpenSubKey\|GetSubKeyNames\|GetValueNames" src --include=*.cs
grep -rn "SetValue\|CreateSubKey\|DeleteSubKey" src --include=*.cs      # must stay empty
grep -rn "AppendAllText" src --include=*.cs
```

## Requirements

- [ ] Every R1–R9 and P1–P9 / W1–W7 hit is listed with `file:line`, verdict, and reason.
- [ ] Every `redundant` hit is fixed or has a written reason it was left.
- [ ] Every read path opens or enumerates once; no `Exists` precedes a read.
- [ ] Every write path makes one mutating call; the only pre-checks are the four contract preconditions.
- [ ] `grep -rn "SetValue\|CreateSubKey\|DeleteSubKey" src` stays empty.
- [ ] No abstraction, no caching layer, no new dependency.
- [ ] No detection behaviour change: the 22-case matrix and the test suite stay green.
- [ ] `planning/135-io-call-audit.md` exists, with the read/write column and the deliberate list.
- [ ] No task IDs in the plan document.

## Verification

- [ ] `git diff --stat` shows deletions dominating; no new source file except the audit document.
- [ ] The greps above return no unexplained `File.Exists`-before-read or `Exists`-before-`CreateDirectory`.
- [ ] `grep -c "Directory.Exists" src` is reduced to the few places where "unreadable vs empty" is
      reported differently.
- [ ] `dotnet build` / `dotnet test` green.
- [ ] Detection matrix 22/22 on the release build; no diff in `games.json` for the same scan.
- [ ] Optional human-run Process Monitor spot check: the same scan shows fewer `CreateFile` /
      `QueryDirectory` / `RegQueryValue` operations, and no `CreateDirectory` outside `data/`.

## Stop rules

- **Stop at the call, not the design.** If removing a pattern needs a refactor, leave it and record it.
- **A precondition that enforces a contract is not redundant**, even when the operation would fail anyway;
  the message and the guarantee differ.
- **Never turn a read into a probe.** If a probe is needed to produce a better message, it replaces the
  read, it does not precede it.
- **No speculative caching.** A stale answer is a worse bug than an extra call.
- **One pattern per change.**

## Context

- Process Monitor is the external reference; this environment cannot run it. Findings are code-level, with
  an optional human-run confirmation.
- `WindowsRegistryReader` is the only registry surface; store detection reads HKLM/HKCU values and never
  writes them.
- The scan pipeline (`LibraryManager` → scanners → `FolderScanner`/`ContainerScanner` →
  `ExecutableDiscovery`) is where P2/P3/P4/P6/P8 concentrate; persistence (`planning/132`) is where W1/W3
  do; repair (`planning/133`) is where W3/W5 do.
- `META/ARCHITECTURE.md` records the detection pipeline; the map from T124A is the call-site index.

## Completion Notes

- **Completed:**
- **What was done:**
- **Verification:**
- **Issues encountered:**
