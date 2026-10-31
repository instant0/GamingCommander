# Task T117A: Locators — File-Backed Stores (split)

**Status:** split (2026-10-31) — do not execute this document.
**Split into:**
- [T117A1 — Steam and Epic (known)](T117A1-locators-steam-epic.md) — **done**
- [T117A2 — GOG (known file signals; Galaxy path)](T117A2-locator-gog.md)
- [T117A3 — Ubisoft registry inventory](T117A3-locator-ubisoft.md) — the preview needs this source; EA stays detection-only

## Why this split

Steam is resolved from **files** (`steamapps`, ACF, vdf) and Epic from the **known ProgramData manifests**
directory. Those are understood and shipped. GOG's per-game registry/`.info` signals are already used for
classification and its Galaxy path is documented.

EA and Ubisoft are the unknowns: the existing file/registry signals classify folders, but a launcher data
source (install record/inventory) is not confirmed. They go through the data-source research task first.

Do not merge them back into one checklist. `T136` decides whether a source is implementable; the locator
task then follows that decision or leaves the store `Unsupported`.
