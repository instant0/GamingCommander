# Plan 101 — Top-Level Mode Switcher, Flatten Library, and Game Filter

**Status:** DRAFT — awaiting approval  
**Audience:** Planner / Builder  
**Priority:** P2 (post-MVP feature)  
**Effort:** 3 phases, ~4–6 sessions total  
**Depends on:** MVP complete (T61–T70)

---

## 0. Problem Statement

The current top-level view shows library roots (e.g., `D:\SteamLibrary`, `D:\Games`, `E:\Games`). This is useful for managing roots, but users think in terms of **stores** (Steam, EA, GOG) and **engines** (Unreal, Unity), not filesystem paths. The F9 button (currently "Library Roots") and F5 (removed in T71) offer opportunities to reorganize navigation.

Additionally, games spread across multiple library roots are hard to browse as a unified collection. Users may want to see "all my games" regardless of which root they live in.

---

## 1. Proposed Architecture (3 Phases)

### Phase 1: Top-Level Mode Switcher (F9)

**Current behavior:** F9 = "Jump to Library Roots" (goes to root list)  
**New behavior:** F9 = "Cycle Top-Level Mode" (toggles between 3 views)

| Mode | Top-Level Shows | Drill-In Shows |
|------|----------------|----------------|
| **Library** (default) | Library roots: `D:\SteamLibrary`, `D:\Games`, `E:\Games` | Games in that root |
| **Store** | Game stores: `Steam`, `EA`, `Ubisoft`, `GOG`, `Standalone`, etc. | Games detected from that store (across all roots) |
| **Engine** | Game engines: `Unreal Engine`, `Unity`, `RAGE`, `Frostbite`, `Unknown` | Games detected with that engine (across all roots) |

**Navigation flow:**
```
F9 press 1: Library → Store
F9 press 2: Store → Engine  
F9 press 3: Engine → Library
```

**Status bar:** Shows current mode and count, e.g.:
- `Mode: Library (3 roots)`
- `Mode: Store (5 stores, 127 games)`
- `Mode: Engine (4 engines, 89 games)`

**Interaction hint updates:**
- `F9: mode` (was `F9: Library Roots`)
- Left pane title changes per mode:
  - Library: `Library Roots`
  - Store: `Game Stores`
  - Engine: `Game Engines`

**Command bar:** F9 label changes to `Mode` (was `Library Roots`)

**Store mode detail:**
- Groups games by `GameEntry.GameSource` (the `GameSourceKind` enum)
- Display names: `Steam`, `GOG`, `Epic`, `EA App`, `Ubisoft Connect`, `Battle.net`, `Xbox`, `Rockstar`, `Standalone`, `Steam Emulator`
- A store group shows only if it has ≥1 game
- Drill into a store → flat list of all games from that store across all roots

**Engine mode detail:**
- Groups games by engine detected during scan
- **Problem:** Engine is NOT currently stored in `GameEntry`. It's detected in `detect.py` but not persisted in C#.
- **Solution:** Add `GameEngine` field to `GameEntry` (see Phase 1 data model changes)
- Engine detection already exists in `detect.py` (`_detect_engine()`) and partially in `FallbackSignalDetector.HasUnrealLayoutSignal()` — needs to be extended and persisted
- Display names: `Unreal Engine`, `Unity`, `RAGE`, `Frostbite`, `Unknown`
- Drill into an engine → flat list of all games using that engine

**Backspace / ".." behavior:**
- In Library mode: Backspace at root level = no-op (already at top)
- In Store/Engine mode: Backspace from a store/engine group → back to mode list
- ".." entry at top of each drill-in → same as Backspace
- User is correct: no dedicated "go to top" button needed since Backspace and ".." handle this

### Phase 2: Flatten Library View (Config Setting)

**Current behavior:** Each library root shows its games independently.  
**New behavior:** Optional "flatten" mode that merges all roots into one list.

**Config setting:**
```csharp
// In AppConfig:
bool FlattenLibraryView = false;  // default: off
```

**When FlattenLibraryView = true:**
- Library mode top-level shows ONE entry: `All Games (127 games)`
- Drill into it shows ALL games from ALL roots in a single flat list
- Store/Engine modes are unaffected (they already aggregate across roots)

**When FlattenLibraryView = false (default):**
- Library mode works as today: each root is a separate entry
- Store/Engine modes aggregate across roots (as in Phase 1)

**UI:**
- Setting accessible via F2 Library Setup (checkbox or toggle)
- Or: a new F-key binding (but F5 is reserved for Phase 3 filter)
- Simplest: add toggle to the config UI in LibrarySetupWindow

### Phase 3: Game Filter (F5) + User-Editable Tags

**Current behavior:** F5 removed in T71.  
**New behavior:** F5 = "Filter Games" (opens filter overlay)

**Foundation: User-Editable Tags**

Before filtering by genre/category, we need tags to exist. The approach:

1. **Add `Tags` field to `GameEntry`** — `List<string>` of freeform tags (e.g., `["RPG", "Open World", "Co-op", "Story Rich"]`)
2. **Extend F4 editor** — add a tag input field where users can add/remove tags
3. **Filter system reads tags** — no external dependency; tags are user-curated
4. **Future: PCGW auto-populate** — optional enhancement to bulk-add tags from PCGamingWiki

**Why user-first, not PCGW-first:**
- PCGW coverage is incomplete (many indie/older games missing)
- User knows their games better than any database
- Manual tagging is immediate; PCGW requires network + parsing
- PCGW becomes a convenience layer, not a requirement

**Filter categories:**
1. **Store** — filter by `GameSourceKind` (Steam, EA, GOG, etc.) — always available
2. **Engine** — filter by `GameEngine` field (Unreal, Unity, etc.) — always available after Phase 1
3. **Tags** — filter by user-defined tags (RPG, Action, Co-op, etc.) — available when tags exist

**UX flow:**
1. User presses F5 from any view mode
2. Filter panel appears (modal dialog)
3. User selects filter criteria:
   - Store: checkboxes (Steam, GOG, Epic, etc.)
   - Engine: checkboxes (Unreal, Unity, RAGE, Frostbite)
   - Tags: multi-select from all tags used across games
4. Left pane updates to show only matching games
5. Filter persists until cleared (F5 again or Escape)
6. Status bar shows active filter: `Filter: Steam + RPG (23 games)`

**Filter panel design:**
- **Option A: Modal dialog** — simple, consistent with F2/F4 pattern
- **Option B: Inline panel** — replaces right pane temporarily, more interactive

Recommendation: **Option A (modal)** for simplicity.

**Filter logic:**
- AND between categories (Steam AND RPG)
- OR within categories (Steam OR GOG)
- Results displayed as standard game list in left pane
- Clear filter → restore previous view

**Tag editing in F4 (GameSetupWindow):**
- Add "Tags" field below existing fields
- Comma-separated input: `"RPG, Open World, Co-op"`
- Tags are normalized: trimmed, case-preserved, deduplicated
- Existing tags auto-suggest as user types (future enhancement)

**Data flow:**
```
User adds tags via F4 → stored in GameEntry.Tags → filter reads Tags
                                                 ↘ PCGW auto-populates later (optional)
```

---

## 2. Data Model Changes

### Phase 1: Add Engine to GameEntry

```csharp
// New enum (Core/Models/GameEngineKind.cs)
public enum GameEngineKind
{
    Unknown = 0,
    UnrealEngine = 1,
    Unity = 2,
    Rage = 3,
    Frostbite = 4,
    // Extensible for future engines
}

// Addition to GameEntry record:
GameEngineKind GameEngine = GameEngineKind.Unknown
```

**Detection source:** Port `_detect_engine()` from `detect.py` to C# `FolderScanner` or new `EngineDetector` class. Detection signals:
- Unreal: `Engine/` directory + `Binaries/` child
- Unity: `UnityPlayer.dll` + `*_Data/` directory
- RAGE: `title.rgl` + `common.rpf`
- Frostbite: `Engine.BuildInfo_Win64_retail.dll`

**Persistence:** Add `GameEngine` to `GameEntryDto` in `GamesDatabaseService`. Backward-compatible: missing field defaults to `Unknown`.

### Phase 2: Add FlattenLibraryView to AppConfig

```csharp
// Addition to AppConfig record:
bool FlattenLibraryView = false
```

### Phase 3: Add Tags to GameEntry + Tag Editing

```csharp
// Addition to GameEntry record:
List<string> Tags = []   // User-defined tags: ["RPG", "Open World", "Co-op"]
```

**Why List<string> (not comma-separated string):**
- Type-safe, no parsing overhead
- Easy to query: `game.Tags.Contains("RPG")`
- Natural for UI: add/remove individual tags
- Serializable as JSON array in games.json

**Tag conventions:**
- Case-preserved in storage: "RPG" and "Rpg" are stored as-is
- **Normalization on input:** lowercase + trim + collapse whitespace for dedup checking
- **No predefined vocabulary:** user creates tags freely
- Empty by default: tags are opt-in, not auto-populated
- Max practical limit: ~20 tags per game (no hard limit)

**Tag deduplication strategy (critical for PCGW import):**

When PCGW auto-populates tags, duplicate/near-duplicate tags are inevitable:
- Case: "RPG" vs "Rpg" vs "rpg"
- Synonyms: "Roleplaying Game" vs "RPG" vs "Role-Playing Game"
- Spacing: "Co-op" vs "Co op" vs "CoOp"

**Solution: Two-layer approach**

1. **Layer 1 — Normalization (immediate):**
   - On any tag add (user or PCGW): lowercase, trim, collapse whitespace
   - Before adding, check if normalized version already exists
   - Example: "RPG" exists → skip "rpg", skip "Rpg", skip " roleplaying game " (after normalization)

2. **Layer 2 — Alias map (Phase 4, PCGW import):**
   - Predefined mapping in `data/tag-aliases.json`:
     ```json
     {
       "roleplaying game": "RPG",
       "role-playing game": "RPG",
       "rpg": "RPG",
       "coop": "Co-op",
       "co-op": "Co-op",
       "co op": "Co-op"
     }
     ```
   - On PCGW import: resolve alias before checking duplicates
   - User can edit alias map to customize normalization

3. **Layer 3 — User review (Phase 4, optional):**
   - PCGW import shows proposed tags in a review dialog
   - User can accept, reject, or merge before applying
   - Prevents unwanted tags from polluting the library

**For MVP of tag system (Phase 3):** Only Layer 1 (normalization) is needed. Layers 2-3 are Phase 4 enhancements.

**Future: PCGW auto-populate:**
- When PCGW metadata lookup runs, it can bulk-add common tags
- User tags are never overwritten; PCGW adds to existing list
- Deduplication on add: normalize + alias resolution + distinct check
- Alias map is the key to preventing "Roleplaying Game" / "RPG" duplicates

**Persistence:** Add `Tags` to `GameEntryDto` in `GamesDatabaseService`. Backward-compatible: missing field defaults to empty list.

---

## 3. Code Changes Summary

### Phase 1: Mode Switcher

| File | Change |
|------|--------|
| `Core/Models/GameEngineKind.cs` | **NEW** — engine enum |
| `Core/Models/GameEntry.cs` | Add `GameEngine` field |
| `App/Services/EngineDetector.cs` | **NEW** — port `_detect_engine()` from detect.py |
| `App/Services/FolderScanner.cs` | Call `EngineDetector` during scan, set `GameEngine` |
| `App/Services/GamesDatabaseService.cs` | Add `GameEngine` to DTO, backward-compat default |
| `UI/ViewModels/ShellViewModel.cs` | Add `ViewMode` enum, mode cycling logic, `LoadStoreView()`, `LoadEngineView()` |
| `App/MainWindow.axaml.cs` | F9 handler: cycle mode instead of jump to roots |
| `App/MainWindow.axaml` | Update F9 button label binding |
| `App/Services/HelpDialogBuilder.cs` | Update F9 description |

### Phase 2: Flatten Library

| File | Change |
|------|--------|
| `Core/Models/AppConfig.cs` | Add `FlattenLibraryView` field |
| `App/Services/GamesDatabaseService.cs` | (DTO already handles unknown fields via Extra) |
| `App/LibrarySetupWindow.axaml` | Add flatten toggle UI |
| `App/LibrarySetupWindow.axaml.cs` | Handle toggle, persist to config |
| `UI/ViewModels/ShellViewModel.cs` | Check `FlattenLibraryView` in `JumpToLibraryRoots()` |

### Phase 3: Filter (F5) + Tag Editing

| File | Change |
|------|--------|
| `Core/Models/GameEntry.cs` | Add `Tags` field (`List<string>`) |
| `App/Services/GamesDatabaseService.cs` | Add `Tags` to DTO, backward-compat default |
| `App/GameSetupWindow.axaml` | Add tags input field UI |
| `App/GameSetupWindow.axaml.cs` | Handle tag editing, parse comma-separated input |
| `App/Services/FilterService.cs` | **NEW** — filter logic (store, engine, tags) |
| `App/FilterWindow.axaml` | **NEW** — filter dialog UI |
| `App/FilterWindow.axaml.cs` | **NEW** — filter dialog code-behind |
| `App/MainWindow.axaml.cs` | F5 handler: open filter dialog |
| `UI/ViewModels/ShellViewModel.cs` | `ApplyFilter()`, `ClearFilter()` methods |
| `App/Services/HelpDialogBuilder.cs` | Add F5 description |

---

## 4. Prerequisite Analysis

| Phase | Prerequisite | Status |
|-------|-------------|--------|
| Phase 1 | MVP complete | ✅ After T70 |
| Phase 1 | Engine detection in C# | ❌ Not yet — needs `EngineDetector` port from detect.py |
| Phase 2 | Phase 1 complete | ❌ |
| Phase 2 | Config UI (LibrarySetupWindow) | ✅ Exists |
| Phase 3 | Phase 1 complete (engine data) | ❌ |
| Phase 3 | GameEntry.Tags field | ❌ Simple addition |
| Phase 3 | F4 editor extension | ✅ Exists (just needs tag input row) |

**Critical path:** Phase 1 is self-contained (engine detection is a local filesystem probe). Phase 3 depends on Phase 1 for engine data but has NO external dependency — tags are user-curated, not fetched.

---

## 5. Relationship to Existing Plan 05 (Category Browse)

Plan 05 (`05-phase-3-category-browse.md`) proposes a similar but different system:

| Aspect | Plan 05 | Plan 101 (this) |
|--------|---------|-----------------|
| Toggle key | F8 | F9 |
| Modes | 2 (Library Roots ↔ Categories) | 3 (Library ↔ Store ↔ Engine) |
| Category drill-down | 3-level (Category → Value → Games) | 2-level (Store/Engine → Games) |
| Filter | S (search) | F5 (filter dialog) |
| Flatten | Not addressed | Phase 2 config option |
| Metadata dependency | Heavy (Genre, Publisher, Year, Rating) | Light (Phase 1 needs only engine detection) |

**Recommendation:** Plan 101 is simpler and more immediately useful. Plan 05's category browsing (Genre, Publisher, Year) can be added later as an extension to Plan 101's filter system (Phase 3). The two plans are complementary, not competing.

**Merge strategy:**
- Phase 1 (Mode Switcher) replaces Plan 05's F8 toggle with F9 cycling
- Phase 3 (Filter) replaces Plan 05's S search with F5 filter (search can coexist)
- Plan 05's category browsing becomes a Phase 4 extension (add Genre/Publisher/Year as filter categories once metadata exists)

---

## 6. Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| Engine detection false positives | Low | Medium | Use same signals as detect.py (proven on real libraries) |
| Performance with many games | Low | Low | In-memory filtering is fast for <1000 games |
| Backward compatibility (GameEntry change) | Low | Medium | Default `GameEngine = Unknown`, `Tags = []` for existing entries |
| F9 cycling confusion | Medium | Low | Status bar shows current mode; hint text updates |
| Tag sparsity (user hasn't tagged enough) | Medium | Low | Filter shows "no matches" gracefully; tags are opt-in |
| Flatten view loses root context | Low | Low | User explicitly opts in; roots still visible in Store/Engine modes |

---

## 7. Testing Strategy

### Phase 1 Tests
- `EngineDetectorTests.cs`: Test each engine signal (Unreal, Unity, RAGE, Frostbite, Unknown)
- `ShellViewModelModeTests.cs`: Test mode cycling (Library → Store → Engine → Library)
- `StoreGroupingTests.cs`: Test games grouped correctly by `GameSourceKind`
- `EngineGroupingTests.cs`: Test games grouped correctly by `GameEngine`

### Phase 2 Tests
- `FlattenLibraryTests.cs`: Test flatten config toggle affects top-level view
- `FlattenWithMultipleRootsTests.cs`: Test all games merged correctly

### Phase 3 Tests
- `TagTests.cs`: Test tag add/remove/dedup logic, normalization (case, whitespace)
- `TagAliasTests.cs`: Test alias resolution (Phase 4 prep — define interface now)
- `FilterServiceTests.cs`: Test filter by store, engine, tags
- `FilterDialogTests.cs`: Test filter dialog state management
- `GameSetupWindowTagTests.cs`: Test tag editing in F4

---

## 8. Success Criteria

### Phase 1
- [ ] F9 cycles through Library → Store → Engine modes
- [ ] Status bar shows current mode and game count
- [ ] Store mode groups games by `GameSourceKind` across all roots
- [ ] Engine mode groups games by `GameEngine` across all roots
- [ ] Engine detection works for Unreal, Unity, RAGE, Frostbite
- [ ] Existing library root view unchanged when in Library mode
- [ ] Build clean, all tests pass

### Phase 2
- [ ] `FlattenLibraryView` config option in settings.json
- [ ] Toggle in LibrarySetupWindow UI
- [ ] When enabled, Library mode shows single "All Games" entry
- [ ] Store/Engine modes unaffected by flatten setting
- [ ] Build clean, all tests pass

### Phase 3
- [ ] `GameEntry.Tags` field added (List<string>)
- [ ] F4 editor has tags input field (comma-separated)
- [ ] Tags persist to games.json
- [ ] F5 opens filter dialog
- [ ] Filter by store (checkboxes)
- [ ] Filter by engine (checkboxes)
- [ ] Filter by tags (multi-select from all used tags)
- [ ] Filter updates left pane in real-time
- [ ] Clear filter restores previous view
- [ ] Status bar shows active filter summary
- [ ] Build clean, all tests pass

---

## 9. Execution Order

```
Phase 1 (Mode Switcher) → Phase 2 (Flatten) → Phase 3 (Filter)
         ↓                       ↓                    ↓
    EngineDetector          Config UI           FilterService
    ViewMode enum           Flatten logic       FilterWindow
    Store/Engine views      LibrarySetup        Store/Engine/Genre
```

**Phase 1 is the foundation.** Phase 2 and 3 are independent of each other but both depend on Phase 1.

---

## 10. Out of Scope

- Plan 05's S-key quick search (can coexist, not blocked by this plan)
- Plan 05's 3-level category drill-down (Genre → Value → Games) — deferred to Phase 4
- PCGW auto-populate tags — future enhancement (Phase 4), user tags are primary
- Custom user-defined tag categories/groups — future extension
- Persistent filter presets — future extension
- Tag auto-suggestion in F4 — future enhancement

---

**Planner note:** This plan is deliberately simpler than Plan 05. The 3-mode toggle (Library/Store/Engine) covers the most common browsing patterns without requiring metadata. The filter system (Phase 3) uses user-editable tags as its foundation — no external dependency. PCGW auto-populate is a future enhancement, not a prerequisite. Plan 05's full category browsing can be layered on top as a Phase 4 extension.
