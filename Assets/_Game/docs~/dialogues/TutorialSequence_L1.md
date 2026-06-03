# Level 1: The Ritual Awakening - Refined Dialogue

**Setting**: A dimly lit ritual chamber corridor.
**Main Tutorial Character**: Tina (Shadow Guide)

---

## Technical Specifications: Prefabs & UI Click/Drag Map

For automated testing via **Salavan**, the following prefabs and UI elements are mapped for coordinates, clicks, and drag interactions:

### 1. Prefabs and UI Elements Used
| UI Context | Prefab Source | Target UI Component / GameObject Name | Action Required | Description |
| :--- | :--- | :--- | :--- | :--- |
| **Loading Screen** | `LoadingScreen_Root.prefab` | `StartButton` | Click | Launches the lobby from the initial loading screen. |
| **Lobby Main** | N/A (Scene Hierarchy) | `Conquest_Btn` | Click | Opens the Campaign conquest map. |
| **Briefing Panel** | `BriefingPanel.prefab` | `Briefing_Engage_Button` | Click | Opens the mission readiness setup. |
| **Mission Readiness** | `MissionReadinessPanel.prefab` | `MissionStart_Btn` | Click | Commences the battle. |
| **Lobby Navigation** | N/A (Scene Hierarchy) | `NavBtn_Home`, `NavBtn_Campaign` | Click | Navigation bar options. |
| **Unit Placement** | `UnitBar` / `UnitButton.prefab` | `UnitButton` (Text: "Ignis") | Drag | Drag Ignis onto the grid tile **(7, 6)**. |
| **Ultimate Tutorial** | N/A (HUD Panel) | `Ult_Btn` | Click | Triggers Ignis's ultimate when charge reaches 100%. |
| **Dialogue Skip** | `DialoguePanel.prefab` | `SkipButton` / `NextButton` | Click | Fast-forwards dialogue sequence. |
| **Victory Screen** | `VictoryScreen.prefab` | `ReturnButton_Victory` | Click | Returns the player to the Main Menu lobby. |

### 2. Resolution Scaling & Canvas Setup
- **Reference Resolution**: `1280x720` (top-left origin `0,0`).
- **Canvas Scaler Mode**: `Scale With Screen Size` (Reference Resolution: `1920x1080` with a Match width/height of `0.5`).
- **Coordinate Conversion Formula**:
  - $\text{click\_x} = X_{\text{win}} + \left(\frac{x_{\text{ref}}}{1280}\right) \times W_{\text{actual}}$
  - $\text{click\_y} = Y_{\text{win}} + \left(\frac{y_{\text{ref}}}{720}\right) \times H_{\text{actual}}$

---

## Dialogue & Interaction Sequence

### Step 1: Awakening
- **Trigger**: Scene Load
- **Action**: Pause Game.
- **Dialogue**:
  - Tina: "At last... the seal is being undone. My Sovereign, are you awake?"
  - Tina: "Forgive my intrusion, but your awakening has stirred the depths. Feral echoes, drawn to the trail of your power, have breached the outer corridors."
  - Tina: "Your strength is still recovering, but your command over the Obsidian Aegis remains. Command Ignis to hold the bottleneck against these remnants."

### Step 2: Placement Tutorial
- **Trigger**: Dialogue End
- **Action**: Pause Game (Interactive). Show Hand UI.
- **Target**: Drag Ignis to the highlighted tile.
- **Dialogue (Post-Placement)**:
  - Tina: "Exquisite. She stands as an unbreakable wall of shadow. I have woven her soul-link into your Authority Seals; she is now a direct extension of your divine will."

### Step 3: First Wave
- **Trigger**: Instruction End
- **Action**: Unpause. Spawn 2 Lesser Shadows.

### Step 4: Crisis & Ultimate Tutorial
- **Trigger**: Wave 1 Cleared
- **Action**: Pause Game.
- **Dialogue**:
  - Tina: "Wait! The air grows cold—a larger cluster of feral spirits manifests! They seek to reclaim the Sanctum through sheer, mindless numbers."
  - Tina: "Use your Authority! Command Ignis to unleash the [Obsidian Aegis] and incinerate these wretched souls."
- **Interaction**:
  - Logic: Charge Ignis to 100%.
  - Prompt: "Seize her Authority, then manifest the [Obsidian Aegis]."

### Step 5: Level End
- **Trigger**: Wave 2 Cleared
- **Dialogue**:
  - Tina (Kneeling): "The corridor is cleansed, My Sovereign. These shadows were but the trail of your awakening—mere echoes of the power you have reclaimed."
  - Ignis (Kneeling): "Your strength returns, My Lord. But the sanctuary is still compromised. These distortions will only grow more numerous as you fully awaken."
  - Tina: "She is right. To truly stabilize your presence and restore the Obsidian Throne, we must awaken the Matriarch of Lust... Lilith."
  - Ignis: "Lilith... A dangerous gamble, but a necessary one. Her siphoning abilities are the only way to fully replenish your Seals and reclaim your lost Crests."
- **Outcome**: Ritual Reclamation (Victory).
