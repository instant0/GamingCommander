# Plan 106 — Unified Setup Screen (Merge Wizard + F2)

**Status:** DRAFT — awaiting approval  
**Audience:** Planner / Builder  
**Priority:** P1 (UX consistency + maintenance reduction)  
**Effort:** ~4–6 hours  
**Depends on:** Plan 105 (F5 rescan fix)  

---

## 0. Problem Statement

GamingCommander has **two separate setup screens** that do overlapping things:

| Screen | Trigger | Purpose |
|--------|---------|---------|
| **First-Run Wizard** | Auto-launch on first run / version upgrade | Onboarding: add initial library roots |
| **F2 Library Setup** | User presses F2 | Ongoing management: add/remove/rescan roots |

They share ~60-70% of their logic but implement it independently:
- Both have folder picking, nesting validation, type selection, scanning
- Wizard bypasses `ILibraryManager` — creates its own `FolderScanner` directly
- F2 uses `ILibraryManager` properly
- Wizard has features F2 lacks (online metadata toggle, scan progress badges, blacklist)
- F2 has features Wizard lacks (loads existing roots, empty-state message, immediate persistence)

**The user's observation is correct:** "Why do we have two different setup screens that are supposed to do the same thing?"

---

## 1. Analysis: What Each Screen Provides

### Wizard Features (not in F2)

| Feature | Description |
|---------|-------------|
| Online metadata toggle | `EnableOnlineMetadata` checkbox |
| Per-entry scan progress | "Scanning D:\Games..." badge per entry |
| Scan state badges | "not scanned" / "N games" / "scanning..." |
| Deferred persistence | Entries saved only on Finish/Cancel |
| Finish/Skip semantics | "Skip" saves only scanned entries |
| Blacklist loading | Manual `BlacklistLoader` + passes to `FolderScanner` |

### F2 Features (not in Wizard)

| Feature | Description |
|---------|-------------|
| Loads existing roots | Shows currently configured roots on open |
| Immediate persistence | Add/remove saved to config + DB immediately |
| Empty-state message | "(no library roots configured — click '+ Add Root' to begin)" |
| Uses `ILibraryManager` | Clean abstraction, no duplicate scanner instantiation |
| Close button | Simple close, no Finish/Cancel semantics |

### Shared Logic (duplicated)

| Logic | Wizard Location | F2 Location |
|-------|----------------|-------------|
| Folder picking | `AddEntryAsync()` | `AddRootAsync()` |
| Nesting validation | `IsChildOf()` check | Identical copy |
| Duplicate rejection | Path comparison | Identical copy |
| Type auto-inference | `GameSourceParser.InferFromPath()` | Same call |
| Path normalization | `LibraryManager.NormalizeLibraryRoot()` | Same call |
| Scanning | Direct `FolderScanner`/`SteamLibraryScanner` | Via `ILibraryManager.AddRoot()` |

---

## 2. Proposed Solution: Single `LibrarySetupWindow`

**Keep F2 Library Setup as the single setup screen.** Enhance it with the Wizard's missing features.

### What F2 Gains from Wizard

| Feature | How |
|---------|-----|
| Online metadata toggle | Add `EnableOnlineMetadata` checkbox to F2 UI |
| Per-entry scan progress | Add `IsScanning` property to `LibraryRootEntry`, show in UI |
| Scan state badges | Show "Not scanned" / "N games" / "Scanning..." per entry |

### What F2 Already Does Better

| Feature | Why it stays |
|---------|-------------|
| Uses `ILibraryManager` | Clean abstraction |
| Immediate persistence | Crash-safe |
| Loads existing roots | Shows current state |
| Simple Close | No confusing Finish/Cancel |

### What Gets Removed

| Component | Why |
|-----------|-----|
| `WizardWindow.axaml` | Replaced by enhanced F2 |
| `WizardWindow.axaml.cs` | Replaced by enhanced F2 |
| `WizardViewModel.cs` | Replaced by enhanced F2 |
| `WizardLibraryEntry` | Replaced by `LibraryRootEntry` |
| First-run wizard trigger in `App.axaml.cs` | Redirect to F2 |

---

## 3. First-Run Flow

### Current Flow

```
App launch → App.axaml.cs checks IsFirstRun
  → true: Show WizardWindow
  → false: Show MainWindow
```

### New Flow

```
App launch → App.axaml.cs checks IsFirstRun
  → true: Show MainWindow, then auto-open LibrarySetupWindow (F2)
  → false: Show MainWindow
```

The Wizard becomes unnecessary. On first run, the app opens normally and immediately shows the F2 setup dialog. The user adds roots, closes the dialog, and sees their games.

**Implementation in `App.axaml.cs`:**
```csharp
// After MainWindow construction:
if (config.IsFirstRun || config.LibraryRoots.Count == 0)
{
    // Auto-open F2 setup on first run
   Dispatcher.UIThread.Post(async () =>
{
        await ShowLibrarySetupAsync();
    }, DispatcherPriority.ApplicationIdle);
}
```

---

## 4. Enhanced LibrarySetupWindow UI

### Current F2 Layout

```
┌─────────────────────────────────────────────┐
│  Library Root Setup                         │
│                                             │
│  ┌─────────────────────────────────────┐    │
│  │ D:\SteamLibrary        [Steam ▼] [X]│    │
│  │ D:\Games               [Auto  ▼] [X]│    │
│  │ E:\Games               [Auto  ▼] [X]│    │
│  └─────────────────────────────────────┘    │
│                                             │
│  Status: Scanning complete                  │
│                                             │
│  [+ Add Root]              [Close]          │
└─────────────────────────────────────────────┘
```

### Enhanced F2 Layout (with Wizard features)

```
┌─────────────────────────────────────────────────────┐
│  Library Setup                                       │
│                                                      │
│  ┌──────────────────────────────────────────────┐    │
│  │ D:\SteamLibrary     [Steam ▼]  ✓ 127 games  [X]│
│  │ D:\Games            [Auto ▼]   ✓ 43 games   [X]│
│  │ E:\Games            [Auto ▼]   ⏳ Scanning...[X]│
│  │ (empty)             [Auto ▼]   ⚠ Not scanned [X]│
│  └──────────────────────────────────────────────┘    │
│                                                      │
│  ☑ Enable online metadata lookups (PCGW, Steam)     │
│                                                      │
│  Status: Scanning D:\Games... (2/4)                  │
│                                                      │
│  [+ Add Root]                    [Close]             │
└─────────────────────────────────────────────────────┘
```

### New Properties on `LibraryRootEntry`

```csharp
public record LibraryRootEntry : ReactiveObject
{
    // ... existing: Path, DefaultType, GameCount ...
    
    /// <summary>True while this root is being scanned.</summary>
    public bool IsScanning { get; set; }
    
    /// <summary>Status text: "✓ 127 games", "⏳ Scanning...", "⚠ Not scanned"</summary>
    public string StatusText { get; set; } = "Not scanned";
}
```

---

## 5. Code Changes

### Remove

| File | Action |
|------|--------|
| `App/WizardWindow.axaml` | Delete |
| `App/WizardWindow.axaml.cs` | Delete |
| `App/ViewModels/WizardViewModel.cs` | Delete |
| `Core/Models/WizardLibraryEntry.cs` | Delete (if exists as separate file) |

### Modify

| File | Change |
|------|--------|
| `App/ViewModels/LibrarySetupViewModel.cs` | Add scan progress tracking, metadata toggle, enhanced status |
| `App/LibrarySetupWindow.axaml` | Add metadata checkbox, scan progress badges, wider type ComboBox |
| `App/LibrarySetupWindow.axaml.cs` | Add scan progress rendering |
| `App/App.axaml.cs` | Replace wizard trigger with F2 auto-open |

### Data Model

| File | Change |
|------|--------|
| `Core/Models/LibraryRootEntry.cs` | Add `IsScanning`, `StatusText` properties |

---

## 6. WizardViewModel Features to Preserve

### Online Metadata Toggle

Already exists in `AppConfig.EnableOnlineMetadata`. Just needs a checkbox in F2 UI:
```xml
<CheckBox IsChecked="{Binding EnableOnlineMetadata}"
          Content="Enable online metadata lookups (PCGW, Steam)" />
```

### Scan Progress Badges

In `LibrarySetupViewModel.AddRootAsync()` and `ScanAndSaveAsync()`:
```csharp
entry.IsScanning = true;
entry.StatusText = "Scanning...";
// ... scan ...
entry.IsScanning = false;
entry.StatusText = $"✓ {entry.GameCount} games";
```

### Deferred Persistence (NOT preserved)

The Wizard's "save only on Finish" approach is inferior — crash-unsafe. F2's immediate persistence is better. This feature is intentionally dropped.

---

## 7. Migration Path

### Step 1: Enhance F2 (no removal yet)
- Add metadata toggle, scan progress, status badges to F2
- Test F2 works with all existing functionality

### Step 2: Update first-run trigger
- Replace wizard launch with F2 auto-open in `App.axaml.cs`
- Test first-run flow works

### Step 3: Remove Wizard
- Delete `WizardWindow`, `WizardViewModel`, `WizardLibraryEntry`
- Remove wizard references from `App.axaml.cs`
- Build and test

---

## 8. Tests

- `LibrarySetupViewModelTests.cs`: Test metadata toggle persistence
- `LibrarySetupViewModelTests.cs`: Test scan progress state transitions
- `LibrarySetupViewModelTests.cs`: Test status badge text
- `AppStartupTests.cs`: Test first-run opens F2 (not wizard)

---

## 9. Success Criteria

- [ ] Single setup screen (F2) handles both first-run and ongoing management
- [ ] Online metadata toggle available in F2
- [ ] Per-entry scan progress badges show "Not scanned" / "Scanning..." / "N games"
- [ ] First-run auto-opens F2 instead of wizard
- [ ] Wizard code fully removed
- [ ] No duplicate scanner instantiation (all through `ILibraryManager`)
- [ ] Build clean, all tests pass

---

## 10. Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| F2 loses Wizard-only functionality | Low | Medium | Feature parity checklist before removal |
| First-run flow confusers users | Low | Low | F2 is self-explanatory with empty state message |
| Scan progress causes UI freeze | Low | Medium | Async scanning with progress updates |
| Removing Wizard breaks version upgrade path | Low | Low | Version check still works; F2 opens if no roots |

---

**Planner note:** This plan eliminates ~500 lines of duplicated code (WizardViewModel + WizardWindow) and ensures a single, well-tested setup path. The F2 dialog becomes the one place users manage library roots, whether it's their first time or their hundredth.
