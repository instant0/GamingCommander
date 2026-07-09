# GamingCommander Architecture

Core Concept

GamingCommander presents a virtual filesystem.

The user navigates game records.

The user does not browse raw folders.

Navigation Flow

F9
↓
Library Roots
↓
Games from games.json
↓
Game Details

Filesystem Access

Allowed:

* Setup
* Rescan

Forbidden:

* Browse()
* Navigation
* Details panel

Browse()

Browse() reads only from:

data/games.json

Virtual Item Types

LibraryRoot
Game
Category (future)
CategoryValue (future)

A Game is not a Directory.

Selecting a game updates details.

Selecting a game does not navigate.

Executable Detection Pipeline

1. Enumerate candidates
2. Apply exclusion scoring
3. Apply positive scoring
4. Rank candidates
5. Return highest score
6. Return confidence score

Provider Detection Pipeline

Steam
Epic
GOG
EA
Ubisoft
Battle.net
Xbox

Each detector must be isolated.

Future Metadata Pipeline

PCGamingWiki
↓
SteamDB
↓
Steam Store
↓
IGDB
↓
games_db.json

Future Migration Pipeline

Game
↓
SyncMove
↓
Backup Manifest
↓
Move Files
↓
Update Manifest
↓
Create Junction
↓
Validate

