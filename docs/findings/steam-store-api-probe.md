# Steam Store API Probe (Plan 102 Phase 3, Priority 1)

**Date:** 2026-08-22  
**Harness:** `tools/probe_steam_store.py`  
**C#:** `SteamStoreLookup` + `SteamStoreParser` (Plan 119). Tests use captured fixtures, not live Valve.

## What already existed

| Artifact | Covers Steam Store `appdetails`? |
|----------|----------------------------------|
| `planning/102-tags-metadata-display.md` §5.5 | Theory + stub C# — **not implemented** |
| `planning/04-phase-2-metadata-lookup.md` §4 | Documents the URL and expected fields |
| `planning/111-logging-toggle-readme-metadata.md` | Privacy/readme text for a future client |
| `tools/lookup_metadata.py` | **No.** Extracts AppID, queries **PCGW Cargo** |
| `docs/findings/metadata-lookup-verification.md` | PCGW verified (incl. by Steam AppID). Not Store API |
| `AppConfig.EnableOnlineMetadata` | Flag exists (default **false**). Nothing reads it for HTTP |

Conclusion: Priority 1 was specified, not live-tested. The 2026-07-09 findings are PCGW, not Valve.

## Probe (this session)

Public catalog AppIDs only. No local library.

```
python3 tools/probe_steam_store.py
```

| AppID | HTTP | Result |
|-------|------|--------|
| 1091500 | 200 | **ok** — Cyberpunk 2077 |
| 271590 | 200 | **ok** — Grand Theft Auto V Legacy |
| 480 | 200 | `success=false` (Spacewar, not a store page) |
| 1 | 200 | `success=false` (invalid) |

Endpoint works from this Linux host with a non-empty User-Agent.

## JSON shape vs Plan 102

Envelope: `{ "<appid>": { "success": bool, "data": { ... } } }`

C# **must** check `success`. HTTP 200 is not enough.

| Plan 102 field | Store path | Live note |
|----------------|------------|-----------|
| Display / name | `data.name` | Present. May include marketing suffix (`Legacy`) |
| Developer | `data.developers[]` | string array → join |
| Publisher | `data.publishers[]` | string array → join |
| ReleaseDate | `data.release_date.date` | **Locale string**, not ISO (`"9 Dec, 2020"`) |
| Genre | `data.genres[].description` | objects `{id, description}`, not a CSV |
| Description | `data.short_description` | HTML-ish plain text; `detailed_description` is HTML |
| Engine | — | **absent** |
| MetacriticScore | `data.metacritic.score` | object `{score, url}`; may be missing |
| SteamAppId | `data.steam_appid` | number |
| CoverArtUrl | `data.header_image` | CDN URL + cache-buster query |
| OfficialWebsite | `data.website` | present when Valve has it |

No engine. PCGW Parse remains the engine source (Phase 3 pri 2–3).

## C# implications (do not implement yet)

1. `HttpClient` + User-Agent required.
2. Parse `success` before `data`.
3. Rate limit 10s/request (plan). Probe used 10s; no 429 seen on 4 calls.
4. Do not treat `release_date.date` as `DateTime` without a loose parser.
5. Missing AppID → skip this provider (return null).
6. Store name can disagree with ACF `name` (GTA “Legacy”). Prefer ACF for title unless user asked for enrichment.

## Next (still not C# until asked)

- Optional: same-style PCGW Cargo probe (pri 2) — `lookup_metadata.py` already did this in 2026-07.
- Then implement `SteamStoreProvider` against this mapping.
