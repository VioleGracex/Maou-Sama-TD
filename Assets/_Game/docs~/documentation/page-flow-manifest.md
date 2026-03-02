# Page Flow & Navigation Manifest

This document maps the architectural flow of the Maou-Sama: Tower Defense application, detailing every view and its transitional logic.

---

## 1. Initial Entry & Onboarding Flow
*Used exclusively for new players or after a data wipe.*

1.  **Character Creation (`CharacterCreation.tsx`)**
    *   **Trigger**: `saveData.characterCreated === false`.
    *   **Destination**: On "Arise !", proceeds to Prologue.
2.  **Prologue (`Prologue.tsx`)**
    *   **Trigger**: `saveData.storyFlags.prologueSeen === false`.
    *   **Destination**: On completion, triggers `handleStageSelect('stage_1_1')` (Tutorial).
3.  **Tutorial Battle (`Battle.tsx`)**
    *   **Target**: Stage 1-1 logic.
    *   **Destination**: On "Return to Citadel", lands on the **Home Hub**.

---

## 2. Core Gameplay Loop (The Conquest Cycle)

1.  **Home Hub (`Home.tsx`)**
    *   The central switching station.
2.  **Stage Select (`StageSelect.tsx`)**
    *   Vertical scroll of campaign missions.
    *   **Flow**: Tapping a stage opens the "Tactical Analysis" sidebar. Tapping "Engage Cohort" opens the Cohort Drawer.
3.  **Story Interlude (`VisualNovel.tsx`)**
    *   **Pre-Story**: Triggers automatically if `stage.preStoryId` exists and is unseen.
    *   **Post-Story**: Triggers automatically after Victory if `stage.postStoryId` exists and is unseen.
4.  **Battle Canvas (`Battle.tsx`)**
    *   Primary interactive engine.
    *   **Flow**: On Victory/Defeat, redirects back to the source (Campaign, Tower, or Daily).

---

## 3. Meta-Game & Management (The Citadel Infrastructure)

*Accessible via the Home Hub or the Global Header.*

*   **Legion Archives (`Characters.tsx`)**: Manage, upgrade, and manifest Stellar Nodes for vassals.
*   **Soul Ritual (`Gacha.tsx`)**: Summon new units using Soul Gems.
*   **Royal Chambers (`Chambers.tsx`)**: 1-on-1 interaction.
    *   **Inner Flow**: Select Vassal -> Private Audience (Visual Novel) OR Bestow Gift.
*   **Citadel Repository (`Bag.tsx`)**:
    *   **Inventory Tab**: Use EXP potions/Stamina refills.
    *   **Altar of Dissolution Tab**: Scrap duplicate seals for Abyssal Dust.
*   **Treasury Vault (`Shop.tsx`)**: Purchase Soul Gems or exchange Abyssal Dust for SSR essences.

---

## 4. Endgame & Resource Routes

*   **Tower of Babel (`Tower.tsx`)**: Hardcore 100-floor progression. Persistent HP logic (planned).
*   **Resource Mandates (`DailyDungeons.tsx`)**: Rotating stages for Gold and EXP focus.
*   **Apex Rankings (`Leaderboard.tsx`)**: Global power tracking.

---

## 5. System & Identity Routes

*   **Sovereign Profile (`Profile.tsx`)**: Change name, swap avatar character, view legacy achievements.
*   **System Config (`Settings.tsx`)**: Adjust audio volumes, toggle performance mode, and change language.

---

## Navigation Summary Diagram

```text
[START] -> Character Creation -> Prologue -> Tutorial (1-1)
                                                 |
                                                 v
[GLOBAL HEADER] <------------------------> [ HOME HUB ] <----------------------> [DRAWER/EXP]
      |                                          |                                     |
      +--> Profile                               +--> Stage Select -> Battle           +--> Asset Forge
      +--> Settings                              +--> Gacha (Ritual)                   
      +--> Citadel (Home)                        +--> Characters (Archives)
      +--> Chambers (🍷)                          +--> Bag (Vault)
                                                 +--> Daily/Tower
```