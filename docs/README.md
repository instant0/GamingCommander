# Docs — what to read

Do **not** treat every file here as current. Use this map.

## Live (keep these current)

| File | Role |
|------|------|
| [ONLINE-AND-DATA.md](ONLINE-AND-DATA.md) | **Contract** — HTTP, JSON files, launch, ACF/`.item` writes |
| [../README.md](../README.md) | GitHub / how to build |
| [../GamingCommander.Readme.txt](../GamingCommander.Readme.txt) | Shipped with the exe |
| [../META/ROADMAP.md](../META/ROADMAP.md) | Shipped vs not |
| [../META/CODE_MAP.md](../META/CODE_MAP.md) | Code layout |
| [../META/ARCHITECTURE.md](../META/ARCHITECTURE.md) | Decisions (append-only) |
| [../planning/README.md](../planning/README.md) | Which plan is next |

## Research (keep; do not rewrite as product spec)

`docs/research/*` — formats (ACF, Epic `.item`, GOG, …).  
**Epic DLC / orphan / regen rules:** [research/epic_item_format.md](research/epic_item_format.md) (2026-08).  
[EPIC-MANIFEST-ENRICHMENT.md](EPIC-MANIFEST-ENRICHMENT.md) is Plan **109** analysis; VFS catalog is Plan **121**.

## Already retired (stubs only)

| File | Instead |
|------|---------|
| [FEATURES.md](FEATURES.md) | ROADMAP + SESSION |
| [CODE_MAP.md](CODE_MAP.md) | `META/CODE_MAP.md` |

## Historical / can lag

| File | Note |
|------|------|
| [GAME-DETECTION-LOGIC.md](GAME-DETECTION-LOGIC.md) | Long reference; if it disagrees with code, **code wins** |
| [findings/](findings/) | One-off probes — not the contract |
| [sbom/](sbom/) | Generated SBOM |

## Do not delete

`META/COMPLETED/*`, `META/ADR/*`, `planning/*` completed plans, `docs/research/*`. Mark **SUPERSEDED** in the header instead of removing.
