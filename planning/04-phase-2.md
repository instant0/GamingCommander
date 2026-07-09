# Phase 2: Steam & Stand-alone Games

## Goal
Implement the baseline game management features.

## Tasks
1. [ ] **Steam Integration**
    - Implement Steam library detection (reading `libraryfolders.vdf`).
    - Parse Steam manifest files (`appmanifest_*.acf`).
2. [ ] **Stand-alone Detection**
    - Implement generic scanning for `.exe` files in user-supplied folders.
3. [ ] **Migration (Steam)**
    - Implement manifest repair for relocated game folders.
    - Implement Steam manifest patching (updating install path).
4. [ ] **Launcher Logic**
    - Implement game launching via URI schemes/process execution.
5. [ ] **Deliverables**
    - Steam games appear in the UI.
    - Stand-alone games detected via folder scan.
    - Steam games can be migrated safely.
