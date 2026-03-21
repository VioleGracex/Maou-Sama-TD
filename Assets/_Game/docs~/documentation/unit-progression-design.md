# Unit Progression & Inspector Design (MAOU TD)

This document outlines the implementation of the Arknights-style Unit Inspector and the underlying progression systems.

## 1. Unit Inspector UI Layout
Inspired by Arknights, the UI is a full-screen window with a high-end dark aesthetic.

### Visual Architecture
- **Left Region**: Large Character Preview (Full Body Art).
- **Right Region**: Functional Panels (Scrollable or Tabbed).
- **Top Bar**: Global Navigation (Back to Home, Resource Displays).
- **Floating Buttons**: 
    - **Skins**: Toggle skin selection overlay.
    - **Level Up (+)**: Directly open level-up modal.

### Interaction Tabs
1.  **Attributes**: Display base stats (HP, ATK, DEF, RES) and derived properties (Block, Cost, Range).
2.  **Promote (Rank Up)**: UI for increasing the unit's Star/Rank level (e.g., Level 50 Rank 0 -> Level 1 Rank 1).
3.  **Potential (Resonance)**: Vertical scroll for unlocking nodes using duplicate tokens.
4.  **Skills**: Select/View active skills and upgrade their Masteries.

## 2. Progression Systems

### XP & Leveling
- **Cost**: Gold only (no premium currency).
- **Logic**: Consuming "Memory Shards" (mats) in the Inspector.

### Rank Up (Star Advancement)
- **Concept**: Advancing a unit increases its **Star** count (Rarity).
- **Requirements**: Reach the **Max Level** for the current star count + consume materials.
- **Action**: 
    - Increase Star count (up to 6).
    - **Reset Level to 1**.
    - Apply a permanent **Base Stat Multiplier** (making the unit fundamentally stronger at Lv.1 than it was before).
- **Level Caps by Stars**:
    - **1 Star**: Max Lv. 20
    - **2 Stars**: Max Lv. 30
    - **3 Stars**: Max Lv. 45
    - **4 Stars**: Max Lv. 60
    - **5 Stars**: Max Lv. 80
    - **6 Stars**: Max Lv. 90 (Mastery)

### XP Acquisition
- **Leveling**: Units gain XP by consuming "Memory Shards" (mats) in the Inspector.
- **Battle XP**: Units deployed during a mission receive a portion of the total Mission XP on completion.
    - *Calculation*: `UnitXP = MissionXP / DeployedUnitsCount`.

### Skins System
- **Unlock**: Premium skins are purchased using **Bloodcrests**.
- **Management**: Equipped via the Skins tab in the Inspector.
- **Visuals**: Swaps the UI portrait and the in-game unit's sprite/animator.

## 3. Implementation Tasks
- [ ] Create `SkinData` ScriptableObject system.
- [ ] Extend `UnitData` with `MaxLevel`, `CurrentRank`, and `Experience`.
- [ ] Implement `UnitInspectorFullScreenUI` controller.
- [ ] Create UI Prefabs using Unity UI Toolkit or UGUI (as per project standard).
- [ ] Populate `UnitData` assets for core characters (Ignis, Lilith, Maou) from `docs~`.
