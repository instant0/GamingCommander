# Plan 131 — Security and Release Integrity

**Status:** PROPOSED (2026-10-29)
**Source:** advisory security review (documentation-first, class-calibrated)
**Scope:** release gate, data-contract accuracy, local-state integrity, supply-chain hygiene

---

## Why

The product is a local, single-user, offline-by-default launcher. Its security posture is set by
restraint: no elevation, no services, no telemetry, no writes into game installs, no hidden network.
The review found that posture appropriate for the class and confirmed it against the documents.

It also found four gaps where the documents promise more than the build proves, or where a realistic
failure would damage the user:

1. **Local state can be lost silently.** Catalogue writes are non-atomic and a read failure resets to
   defaults. This is the most likely harm to a real user (power loss, antivirus lock, sync client).
2. **Two repair writers mutate state outside the application.** They write into a Steam library and
   the Epic ProgramData manifest folder without backup, journal, dry-run, or target validation,
   although ADR-005 requires all four for exactly this class of operation.
3. **The release gate does not gate.** Two overlapping workflows exist; the vulnerability step is
   advisory (`continue-on-error`); one action is pinned by branch; no checksum is published.
4. **The live data contract is incomplete.** `ONLINE-AND-DATA.md §2` omits the Epic `.item` write and
   still lists a store endpoint the shipped app never contacts, while an unreferenced client for that
   endpoint remains compiled.

Out of scope, explicitly accepted for this class: Authenticode signing, SLSA/provenance, reproducible
builds, PSIRT/CVE process, bug bounty, fuzzing programme, sandboxing, encryption at rest. A security
contact line is included; a disclosure programme is not.

---

## What changes

### 1. Release integrity

- Collapse the two release workflows into one path that publishes the artifact, the SBOM, and a
  `SHA256SUMS` file.
- Make the vulnerability scan fail the build on findings instead of reporting them.
- Pin every action by commit SHA, including `aquasecurity/trivy-action` (currently `@master`).
- Set minimal `permissions:` on the release job.

### 2. Data-contract accuracy

- Add the Epic `.item` write to `docs/ONLINE-AND-DATA.md §2` and reconcile its "never written" and
  "never contacted" lists with the code.
- Bring the shipped `GamingCommander.Readme.txt` access inventory into line with actual writes.
- Record the `data/games.json` trust boundary: it supplies the executable path and arguments, and is
  trusted local input, not a security boundary.
- State what deleting `data/` removes and what backups/journals retain.

### 3. Local-state integrity

- Define and implement one shared JSON write path: atomic replace, `.bak`, and a read result that
  distinguishes "missing" from "corrupt". A corrupt catalogue surfaces an error; it does not silently
  reset.
- Classify the two repair writers as either one guarded contract (backup, journal, dry-run, target
  validation) or removal candidates. No third option.

### 4. Supply-chain hygiene

- Dependabot for NuGet and GitHub Actions.
- Lock files with locked-mode restore in CI.
- Remove EOL test package pins that are no longer needed.
- Warnings-as-errors for the app and core projects, applied at a phase boundary so a mid-phase build
  does not break on pre-existing warnings.

---

## Affected files

| Area | Files |
|---|---|
| Release | `.github/workflows/release.yml`, `.github/workflows/build-release-enhanced.yml` |
| Contract docs | `docs/ONLINE-AND-DATA.md`, `GamingCommander.Readme.txt`, `docs/FAQ.md` |
| Local state | `src/GamingCommander.App/Services/JsonFileHelper.cs`, its consumers, database services |
| Repair writes | `src/GamingCommander.App/Services/SteamAcfWriter.cs`, `src/GamingCommander.App/Services/EpicItemWriter.cs`, `src/GamingCommander.App/MainWindow.PlatformRepair.cs` |
| Dead client | `src/GamingCommander.App/Services/EpicStoreSearch.cs` |
| Supply chain | `.github/dependabot.yml` (new), `Directory.Build.props`, `tests/Directory.Build.props`, `packages.lock.json` (new) |

---

## Ordering

1. **Contract accuracy** before the launcher-discovery and inventory work, so the new readers are
   written against a correct contract.
2. **Repair-write and JSON-write design** are owned by the duplicate-capability review; implementation
   follows that plan and precedes the new readers, so the readers consume one shared writer.
3. **Release integrity** is independent and can run at any time.
4. **Dead-client removal** follows the duplicate-capability classification, so the review can record it
   as evidence first.
5. **Supply-chain hygiene** lands at a phase boundary.

---

## Success criteria

- One release workflow publishes a verifiable artifact and a checksum file; a scan finding fails it.
- No document states a write, contact, or "never" claim that the code contradicts.
- An interrupted or corrupt catalogue write leaves a readable backup and a visible error.
- Each repair writer either has backup + journal + dry-run + target validation, or no longer exists.
- Restore is locked in CI; no EOL package remains; app/core build with warnings as errors.
- The accepted-risk list above is recorded and unchanged by the implementation.

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
