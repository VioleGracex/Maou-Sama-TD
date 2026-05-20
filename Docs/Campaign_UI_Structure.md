# Campaign UI Architecture: Structural vs. Dynamic Elements

To maximize developer and artist productivity, the Campaign UI is divided strictly into **Pre-built Scene Components** (edited directly in the Inspector or Prefabs for visual aesthetics) and **Dynamically Spawned Elements** (configured programmatically based on level data and user progress).

This architecture prevents brittle runtime generation of UI layouts, allowing immediate previews in the Editor while retaining dynamic data-driven content.

---

## 🏛️ Pre-built Scene Components
These elements are constructed directly in the Unity Scene or as part of the page prefab. They **must not** be spawned dynamically at runtime. All text fonts, font sizes, glassmorphism overlays, border colors, scroll bars, and layouts can be edited comfortably in the Inspector.

| UI Element | Hierarchy Name | Purpose | Editable Settings |
| :--- | :--- | :--- | :--- |
| **Left Sidebar Container** | `LeftSidebar` | Main container for the scrolling levels list. | Background Image, glassmorphism alpha, right glow border (`RightBorder`), size, anchor. |
| **Sidebar Title** | `LeftSidebar/SidebarTitle` | Text heading displaying the campaign theme. | Typography, font size, bold style, gold color. |
| **Sidebar Scroll View** | `LeftSidebar/ScrollView` | Provides scrollable viewport bounding. | Rect limits, scroll speed, scroll bars. |
| **Sidebar Content Container** | `LeftSidebar/ScrollView/Viewport/Content` | Holds the spawned level items. | Layout padding, spacing between items (`VerticalLayoutGroup`). |
| **Zoom Buttons Container** | `ZoomContainer` | Holds the zoom interaction controls. | Positioning (anchored bottom-right), spacing. |
| **Zoom In Button** | `ZoomContainer/ZoomInButton` | Button to increase camera zoom scale. | Button component, Hover/Pressed tint, text "+" style. |
| **Zoom Out Button** | `ZoomContainer/ZoomOutButton` | Button to decrease camera zoom scale. | Button component, Hover/Pressed tint, text "-" style. |

> [!TIP]
> If any of these objects are missing at runtime, a warning will be logged to the Unity Console. They are bound to `CampaignPage.cs` via serialized inspector references:
> - `_sidebarRoot` (GameObject)
> - `_sidebarContentContainer` (Transform)
> - `_zoomInButton` (Button)
> - `_zoomOutButton` (Button)

---

## ⚡ Dynamically Spawned Elements
These elements depend on variable runtime data (e.g., player progression, loaded level databases, addressable assets) and are instantiated dynamically at runtime.

### 1. Sidebar Level Items (`SidebarLevelItem` Prefab)
Spawned inside the **Sidebar Content Container** for each level in the selected category tab (Main Story, Resource Dungeon, Rite Dungeon).
* **Why it is dynamic:** The list scale, selection highlights, unlocked/locked states, and completion checkboxes must reflect the player's live progression data.
* **Aesthetics Customization:** Instantiated from the `_sidebarItemPrefab` reference. You can edit the item prefab directly in the Inspector to control:
  - Text typography and colors for unlocked vs. locked items.
  - Hover/Selected color states on the Button component.
  - Custom status indicators (completed checks ✔, locked icons 🔒, placed pins 📍).

### 2. Campaign Map Node Buttons (`LevelButton` Prefab)
Spawned at their specific coordinates (`CampaignMapPosition`) on the campaign map scrollable canvas.
* **Why it is dynamic:** Only the levels that belong to the active category are rendered as nodes on the map. They dynamically represent unlock states, earned star counts (0 to 3), and level details.
* **Aesthetics Customization:** Instantiated from `LevelButton` prefabs. Allows standardizing star sprites, lock overlays, and button animations inside the prefab.

---

## 🎨 Workflow Guide for Designers & Developers
1. **To Adjust Sidebar Background or Borders:** Select `LeftSidebar` in the scene hierarchy and adjust the `Image` component color/alpha or the `RightBorder` width and tint.
2. **To Style Sidebar Text:** Modify the TextMeshPro properties of `SidebarTitle` in the scene, or edit the `SidebarLevelItem` Prefab's name/status fields.
3. **To Customize Zoom Buttons:** Go to `ZoomContainer` in the scene and modify the button sprites, colors, or hover transitions.
