# Plan 115: Multi-Launcher Registry Fallback

**Created:** 2026-07-26
**Updated:** 2026-07-26 — Implemented and verified
**Priority:** P2
**Status:** ✅ COMPLETE
**Source:** Real registry exports (`/home/malware/projects/game-text/*.reg`), `docs/research/launcher_discovery.md`

---

## 1. Problem Statement

Games exist on disk but lack filesystem markers that identify their launcher. Current detection (Pass 1 store signals + Pass 2 fallback) misses games whose store launcher metadata files were deleted, moved, or never created.

| Store | Filesystem Signal | Gap |
|-------|------------------|-----|
| EA | `__Installer/`, `Touchup.exe`, `ActivationUI.exe` | EA Desktop may not create `__Installer/` for all installs |
| Ubisoft | `uplay_install.manifest`, loader DLLs | Modern Ubisoft Connect may not create these markers |
| GOG | `goggame-*.info` | Info files may be deleted or in subdirectories |
| Rockstar | `title.rgl` | May not exist for all installations |
| Epic | `.egstore/` directory | May not exist if launcher data was cleaned |

**Registry fallback** reads install paths from Windows registry keys that all launchers maintain, bridging the gap between "game files exist" and "launcher knows about them."

---

## 2. Real Registry Key Map (from exports)

### EA App — Per-game keys (REAL)

```
HKLM\SOFTWARE\WOW6432Node\EA Games\{gameName}\Install Dir    = "V:\Games\Dead Space 3\"
  Values: GDFBinary, DisplayName, Locale, Product GUID, Install Dir

HKLM\SOFTWARE\WOW6432Node\Electronic Arts\EA Core
  "ClientPath" = "C:\Program Files\Electronic Arts\EA Desktop\EALauncher.exe"

HKLM\SOFTWARE\WOW6432Node\Electronic Arts\EA Desktop
  "DesktopAppPath"  = "C:\...\EA Desktop.exe"
  "LauncherAppPath" = "C:\...\EADesktop.exe"
  "InstallLocation" = "C:\Program Files\Electronic Arts\EA Desktop\"
```

**Key insight:** Per-game keys are under **publisher subfolders** (`EA Games`, `BioWare`, `Respawn`). Each has `Install Dir` with the actual game path. The EA Desktop `InstallLocation` only points to the launcher itself — NOT the games directory.

**IMPORTANT — Our mock was WRONG:** `HKCU\...\EA Core\GameInstallFolder` does NOT exist in real exports. Per-game `Install Dir` under `EA Games\{gameName}` is the correct key.

### Ubisoft Connect — Per-game keys (REAL)

```
HKLM\SOFTWARE\WOW6432Node\Ubisoft\Launcher\Installs\{gameId}\InstallDir
  = "E:/Games/Ghost Recon Breakpoint/"

  Values: Language, InstallDir
  Game IDs: 11903 (Breakpoint), 4932 (Division 2), 5405 (Riders Republic)

HKLM\SOFTWARE\WOW6432Node\Ubisoft\Launcher
  "InstallDir" = "C:\...\Ubisoft Game Launcher\"

HKLM\SOFTWARE\WOW6432Node\Ubisoft\{gameName}
  "Language" = "en_US"
```

**Key insight:** Per-game keys are under `Launcher\Installs\{numericGameId}\InstallDir`. The `Launcher\InstallDir` only points to the launcher itself.

**IMPORTANT — Our mock was WRONG:** `GameInstallPath` does NOT exist in real exports. Per-game `InstallDir` under `Launcher\Installs\{gameId}` is the correct key.

### GOG Galaxy — Per-game keys (richest data, REAL)

```
HKLM\SOFTWARE\WOW6432Node\GOG.com\Games\{gameId}
  "path"            = "E:\\Games\\Blasphemous 2"
  "exe"             = "\"E:\\Games\\Blasphemous 2\\Blasphemous2.exe\""
  "exeFile"         = "Blasphemous2.exe"
  "gameName"        = "Blasphemous 2"
  "gameID"          = "1201963702"
  "launchCommand"   = "\"E:\\Games\\Blasphemous 2\\Blasphemous2.exe\""
  "workingDir"      = "E:\\Games\\Blasphemous 2"
  "BUILDID"         = "58798301257626864"
  "INSTALLDATE"     = "1675700403406"
  "DLC"             = "1384849844"
  "uninstallCommand"= "\"GOGGalaxy.exe\" /uninstall=..."
  "language"        = "en"
  "lang_code"       = "en"
  "ver"             = "gog-2"

HKLM\SOFTWARE\WOW6432Node\GOG.com\GalaxyClient\paths\client
  = "C:\Program Files (x86)\GOG Galaxy"

HKLM\SOFTWARE\WOW6432Node\GOG.com\DefaultPackPath
  = "C:\GOG Games"
```

**Key insight:** GOG per-game keys contain `path`, `exe`, `gameName`, `gameID`, `launchCommand` — everything needed for full game identification and metadata enrichment.

### Epic Games Store (REAL)

```
HKLM\SOFTWARE\Epic Games\EpicGamesLauncher
  "AppDataPath"     = "C:\ProgramData\Epic\EpicGamesLauncher\Data\"

HKCU\Software\Epic Games\EpicGamesLauncher
  "DataPath"        = "C:\ProgramData\Epic\EpicGamesLauncher\Data\"
```

**Key insight:** Epic has NO per-game registry keys. Only the launcher data path. Per-game manifests are `.item` files inside `{AppDataPath}\Manifests\`. Already handled by `EpicManifestParser.CrossReferenceGlobalManifests()`.

### Rockstar Games — Per-game keys (REAL)

```
HKLM\SOFTWARE\WOW6432Node\Rockstar Games\{gameName}\InstallFolder
  = "E:\Games\Grand Theft Auto V Enhanced"
  Sub-values: "GTAV", "GTAVLauncher", "Launcher"

HKLM\SOFTWARE\WOW6432Node\Rockstar Games\Launcher\InstallFolder
  = "C:\Program Files\Rockstar Games\Launcher\"
```

### Origin (Legacy) (REAL)

```
HKLM\SOFTWARE\WOW6432Node\Origin Games\{gameId}
  "DisplayName" = "Dragon Age™: Inquisition"
  (NO install path value — only display name)
```

**Key insight:** Origin per-game keys have DisplayName but NOT install path. The `.mfst` files at `%ProgramData%\Origin\LocalContent\` contain `dipInstallPath` query strings.

### Steam (not needed for registry fallback)

Steam detection is structural (`steamapps/common/`). Registry `HKCU\Software\Valve\Steam\SteamPath` exists but is redundant since `SteamLibraryScanner` already handles Steam.

---

## 3. Architecture

### Current Detection Chain

```
FolderScanner.Scan()
  ├── Pass 1:   StoreSignalDetector.DetectType()     ← filesystem signals
  ├── Pass 1b:  Parent BattleNet propagation
  ├── Pass 2:   FallbackSignalDetector.DetectFallbackType()  ← deep filesystem
  └── Pass 3:   ContainerScanner.ScanContainerChildren()
```

### Proposed Addition — Pass 1c

```
FolderScanner.Scan()
  ├── Pass 1:   StoreSignalDetector.DetectType()     ← filesystem signals
  ├── Pass 1b:  Parent BattleNet propagation
  ├── Pass 1c:  RegistryFallbackDetector.Detect()     ← NEW: registry-based detection
  ├── Pass 2:   FallbackSignalDetector.DetectFallbackType()
  └── Pass 3:   ContainerScanner.ScanContainerChildren()
```

**Why between Pass 1b and Pass 2:**
- Pass 1 catches games with filesystem markers (highest confidence)
- Pass 1b catches BattleNet games inside launcher dirs
- Pass 1c catches games without markers but with registry entries (medium confidence)
- Pass 2 catches games with deep filesystem signals (lower confidence)
- Pass 3 catches container patterns (lowest confidence)

### IRegistryReader Interface

The interface needs three methods for per-game key enumeration:

```csharp
public interface IRegistryReader
{
    string? ReadStringValue(string keyPath, string valueName);
    IReadOnlyDictionary<string, string> ReadKeyValues(string keyPath);
    IReadOnlyList<string> EnumerateSubKeyNames(string keyPath);  // NEW
}
```

### RegistryFallbackDetector Strategy

The detector uses a two-tier approach for EA and Ubisoft:

1. **Launcher directory check** (fast): Read the launcher's default games directory from registry. If the game folder is a child of that directory, classify it.
2. **Per-game enumeration** (thorough, only if launcher check fails): Enumerate per-game registry keys, collect all known install paths, check if the game folder matches any of them.

For GOG: Per-game keys are rich enough to use as primary detection (no launcher directory check needed).

For Rockstar: Per-game keys provide install path directly.

For Epic: No per-game registry — already handled by `EpicManifestParser`.

---

## 4. Implementation Steps

### Step 1: Create `IRegistryReader` Interface

**File:** `src/GamingCommander.Core/Services/IRegistryReader.cs`

```csharp
namespace GamingCommander.Core.Services;

public interface IRegistryReader
{
    string? ReadStringValue(string keyPath, string valueName);
    IReadOnlyDictionary<string, string> ReadKeyValues(string keyPath);
    IReadOnlyList<string> EnumerateSubKeyNames(string keyPath);
}
```

**Why `EnumerateSubKeyNames`:** Required to enumerate per-game keys under `EA Games\`, `Ubisoft\Launcher\Installs\`, `GOG.com\Games\`, `Rockstar Games\`.

### Step 2: Create `WindowsRegistryReader`

**File:** `src/GamingCommander.App/Services/WindowsRegistryReader.cs`

- Uses `Microsoft.Win32.Registry`
- Handles HKLM (64-bit view) and WOW6432Node (32-bit view)
- `EnumerateSubKeyNames` opens the key and returns `GetSubKeyNames()`

### Step 3: Update Mock Registry Files

**Files:** `data/mock/registry/*.reg.txt`

Replace mock data with corrected keys matching real exports:

**ea.reg.txt** — Add per-game keys under EA Games publisher subfolders:
```
[HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\EA Games\Dead Space 3]
"GDFBinary"=str(2):"GDFBinary.dll"
"DisplayName"=str(2):"Dead Space 3"
"Install Dir"=str(2):"/home/malware/projects/gamingCommander/data/mock/ea/Games/Dead Space 3"
"Locale"=str(2):"en_US"

[HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\EA Games\Mass Effect 3]
"Install Dir"=str(2):"/home/malware/projects/gamingCommander/data/mock/ea/Games/Mass Effect 3"
"DisplayName"=str(2):"Mass Effect 3"
```

**ubisoft.reg.txt** — Add per-game keys under Launcher\Installs:
```
[HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Ubisoft\Launcher\Installs\11903]
"InstallDir"=str(2):"/home/malware/projects/gamingCommander/data/mock/ubi/Games/Ghost Recon Breakpoint"
"Language"=str(2):"en_US"

[HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Ubisoft\Launcher\Installs\4932]
"InstallDir"=str(2):"/home/malware/projects/gamingCommander/data/mock/ubi/Games/The Division 2"
```

**gog.reg.txt** — Add per-game keys with full metadata:
```
[HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\GOG.com\Games\1201963702]
"path"=str(2):"/home/malware/projects/gamingCommander/data/mock/gog/Games/Blasphemous 2"
"exe"=str(2):"\"/home/malware/projects/gamingCommander/data/mock/gog/Games/Blasphemous 2/Blasphemous2.exe\""
"gameName"=str(2):"Blasphemous 2"
"gameID"=str(2):"1201963702"
```

**rockstar.reg.txt** — NEW file with per-game keys:
```
[HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Rockstar Games\Grand Theft Auto V Enhanced]
"InstallFolder"=str(2):"/home/malware/projects/gamingCommander/data/mock/rockstar/Games/Grand Theft Auto V Enhanced"
```

### Step 4: Create `MockRegistryReader`

**File:** `tests/GamingCommander.App.Tests/MockRegistryReader.cs`

- Parses `.reg` files using same regex logic as `parse_registry.py`
- Supports string values, DWORD values, multi-line hex (skipped)
- `EnumerateSubKeyNames` returns all keys that start with the given prefix

### Step 5: Create `RegistryFallbackDetector`

**File:** `src/GamingCommander.App/Services/RegistryFallbackDetector.cs`

```csharp
internal sealed class RegistryFallbackDetector
{
    private readonly IRegistryReader _registry;
    private readonly Dictionary<string, string> _eaGamePaths;    // gameName → installPath
    private readonly Dictionary<string, string> _ubiGamePaths;   // gameId → installPath
    private readonly Dictionary<string, string> _gogGamePaths;   // gameId → installPath
    private readonly Dictionary<string, string> _rockstarPaths;  // gameName → installPath

    public RegistryFallbackDetector(IRegistryReader registry) { ... }

    /// Returns detected GameSourceKind or Unknown.
    public GameSourceKind DetectType(DirectoryInfo gameDir) { ... }
}
```

**Detection strategy:**
1. Check if `gameDir` matches any EA per-game `Install Dir` path
2. Check if `gameDir` matches any Ubisoft per-game `InstallDir` path
3. Check if `gameDir` matches any GOG per-game `path` value
4. Check if `gameDir` matches any Rockstar per-game `InstallFolder` path

**Caching:** Per-game paths are enumerated once in the constructor and cached in dictionaries. This is O(1) per lookup.

### Step 6: Integrate into `FolderScanner`

**File:** `src/GamingCommander.App/Services/FolderScanner.cs`

Add a constructor overload accepting `IRegistryReader?` and create `RegistryFallbackDetector`:

```csharp
private readonly RegistryFallbackDetector? _registryFallback;

public FolderScanner(
    IEnumerable<string> hiddenFolderNames,
    BlacklistData blacklist,
    IRegistryReader? registryReader = null)
    : this(hiddenFolderNames, blacklist.ExeNamePatterns, ...)
{
    _registryFallback = registryReader is not null
        ? new RegistryFallbackDetector(registryReader)
        : null;
}
```

Insert Pass 1c in `Scan()` between Pass 1b and Pass 2:

```csharp
// Pass 1c: Registry fallback (EA, Ubisoft, GOG, Rockstar)
if (signalType == GameSourceKind.Unknown && _registryFallback is not null)
{
    GameSourceKind registryType = _registryFallback.DetectType(subDir);
    if (registryType != GameSourceKind.Unknown)
        signalType = registryType;
}
```

### Step 7: Wire Up in `LibraryManager` / DI

**File:** `src/GamingCommander.App/Services/LibraryManager.cs`

```csharp
// In constructor or DI registration:
IRegistryReader registryReader = OperatingSystem.IsWindows()
    ? new WindowsRegistryReader()
    : new MockRegistryReader(/* default test path */);

var scanner = new FolderScanner(hiddenFolderNames, blacklist, registryReader);
```

Note: `LibraryManager` doesn't currently construct `FolderScanner` — it receives it via DI. The wiring happens in the app's DI container. The scanner is constructed in `MainWindow.axaml.cs` or the DI setup.

### Step 8: Add Tests

**File:** `tests/GamingCommander.App.Tests/RegistryFallbackDetectorTests.cs`

| Test Case | Input | Expected |
|-----------|-------|----------|
| EA game matches per-game key | `EA Games/Dead Space 3/` with EA per-game registry | `EaApp` |
| Ubisoft game matches per-game key | `Games/Ghost Recon Breakpoint/` with Ubisoft per-game registry | `UbisoftConnect` |
| GOG game matches per-game key | `Games/Blasphemous 2/` with GOG per-game registry | `Gog` |
| Rockstar game matches per-game key | `Games/GTA V/` with Rockstar per-game registry | `Rockstar` |
| Game NOT in any registry | `D:\RandomGame\` | `Unknown` |
| EA registry empty | Game dir with no EA keys | `Unknown` |
| Ubisoft registry empty | Game dir with no Ubisoft keys | `Unknown` |
| Case-insensitive path match | Windows paths are case-insensitive | Correct match |
| Path with mixed separators | Forward/back slashes | Correct match |
| Game in registry but not on disk | Registry path exists, dir doesn't | Skipped |

**File:** `tests/GamingCommander.App.Tests/MockRegistryReaderTests.cs`

| Test Case | Input | Expected |
|-----------|-------|----------|
| Parse EA .reg file | `ea.reg.txt` | EA per-game keys parsed |
| Parse Ubisoft .reg file | `ubisoft.reg.txt` | Ubisoft per-game keys parsed |
| Parse GOG .reg file | `gog.reg.txt` | GOG per-game keys parsed |
| EnumerateSubKeyNames | EA key with 2 subkeys | ["Dead Space 3", "Mass Effect 3"] |
| Missing key returns empty | Non-existent key | Empty list |
| Missing value returns null | Non-existent value | null |
| DWORD value parsed as string | `dword:00000001` | "1" |
| Multi-line hex skipped | Hex value spanning lines | Not in result |

---

## 5. Files Affected

| File | Change |
|------|--------|
| `src/GamingCommander.Core/Services/IRegistryReader.cs` | **NEW** — interface with 3 methods |
| `src/GamingCommander.App/Services/WindowsRegistryReader.cs` | **NEW** — production implementation |
| `src/GamingCommander.App/Services/RegistryFallbackDetector.cs` | **NEW** — detection logic with caching |
| `src/GamingCommander.App/Services/FolderScanner.cs` | Add Pass 1c, new constructor overload |
| `src/GamingCommander.App/Services/LibraryManager.cs` | Wire `IRegistryReader` to scanner |
| `tests/GamingCommander.App.Tests/MockRegistryReader.cs` | **NEW** — .reg parser for tests |
| `tests/GamingCommander.App.Tests/RegistryFallbackDetectorTests.cs` | **NEW** — ~10 tests |
| `tests/GamingCommander.App.Tests/MockRegistryReaderTests.cs` | **NEW** — ~8 tests |
| `data/mock/registry/ea.reg.txt` | Rewrite with per-game keys |
| `data/mock/registry/ubisoft.reg.txt` | Rewrite with per-game keys |
| `data/mock/registry/gog.reg.txt` | Rewrite with per-game keys |
| `data/mock/registry/rockstar.reg.txt` | **NEW** — per-game keys |

---

## 6. Risk Assessment

| Risk | Severity | Mitigation |
|------|----------|------------|
| `Microsoft.Win32.Registry` not available on Linux | MEDIUM | Abstract behind `IRegistryReader`; mock for tests |
| Registry keys may not exist on some systems | LOW | All reads return null/empty; fallback is graceful |
| Per-game enumeration requires 4 registry walks | LOW | Cached once in constructor; O(1) per lookup |
| Path comparison edge cases (separators, UNC) | LOW | Normalize with `Path.GetFullPath()` and case-insensitive compare |
| ADR-004 read-only constraint | NONE | Registry reading is read-only |
| GOG per-game data is richest — may need more parsing | LOW | Start with path matching; enrich later |

---

## 7. Success Criteria

- [x] `IRegistryReader` interface in Core with 3 methods
- [x] `WindowsRegistryReader` production implementation
- [x] `MockRegistryReader` parses real .reg format
- [x] Mock `.reg.txt` files corrected with real key paths
- [x] `RegistryFallbackDetector` detects EA, Ubisoft, GOG, Rockstar via registry
- [x] `FolderScanner` consults registry when no filesystem signal (Pass 1c)
- [x] 26 new tests passing (13 detector + 13 mock reader)
- [x] Build clean, all 353 tests pass
- [x] No regressions in existing detection

---

## 8. Future Enrichment (Out of Scope)

This plan covers **detection** only (classifying games by store). Future work:

- **EA per-game metadata**: `DisplayName`, `GDFBinary`, `Locale`, `Product GUID` → enrich game metadata
- **Ubisoft per-game metadata**: `Language` → add to platform metadata
- **GOG per-game metadata**: `gameName`, `launchCommand`, `BUILDID`, `INSTALLDATE`, `DLC`, `ver` → rich metadata enrichment (exceeds `GogInfoParser` from .info files)
- **Rockstar per-game metadata**: Sub-values (`GTAV`, `GTAVLauncher`, `Launcher`) → launcher path resolution
- **Origin per-game metadata**: `DisplayName` only → no install path available
