# Maou-Sama-TD: Battle Scene UI Hierarchy Map

This document maps out the entire layout and hierarchy of the battle interface canvas (`MainCanvas`) in `BattleScene.unity`. It lists every visual system, its children, attached components, and its default active states.

---

## 🗺️ Visual Map of Battle UI

```
┌────────────────────────────────────────────────────────────────────────┐
│ [DialogueUI: MiniTopPanel] (Top-Center Mini Dialogues)                 │
│                                                                        │
│  [BaseHP]                 [WaveNumberText]              [PauseButton]  │
│  (Objective HP bar)       (e.g., "Wave 1 / 3")          [SpeedButton]  │
│                                                                        │
│                                                                        │
│                                                                        │
│                                                                        │
│                                                                        │
│                                                                        │
│  [SOVEREIGN_RITES_SKILLS_UI]                    [CameraButtonsHolder]  │
│  (Sovereign Rites Menu)                         (Lock & 2D/3D Toggle)  │
│                                                                        │
│  [UnitHolderPanel]        [Unit_Inspector_UI]   [WaveEnemyCountText]   │
│  (Vassal Cards bar)       (Vassal Details pane) (Remaining: 23)        │
└────────────────────────────────────────────────────────────────────────┘
```

---

## 🗂️ Complete UI Hierarchy Details

### 1. Active HUD Elements (Always On Screen)

* **`BaseHP`** *(Active: True)*
  * **Description**: Centralized top-center objective health indicator (Wagon/Tina/Sovereign).
  * **Components**: `RectTransform`, `CanvasRenderer`, `Image`
  * **Children**:
    * `Hp_text` *(TextMeshProUGUI)*: Raw numbers display.
    * `BasePercetange` *(TextMeshProUGUI)*: Percentage calculation text.
    * `Stats_HPBar_Back` *(Image)*: Outer bar container.
      * `Stats_HPBar` *(Image)*: Inner color fill bar.

* **`Authority`** *(Active: True)*
  * **Description**: Secondary currency display tracking active **Authority Seals** quantity.
  * **Children**:
    * `SealsLabelText` *(TextMeshProUGUI)*
    * `SealsAmountTxt` *(TextMeshProUGUI)*

* **`WaveNumberText`** *(Active: True)*
  * **Description**: Display of active level wave progression (e.g., `Wave 1 / 3`).
  * **Components**: `RectTransform`, `CanvasRenderer`, `TextMeshProUGUI`

* **`WaveEnemyCountText`** *(Active: True)*
  * **Description**: Displays the remaining monster counts in the active wave.
  * **Components**: `RectTransform`, `CanvasRenderer`, `TextMeshProUGUI`

* **`UnitHolderPanel`** *(Active: True)*
  * **Description**: The lower-left deck/deployment bar containing placeable Vassal cards.
  * **Components**: `RectTransform`, `VerticalLayoutGroup`
  * **Children**:
    * `UnitBarParent` *(RectTransform)*
      * `UnitBar` *(HorizontalLayoutGroup)*: Contains instances of vassal deploy cards.
        * `UnitButton` *(Active: False template)*: Instantiated cards with `UnitButtonUI`, Class Icons, deployment Seal Costs, and CoolDown Overlays.

* **`CameraButtonsHolder`** *(Active: True)*
  * **Description**: Small isometric camera controller anchors.
  * **Children**:
    * `LockButton` *(Button)*: Standard lock-to-center button.
    * `ViewGroup` / `ViewButton` *(Button)*: Toggle between **2D Orthographic / 3D Isometric Viewports**.

* **`PauseButton`** & **`SpeedButton`** *(Active: True)*
  * **Description**: Speed-multiplier and game pausing controllers.
  * **Components**: Buttons & `TextMeshProUGUI` indicators showing time manipulation cycles.

---

### 2. Contextual UI Elements (Fades/Toggles on Demand)

* **`Unit_Inspector_UI`** *(Active: False)*
  * **Description**: The full detailed stats inspector panel that slides/pops open when a deployed vassal is selected.
  * **Children**:
    * `TopArea`: Shows Unit name, health bars (`Stats_HP_Number_Txt`), and action items like `Stats_Close_Btn` and `Stats_Retreat_Btn`.
    * `MiddleArea`: Shows attack damage (`Dmg_Txt`), range description, and a visual **25-tile Range pattern grid** (`RangePatternUI`).
    * `UltMeter`: Shows charging progression (`Stats_UltBar`) and the **Ultimate Activation Button** (`Ult_Btn`).

* **`SOVEREIGN_RITES_SKILLS_UI`** *(Active: False)*
  * **Description**: The slide-out menu that lists active Sovereign Rites and combat skills.
  * **Children**:
    * `ButtonContainer` *(VerticalLayoutGroup)*: Holds lists of active castable skills.
      * `SkillButton` *(Active: False template)*: Custom `SkillButtonUI` containing cost markers and dynamic cooldown timers.
    * `SovereignRiteToggle` *(Button)*: The pull-out handle button to open/close this panel.

* **`DialogueUI`** *(Active: True, sub-panels inactive)*
  * **Description**: Handles both story cutscenes and mid-level dialogue banners.
  * **Children**:
    * **`FullScreenPanel`** *(Active: False)*: Full cinematic story interface. Houses portraits (`LeftPortrait`, `MiddlePortrait`, `RightPortrait`), story background frames, and the main `DialougeBox` with click-to-next and skip buttons.
    * **`MiniTopPanel`** *(Active: False)*: Floating mid-level chat balloon. Contains a mini-portrait (`_miniTopPortrait`), small speech bubble, and skip icons.

* **`UltimateCutInUI`** *(Active: False)*
  * **Description**: A full-screen overlay banner that is animated whenever a unit triggers its Ultimate skill, darkening the background and showing a localized cut-in.

---

### 3. State & Menu Overlays (Full Screen Blocks)

* **`Pause_Overlay`** & **`PausePanel`** *(Active: False)*: Semi-translucent overlay and menu containing `Resume`, `Restart`, and `Retreat` buttons.
* **`ConfirmationPanel`** *(Active: False)*: Safe check before starting actions (Yes/No prompts).
* **`VictoryPanel`** *(Active: False)*: Shows star conditions, level titles, stars achieved (`StarConditionItem`), and next-level navigators.
* **`LosePanel`** *(Active: False)*: Re-try or main menu options upon objective defeat.
* **`DebugButtons`** *(Active: False)*: Fast-spawn triggers and Seals cheats for in-editor testing.
