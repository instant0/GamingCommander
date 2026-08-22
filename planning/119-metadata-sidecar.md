# Plan 119 — Online metadata sidecar (right-pane extras)

**Status:** Steps 1–5 ✅ (2026-08-22). Cargo not used. Epic GraphQL still out.  
**Audience:** Planner / Builder  
**Depends on:** Plan 102 Phases 1–2+4 (tags, local engine, right-pane badges). Plan 102 Phase 3 **superseded** by this file for storage and PCGW path.  
**Probes:** `tools/probe_steam_store.py` ✅, `tools/probe_pcgw.py` ✅ (Cargo ❌, OpenSearch+Parse ✅)

---

## 1. Decision (proposal evaluation)

**Accepted.**

| Proposal | Verdict |
|----------|---------|
| Do not write extras into the offline VFS (`data/games.json`) | **Required.** Scan/launch must stay offline and small. |
| Right-pane extra-details in its own file | **Required.** Sidecar `data/games_metadata.json` keyed by `GameEntry.Id`. |
| Each lookup is its own function | **Required.** |
| Raw response → `<source>` parser → common parser → store | **Required.** Matches ADR-007 (native → domain). |
| Steam + PCGW + Epic | Steam **online** first. PCGW **online** via Parse (not Cargo). Epic **local already shipped**; Epic GraphQL **out of v1** (probe 500/404). |

**Rejected / narrowed**

- Plan 102 auto-writing genre strings into `GameEntry.Tags` (pollutes VFS, fights F4 user tags). Genres live on the sidecar; left-pane subtitle stays user + local-engine tags only.
- Plan 102 Cargo-first PCGW. Live probe: arbitrary Cargo is **denied**.
- Epic `searchStore` GraphQL in v1. Local `.item` / `.mancpn` already fill `DisplayName` + `PlatformMetadata`. Online Epic is a later fallback.

---

## 2. How the probes work (do not skip)

### Steam Store — `tools/probe_steam_store.py`

1. `GET https://store.steampowered.com/api/appdetails?appids={id}` + User-Agent.
2. HTTP 200 is not success. Envelope is `{ "<id>": { "success": bool, "data": {...} } }`.
3. `map_record` is the **source parser sketch**: arrays → joined strings; `genres[].description`; `metacritic.score`; `release_date.date` (locale, not ISO).
4. No engine field. Invalid / non-store AppIDs → `success=false`.
5. 10s between calls (plan rate). Live: 1091500 and 271590 OK.

### PCGW — `tools/probe_pcgw.py`

1. Cargo HOLDS → `permissiondenied` (2026-08-22). Do not copy `lookup_metadata.py` Cargo as C#.
2. `appid.php?appid=` → HTML; parse `<title>` for wiki page name.
3. OpenSearch by display name → page title + URL.
4. `action=parse&prop=wikitext` → Infobox markup (~60KB). Source parser extracts developer rows etc.

### Epic online

`store.epicgames.com/graphql` → 500; `graphql.epicgames.com/graphql` → 404. No v1 client.

---

## 3. Two files, two jobs

| File | Owner | Contents | Network |
|------|--------|----------|---------|
| `data/games.json` | Scan / F4 / launch | Roots, `GameEntry` (paths, exe, Steam status, AppIDs, user tags, local `GameEngine`) | Never |
| `data/games_metadata.json` | Metadata service | Right-pane extras: developer, publisher, genre, scores, PCGW URL, cover URL, per-source facts | Only if `EnableOnlineMetadata` |

Lookup keys **read** from `GameEntry` / `PlatformMetadata` (`SteamAppId`, `EpicCatalogNamespace`, GOG id). They are identity, not extras.

Rescan **must not** wipe the sidecar. Match on `GameEntry.Id`.

---

## 4. Pipeline

```
GameEntry (offline)
    │
    ├─ if !EnableOnlineMetadata → stop
    ├─ if sidecar fresh → use cache
    │
    ├─ LookupSteamStore(appId)        ── raw JSON
    ├─ LookupPcgwPage(appId|title)    ── wikitext / HTML
    └─ (v1 no LookupEpicGraphQL)
            │
            ▼
    SteamStoreParser / PcgwInfoboxParser / (EpicItemParser already exists for local)
            │  SourceFacts (still source-shaped)
            ▼
    CommonMetadataParser.ToRecord(facts, source)
            │  GameMetadataRecord (normalized, nullable fields)
            ▼
    MetadataStore.Merge(gameId, record, source)
            │  first non-null wins per field by source priority
            ▼
    data/games_metadata.json
            │
            ▼
    Right pane binds Details* extras from sidecar, not GameEntry
```

Each `Lookup*` is isolated (own URL, timeout, rate limiter). Parsers have **no HttpClient**. Common parser has **no source ifs** beyond an enum `MetadataSource`.

### Source priority (merge)

| Field | Prefer |
|-------|--------|
| Name enrichment (optional, never overwrite F4 / ACF if `UserOverrides` has DisplayName) | Steam Store `name` |
| Developer / Publisher | Steam, then PCGW |
| Genre | Steam, then PCGW |
| Release date | PCGW (closer to ISO) then Steam locale string |
| Engine (online) | PCGW Parse only; do not overwrite local `GameEntry.GameEngine` |
| Metacritic | Steam |
| Cover / website / PCGW URL | Steam cover, PCGW page URL |
| Description | Steam `short_description` |

Keep **per-source facts** in the sidecar so a parser fix can re-run without refetch.

### Sidecar shape

```json
{
  "version": 1,
  "entries": {
    "<gameEntryId>": {
      "merged": {
        "developer": "CD PROJEKT RED",
        "publisher": "CD PROJEKT RED",
        "releaseDate": "2020-12-10",
        "genre": "RPG",
        "description": "...",
        "metacriticScore": 86,
        "coverArtUrl": "https://...",
        "officialWebsite": "https://www.cyberpunk.net",
        "pcgwUrl": "https://www.pcgamingwiki.com/wiki/Cyberpunk_2077",
        "steamAppId": "1091500",
        "lastUpdated": "2026-08-22T00:00:00Z"
      },
      "sources": {
        "steam": { "fetchedAt": "...", "facts": { } },
        "pcgw": { "fetchedAt": "...", "facts": { } }
      }
    }
  }
}
```

---

## 5. When lookup runs

- **Never during scan.** Scan stays local.
- **On select** (right pane): if enabled, cache miss or stale (>30 days), queue one background lookup. UI shows cached/empty immediately.
- **Optional later:** F4 “Refresh metadata” button (Plan 110 §13) — not required for v1.
- Cancellation: share scan `CancellationToken` style; closing the app cancels.

`EnableOnlineMetadata` default remains **false**.

---

## 6. Right pane

Keep existing identity rows (name, path, type, Steam status, user tags, exe) on `GameEntry`.

Add a second block **only if sidecar has merged data**:

- Developer / Publisher  
- Genre  
- Release  
- Metacritic  
- PCGW link (text URL is enough; no browser host required)  

No cover-art image control in v1 (URL stored only).

`ShellViewModel` loads sidecar by `SelectedItem.GameId`. Missing file = hide extra block.

---

## 7. Files to add / change (when implementing)

**New**

| File | Role |
|------|------|
| `Core/Models/GameMetadataRecord.cs` | merged record |
| `Core/Models/MetadataSource.cs` | Steam, Pcgw, EpicLocal |
| `Core/Services/IMetadataProvider.cs` | `LookupRawAsync` only if useful; prefer App-layer |
| `App/Services/Metadata/SteamStoreLookup.cs` | HTTP |
| `App/Services/Metadata/SteamStoreParser.cs` | source parser |
| `App/Services/Metadata/PcgwLookup.cs` | OpenSearch + appid.php + parse |
| `App/Services/Metadata/PcgwInfoboxParser.cs` | source parser |
| `App/Services/Metadata/CommonMetadataParser.cs` | normalize dates, lists, Company: strip |
| `App/Services/Metadata/MetadataStore.cs` | read/write sidecar, merge |
| `App/Services/Metadata/MetadataService.cs` | orchestrate |

**Existing, touch lightly**

| File | Change |
|------|--------|
| `MainWindow.axaml` | extra-details block bound to sidecar VM props |
| `ShellViewModel.cs` | request extras for selection; do not stuff into `GameEntry` |
| `AppConfig` / F2 | already has `EnableOnlineMetadata` — **read it** |
| `GamesDatabaseService` | **no schema change** |

Reuse `EpicManifestParser` as the Epic **local** source parser if we ever copy DisplayName/developer-like fields into the sidecar. Do not add GraphQL.

---

## 8. Tests (when implementing)

- Parsers: fixture JSON/wikitext from probes (no network).
- Common parser: locale date, genre join, Company: strip.
- Store merge: second source fills holes only; user override on DisplayName never written from sidecar.
- Service: `EnableOnlineMetadata=false` → zero HTTP (inject fake handler).
- Sidecar missing / corrupt → empty extras, games.json untouched.

Do **not** hit live Valve/PCGW in `dotnet test`.

---

## 9. Success criteria

- [x] `games.json` unchanged by a successful online lookup
- [x] Sidecar created/updated under `data/games_metadata.json`
- [x] Steam AppID path fills developer/publisher/genre/score from parser fixtures + one optional live probe
- [x] PCGW path uses OpenSearch/Parse, not Cargo
- [x] Right pane shows extras when sidecar exists; hides when not
- [x] Flag off or offline: scan + launch + VFS identical to today
- [x] Build clean, existing tests pass

---

## 10. Out of scope

- IGDB / SteamDB / cover image UI  
- Epic GraphQL  
- Writing metadata into `GameEntry.Tags` or `PlatformMetadata`  
- Changing scan  
- SyncMove  

---

## 11. Implementation order

1. [x] Models + `MetadataStore` + empty right-pane bindings (offline fixtures).  
2. [x] `SteamStoreParser` + `SteamStoreLookup` (fixtures from `docs/findings/steam-store-api-probe.md`).  
3. [x] `PcgwInfoboxParser` + `PcgwLookup` (fixtures from Parse probe).  
4. [x] `MetadataService` merge + `EnableOnlineMetadata`.  
5. [x] Wire select-to-background fetch.

Stop after each step if parsers disagree with fixtures.
