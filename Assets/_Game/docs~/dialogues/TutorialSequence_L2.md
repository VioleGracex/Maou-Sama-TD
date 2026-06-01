# Level 2: Tomb of Lilith - Tutorial Flow

**Setting**: Deep within the Tomb of Lilith.
**Characters**: Tina (Guide), Lilith (Void Matriarch), Maou (Player), Ignis (The Crimson Bastion).

## Technical Specifications: Resource Math & Balancing

In Level 2, the Sovereign's authority is initially fragmented. The following parameters define the resource economy and unit scaling to ensure an **Easy** difficulty experience:

### 1. Authority Capacity Logic
- **Stage 1 (Pre-Unsealing)**: Max Authority Seals = **50**.
- **Stage 2 (Post-Lilith)**: Max Authority Seals = **99** (Restoration of the First Seal).
- **Boss Phase**: Seals refilled to **99** via `SetMaxAuthoritySeals` CustomCommand before the One-Shot rite step.

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
| Wave | Index | Enemy Type | Count | Behavior |
| :--- | :--- | :--- | :--- | :--- |
| **Wave 1** | 0 | Lesser Melee Shadows | 6 | Split into two groups of 3. |
| **Wave 2** | 1 | Shadow Wings (Flying) | 8 | Arrive in staggered pairs. Aerial path. |
| **Wave 3** | 2 | Mixed Shadows | 12 | 8 Lesser, 4 Shadow Constructs (Medium). |
| **Wave 4 (Boss)** | 3 | **Abyssal Shade (Boss)** | 1 + 6 | Boss arrives with Lesser Shadow escort. |

### 4. Map & Placements
- **Map Size**: 15x15 Solid Grid
- **Spawn Gates**: (0, 4) [Ground Spawn] and (0, 11) [Top/High Ground Spawn]
- **Exit Gates**: (7, 7) [Close Ground Exit], (14, 10) [Far Ground Exit], and (14, 11) [Top/High Exit]
- **Ignis Placement**: (7, 6) [Ground lane, blocks ground path]
- **Lilith Sealed Spot Image Decor**: (7, 8) [Altar where Lilith is sealed]
- **Lilith Placement**: (7, 8) [High Ground tile directly above close exit (7, 10)]

### 5. Boss Mechanics
- **IsBoss**: true — escaping the exit triggers immediate **Game Over**.
- **HP Gate**: Boss has `PreventDeathForTutorial = true` until HP drops to **≤ 70%**, preventing early kill.
- **Kill Step**: `PreventDeathForTutorial` is cleared when `Execute One-Shot Rite` step begins (RequiredCount = 0). Tutorial waits for `boss.IsDead = true`.
- **Time Freezing**: ALL boss phase steps have `ResumeTime: 0` — time stays paused from `BossPassedUnit` trigger until `Final Victory Dialogue` completes.

---

## Dialogue & Sequence Flow

### Step 0: The Silent Tomb
- **Type**: CustomCommand (x2) → DialogueOnly
- **Action**: **UI Blocker Active**.
- **Action**: **CustomCommand**: `SetUnitButtonActive` (Target: "Lilith", Count: 0) — hides Lilith from the unit bar.
- **Action**: **CustomCommand**: `SetMaxAuthoritySeals` (Value: 50) — Stage 1 seal cap.
- **Dialogue**:
  - Tina: "We have reached the lower sanctum where Lilith is sealed. I will begin the unsealing rite now. Sovereign, please protect me while I perform the ritual."
  - Ignis: "Sovereign, shadows are approaching from the tomb's entrance! I will block the ground path to keep them from interrupting the ritual."

### Step 1: Holding the Line
- **Type**: StartWave (WaveIndex: 0) → runs real-time
- **Action**: Wave 1 starts (6 Lesser Melee Shadows).
- **Time**: Resumes immediately. Combat flows freely.
- **Dialogue**:
  - Tina: "My Sovereign, the unsealing process is delicate. We must buy time for the ritual to complete."
  - Tina: "Ignis, hold the corridor. Do not let those shades interrupt the flow of mana."
  - Ignis: "By the Crimson Flame... they shall not pass!"

### Step 2: The Unsealing
- **Type**: WaitForWave (WaveIndex: 0, all enemies defeated) → CustomCommands → DialogueOnly
- **Trigger**: Wave 1 fully cleared.
- **Action**: **UI Blocker Active**. Time stops.
- **Action**: **CustomCommand**: `SetUnitButtonActive` (Target: "Lilith", Count: 1) — unlocks Lilith.
- **Action**: **CustomCommand**: `SetMaxAuthoritySeals` (Value: 99) — Stage 2 seal cap restored.
- **Action**: **CustomCommand**: `SetSpawnMapping` (Spawn: (0, 4), Exit: 1) — shifts ground spawn path to far exit (14, 10).
- **Dialogue**:
  - Lilith: "Mmm, such a powerful, cold grip. Is that you, my delicious Sovereign? You've certainly taken your time."
  - Tina: "Control your tongue, creature. But wait... look at the central seal!"
  - Lilith: "Oh? The central seal is breaking! Ah, it seems unsealing us has also unsealed that nasty ancient thing... the Abyssal Shade is awake!"
  - Tina: "Oh no! The unsealing has released the abyssal shadow! The exit to the tomb has shifted far deeper into the depths! The shadows are routing to the far exit!"
  - Lilith: "Fufu, well, let's not let them run away. Sovereign, place me on the high path to cut off their escape!"

### Step 3: Aerial Interruption
- **Type**: StartWave (WaveIndex: 1) — ResumeTime: 1
- **Trigger**: Dialogue ends.
- **Action**: Wave 2 starts (8 Shadow Wings, aerial path). Time resumes.
- **Dialogue**:
  - Lilith: "Look at those filthy 'Shadow Wings'—trying to bypass us through the upper air. They think we're easy prey."
  - Lilith: "Let me handle them, Sovereign. My magic can reach where Ignis's sword falters. Just... give me a good view."

### Step 4: Lilith Placement Tutorial
- **Type**: WaitForAction (`UnitPlaced`) — UseBlocker: true, StopTime: true
- **Objective**: Place **Lilith** on tile **(7, 11)** [High Ground].
- **UI**: Hand animation drags from Lilith button → target tile.
- **Dialogue**:
  - Tina: "Lilith is a Warlock. Place her on the High Ground to maximize her reach and Magical damage."

### Step 5: The Battle Continues
- **Trigger**: Lilith placed. Time resumes. Wave 2 continues to conclusion.
- **Type**: WaitForWave (WaveIndex: 2, waits for all aerial enemies to die)

### Step 5.5: Mobs are Swarming! (AOE Tutorial Context)
- **Type**: DialogueOnly — UseBlocker: true, StopTime: true, ResumeTime: true
- **Trigger**: Wave 2 finished spawning.
- **Dialogue**:
  - Ignis: "Sovereign! We're being swarmed! My blade cannot keep up with these numbers!"
  - Lilith: "They're overwhelming us! Sovereign, please... call upon your Sovereign Rite and wipe them all out at once!"

### Step 5.6: Teach AOE Rite
- **Type**: WaitForAction (`RiteMenuOpened`) — UseBlocker: true, StopTime: true, ResumeTime: false
- **UI Highlight**: `SovereignRiteToggle` button.

### Step 5.7: Execute AOE Rite
- **Type**: WaitForAction (`SkillUsed`) — UseBlocker: true, StopTime: true, ResumeTime: true
- **Objective**: Use **Cataclysmic Grand Cross** or **Star-Fall Requiem** on tile (3, 4).

### Step 6: Wait for Wave 3 Completion
- **Type**: WaitForWave — ResumeTime: true

### Step 6.5: Boss Warning
- **Type**: DialogueOnly — UseBlocker: true, StopTime: true, ResumeTime: true
- **Dialogue**:
  - Tina: "A powerful presence approaches... the Abyssal Shade has fully unsealed!"

### Step 7: Start Boss Wave
- **Type**: StartWave (WaveIndex: 3) — ResumeTime: true

### Step 8: Fight until HP Gated
- **Type**: WaitForCondition (`BossHealth`, RequiredCount: 70) — ResumeTime: false

### Step 9: Boss Bypasses Ignis
- **Type**: WaitForCondition (`BossPassedUnit`, TargetUI: "Ignis") — ResumeTime: false
- **Trigger**: Boss's position crosses past Ignis's position.

### Step 10: Boss is Bypassing Defenses!
- **Type**: DialogueOnly — UseBlocker: true, StopTime: true, ResumeTime: false
- **Dialogue**:
  - Ignis: "Tch! My blade... it's passing straight through him! He's turned into pure shadow!"
  - Lilith: "My Sovereign! That beast is bypassing your defenses! I have whispered to the seals to release their full power... Your Authority Seals are now restored to their absolute limit of 99!"
  - Lilith: "Use your most powerful Sovereign Rite now. Show this 'Boss' the true meaning of your divine might!"

### Step 10.5: Refill Seals for One-Shot
- **Type**: CustomCommand (`SetMaxAuthoritySeals`, RequiredCount: 99)

### Step 11: Open Rite Menu for One-Shot
- **Type**: WaitForAction (`RiteMenuOpened`)

### Step 12: Execute One-Shot Rite
- **Type**: WaitForCondition (`BossHealth`, RequiredCount: 0) — UseBlocker: true, StopTime: true, ResumeTime: false
- **Objective**: Use **Abyssal Guillotine** or **Event Horizon** on the Boss.

### Step 13: Final Victory Dialogue
- **Type**: DialogueOnly — UseBlocker: true, StopTime: true, ResumeTime: true
- **Dialogue**:
  - Lilith: "Mmm... what a magnificent display. I might have to reconsider my opinion of you, Sovereign."
  - Tina: "The first seal of your authority is restored. Well done, My Sovereign."
  - Lilith: "Don't look so stiff, Tina. We're all on the same side... for now."
  - Tina: "We proceed to the next chamber. There are more sisters to awaken."
  - Tina: "And then... we return to the surface. Your kingdom awaits, My Sovereign. It is time the world remembered who truly rules these lands."
