# Level 1: Ignis Ultimate Tutorial Flow

This document details the step-by-step sequence for the Ignis Ultimate tutorial, utilizing the new **WaitForCondition** and **Isometric Mask** features.

---

## 🟢 Step 1: Unit Deployment
*   **Goal**: Teach player to drag Ignis to the field.
*   **Target**: `UnitButton_Ignis` -> `TargetTile` (e.g., 5,5).
*   **System Action**: Hand animates from the button to the tile in a loop.
*   **Completion Condition**: `WaitForAction` -> `UnitPlaced`.

## 🟠 Step 2: Combat & Kill Requirement
*   **Goal**: Allow Ignis to gather experience/kills.
*   **System Action**: Game resumes at 1x speed. Tutorial "sleeps" in the background.
*   **Completion Condition**: `WaitForCondition`
    *   **ActionKey**: `UnitKills`
    *   **TargetUIName**: `Ignis`
    *   **RequiredCount**: `2`
*   **Result**: Tutorial triggers immediately once she defeats her 2nd monster.

## 🔴 Step 3: Enemy Density Check
*   **Goal**: Ensure the Ultimate hits a significant "Next Wave" of enemies.
*   **System Action**: Game remains at 1x speed. Tutorial waits for an enemy cluster.
*   **Completion Condition**: `WaitForCondition`
    *   **ActionKey**: `EnemiesInRange`
    *   **TargetUIName**: `Ignis`
    *   **RequiredCount**: `5`
    *   **HandDragTargetRadius**: `2.0` (Tiles)
*   **Result**: When 5 enemies are within 2 tiles of Ignis, the tutorial **Pauses the Game** (`TimeScale = 0`).

## 🔵 Step 4: Activating Stats
*   **Goal**: Open the unit inspector.
*   **Visual**: A squashed isometric oval highlights Ignis on the field.
*   **Hand Animation**: "Tap" animation on the Ignis world unit.
*   **Completion Condition**: `HighlightUI` -> `UnitStatsOpened`.
*   **Refinement**: Once the stats window opens, **ALL other UI is blocked** except for the Ultimate Button.

## 🔥 Step 5: The Phoenix Ultimate
*   **Goal**: Fire the ultimate.
*   **Target**: `UltButton` (The button inside the Stats Panel).
*   **System Action**: Blocker highlights ONLY the button; hand animates a tap.
*   **Completion Condition**: `HighlightUI` -> `UltimateUsed`.
*   **Special Sequence**:
    1.  Tapping the button triggers the **Ultimate Cut-In**.
    2.  Screen goes black (soft overlay).
    3.  Red Banner with "**ULTIMATE**" slides in (Game paused).
    4.  Animation plays for ~1.5 seconds.
    5.  Screen fades back to normal.
    6.  **Phoenix Projectile** is spawned on the field and time resumes.

---

## 🛠️ New System Features

| Feature | Description |
| :--- | :--- |
| **Isometric Projection** | Highlights (`UIPopupBlocker`) use a **0.6x vertical scale** to align with the isometric grid. |
| **Pause-Proof UI** | All tutorial animations use `.SetUpdate(true)` to run while the game is frozen. |
| **Mask Cleanup** | Masks now reset automatically when dialogue closes or the blocker is hidden. |
| **Phoenix Logic** | Automatic directional scanning picks the best vector (N/S/E/W) based on enemy density. |
