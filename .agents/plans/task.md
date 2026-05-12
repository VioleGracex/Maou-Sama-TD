# Maou-Sama-TD: Balancing & Polish Task List

## Stage 1: Playable Levels & Balance 🟢
- [x] **Stat Balancing**
    - [ ] Update `EnemyData` and `UnitData` stats via automated script.
    - [ ] Balance "Leeway Distance" for monsters to reach the goal.
- [x] **Level Clarity**
    - [ ] Enhance `PathVisualizer` with flow animations/indicators.
    - [x] Implement clear Wave Count display in `GameControlUI`.
    - [x] Correct `MapDataEditor` Shift buttons (N/S/E/W/NE/NW/SE/SW).
    - [x] Standardize `EnemyData` custom editor fields (Priority, HighGround, Evasion, etc.)
    - [ ] Investigate and explain Ignis damage vs Immune boss in Level 2
    - [ ] Finalize `LevelDataEditor` wave balancing configurations
    - [ ] Ensure consistent map/unit scaling across different levels.

## Stage 2: Visual Clarity & UX 🔵
- [ ] **Unit Visuals**
    - [ ] Redesign HP bars for better visibility (Units & Enemies).
    - [ ] Add numeric HP display to unit bars.
    - [ ] Create "Fallback" minimalistic VFX for skills/rites.
- [ ] **UI Polish**
    - [ ] **Tooltips**: Implement hover tips for skills/rites using UI prefabs.
    - [x] **Tactical Mode**: Implement "0x Time" toggle (Speed cycle x1->x2->x0) in `GameControlUI`.
    - [x] **Path Estimation**: Add arrival time simulation in Editor (MapDataEditor).
    - [ ] **Deployment Bar**:
        - [x] Distinct visual feedback for "Insufficient Seals" (Tinting).
        - [x] "Unit Already Placed" visual (Desaturation) + "Retreat" button integration.
    - [ ] **Player HP**: Clarify distinction between Wagon/Nexus HP vs Maou HP.
- [/] **Rethink UI Layout**
    - [ ] Reposition UI elements to accommodate:
        - [x] Combat Log (Implemented + Event Integrated).
        - [ ] Wave Progress.
        - [ ] Authority Seals.
        - [ ] Top-Center Dialogue overlaps.

## Stage 3: Automation & content  purple
- [ ] **Python Automation**
    - [ ] Refine `update_unity_stats.py` to handle full stat grid.
    - [ ] Update Power Scaling formulas to include skill multipliers.
- [ ] **Skill Implementation**
    - [ ] Define and write Ultimate Skill data for all 70+ units.
    - [ ] Define enemy skills/abilities.
- [ ] **Unit Browser Enhancements**
    - [ ] Add global info/checklist for Ultimates, Skins, and Passive counts.
    - [ ] Expand Unit Detail view with skill breakdown.
- [ ] **VFX Fallbacks**
    - [ ] Document and implement generic "Text/Sprite Fallback" for missing animations.
