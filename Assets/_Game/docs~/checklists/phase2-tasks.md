
# Phase 2 Checklist: Gameplay Depth

## Battle Engine V2
- [x] **Projectile System**
    - [x] Create `Projectile` interface (x, y, targetId, speed, damage).
    - [x] Update `CombatSystem` to spawn projectiles instead of instant damage for Archers/Mages.
    - [x] Implement collision detection in `updateBattle` loop.
- [x] **Status Effects**
    - [x] Add `buffs` array to `ActiveEnemy` and `ActiveUnit`.
    - [x] Implement `Slow` (modifies speed multiplier).
    - [x] Implement `Burn` (DoT every 1s).
    - [x] Visual indicators (Embers for burn, grayscale for slow).
- [x] **Specialized AI**
    - [x] Implement `SAPPER` (Targets traps).
    - [x] Implement `ASSASSIN` (Ignores blockers).

## UI/UX
- [x] **Damage Numbers**
    - [x] Create a `FloatingText` system in React overlaying the canvas/grid.
    - [x] Show Critical Hits in yellow/large font.
- [x] **Tactical Indicators**
    - [x] Ghost preview during unit drag.
    - [x] Real-time range circle on hover/drag.
    - [x] Placement validity highlighting.

## Content
- [x] **Stage 1-3 to 1-5**
    - [x] Design map layouts.
    - [x] Define enemy waves including Boss Wyvern.
- [x] **Narrative Integration**
    - [x] Chapter 1 story arcs implemented for stages 1-1, 1-3, and 1-5.
