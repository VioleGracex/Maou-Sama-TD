# Animation Naming Conventions

This document defines the standard naming for animation assets, parameters, and states to ensure compatibility with the automated `UnitBase`, `PlayerUnit`, and `EnemyUnit` logic.

## 1. Animator Parameter Names (Triggers/Bools)

The following **State Names** are required or supported by the codebase. Ensure your Animator states use these exact names (Case Sensitive) to work with the `animator.Play()` command.

| State Name | Description | Triggered By |
| :--- | :--- | :--- |
| **`Attack`** | Plays the basic attack animation. | `PlayerUnit.Attack`, `EnemyUnit.HandleAttack` |
| **`Ultimate`** | Plays the skill/ultimate animation. | `PlayerUnit.ExecuteUltimateRoutine` |
| **`Die`** / **`Death`** | Plays the death animation. The code tries both. | `UnitBase.Die` |
| **`Idle`** | Standard waiting animation. | Set as Default State in Animator. |

## 2. Special Effect States (Projectiles/VFX)

Some specialized effects use their own state sets for timing:

### Phoenix Projectile (`PhoenixProjectile.cs`)
- **`Rise`**: Played during the 1-second warm-up phase.
- **`Dash`**: Played once the phoenix starts moving forward.

## 2. State Naming

To ensure smooth transitions and potential automated state checking (like the death timer), use these state names within the Animator layers:

- **`Idle`**: The default loop state.
- **`Attack`**: The state for basic attacks.
- **`Ultimate`**: The state for skills/ultimates.
- **`Die`** or **`Death`**: The final state. Ensure "Loop Time" is **OFF** for this state.

## 3. Asset Naming (Project Window)

To keep the `Assets/_Game/Art/Characters/` and `Assets/_Game/Art/Enemies/` folders organized:

### Animation Clips (`.anim`)
Format: `anim_[CharacterName]_[Action]`
- *Example*: `anim_Ignis_Attack.anim`
- *Example*: `anim_LesserShadow_Die.anim`

### Animator Controllers (`.controller`)
Format: `ac_[CharacterName]`
- *Example*: `ac_Ignis.controller`
- *Example*: `ac_AbyssalShadeBoss.controller`

## 4. Best Practices

1. **Death Animation Transitions**: The `UnitBase.Die` logic handles destruction. Ensure your `Die` state does not loop and that any transition back to Idle is prevented.
2. **Attack Speed**: Most attack animations should be tuned to finish or reach their "impact point" within the unit's `AttackInterval`.
3. **Sprite Flipping**: The code handles `flipX` based on movement/targeting. Animations should be designed facing **Right** by default.
