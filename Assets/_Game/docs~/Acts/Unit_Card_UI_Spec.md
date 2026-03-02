# UI Specification: Unit UI Components
This document clarifies the three distinct components used to display Units in the UI.

## 1. The Architecture
The distinctions exist to separate **Lists**, **Slots**, and **Visuals**.

| Component | Role | Where it is used |
| :--- | :--- | :--- |
| **`UnitCardUI`** | **The Inventory Item.** deeply interactive, shows full stats/level. | **Unit Selection Panel** (The "Barracks" list). |
| **`UnitCardSlot`** | **The Container.** Holds a position in a team. Handles logic like "Click to Select". | **Mission Readiness**, **Cohort Selection** (The "Squad" bar). |
| **`UnitCardView`** | **The Renderer.** A lightweight visual wrapper. | **Inside `UnitCardSlot`**. The slot spawns this to show the unit's face. |

---

## 2. Component Details

### A. UnitCardUI (The "Card")
**Script:** `maouSamaTD.UI.MainMenu.UnitCardUI`
*   **Purpose:** Represents a unit in a large collection.
*   **Features:**
    *   Full details: Name, Level, Rarity background.
    *   Selection State: Handles "Multi-select" numbering (1, 2, 3) and Checkmarks.
    *   **Heavier:** designed for the main scrolling list.

### B. UnitCardSlot (The "Slot")
**Script:** `maouSamaTD.UI.UnitCardSlot`
*   **Purpose:** A fixed placeholder for a squad member (Index 0-11).
*   **Logic:**
    *   It is **NOT** the unit itself; it is the *chair* the unit sits in.
    *   It has an `Click` event that tells the system "I was clicked, change the unit in this chair".
    *   When a unit is assigned, it instantiates a **Visual** (Prefab) inside itself.

### C. UnitCardView (The "View")
**Script:** `maouSamaTD.UI.UnitCardView`
*   **Purpose:** Purely visual. It just takes `UnitData` and sets the Sprite/Icon.
*   **Usage:**
    *   `UnitCardSlot` uses this script on its spawned prefab to show the unit.
    *   It is "Lightweight" – no button logic, no selection state, just the image.

## 3. Why two codes?
*   **`UnitCardUI`** is for **Picking** (Menu). It needs selection counters, overlays, and detailed text.
*   **`UnitCardSlot` + `UnitCardView`** is for **Placing** (Squad). It needs to be simple, standardized, and just show *who* is in the slot.

## 4. Flow
1.  Player clicks a **`UnitCardSlot`** (in the Squad Bar).
2.  Game opens the **`UnitSelectionPanel`**.
3.  Panel shows 50 **`UnitCardUI`** items.
4.  Player clicks one **`UnitCardUI`**.
5.  System updates the data, and the **`UnitCardSlot`** updates its internal **`UnitCardView`** to match the new unit.
