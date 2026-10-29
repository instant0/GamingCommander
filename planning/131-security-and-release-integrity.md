# Plan 131 — Security and Release Integrity

**Status:** PROPOSED (2026-10-29)
**Source:** advisory security review (documentation-first, class-calibrated), revised after review
**Scope:** release gate, data-contract accuracy, local-state integrity, repair-write safety, supply-chain hygiene

---

## Why

The product is a local, single-user, offline-by-default launcher. Its security posture is set by
restraint: no elevation, no services, no telemetry, no writes into game installs, no hidden network.
The review found that posture appropriate for the class and confirmed it against the documents.

It also found four gaps where the documents promise more than the build proves, or where a realistic
failure would damage the user:

1. **Local state can be lost silently.** `File.WriteAllText` truncates the target before writing
   (`FileMode.Create`), and `ReadFromFile` returns defaults on any parse failure. A kill, a full disk,
   or an antivirus lock during a save therefore leaves a short file, which then reads as an empty
   catalogue, which the next save persists over the damaged one. This is the most likely harm to a
   real user, and it is not a security problem.
2. **Two repair writers mutate state outside the application.** They write into a Steam library and
   the Epic ProgramData manifest folder without backup, journal, dry-run, or target validation, and
   both call `Directory.CreateDirectory` on external paths.
3. **The release path has drifted.** Two overlapping workflows can both publish; the vulnerability
   step is advisory (`continue-on-error`); one action is pinned by branch; two SBOMs disagree; no
   checksum is published, so a truncated download cannot be spotted.
4. **The live data contract is incomplete.** `ONLINE-AND-DATA.md §2` omits the Epic `.item` write, and
   its "never contacted" list does not distinguish a deferred endpoint from an absent one.

### Corrections to the first review

- **The Epic store client is not dead code.** It is the prepared Epic leg of the planned online
  metadata feature. `planning/109` defers GraphQL as a fallback, `planning/111` lists the endpoint and
  payload as future work, and `planning/119` calls online Epic a later fallback. The client stays; it
  is gated, documented as deferred, and never called by default. The earlier removal proposal is
  withdrawn.
- **The `games.json` "trust boundary" item is dropped.** An attacker who can write that file already
  has the user's own access and can do worse. There is no integrity control worth adding. Imported
  store paths are still validated for correctness, not as a security boundary.
- **No hashing, signing, or hash chain for local state.** A hash inside the file changes the hash; a
  hash beside it can be edited to match; a hash of the hash file has the same problem; a signing key
  would sit on the same machine. The same reasoning applies to the repair writers: a size and a
  re-parse, not a digest.
- **Durability is a sequencing choice, not a hard prerequisite for every reader.** The shared write
  path is required before per-launch sync, which multiplies the number of writes. A one-shot import
  may land first.

Out of scope, explicitly accepted for this class: Authenticode signing, SLSA/provenance, reproducible
builds, PSIRT/CVE process, bug bounty, fuzzing programme, sandboxing, encryption at rest. A security
contact line is included; a disclosure programme is not.

---

## What changes

### 1. Release path

- Collapse the two release workflows into one. Two paths that can both publish is the actual defect.
- Publish the archive with a `SHA256SUMS` file produced by the same release job, alongside the SBOM
  and the scan report. Described as a download-corruption check: it is not a signature and does not
  prove who built the file, because the checksum travels with the artifact and whoever can replace one
  can replace both. Authenticode would be the only authenticity control, and it is out of scope by
  decision.
- Fail the scan on high/critical findings, with an explicit ignore list. Low/medium stays advisory.
- Pin every action by commit SHA, including `aquasecurity/trivy-action` (currently `@master`).
- Set minimal `permissions:` on the release job.
- Keep one SBOM (the CI-generated one); delete the hand-maintained copy instead of reconciling two
  files that will drift again.

### 2. Data-contract accuracy

- Add the Epic `.item` write to `docs/ONLINE-AND-DATA.md §2` and reconcile its "never written" list
  with the code.
- Bring the shipped `GamingCommander.Readme.txt` access inventory into line with actual writes.
- Describe the Epic storefront endpoint as **not called in v1, reserved for opt-in Epic metadata**.
  Do not describe it as absent or removed.
- State that repair writes never create a store or ProgramData directory; a missing target is a
  refusal, not a creation.
- State what deleting `data/` removes and what backups/journals retain.

### 3. Local-state durability

One write path for every JSON file the app owns, and one safeguard:

1. Copy the current file to `data/<name>.bak`, overwriting the previous backup.
2. Write the new JSON.
3. Read the file back and deserialize it.
4. Accept if it parses. If it does not, copy `.bak` back over the file and report the failure.

A single rolling backup, replaced on every write. No temp file, no rename, no digest.

Read path:

- Missing file → defaults (first run).
- Parse failure → keep the damaged bytes as `<name>.corrupt`, restore `<name>.bak`, show a message.
  Never hand back an empty catalogue as if nothing happened.

#### Why this, and not more

`data/games.json` can hold hundreds of entries and is rewritten on every scan and sync, so a damaged
write is expensive; the repair writers touch one small file once, which is why they get a preflight
and a length check rather than this sequence.

The verify step catches a short or failed write while the process is still alive. A crash mid-write
skips it, which is what the `.bak` is for: the next read finds a file that will not parse, restores
the backup, and says so. That is the whole mechanism — one backup, one verify, one recovery path.

No hash, signature, or hash chain: a hash inside the file changes the hash, a hash beside it can be
edited to match, and a key would live on the same machine. This is user data, not untrusted input.

### 4. Repair-write safety

- Classify the two repair writers as either one guarded contract or removal candidates.
- Guarded means: target validation, preflight, no external directory creation, payload generated and
  validated in the application folder, copy to the target, backup where a file is replaced, journal,
  dry-run, and restore.
- A failed copy is detected by comparing the target length with the validated payload. A short or
  empty result is deleted and reported, and the attempt is journalled either way.

### 5. Supply-chain hygiene

- Dependabot for NuGet and GitHub Actions.
- Lock files with locked-mode restore in CI.
- Remove EOL test package pins that are no longer needed.
- Warnings-as-errors for the app and core projects, applied at a phase boundary.

---

## Local write rules (detail)

### Windows does not create parent directories

`File.WriteAllText`, `File.Copy`, and `File.Move` fail with `DirectoryNotFoundException` when the
parent directory is missing. A `CreateDirectory` frame in ProcMon is an explicit API call from some
process, not implicit filesystem behaviour. If the application stops calling it, nothing creates the
directory. The rule is therefore enforceable in code, review, and tests.

### Directory creation

- `CreateDirectory` is allowed only under the application data root: `data/`, `data/backups/`,
  `data/staging/`.
- One guard helper asserts that a path is under that root; any other call is a defect.
- Repair writers never create a store or ProgramData directory. If the target directory is missing,
  the repair is refused with a message naming the expected location.
- `File.Copy` and `File.Move` also require the destination directory to exist; the preflight covers
  them.

### Preflight (repair writers)

1. `Directory.Exists(targetDir)` — refuse if false.
2. `File.Exists(targetFile)` — refuse if true, except the reviewed hollow-`.item` replacement case.
3. Validate the target marker:
   - Steam: `targetDir\libraryfolders.vdf` exists.
   - Epic: `targetDir` is the known ProgramData `EpicGamesLauncher\Data\Manifests` folder.
4. Refuse relative paths, UNC paths, and paths the application did not discover itself.

### Repair write algorithm

**Create path** (target file absent):

1. Build the payload in memory from the game entry — an ACF or `.item` is a few KB.
2. Serialize, re-read, and re-parse it in memory; check required fields and byte length.
3. Write the bytes to `data/staging/<name>` and read that file back, so the exact bytes that will be
   copied are proven to parse. This is the "generate it correctly in our own folder" step.
4. Preflight the target: directory exists, file absent, marker present.
5. `File.Copy(staged, target)` — one call, from a known-good source.
6. Compare `new FileInfo(target).Length` with the staged length. On mismatch, delete the target and
   report. On `IOException`, delete a zero-length or oversized target if one exists, then report.
7. Delete the staged file; append the journal entry either way.

**Replace path** (hollow `.item` exists): same sequence, plus a backup to
`data/backups/<timestamp>/` before the copy, and a restore from that backup if the length check fails.

### Why generate in the application folder

Because the payload is proven correct before the target is touched, and the bytes that land in the
store are the bytes we validated. The staged file also gives the dry run something to show and the
journal a length to compare against.

`File.Copy` writes the destination in place. If it is interrupted, the result is a short file; the
length check detects it, deletes it, and reports it. Nothing else is ever written to the store
directory — no temp files, no renames, no second pass.

| Approach | Payload validated before target | Interrupted copy | Recovery |
|---|---|---|---|
| Direct write to final | No | Short file | Delete, re-run |
| Generate in app folder → copy | Yes | Short file | Length check, delete, re-run; backup for replace |

Recommendation: generate in `data/staging/`, validate, copy. That is the whole mechanism.

---

## Review split

The security items are not one review. They are separate decisions with separate deliverables:

| Review | Question | Deliverable |
|---|---|---|
| Core consolidation | Detection, store-pipeline, and identity duplicates | `planning/130-duplicate-feature-consolidation.md` |
| Connection map | How the pieces connect, read/write/network boundaries | `docs/architecture/CONNECTION_MAP.md` or a `CODE_MAP.md` section |
| Persistence write durability | One JSON store, verified write with a rolling `.bak`, `OK`/`Missing`/`Corrupt`, app-folder-only `CreateDirectory` | `planning/132-persistence-write-integrity.md` |
| Repair-write safety | Guard or remove; preflight, generate-validate-copy, journal, dry run | `planning/133-repair-write-safety.md` |
| Epic online client | Keep as deferred opt-in metadata; define the gate | `planning/134-epic-online-metadata-disposition.md` |

The core review links to the other three instead of restating them.

---

## Affected files

| Area | Files |
|---|---|
| Release | `.github/workflows/release.yml`, `.github/workflows/build-release-enhanced.yml` |
| Contract docs | `docs/ONLINE-AND-DATA.md`, `GamingCommander.Readme.txt`, `docs/FAQ.md` |
| Local state | `src/GamingCommander.App/Services/JsonFileHelper.cs`, its consumers, database services |
| Repair writes | `src/GamingCommander.App/Services/SteamAcfWriter.cs`, `src/GamingCommander.App/Services/EpicItemWriter.cs`, `src/GamingCommander.App/MainWindow.PlatformRepair.cs` |
| Epic online client | `src/GamingCommander.App/Services/EpicStoreSearch.cs` (kept, deferred, gated) |
| Supply chain | `.github/dependabot.yml` (new), `Directory.Build.props`, `tests/Directory.Build.props`, `packages.lock.json` (new) |

---

## Ordering

1. **Contract accuracy** before the launcher-discovery and inventory work, so the new readers are
   written against a correct contract.
2. **Persistence and repair reviews** before the readers consume them, so the readers use one writer
   and one repair contract. The persistence work is required before per-launch sync; a one-shot
   import may land first.
3. **Release integrity** is independent and can run at any time.
4. **Epic client disposition** before the contract wording is updated. The client stays unreferenced
   until its gate is scheduled.
5. **Supply-chain hygiene** lands at a phase boundary.

---

## Success criteria

- One release workflow publishes the archive and a checksum; a high/critical finding fails it. The
  checksum is documented as a corruption check, never as proof of authenticity.
- No document states a write, contact, or "never" claim that the code contradicts.
- A failed verify restores the rolling `.bak` and reports it; a crash mid-write is recovered from the
  same `.bak` on the next read.
- Each repair writer either has target validation + preflight + validated payload generated in the
  app folder + copy + backup + journal + dry run, or no longer exists.
- No `Directory.CreateDirectory` call exists outside the application data root.
- A create-path repair never leaves a wrong-length file silently: the copy is length-checked, a bad
  result is deleted, and the attempt is journalled.
- The Epic online client is preserved, unreferenced by default, and documented as deferred.
- Restore is locked in CI; no EOL package remains; app/core build with warnings as errors.
- The accepted-risk list below is recorded and unchanged by the implementation.

---

## Non-goals

- No new network surface, service, account, updater, or telemetry.
- No changes to detection behaviour or store-format parsing.
- No signing infrastructure, provenance attestations, or disclosure programme.
- No encryption of local state.

### Not carried into tasks

- **CC BY-NC 4.0 licence terms.** A legal/distribution question (patent grant, source redistribution),
  not a security control. It needs a separate decision.
- **Version stamping and the hardcoded publish path.** Release hygiene outside this plan's scope.
- **Absolute paths in `startup.log`.** A privacy nicety; the FAQ already warns users before they paste it.
- **`games.json` integrity checks.** Dropped; same-user write access is not a meaningful boundary.
- **Removal of the Epic store client.** Withdrawn; it is a deferred opt-in feature.
