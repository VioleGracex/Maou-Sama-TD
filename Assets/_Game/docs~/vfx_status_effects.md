# Status Effect VFX Design Guide

This document outlines the standardized visual language for combat status effects in the game. Consistency in colors and shapes is crucial for player readability during fast-paced battles.

## 1. Color Palette

Status effects strictly adhere to the following color codes to instantly communicate their nature:

| Effect Type | Concept | Color | Hex Code |
| :--- | :--- | :--- | :--- |
| **Buff** | Stat increases, shields, positive auras | **Yellow** | `#FFE61A` (1.0, 0.9, 0.1) |
| **Heal** | HP restoration, regeneration | **Bright Green** | `#1AFF4D` (0.1, 1.0, 0.3) |
| **Burst Damage** | Direct hits, critical strikes | **Red** | `#FF1A1A` (1.0, 0.1, 0.1) |
| **Damage Over Time** | Poison, burn, bleed, environmental damage | **Dark Green** | `#1A801A` (0.1, 0.5, 0.1) |
| **Debuff** | Stat decreases, stuns, freezes, vulnerabilities | **Dark Violet** | `#8000CC` (0.5, 0.0, 0.8) |

## 2. Shape and Texture Language

Each status effect prefab consists of two components: the **Burst** (immediate feedback) and the **Persistent Floor** (duration feedback).

### 2.1 Burst Particles
- **Purpose**: To provide an immediate, satisfying visual "pop" when an effect is first applied.
- **Texture**: `Assets/_Game/VFX/star.png`
- **Behavior**: A quick, non-looping cone-shaped burst shooting upward from the unit. High initial velocity, fading out over 0.5 - 1.0 seconds.

### 2.2 Persistent Floor Particles
- **Purpose**: To clearly show that a unit is currently under the influence of an effect over time.
- **Textures**: 
  - Buffs & Heals: `Assets/_Game/Art/UI/Icons/icon_magic_buff.png`
  - Burst Damage & DoT: `Assets/_Game/Art/UI/Icons/icon_magic_damage.png`
  - Debuffs: `Assets/_Game/Art/UI/Icons/icon_magic_debuff.png`
- **Behavior**: A flat, square-shaped particle system (`X rotation = 90` if using 3D quads, or rendered horizontally) hovering slightly above the ground tile (`Y = 0.05`). It loops continuously for the duration of the effect.

## 3. Implementation Workflow

When adding a new status effect skill to the game:
1. Identify the effect type from the palette above.
2. Load the corresponding prefab from `Assets/_Game/Prefabs/VFX/StatusEffects/`.
3. Instantiate the prefab at the target unit's position.
4. The burst will play automatically. Retain a reference to the instantiated GameObject and `Destroy()` it when the status effect expires to remove the persistent floor aura.
