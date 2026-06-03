# Sylvan-HUD Salavan Game Test Panel — Technical Documentation

The **Salavan Game Test Panel** (formerly Sylvan Game Tester) is a professional, high-performance automated testing client designed to execute Lua scenarios, perform computer vision matching, and query live UI states from the Unity game client.

---

## Key Features

### 1. Interactive Step Control (Skip & Include)
In the left sidebar's accordion scenario selector, each step parses its name and features an interactive checkbox indicator:
- `☑` **Included (Default)**: The step will run normally.
- `☐` **Skipped**: The runner will fast-forward through this step instantly by stubbing any click, drag, wait, or search actions inside it.
- **Visual indicators**: Skipped steps are grayed-out and italicized in the UI list to provide clean status mapping.

### 2. Time-Travel Debugger Navigation
Three control buttons are located under the main execution controls to manage scenario state:
- `⏮ PREV`: Terminate execution, rewind to the previous step index, and auto-pause.
- `🔁 REPEAT`: Terminate execution, restart the scenario, fast-forward to the current step, and auto-pause to repeat it.
- `⏭ NEXT`: Skip the remainder of the current step. If paused, it will run to the beginning of the next step and auto-pause.

### 3. Automated Unity UI Coordinates & Dimensions
To avoid manual template cropping, **Salavan** connects directly with the Unity editor or built client via `game_state.json`.
- **Automatic Sync**: When a scenario calls `wait_template("name")`, the client first scans `game_state.json` exported by `GameStateExporter.cs` at 150ms intervals.
- **Extended Metadata**: The Unity exporter dumps:
  - Coordinate position (`x`, `y`) scaled to reference `1280x720` resolution.
  - RectTransform dimensions (`w`, `h`) scaled to reference resolution.
  - Embedded button label string/text contents (extracts both `TextMeshPro` and legacy `UI.Text`).
- **CV Fallback**: If a button is not exported, Salavan falls back to computer vision matching (only if template PNG exists) before launching the wizard.

### 4. Locations & Coordinates Inspector Tab
The `📍 LOCATIONS` tab split-pane display:
- **Left Panel (Active Scenario Actions)**: Hierarchical Treeview parsing the Lua script. It maps steps to their actions (`wait_template`, `click`, `drag`, `wait`) and displays resolved coordinates and button metadata dynamically.
- **Right Panel (Live Unity UI Coordinates)**: Real-time grid table listing all currently active, visible, and interactable buttons exported by the running game client, displaying name, text content, coordinates, and size dimensions.

---

## Unity Exporter Configuration

Ensure that `GameStateExporter.cs` is attached to a persistent GameObject in your Unity scene. It automatically handles serialization and exports active button transforms, text values, and viewport dimensions to `salavan/game_state.json`.
