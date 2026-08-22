# PCGamingWiki API Probe (Plan 102 Phase 3, Priority 2)

**Date:** 2026-08-22  
**Harness:** `tools/probe_pcgw.py`  
**C#:** `PcgwLookup` + `PcgwInfoboxParser` (Plan 119). Cargo still unused.

## What already existed

July 2026 verification (`docs/findings/metadata-lookup-verification.md`) treated **Cargo `HOLDS`** as the primary PCGW path. `tools/lookup_metadata.py` still implements that.

## Live result (this session)

| Call | Result |
|------|--------|
| `action=cargoquery` `Steam_AppID HOLDS "1091500"` | HTTP 200, **`permissiondenied`**: "You don't have permission to run arbitrary Cargo queries." |
| `/api/appid.php?appid=1091500` | HTTP 200 HTML wiki page; `<title>` = `Cyberpunk 2077` |
| `action=opensearch&search=Cyberpunk 2077` | HTTP 200 list → page title + wiki URL |
| `action=parse&page=Cyberpunk 2077&prop=wikitext` | HTTP 200, Infobox present, developers extractable |

**Plan 102 Cargo-first is stale.** Do not implement `PcgwProvider` Cargo HOLDS as the happy path.

## Working PCGW path

1. Resolve page: Steam AppID → `appid.php` HTML title, **or** OpenSearch by display name.
2. `action=parse` wikitext → source parser (`PcgwInfoboxParser`) for `{{Infobox game}}` rows.
3. Common parser → sidecar record.

Rate limit 0.6s still assumed (429 documented in July). Parse payloads are large (~60KB); cache aggressively.

## C# (Plan 119)

Implemented against this probe: OpenSearch + `appid.php` + Parse. Do not port `lookup_metadata.py` Cargo.
