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
- **Map Size**: 9x9 Grid
- **Spawn Gates**: (0, 4) [Ground] and (0, 5) [High Ground/Aerial]
- **Exit Gates**: (8, 4) [Ground] and (8, 5) [High Ground]
- **Ignis Placement**: (2, 4) [Ground lane, 2 tiles from spawn]
- **Lilith Placement**: (2, 5) [High Ground lane, 2 tiles from spawn]

### 5. Boss Mechanics
- **IsBoss**: true — escaping the exit triggers immediate **Game Over** (not just HP damage).
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
  - Tina: "The air is heavy with ancient magic... stay alert, My Sovereign."

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
- **Trigger**: Wave 1 fully cleared (ActiveEnemyCount == 0, IsSpawning == false).
- **Action**: **UI Blocker Active**. Time stops.
- **Action**: **CustomCommand**: `SetUnitButtonActive` (Target: "Lilith", Count: 1) — unlocks Lilith.
- **Action**: **CustomCommand**: `SetMaxAuthoritySeals` (Value: 99) — Stage 2 seal cap restored.
- **Audio**: A playful, smug laugh echoes through the chamber.
- **Dialogue**:
  - Lilith: "Mmm, such a sweet, delicious smell... is that you, Tina? Still playing the dutiful little maid?"
  - Tina: "Lilith. Your teasing is as poorly timed as ever. The Sovereign has returned."
  - Lilith: "Oh? The little Maou? My, how you've... changed. So adorably small now."

### Step 3: Aerial Interruption
- **Type**: StartWave (WaveIndex: 1) — ResumeTime: 1
- **Trigger**: Dialogue ends.
- **Action**: Wave 2 starts (8 Shadow Wings, aerial path). Time resumes.
- **Dialogue**:
  - Lilith: "Look at those filthy 'Shadow Wings'—circling like vultures. They think we're easy prey."
  - Lilith: "Let me handle them, Sovereign. My magic can reach where Ignis's sword falters. Just... give me a good view."

### Step 3.5: Wait for Shadow Wings Midpoint
- **Type**: WaitForCondition (`EnemiesInRange`, Tile: {4,5}, Size: 1.5, RequiredCount: 1)
- **Trigger**: At least 1 Shadow Wing reaches x=4 (grid midpoint).
- **Time**: NOT stopped — enemies continue moving. Tutorial waits silently.
- **Purpose**: Ensures Lilith placement tutorial fires while enemies are actively threatening, not before they appear.

### Step 4: Lilith Placement Tutorial
- **Type**: WaitForAction (`UnitPlaced`) — UseBlocker: true, StopTime: true
- **Trigger**: Shadow Wings reach midpoint (Step 3.5 resolved).
- **Action**: **TIME STOPS.** Placement tutorial for Lilith activates.
- **Objective**: Place **Lilith** on tile **(2, 5)** [High Ground].
- **UI**: Hand animation drags from Lilith button → target tile.
- **Dialogue**:
  - Tina: "Lilith is a Warlock. Place her on the High Ground to maximize her reach and Magical damage."

### Step 5: The Battle Continues
- **Trigger**: Lilith placed. Time resumes. Wave 2 continues to conclusion.
- **Type**: WaitForWave (WaveIndex: 2, waits for all aerial enemies to die)
- **Action**: After wave clears, proceed.

### Step 5.5: Mobs are Swarming! (AOE Tutorial Context)
- **Type**: DialogueOnly — UseBlocker: true, StopTime: true, ResumeTime: true
- **Trigger**: Wave 2 confirmed spawned (WaveFinishedSpawning, WaveIndex: 2).
- **Action**: UI blocker shows. Brief combat context dialogue fires.
- **Note**: After dialogue, time resumes. Wave 3 enemies continue engaging units.

### Step 5.6: Teach AOE Rite
- **Type**: WaitForAction (`RiteMenuOpened`) — UseBlocker: true, StopTime: true, ResumeTime: false
- **Trigger**: Post-dialogue. Time stops.
- **UI Highlight**: `SovereignRiteToggle` button.
- **Auto-skip**: If the Rite Menu is already open when this step starts, it resolves immediately (no re-open required).
- **Dialogue**:
  - Tina: "Open the Sovereign's Rite menu to prepare an area ritual."

### Step 5.7: Execute AOE Rite
- **Type**: WaitForAction (`SkillUsed`) — UseBlocker: true, StopTime: true, ResumeTime: true
- **Trigger**: Rite Menu opened. Time still paused.
- **Objective**: Use **Cataclysmic Grand Cross** (Male) or **Star-Fall Requiem** (Female).
- **Action**: After skill fires, time resumes. Remaining Wave 3 enemies continue.

### Step 6: Wait for Wave 3 Completion
- **Type**: WaitForWave — ResumeTime: true
- **Trigger**: All Wave 3 enemies defeated.

### Step 6.5: Boss Warning
- **Type**: DialogueOnly — UseBlocker: true, StopTime: true, ResumeTime: true
- **Dialogue**: Boss incoming warning.

### Step 7: Start Boss Wave
- **Type**: StartWave (WaveIndex: 3) — ResumeTime: true
- **Action**: Abyssal Shade + 6 Lesser Shadows spawn.
- **Boss AI**: `PreventDeathForTutorial = true` is automatically applied when the `BossHealth ≤ 70` condition begins polling.

### Step 8: Fight until HP Gated
- **Type**: WaitForCondition (`BossHealth`, RequiredCount: 70) — ResumeTime: false (time stays running)
- **Trigger**: Boss HP drops to **≤ 70%**.
- **Behavior**: Every frame while polling, boss has `PreventDeathForTutorial = true` — he cannot die during this phase.

### Step 9: Boss Teleports Behind Ignis
- **Type**: WaitForCondition (`BossPassedUnit`, TargetUI: "Ignis") — ResumeTime: false
- **Trigger**: Boss's x-position crosses past Ignis's position (directional check using SpawnPoint vs ExitPoint).
- **Behavior**: Time remains paused. Boss is frozen in place once condition is met.
- **Fallback**: If Ignis is missing, condition auto-resolves (soft-lock prevention).

### Step 10: Boss is Bypassing Defenses!
- **Type**: DialogueOnly — UseBlocker: true, StopTime: true, ResumeTime: false
- **Dialogue**:
  - Ignis: "Tch! My blade... it's passing straight through him! He's turned into pure shadow!"
  - Tina: "He's entered an ethereal state. Melee strikes are useless now. Only Rites can harm him."

### Step 10.5: Refill Seals for One-Shot
- **Type**: CustomCommand (`SetMaxAuthoritySeals`, RequiredCount: 99) — StopTime: false, ResumeTime: false
- **Action**: Silently fills seals to 99 so the player can afford the One-Shot Rite.

### Step 11: Open Rite Menu for One-Shot
- **Type**: WaitForAction (`RiteMenuOpened`) — UseBlocker: true, StopTime: true, ResumeTime: false
- **UI Highlight**: `SovereignRiteToggle`.

### Step 12: Execute One-Shot Rite
- **Type**: WaitForCondition (`BossHealth`, RequiredCount: 0) — UseBlocker: true, StopTime: true, ResumeTime: false
- **UI Highlight**: `SkillButton_AbyssalGuillotine_Male` + `SkillButton_EventHorizon_Female` (additional).
- **Objective**: Use **Abyssal Guillotine** (Male) or **Event Horizon** (Female) on the Boss.
- **Kill mechanic**:
  1. Step starts → `PreventDeathForTutorial` set to **false** (boss is now mortal).
  2. `CheckCondition` polls for `boss.IsDead == true` (or `boss == null` if already destroyed).
  3. Condition met → tutorial advances.
- **⚠️ IMPORTANT**: If the boss somehow reaches the exit before this step resolves → **immediate Game Over** (IsBoss path in GameManager.EnemyEscaped).

### Step 13: Final Victory Dialogue
- **Type**: DialogueOnly — UseBlocker: true, StopTime: true, ResumeTime: true
- **Trigger**: Boss defeated.
- **Dialogue**:
  - Lilith: "Mmm... what a magnificent display. I might have to reconsider my opinion of you, Sovereign."
  - Tina: "The first seal of your authority is restored. Well done, My Sovereign."
  - Lilith: "Don't look so stiff, Tina. We're all on the same side... for now."
  - Tina: "We proceed to the next chamber. There are more sisters to awaken."
  - Tina: "And then... we return to the surface. Your kingdom awaits, My Sovereign. It is time the world remembered who truly rules these lands."

---

## Code-Level Notes

| Issue | Root Cause | Fix Applied |
| :--- | :--- | :--- |
| Boss passed Ignis and continued to exit | `WaitForCondition` (BossPassedUnit) had `ResumeTime: 1` in old asset — time resumed while boss walked | Set `ResumeTime: 0` on all boss phase steps |
| Boss teleport step leaked time | Same `ResumeTime` bug | Fixed |
| Seals showed 75/75 | Asset had `RequiredCount: 75` on SetMaxAuthoritySeals steps | Corrected to 99 |
| Execute One-Shot Rite never completed | Step was **Type: 3 (WaitForAction)** but `BossHealth` is only checked in `CheckCondition` (Type: 8) | Changed to **Type: 8 (WaitForCondition)** |
| Boss immortal during kill step | `CheckCondition("BossHealth")` always set `PreventDeathForTutorial = true`, clamping HP ≥ 1 forever | Split condition: RequiredCount > 0 = HP gate (immortal), RequiredCount = 0 = kill (lifts flag, waits for `IsDead`) |
| Mid-cast step appeared skipped | `RiteMenuOpened` auto-resolves if menu already open (by design); `WaitForWave` resolves instantly if wave already cleared | Expected behavior — not a bug |
| Lilith placement before enemies arrived | Placement tutorial fired at Wave 2 start, before Shadow Wings reached midpoint | Added `EnemiesInRange` wait step at tile (4,5) before placement tutorial |
