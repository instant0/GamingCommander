# Task T136: Unknown Store Data Sources — Documentation and Planning

**Tier:** 1 — Documentation / Architecture
**Phase:** M — Store Capabilities
**Risk:** Low (research only)
**Status:** completed (2026-10-31)
**Prerequisites:** None. Blocks the EA/Ubisoft/Battle.net/Arc locator and inventory work.

> **Documentation-only.** Do not run build/test. Do not write code, do not add a store kind, do not invent a
> path or format. The deliverable is a decision per store.

## Objective

The file-signal detectors already classify most folders correctly. What is not established is whether the
remaining launchers expose a **readable data source** (install record, inventory, settings) and whether
reading it adds value over the existing file signals. Decide that in writing before adding locators or
inventory readers.

**Stores in scope:** EA App/Origin, Ubisoft Connect, Battle.net, **Arc** (confirm which client this means),
and Rockstar only where the existing key is unverified. Steam and Epic are out of scope: Steam is resolved
from files (`steamapps`, ACF, vdf), Epic from the known ProgramData manifests directory.

## What the plan must decide per store

For each store, record:

1. **Existing detection today** — the exact signal(s) already used (`StoreSignalDetector`, registry fallback,
   file markers), with the class and marker. This is the baseline that must keep working.
2. **Is there a launcher data source?** — exact file/registry path, format (text/JSON/YAML/SQLite/encrypted),
   and whether it is readable without a dependency or decryption.
3. **What it would add** — install location, inventory completeness, version, launch args, or nothing beyond
   the current folder signal.
4. **Safety** — `Safe` / `NeedsVerification` / `Unsupported`, with the reason.
5. **Decision** — implement a locator, implement a reader, detection-only, or explicitly unsupported.
6. **Verification** — what a fixture must prove; what cannot be confirmed without a real install.
7. **Open questions** — anything that needs a human with the launcher installed.

Arc: determine whether it is a distinct storefront that belongs in `GameSourceKind`/`LauncherKinds`, or a
launcher/signal that maps to an existing kind. Do **not** add an enum value in this task.

## Method

- Read `docs/research/launcher_discovery.md`, `docs/research/*_format.md`, `docs/detection/10-store-capabilities.md`,
  and the existing signals in `StoreSignalDetector`, `RegistryFallbackDetector`, `FolderScanner`.
- Separate **verified in code/fixtures** from **documented but unconfirmed** from **assumed**. Cite the file
  or research note; never present a guess as a fact.
- Prefer the smallest source that answers the question. If the folder signal already classifies a store and
  no inventory is readable, the right decision is `detection-only` / `Unsupported`, not a speculative parser.
- Do not plan to decrypt an encrypted store or add a SQLite/package dependency without an explicit decision.

## Deliverable

`planning/137-unknown-store-data-sources.md`:

- [x] One section per store: EA, Ubisoft, Battle.net, Arc, Rockstar (verification only).
- [x] A summary table: store | existing signal | candidate source | format | safety | decision | blocked by.
- [x] For every candidate source, the exact path/key and the evidence class (verified/documented/assumed).
- [x] The locator/reader tasks that are now unblocked, revised, or cancelled.
- [x] The tasks that must stay `Unsupported` until a real install confirms the source.
- [x] No task IDs in the plan document; no code.

## Requirements

- [x] Steam/Epic are explicitly marked out of scope with their current resolution method.
- [x] Existing file signals are treated as the baseline, not replaced by launcher inventory.
- [x] No new `GameSourceKind` or dependency is added.
- [x] Every “unsupported” decision names what would change it.
- [x] No effort estimates.

## Verification

- [x] `test -f planning/137-unknown-store-data-sources.md`.
- [x] Every store in scope has a decision and a safety value.
- [x] Every cited path/key has an evidence class.
- [x] EA/Ubisoft/Battle.net/Arc locator tasks reference this plan.
- [x] No code, no build/test.

## Stop rules

- Stop at documentation. Do not “just try” a reader to see if it works.
- Do not add a parser for an encrypted/binary format.
- Do not treat one machine's path as a universal format.
- If a store cannot be decided without a real install, record the open question and mark the implementation
  blocked; do not guess.

## Completion Notes

- **Completed:** 2026-10-31
- **What was done:** Wrote `planning/137-unknown-store-data-sources.md` with evidence classes and a decision
  per store. **EA:** detection-only; no locator/reader (launcher path adds no inventory; `installerdata.xml`
  is staged-only evidence; `IS` encrypted). **Ubisoft:** registry `Installs\{gameId}\InstallDir` inventory
  reader, `NeedsVerification`, no manifest parser; no launcher locator. **Battle.net:** detection-only; no
  SQLite/XML reader or locator. **Arc:** not a store; launcher noise only, no enum. **Rockstar:** locator +
  registry reader; `Settings.json` deferred. Cancelled the EA locator/reader and Battle.net locator/reader;
  kept the Ubisoft registry reader and reinstated a minimal Ubisoft registry-inventory locator so the setup
  preview can advertise the source. Rockstar locator + registry reader stay.
- **Verification:** `planning/137-unknown-store-data-sources.md` exists; every store has a decision, safety,
  evidence class, and task effect. No code, no build/test.
- **Issues encountered:** EA evidence is from a staged install; Battle.net/Rockstar/Ubisoft lack complete
  installs. These are recorded as open questions, not guesses.
