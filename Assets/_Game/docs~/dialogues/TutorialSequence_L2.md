# Level 2: Tomb of Lilith - Tutorial Flow

**Setting**: Deep within the Tomb of Lilith.
**Characters**: Tina (Guide), Lilith (Void Matriarch), Maou (Player), Ignis (The Crimson Bastion).

## Technical Specifications: Resource Math & Balancing

In Level 2, the Sovereign's authority is initially fragmented. The following parameters define the resource economy and unit scaling to ensure an **Easy** difficulty experience:

### 1. Authority Capacity Logic
- **Stage 1 (Pre-Unsealing)**: Max Authority Seals = **50**.
- **Stage 2 (Post-Lilith)**: Max Authority Seals = **99** (Restoration of the First Seal).

### 2. Character Power Calculations (Optimized for Easy)
To ensure smooth progression, Vassal power is tuned to overwhelm Lesser Shadows:
- **Ignis (SSR Vanguard)**: 
  - **Stats**: HP: 2100 | ATK: 84 | DEF: 42
  - **Calculated Power**: **810**
  - **Easy Factor**: Can sustain 20+ hits from Lesser Shadows and kills in 2 hits.
- **Lilith (SSR Warlock)**:
  - **Stats**: HP: 840 | ATK: 126 | DEF: 14 | Range: 2
  - **Calculated Power**: **747**
  - **Easy Factor**: High Magical DPS allows 1-shotting most aerial units and 3-shotting the Boss phase.

### 3. Wave Composition (Total: 4 Waves)
| Wave | Enemy Type | Count | Behavior |
| :--- | :--- | :--- | :--- |
| **Wave 1** | Lesser Melee Shadows | 6 | Split into two groups of 3. |
| **Wave 2** | Shadow Wings (Flying) | 8 | Arrive in staggered pairs. |
| **Wave 3** | Mixed Shadows | 12 | 8 Lesser, 4 Shadow Constructs (Medium). |
| **Wave 4** | **Abyssal Shade (Boss)** | 1 + 6 | Boss arrives with Lesser Shadow escort. |

### 4. Map & Placements
- **Map Size**: 9x9 Grid
- **Spawn Gates**: (0, 4) [Ground] and (0, 5) [High Ground]
- **Exit Gates**: (8, 4) [Ground] and (8, 5) [High Ground]
- **Ignis Placement**: (2, 4) [2 squares away from Ground Spawn]
- **Lilith Placement**: (2, 5) [2 squares away from High Ground Spawn]

## Dialogue & Sequence Flow

### Step 0: The Silent Tomb
- **Action**: **UI Blocker Active**.
- **Action**: **CustomCommand**: `SetUnitButtonActive` (Target: "Lilith", Count: 0).
- **Action**: **CustomCommand**: `SetMaxAuthoritySeals` (Value: 50).
- **Dialogue**:
  - Tina: "The air is heavy with ancient magic... stay alert, My Sovereign."

### Step 1: Holding the Line
- **Action**: Game starts. Wave 1 begins immediately.
- **Enemies**: 6 Lesser Melee Shadows (Spawned in 2 groups of 3).
- **Dialogue**:
  - Tina: "My Sovereign, the unsealing process is delicate. We must buy time for the ritual to complete."
  - Tina: "Ignis, hold the corridor. Do not let those shades interrupt the flow of mana."
  - Ignis: "By the Crimson Flame... they shall not pass!"

### Step 2: The Unsealing
- **Trigger**: Wave 1 cleared.
- **Action**: **UI Blocker Active**.
- **Action**: A dark magenta light erupts from the central sarcophagus.
- **Action**: **CustomCommand**: `SetUnitButtonActive` (Target: "Lilith", Count: 1).
- **Action**: **CustomCommand**: `SetMaxAuthoritySeals` (Value: 99).
- **Audio**: A playful, smug laugh echoes through the chamber.
- **Dialogue**:
  - Lilith: "Mmm, such a sweet, delicious smell... is that you, Tina? Still playing the dutiful little maid?"
  - Tina: "Lilith. Your teasing is as poorly timed as ever. The Sovereign has returned."
  - Lilith: "Oh? The little Maou? My, how you've... changed. So adorably small now."

### Step 3: Aerial Interruption
- **Trigger**: Dialogue End.
- **Action**: Wave 2 starts.
- **Enemies**: 8 Shadow Wings (Flying, staggered pairs).
- **Dialogue**:
  - Lilith: "Look at those filthy 'Shadow Wings'—circling like vultures. They think we're easy prey."
  - Lilith: "Let me handle them, Sovereign. My magic can reach where Ignis's sword falters. Just... give me a good view."

### Step 4: Lilith Placement Tutorial
- **Trigger**: Shadow Wings reach midpoint.
- **Action**: **TIME STOPS.** Placement tutorial for Lilith.
- **Objective**: Place **Lilith** on the **High Ground** tile.
- **Dialogue**:
  - Tina: "Lilith is a Warlock. Place her on the High Ground to maximize her reach and Magical damage."
  - *Tutorial Hand points to Lilith icon and then to High Ground tile.*

### Step 5: The Battle Continues
- **Trigger**: Lilith Placed.
- **Action**: Time resumes. Wave 3 (8 Lesser Shadows, 4 Shadow Constructs).

### Step 6: The Abyssal Shade Boss
- **Trigger**: Wave 3 Cleared. Final Wave Starts.
- **Enemy**: **Abyssal Shade (Boss)** + 6 Lesser Shadows.
- **Mechanic**: The Boss has a phase where it becomes **Invulnerable to Melee** after losing 30% HP.
- **Dialogue (When Boss reaches 70% HP)**:
  - Ignis: "Tch! My blade... it's passing straight through him! He's turned into pure shadow!"
  - Tina: "He's entered an ethereal state. Melee strikes are useless now. Only Magic and Rites can harm him."

### Step 7: Regaining Authority (Rite Tutorial)
- **Trigger**: Boss invulnerable phase active.
- **Action**: **TIME STOPS.**
- **Action**: **CustomCommand**: `GrantMaxSeals` (Ensures enough mana for the Rite).
- **Dialogue**:
  - Lilith: "It seems my magic alone isn't enough to end this bore. Sovereign... it's time to help you regain your true power."
  - Lilith (Seductively): "Let me guide your mana. Feel the heat of it... focus it into a single, devastating point."
  - *Lilith's portrait appears, leaning close to the screen/Player.*
  - Lilith: "Now... use your Rite. Finish him."

### Step 7.5: Opening the Vault
- **Trigger**: Scripted Pause (Step 7 sequence continues).
- **Action**: **Wait for Action**: `RiteMenuOpened`.
- **UI Highlight**: `SovereignRiteToggle` (Highlight only if menu is hidden).
- **Dialogue**:
  - Tina: "Our Sovereign's Rites are sealed away for safety. Open the menu to prepare the ritual."
  - *Tutorial Hand points to the 'Show' button on the skill panel.*

### Step 8: The Fatal Rite
- **Trigger**: Rite Menu opened.
- **Objective**: Use **Abyssal Guillotine** (Female Maou) or **Event Horizon** (Male Maou) on the Boss.
- **Action**: Damage of the Rite is secretly boosted to ensure a one-shot.
- **Dialogue (Post-Execution)**:
  - Lilith: "Mmm... what a magnificent display. I might have to reconsider my opinion of you, Sovereign."
  - Tina: "The first seal of your authority is restored. Well done, My Sovereign."

### Step 9: Level Completion
- **Trigger**: Boss Defeated.
- **Action**: **UI Blocker Active**.
- **Dialogue**:
  - Lilith: "Don't look so stiff, Tina. We're all on the same side... for now."
  - Tina: "We proceed to the next chamber. There are more sisters to awaken."
  - Tina: "And then... we return to the surface. Your kingdom awaits, My Sovereign. It is time the world remembered who truly rules these lands."

