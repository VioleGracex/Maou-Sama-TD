# Page Flow & Navigation Manifest (2026 Edition)

This manifest maps the UI architecture for "Maou-Sama TD," optimized for Unity (PC/Mobile) and aligned with the 2026 Gacha/Tower Defense standards.

---

## 1. Global Navigation Architecture
The game uses a **Central Hub** model with a persistent **Global Header** for resources and a **Navigation Drawer** for quick switching.

### Primary Hub: The Citadel (Home)
The main entry point after login. Contains the following dashboard modules:
*   **CONQUEST (Large)**: The main campaign portal. Maps to `CampaignMap.scene`.
*   **COHORTS**: Team/Loadout management. `LoadoutEditor.ui`.
*   **VASSALS**: Unit collection, leveling, and "Soul Bond" progression. `VassalGallery.ui`.
*   **MANDATES**: Daily resource stages, rotating challenges. `DailyMissionHub.ui`.
*   **MANIFEST VASSALS (Yellow/Featured)**: The Gacha summoning altar. `SummonAltar.ui`.
*   **TREASURY**: Premium and resource exchange shop. `Shop.ui`.
*   **RANKS**: Global and friend leaderboards.
*   **DAILY**: Daily login rewards and seasonal check-ins.
*   **GRIMORE**: Lore, bestiary, and story archives.

---

## 2. Core Loop Flows

### Flow A: The Conquest Cycle
`Home` -> `Conquest` -> `Stage Select (List/Nodes)` -> `Tactical Prep` -> `Battle` -> `Results` -> `Story (If applicable)` -> `Conquest/Home`

### Flow B: Manifestation (Gacha)
`Home` -> `Manifest Vassals` -> `Banner Selection` -> `Summon Animation` -> `Reveal (Vassal Preview)` -> `Manifestation Result` -> `Gallery/Home`

### Flow C: Vassal Strengthening
`Home` -> `Vassals/Cohorts` -> `Vassal Detail` -> `Ascension/Level-up/Skill-up` -> `Vassal Detail`

---

## 3. Page Content & Priorities

| Priority | Page | Content / Key Features | Status |
| :--- | :--- | :--- | :--- |
| **0** | **Citadel (Home)** | Dynamic Background, Quick-start, Resource Bar. | [READY] |
| **0** | **Conquest** | Scrollable map, Difficulty toggle, 3-star tracker. | [READY] |
| **0** | **Manifest** | Gacha banners, Pity tracker. | [READY] |
| **0** | **Settings** | Game, Audio, Notification, Account settings. | [READY] |
| **1** | **Cohorts** | Team management, loadout editing (No Start button). | [DEVELOPING] |
| **1** | **Vassals** | Unit collection, barracks-style management. | [DEVELOPING] |
| **2** | **Treasury** | Premium shop, exchange tabs. | [PLANNED] |
| **3** | **Mandates** | Daily rotating resource stages. | [PLANNED] |
| **4** | **Grimore** | Story archives, Bestiary, Lore World Map. | [PLANNED] |
| **5** | **Throne/Vault** | End-game and long-term progression hubs. | [PLANNED] |

---

## 4. Conflict Report & Technical Notes
*   **Pathfinding Check**: No direct UI conflict with `pathfinding-algorithms.md`. However, ensure that `Battle.scene` UI scaling (Deploy buttons) doesn't obscure the 3D grid designed by the **Tactical Architect**.
*   **Unity Implementation**: Pages should be implemented as **Addressable-loaded Prefabs** or **UI Canvases** within a single scene to ensure smooth transitions (modern gacha feel) rather than heavy scene loading.
*   **PC/Mobile Hybrid**: UI must support "Click & Drag" (Vassal deployment) and "Hover" tooltips for PC, while maintaining large "Tappable" targets for Mobile.

---

## Navigation Summary Diagram (2026)

```text
[ LOGIN ] -> [ SPLASH ] -> [ CITADEL (HOME) ]
                                |
      +------------------------+------------------------+
      |            |           |           |            |
 [CONQUEST]    [VASSALS]   [MANIFEST]  [TREASURY]   [MANDATES]
      |            |           |           |            |
 [BATTLE]      [UPGRADE]   [SUMMON]    [EXCHANGE]   [RESOURCES]
      |            |           |           |            |
 [STORY]       [GALLLERY]  [PITY]      [INVOICE]    [COHORTS]
```