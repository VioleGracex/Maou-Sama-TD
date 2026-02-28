# Level 1: The Ritual Awakening

**Setting**: A dimly lit ritual chamber corridor.
**Main Tutorial Character**: Hecatina

## Dialogue & Interaction Sequence

### Step 1: Awakening
- **Trigger**: Scene Load
- **Action**: Pause Game.
- **Dialogue**:
  - Hecatina: "My Lord! You've awakened... The ritual was... intense. But there is no time for rest."
  - Hecatina: "Lesser shadows have breached the outer corridor. They sense your weakened state."
  - Hecatina: "You are still recovering, but you can still guide your vassals. Send Ignis forth to hold the line."

### Step 2: Placement Tutorial
- **Trigger**: Dialogue End
- **Action**: Pause Game (Interactive). Show Hand UI.
- **Instruction**: "Drag Ignis to the highlighted tile in the corridor."
- **Constraints**: Only 1 tile allowed.
- **Dialogue (Post-Placement)**:
  - Hecatina: "Excellent. She will hold them. I've granted her your blessing using the Authority Seals."

### Step 3: First Wave
- **Trigger**: Instruction End
- **Action**: Unpause. Spawn 2 Lesser Shadows.
- **Wave**:
  - 2x Lesser Shadow (Spawn interval 1s).

### Step 4: Crisis & Ultimate Tutorial
- **Trigger**: Wave 1 Cleared
- **Action**: Pause Game.
- **Dialogue**:
  - Hecatina: "Wait! A larger cluster is manifesting! They are overwhelming the corridor."
  - Hecatina: "Lord, use your authority! Select Ignis and activate her [Obsidian Aegis] to incinerate these vermin."
- **Interaction**:
  - Force Ignis Charge to 100%.
  - Prompt: "Click Ignis, then click her Skill Button."
- **Action (On Use)**: Spawn wave of 10 Lesser Shadows instantly. Skill kills them.

### Step 5: Level End
- **Trigger**: Wave 2 Cleared
- **Dialogue**:
  - Hecatina: "The corridor is secure for now. But this was just a scouting party. We must reach the inner sanctum."
- **Outcome**: Victory Screen.
