# Task T103: SteamEmu Classification Decision (ADR)

**Tier:** 1 — Documentation
**Phase:** L — Detection Follow-ups
**Risk:** Minimal
**Status:** blocked
**Prerequisites:** User decision required

> Blocks any code change. Do not modify `StoreSignalDetector` until the decision is recorded.

## Objective

`StoreSignalDetector` currently returns `SteamEmu` for any root `steam_api64.dll`/`steam_api.dll`.
That is a deliberate rule, but real Steamworks games ship the same DLLs, so the semantics must be
decided and recorded rather than silently changed.

## What Needs to Change

### 1. Ask the user (one question)
Options:
- **(a) Keep** — `steam_api*.dll` ⇒ `SteamEmu`; accept the false positive.
- **(b) Require a marker** — only classify as SteamEmu when `steam_emu.ini` or `steam_settings/` is present.
- **(c) Add a distinct kind** — `Steamworks` for generic `steam_api*.dll`, keep `SteamEmu` for emulator markers.

### 2. Record the decision
- [ ] Create `META/ADR/013-steam-emu-semantics.md`: context, decision, consequences, affected code, fixture/matrix impact.
- [ ] Update `META/ADR/INDEX.md` and `META/ARCHITECTURE.md` (append-only).
- [ ] Update `docs/detection/09-blueprint.md` §14 (mark the item decided) and `docs/detection/03-store-signals.md`.
- [ ] Update the `phase-l-detection-followups/STATUS.md` row.
- [ ] If the choice is (b)/(c), mint and author the implementation task via `tasks-plan`; do not implement it here.

## Context

- Corpus fixture `StoreSteamEmu` is built from `steam_api64.dll`; `tools/validate_probe_matrix.py` expects `Steam Emulator`.
- Changing the rule without updating the fixture/matrix will fail validation.
- Detection is currently behavior-preserving by directive; no unilateral change.

## Requirements

- [ ] The ADR names exactly one option and its consequences.
- [ ] Affected files, fixture updates, and Python/matrix changes are listed.
- [ ] A follow-up implementation task ID is recorded (or "none" for option (a)).
- [ ] No code changed in this task.

## Verification

- [ ] `test -f META/ADR/013-steam-emu-semantics.md`.
- [ ] `grep -n "SteamEmu" META/ADR/INDEX.md docs/detection/09-blueprint.md` shows the decision link/status.
- [ ] No build/test (docs-only).

## Completion Notes

- **Completed:**
- **What was done:**
- **Verification:**
- **Issues encountered:**
