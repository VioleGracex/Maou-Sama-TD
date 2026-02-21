# UI Specification: Campaign & Mission Readiness
**Based on:** `MaouSamaTD.UI.MainMenu.CampaignPage` and `MaouSamaTD.UI.MissionReadinessPanel`

## 1. Campaign Page (Level Selection)
**Class:** `MaouSamaTD.UI.MainMenu.CampaignPage`
**Path:** `Assets/_Game/_Scripts/UI/MainMenu/CampaignPage.cs`

### 1.1. Structure
*   **Level Container:** A scrollable content area (`_levelContainer`) that holds the level buttons.
*   **Populator:** The script iterates through a `List<LevelData> _allLevels` and instantiates `_levelButtonPrefab` for each.
*   **Visual Logic:**
    *   **Locked/Unlocked:** Checks `SaveManager.IsLevelCompleted(prevLevelID)`. First level is always unlocked.
    *   **Stars:** Retrieves star count from `SaveManager.CurrentData.LevelStars`.
    *   **Interaction:** Clicking a level button triggers `OnLevelClicked(LevelData)`.

### 1.2. Interaction Flow
1.  **User Tap:** Player taps a Level Button.
2.  **Briefing:** The system calls `_briefingPanel.Setup(level, OnBriefingEngage)`.
3.  **Engage:** When the player confirms on the Briefing Panel, it calls `_missionReadinessUI.Open(level)`.

---

## 2. Mission Readiness Panel (The War Council)
**Class:** `MaouSamaTD.UI.MissionReadinessPanel`
**Path:** `Assets/_Game/Scripts/UI/MissionReadinessPanel.cs`

This is the critical pre-battle state where the player configures their "Cohort" (Squad).

### 2.1. Core Components
*   **Cohort Slots:** A dynamic list of `UnitCardSlot` objects instantiated into `_slotContainer`. Default limit: 12 slots.
*   **Cohort Selection:** A row of buttons (`_cohortButtons`) allowing the player to switch between saved teams (Cohort 1, Cohort 2, etc.), handled by `SwitchCohort(int index)`.
*   **Action Buttons:**
    *   **Barracks:** Opens `UnitSelectionPanel` in Multi-Select mode.
    *   **Remove All:** Clears the current cohort.
    *   **Start Battle:** Validates selection and loads the `BattleScene`.

### 2.2. The "Locked / Premade" Logic
The panel supports forced team compositions (e.g., for Story missions or Tutorials).
*   **Logic Source:** `LevelData.PremadeCohort`.
*   **Locked Mode (`_isLockedMode = true`):**
    *   Slots are filled with specific `UnitIDs` from the Level Data.
    *   Interaction is disabled (`OnSlotClicked` returns early).
    *   Player *must* use this specific team.
*   **Suggestion Mode (`_isLockedMode = false` but Premade exists):**
    *   The system pre-fills the player's current cohort with the suggested units.
    *   Player is free to modify them.

### 2.3. Unit Selection Flow
1.  **Slot Click:** Tapping a slot calls `OnSlotClicked(index)`.
2.  **Unit Selection:** Opens `UnitSelectionPanel` (external script).
3.  **Callback:** When a unit is chosen, `OnUnitSelected(slotIndex, unitID)` updates the `PlayerData` and refreshes the UI.

### 2.4. Battle Start Flow
1.  **Validation:** `OnStartBattle` checks if any units are selected.
2.  **State Injection:**
    *   Sets `GameSelectionState.Level` to `_currentLevel`.
    *   Sets `GameSelectionState.Cohort` to the list of `UnitData` resolved from IDs.
3.  **Scene Load:** Loads `BattleScene`.
