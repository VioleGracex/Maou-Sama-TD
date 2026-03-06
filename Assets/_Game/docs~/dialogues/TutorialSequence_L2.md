# Level 2: Tomb of Lilith - Tutorial Flow

**Setting**: Deep within the Tomb of Lilith.
**Characters**: Hecatina (Guide), Lilith (Void Matriarch), Maou (Player), Ignis (The Crimson Bastion).

## Dialogue & Sequence Flow

### Step 0: The Silent Tomb
- **Action**: **CustomCommand**: `SetUnitButtonActive` (Target: "Lilith", Count: 0).
- **Dialogue**:
  - Hecatina: "The air is heavy with ancient magic... stay alert, My Sovereign."

### Step 1: Holding the Line
- **Action**: Game starts. Wave 1 begins immediately.
- **Enemies**: 4 Lesser Melee Shadows.
- **Dialogue**:
  - Hecatina: "My Sovereign, the unsealing process is delicate. We must buy time for the ritual to complete."
  - Hecatina: "Ignis, hold the southern corridor. Do not let those shades interrupt the flow of mana."
  - Ignis: "By the Crimson Flame... they shall not pass!"

### Step 2: The Unsealing
- **Trigger**: Wave 1 cleared.
- **Action**: A dark magenta light erupts from the central sarcophagus.
- **Action**: **CustomCommand**: `SetUnitButtonActive` (Target: "Lilith", Count: 1).
- **Audio**: A playful, smug laugh echoes through the chamber.
- **Dialogue**:
  - Lilith: "Mmm, such a sweet, delicious smell... is that you, Hecatina? Still playing the dutiful little maid?"
  - Hecatina: "Lilith. Your teasing is as poorly timed as ever. The Sovereign has returned."
  - Lilith: "Oh? The little Maou? My, how you've... changed. So adorably small now."

### Step 3: Aerial Interruption
- **Trigger**: Dialogue End.
- **Action**: Wave 2 starts.
- **Enemies**: Shadow Wings (Flying).
- **Dialogue**:
  - Lilith: "Look at those filthy 'Shadow Wings'—circling like vultures. They think we're easy prey."
  - Lilith: "Let me handle them, Sovereign. My magic can reach where Ignis's sword falters. Just... give me a good view."

### Step 4: Lilith Placement Tutorial
- **Trigger**: Shadow Wings reach midpoint.
- **Action**: **TIME STOPS.** Placement tutorial for Lilith.
- **Objective**: Place **Lilith** on the **High Ground** tile.
- **Dialogue**:
  - Hecatina: "Lilith is a Warlock. Place her on the High Ground to maximize her reach and Magical damage."
  - *Tutorial Hand points to Lilith icon and then to High Ground tile.*

### Step 5: The Battle Continues
- **Trigger**: Lilith Placed.
- **Action**: Time resumes. Wave 2 & 3 (Mixed Melee and Ranged Shadows).

### Step 6: The Abyssal Shade Boss
- **Trigger**: Final Wave Starts.
- **Enemy**: **Abyssal Shade (Boss)**.
- **Mechanic**: The Boss has a phase where it becomes **Invulnerable to Melee** after losing 30% HP.
- **Dialogue (When Boss reaches 70% HP)**:
  - Ignis: "Tch! My blade... it's passing straight through him! He's turned into pure shadow!"
  - Hecatina: "He's entered an ethereal state. Melee strikes are useless now. Only Magic and Rites can harm him."

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
  - Hecatina: "Our Sovereign's Rites are sealed away for safety. Open the menu to prepare the ritual."
  - *Tutorial Hand points to the 'Show' button on the skill panel.*

### Step 8: The Fatal Rite
- **Trigger**: Rite Menu opened.
- **Objective**: Use **Abyssal Guillotine** (Female Maou) or **Event Horizon** (Male Maou) on the Boss.
- **Action**: Damage of the Rite is secretly boosted to ensure a one-shot.
- **Dialogue (Post-Execution)**:
  - Lilith: "Mmm... what a magnificent display. I might have to reconsider my opinion of you, Sovereign."
  - Hecatina: "The first seal of your authority is restored. Well done, My Sovereign."

### Step 9: Level Completion
- **Trigger**: Boss Defeated.
- **Dialogue**:
  - Lilith: "Don't look so stiff, Hecatina. We're all on the same side... for now."
  - Hecatina: "We proceed to the next chamber. There are more sisters to awaken."
  - Hecatina: "And then... we return to the surface. Your kingdom awaits, My Sovereign. It is time the world remembered who truly rules these lands."

