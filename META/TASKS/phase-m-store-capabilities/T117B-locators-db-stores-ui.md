# Task T117B: Battle.net/Rockstar + First-Run Preview (split)

**Status:** split (2026-10-31) — do not execute this document.
**Split into:**
- [T117B1 — Rockstar and Xbox](T117B1-locators-rockstar-xbox.md) — Rockstar locator; Xbox unsupported
- [T117B2 — First-run preview and policy wiring](T117B2-first-run-preview-policy.md) — independent
- [T117B3 — Battle.net (cancelled by T136)](T117B3-locator-battlenet.md) — detection-only; no readable inventory

## Why this split

The preview/policy work is independent of the last registry probes: a store with no locator already renders
as `Unsupported`. Rockstar has a documented key and `Settings.json` marker, so it can land with
`NeedsVerification`.

Battle.net is the uncertain case: the Agent `product.db` is unverified and no readable inventory is
confirmed. It waits for `T136` and must not block the UI or Rockstar.

Xbox has no reader in this build; it is an explicit `Unsupported` row.
