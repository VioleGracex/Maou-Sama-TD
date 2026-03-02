# Class Bestiary: The Legion & The Hostiles

This document serves as the tactical manual for all unit classifications within Aethelgard. Classes define the deployment logic, damage scaling, and positioning priorities of every soul on the battlefield.

---

## 1. Ally Classifications (The Maou's Legion)

Vassals are divided into ten distinct tactical roles, each represented by a signature icon in the Cohort Repository.

| Class | UI Icon | Role | Tactical Summary |
| :--- | :--- | :--- | :--- |
| **Bastion** | 🛡️ | **Heavy Tank** | High HP/DEF. Block Count: 3-4. Essential for holding chokepoints. |
| **Vanguard** | ⚔️ | **Melee DPS** | Balanced stats. Block Count: 2. High SP regeneration while engaged. |
| **Executioner** | 🗡️ | **Burst Assassin** | High ATK/ASPD, low HP. Best for single-target elite elimination. |
| **Ranger** | 🏹 | **Physical Ranged** | High Ground deployment. Prioritizes Flying enemies. High range. |
| **Warlock** | 🔮 | **Magical Ranged** | High Ground. AoE magic damage. Often applies CC (Slow/Stun). |
| **Sage** | ⚕️ | **Healer** | Restores HP to allies within range. Essential for Bastion longevity. |
| **Architect** | 🏗️ | **Fortifier** | Deploys stationary Towers or non-blocking Traps. Immune to standard melee. |
| **Necromancer** | 💀 | **Summoner** | Spawns temporary "Fodder" units to increase effective Block Count. |
| **Support** | 📣 | **Buffer** | Provides passive auras (ATK/DEF/SP speed) to nearby allies. |
| **Gunner** | 🔫 | **Rapid Fire** | Extreme ASPD. Deals True Damage but often consumes Authority per shot. |
| **Assassin** | 👤 | **Infiltrator** | Can be placed near enemy spawns. Ignores 1 enemy to strike the backline. |

---

## 2. Enemy Classifications (The Mortal Host)

Hostiles use "AI Priorities" rather than strict classes to determine their pathing behavior toward the Throne.

### A. The Paladin (Standard AI)
- **Visuals**: Heavy plate, gold accents, glowing shields.
- **Behavior**: Moves along the primary path. Attacks the first Vassal it encounters.
- **Threat**: High durability. Can exhaust a Bastion's `blockCount` through sheer volume.

### B. The Sapper (Strategic AI)
- **Visuals**: Hooded engineers, carrying explosive barrels or clockwork tools.
- **Behavior**: Diverts from the main path to destroy **Architect** units (Traps/Towers) or **Summoned** units.
- **Threat**: Can dismantle your defense infrastructure if left unchecked.

### C. The Bounty Hunter (Assassin AI)
- **Visuals**: Cloaked figures with glowing red eyes, dual daggers.
- **Behavior**: **Ignores all Blockers.** Moves directly toward the Throne at high speed. 
- **Threat**: Must be eliminated by high-DPS Executioners or Ranged units before they leak.

### D. The Sky-Zealot (Flying AI)
- **Visuals**: Griffon riders or winged priests.
- **Behavior**: Moves in a straight line over terrain/walls. Cannot be blocked by Ground units.
- **Threat**: Requires dedicated **Ranger** or **Warlock** placement for interception.

### E. The Inquisitor (Anti-Magic AI)
- **Visuals**: Ornate robes, floating censers, silver masks.
- **Behavior**: Ranged magical attacks.
- **Threat**: Attacks apply "Silence," preventing Vassals from using their Active Skills for several seconds.

---

## 3. UI Iconography Standards

To maintain tactical clarity at a glance, class icons follow a specific color-coding system:

- **Red Icons (Combat)**: Vanguard, Executioner, Assassin. (Focus: Aggression)
- **Blue Icons (Defense)**: Bastion, Architect. (Focus: Stability)
- **Purple Icons (Arcane)**: Warlock, Necromancer. (Focus: Mana Manipulation)
- **Green Icons (Life)**: Sage. (Focus: Restoration)
- **Gold Icons (Special)**: Support, Gunner. (Focus: Command)

### Asset Key for Extraction:
When extracting PNGs for Unity/Toolkit:
- `class_icon_bastion.png`: Shield with obsidian spikes.
- `class_icon_vanguard.png`: Two crossed crimson sabers.
- `class_icon_assassin.png`: A hooded silhouette with a vertical dagger.
- `class_icon_ranger.png`: An arrowhead with wind trails.
- `class_icon_warlock.png`: A crystalline orb wreathed in violet flame.
