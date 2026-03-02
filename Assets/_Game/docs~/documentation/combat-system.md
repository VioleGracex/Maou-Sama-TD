# Combat System: Tactical Sovereignty

This document details the mechanics governing the Battle Engine of Maou Tower Defense.

## 1. Unit Archetypes & Lane Logic

Combat is divided into three layers of interaction:

### A. The Frontline (Ground Blockers)
- **Classes**: Bastion, Vanguard, Executioner.
- **Logic**: Can be placed on **Path Tiles (0)**.
- **Mechanic: Block**: Each melee unit has a `blockCount`. When an enemy enters the unit's tile, they are "Engaged." 
    - While engaged, the enemy's speed is reduced to 0.
    - If `engagedEnemies < blockCount`, the unit continues to stop new enemies.
    - If full, additional enemies pass through.

### B. The Overwatch (High Ground)
- **Classes**: Ranger, Warlock, Sage.
- **Logic**: Must be placed on **High Ground Tiles (1)**.
- **Mechanic: Advantage**: Ranged units are untargetable by standard ground melee enemies. They provide continuous support from elevation.

### C. The Architects (Traps & Towers)
- **Classes**: Architect.
- **Logic**: 
    - **Towers**: Non-blocking stationary units on High Ground.
    - **Traps**: Placed on **Path Tiles (0)**. They are **untargetable** and **non-blocking**. They trigger effects (Damage, Slow, Stun) when an enemy overlaps their footprint.

---

## 2. Damage Formulas

### Physical Damage (Subtractive)
Favors high-impact hits against low-armor targets.
```text
Damage = Max( (Attacker.ATK * SkillMultiplier) - Defender.DEF, Attacker.ATK * 0.05 )
```

### Magical / Dark Damage (Percentage)
Resistance acts as a percentage reduction.
```text
Damage = Attacker.ATK * SkillMultiplier * (1 - (Defender.RES / 100))
```

---

## 3. Status Effects (Status Ailments)
- **Burn / Bleed**: Damage-over-time (DoT) that ignores flat DEF.
- **Slow**: Reduces movement speed multiplier.
- **Stun**: Stops movement, attacking, and skill charging for `N` seconds.
- **Buffs**: Multipliers to base ATK, DEF, or ASPD.

---

## 4. AI & Priority
- **STANDARD**: Moves towards the Throne via the shortest path. Targets ground blockers first.
- **SAPPER**: Prioritizes destroying **Traps** or **Towers** before moving to the Throne.
- **ASSASSIN**: Ignores all non-essential path costs to reach the Throne directly. Often walks through blockers if they are not specifically "Taunting."
- **FLYING**: Ignores path geometry, moves in a straight line to the Throne. Only targetable by Rangers/Warlocks.