# Maou-Sama-TD Automation & Test Flows Guide

This document maps out the precise sequence of operations, visual templates, and UI interactions required for automated testing of **Maou-Sama-TD**. All coordinates are relative to a standardized **1280x720 windowed client area** (ignoring OS margins).

---

## 1. Scenario 1: Fresh Start & Level 1 Tutorial
This scenario validates the clean profile creation and the core mechanics of the initial tutorial level.

### Step-by-Step Flow:
1. **Purge Save Data**: Locate and delete the local save file (`player_save.json`) at AppData or Documents.
2. **Launch & Position**: Boot the executable with `-screen-width 1280 -screen-height 720 -screen-fullscreen 0`. Relocate the window to `(0, 0)` and force focus.
3. **Title Screen / Ascension**:
   - Wait for the **Ascension Panel** to load.
   - Detect and click the **Dice Button** (`dice_button.png` at offset approx `(820, 460)`) to generate a character name.
   - Click the **Arise Button** (`arise_button.png` at offset approx `(640, 580)`) to enter the game.
4. **Tutorial Choice Dialog**:
   - Wait for the prompt: *"Play Tutorial"* or *"Skip Tutorial"*.
   - Find and click the **Play Tutorial Button** (`play_tutorial_btn.png` at offset approx `(480, 450)`).
5. **Introduction Dialogue**:
   - Advance dialogue bubbles by clicking the bottom-right text box area (`(1100, 650)`) 3 times with a `2.0s` delay between clicks.
6. **Unit Deployment**:
   - Find the **Ignis Unit Card** (`ignis_card.png`) in the hand area.
   - Drag the card from its card slot to the center field tile `[7, 4]` (coordinate `(740, 320)`).
   - Click bottom-right `(1100, 650)` once to advance the post-placement dialogue.
7. **Wave Combat & Ultimate Charging**:
   - Wait `18.0s` for the wave enemies to emerge and Ignis to gain energy/kills. The game will auto-pause when the ultimate skill is fully charged.
   - Click `(1100, 650)` to clear the ultimate instructions dialogue.
8. **Ultimate Activation**:
   - Select **Ignis** on the grid by clicking his position `(740, 320)`.
   - Click the **Ignis Ultimate Skill Button** (`ignis_ult_btn.png` at bottom-right HUD area or fallback to coordinate `(1150, 580)`).
9. **Victory Screen**:
   - Wait for the victory banner to display (up to 45 seconds).
   - Find and click the **Next Level Button** (`victory_next_level.png` at approx `(640, 600)`).

---

## 2. Scenario 2: Level 2 Progression
This scenario validates transitions through the Conquest menu and progression to Level 2.

### Step-by-Step Flow:
1. **Lobby & Navigation**:
   - Wait for the Main Lobby UI to load.
   - If not already there, navigate to **Conquest Mode** by clicking the **Conquest Navigation Button** (`conquest_nav_btn.png`).
2. **Node Selection**:
   - Find and click the **Level 2 Node** on the map (`node_level_2.png`).
   - Click the **Engage / Briefing Button** (`engage_btn.png`) to open the mission briefing panel.
3. **Mission Briefing**:
   - Check if a dialogue prompt or skip confirmation is present.
   - Click the **Start Mission Button** (`start_mission_btn.png`) to load the level.
4. **Combat Loop**:
   - Unlike the tutorial, units must be deployed dynamically.
   - Drag **Ignis** and any other available units from the card slots to defensive grid positions.
   - Wait for waves to clear.
5. **Victory Transition**:
   - Upon victory, click the **Next Level Button** or **Return to Lobby** button.

---

## 3. Scenario 3: Level 3+ Cohort & Rite Setup
This scenario tests cohort unit assignment, rite selections, and placement mechanics required for Level 3 and beyond.

### Step-by-Step Flow:
1. **Lobby & Map Navigation**:
   - Navigate from Lobby to the Conquest Screen.
   - Select the **Level 3 Node** (or higher node) and click **Engage**.
2. **Cohort Assignment UI**:
   - When the Cohort panel opens, verify empty slots are present.
   - Click a **Unit Slot** (`empty_unit_slot.png`) and select a unit card (e.g. Ignis or other unlocked characters).
   - Click a **Rite Slot** (`empty_rite_slot.png`) and assign a passive Rite card.
   - Click **Save / Proceed** to load the battlefield interface.
3. **Grid Placement Stage**:
   - Before waves start, the game enters a preparation phase.
   - Drag assigned units from the deployment deck onto the glowing valid tiles on the grid.
   - Click the **Start Battle / Engage Button** (`start_battle_btn.png`) to start the waves.
4. **Combat & Ultimate Automation**:
   - Monitor health and ultimate indicators.
   - Activate ultimates when they are fully charged by clicking the respective character portraits or coordinates on the grid and HUD.
5. **Victory / Defeat Report**:
   - Record results and click **Return to Conquest**.
