# Task T103: Steam Library Location Wins Over Steam-Emu Detection

**Tier:** 3 — Logic/Behavior
**Phase:** L — Detection Follow-ups
**Risk:** Medium
**Status:** completed
**Prerequisites:** None

## Decision (user, 2026-10-25)

A game is a **Steam** game when its folder resides under a known Steam library's
`steamapps/common/` — **even if the `.acf` is missing** (a missing ACF is exactly what this app can
repair). A folder outside every Steam library that carries a root `steam_api*.dll` is a
**SteamEmu** game.

Location is authoritative. The DLL signal is only used outside Steam libraries.

## Segments (execute in order; this umbrella is not executed directly)

| Segment | Deliverable | Verification |
|---|---|---|
| **T103A** | C# membership helper + classification + tests | build + tests |
| **T103B** | Python parity + probe fixture + matrix scenario | `validate_probe_matrix.py` |
| **T103C** | Docs: store signals, blueprint, architecture | docs only |

```
T103A → T103B → T103C
```

## Why split

The C# change, the Python/fixture change, and the docs each have a different verification path.
Keeping them separate lets each subagent finish with an objective pass/fail without carrying the
whole rule across contexts.

## Objective

Stop mislabelling legitimate Steamworks games as `SteamEmu` when they already live in a Steam
library. A `common/` folder without an ACF must remain a Steam entry (Orphaned/fixable), not an
emulated one.

## Requirements (whole feature)

- [ ] Inside a Steam library ⇒ `Steam`, even with no ACF.
- [ ] Outside every Steam library + `steam_api*.dll` ⇒ `SteamEmu`.
- [ ] No other store's classification changes.
- [ ] C#, Python, fixtures, and docs agree.

## Verification (whole feature)

- [ ] `dotnet build` 0 warnings / 0 errors.
- [ ] `dotnet test` all pass.
- [ ] `python3 tools/validate_probe_matrix.py` all pass, including the Steam-library/no-ACF case.
- [ ] Docs state the rule once and link it.

## Completion Notes

- **Completed:** 2026-10-25
- **What was done:** Split into C# classification (T103A), Python parity + probe fixture (T103B), and docs (T103C). All three segments completed and verified.
- **Verification:** build clean; 659 tests pass; probe matrix all pass (including the Steam-library/no-ACF case); docs state the rule.
- **Issues encountered:** none.
