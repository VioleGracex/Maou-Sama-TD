# Core Gameplay & Code Mechanics

This document outlines the actual C# code architecture and core systems driving the gameplay in `Maou-Sama-TD`.

## 1. Unit Architecture (`UnitBase.cs`, `PlayerUnit.cs`, `EnemyUnit.cs`)
All characters on the board inherit from `UnitBase`, which defines the core stats, visuals, health bar management, and damage reception.

### Core Stats & Buffs
*   **Property Calculation:** Properties like `AttackPower`, `Defense`, and `AttackInterval` are calculated dynamically via getters that iterate through an `_activeBuffs` list. Buffs apply their `Multiplier` to the base stat.
*   **Damage Formula:** `TakeDamage` applies damage subtractively: `Mathf.Max(1f, finalAmount - Defense)`. Ultimate casts have an innate damage resistance modifier.
*   **Vigor Penalty System:** In `PlayerUnit.Die()`, if a unit dies, its `Vigor` (morale/energy) drops by an escalating penalty (20 for first death, 30 for second, etc.) which gets saved to the `SaveManager`.

### Attack Loop & Aggro Priorities (`PlayerUnit.Attack()`)
When the `_attackTimer` hits zero, the unit scans `EnemyUnit.ActiveEnemies` within its `AttackPattern`.
Enemies are scored to find the optimal target (`bestTarget`) based on:
1.  **High Ground (+2000):** Prioritizes flying enemies or those on elevated tiles.
2.  **Same Lane (+500):** Prioritizes enemies sharing the same X or Y coordinate.
3.  **Damage Taken (+X):** Aggro system weights enemies who have dealt damage to this unit.
4.  **Revenge (+500):** Prioritizes the `_lastAttacker`.
5.  **Proximity (-Distance):** Closer enemies are prioritized.

## 2. Ultimate Skills (Vassal Specific)
These are localized skills cast by individual Player Units, managed via `CurrentCharge` on `PlayerUnit.cs`. Charge generates passively based on `ChargePerSecond`.
When `UseSkill()` is triggered:
1.  Initiates `ExecuteUltimateRoutine()` and drains `ChargeCost`.
2.  Invokes `UltimateCutInUI` to display the animated 2D splash art.
3.  Calculates `FindBestUltimateDirection()` to point the attack down the lane with the highest density of enemies.
4.  Instantiates the `UltimatePrefab` and executes its `UltimateEffect`.

## 3. Sovereign Rites (Global Maou Skills)
Handled by the `SkillManager.cs`. These are the Demon Lord's global abilities triggered by the player.
*   **Cost & Cooldown:** Checked via `IsSkillReady()`. Unlike Ultimates (which use Charge), Sovereign Rites cost **Authority Seals** and have a strict time-based Cooldown.
*   **Shapes & Execution:** `TryExecuteRite()` checks if targets fall within `AoeShape` (Circle, Square, Cross, DiagonalX, Star, Custom Offsets). 
*   **Effect Types:** Applies instantaneous Damage (ignoring normal pathing/immunities), Buffs/Debuffs (modifying stats via `ApplyBuff`), or Persistent Zone effects that permanently alter tiles (`ApplyPersistentEffect`).

## 4. Retreat and Recovery (`DeploymentUI.cs`)
The game implements a strategic "Bench" mechanic for units that are swapped out or defeated, managed by `DeploymentUI`.
*   **Manual Retreat:** If the player calls `RetreatUnitInstance()`, the unit is removed from the grid. The player receives a **50% Seal Refund** and the unit is placed on a Respawn Cooldown. While benched, they heal rapidly (**10% HP per second**).
*   **Defeat / KO:** If a unit dies in combat, they receive NO refund. They go on Cooldown and heal very slowly (**2% HP per second**). They cannot be re-deployed until fully healed.
*   **Free Retreat (Map Changes):** If the `EnemyManager` dynamically alters the grid (e.g., turning a walkable tile into an enemy spawn point), `RetreatUnitFree()` is invoked. The player gets a **100% Refund**, no cooldown is applied, and the unit is safely benched.

## 5. Enemy & Wave Management (`EnemyManager.cs`)
Controls the overarching flow of a battle level through `WaveData`.
*   **Pathfinding:** Uses `GridManager.GetPath()` to route enemies from `SpawnPoint` to `ExitPoint`.
*   **Dynamic Tile Alterations:** `ApplyWaveTileAlterations` dynamically adds or subtracts Spawn/Exit or Walkable tiles mid-battle (e.g., walls breaking down).
*   **Level Clear Sequence:** Triggers a cinematic slow-motion effect (`SetSpeed(0.5f)`) and camera shake when the final enemy dies.
