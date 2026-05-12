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
- [ ] **Unit Visuals (HP Bar Polish)**
    - [ ] Implement Camera-Distance Scale Compensation (keeps HP bar constant pixel size on screen regardless of camera distance).
    - [ ] Standardize dynamic colors (Green for Vassals, Red for Enemies, pulsing Amber for Bosses).
    - [ ] Implement selective visibility (only show world bars on damaged units or highlighted/selected ones).
    - [ ] Implement segmented health tick notches (every 100/500 HP) instead of tiny pixelated numeric text overlays.
    - [ ] Create "Fallback" minimalistic VFX for skills/rites.
- [ ] **UI Polish & Mobile Adaptation**
    - [ ] **PC Tooltips**: Implement hover tips for skills/rites (using dynamic `SkillTooltipUI` prefab).
    - [ ] **Mobile Active Skill Dropdown**: Build top-center dropdown bar that appears when a skill is active (toggled or dragging).
    - [ ] **Screen Anchor Matrix**: Enforce non-overlapping layout bounds across all screen ratios.
        - [ ] Restructure top-center: Move `WaveNumberText` & `WaveEnemyCountText` into flanking pill panels alongside `BaseHP`.
        - [ ] Reposition mini dialogue panel (`MiniTopPanel`) to sit cleanly below `ActiveSkillDetailsUI`.
        - [ ] Coordinate `CombatLogUI` (middle-right) and `Unit_Inspector_UI` (bottom-right) state switching to auto-collapse on overlap.
    - [x] **Tactical Mode**: Implement "0x Time" toggle (Speed cycle x1->x2->x0) in `GameControlUI`.
    - [ ] **Combat Log**: Add a smooth toggle button with slide expand/collapse docking animations.
    - [ ] **Objective HP (Sovereign/Wagon/Tina)**: Set up theme-adaptive decorative borders per level theme.
    - [x] **Path Estimation**: Add arrival time simulation in Editor (MapDataEditor).
    - [ ] **Deployment Bar**:
        - [x] Distinct visual feedback for "Insufficient Seals" (Tinting).
        - [x] "Unit Already Placed" visual (Desaturation) + "Retreat" button integration.

## Stage 3: Automation & Content 🟣
- [ ] **Python Automation**
    - [ ] Refine `update_unity_stats.py` to handle full stat grid.
    - [ ] Update Power Scaling formulas to include skill multipliers.
- [ ] **Skill Implementation**
    - [ ] Define and write Ultimate Skill data for all 70+ units (add Ultimate Icon to Stats browser).
    - [ ] Define enemy skills/abilities.
- [ ] **Unit Browser Enhancements**
    - [ ] Add global info/checklist for Ultimates, Skins, and Passive counts.
    - [ ] Expand Unit Detail view with skill breakdown.
- [ ] **VFX Fallbacks**
    - [ ] Document and implement generic "Text/Sprite Fallback" for missing animations.
