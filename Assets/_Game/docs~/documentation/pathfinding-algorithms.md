# Pathfinding & Routing Algorithms

In Aethelgard, the battlefield is a dynamic grid. Enemies don't just walk straight; they adapt based on their faction, class, and the "Maou's" defensive choices.

## 1. Core Routing: Weighted A* (A-Star)
Most units (`STANDARD` AI) use a modified A* algorithm.
- **Base Cost**: 1 per tile.
- **Trap Cost**: +5 (Enemies try to avoid traps unless they have no choice).
- **Hazard Cost**: +3 (Slow terrain, burning ground).
- **Bard Benefit**: -2 (Enemies prefer walking through Bard-buffed zones).

## 2. Specialized Routing Algorithms

### A. The Sapper Algorithm (Targeted Multi-goal)
Used by: **Ironclad Sappers**.
1. **Scan**: Identify all active `Architect` units (Traps/Towers) on the map.
2. **Heuristic**: Select the nearest trap with the highest threat level.
3. **Route**: A* directly to the trap.
4. **Fallback**: If no traps exist, default to shortest path to the Throne.

### B. Dynamic Flow Fields (AoE Optimization)
Used by: **Bards / Holy Wall Formations**.
1. Generate a "Flow Field" centered on the Bard.
2. Nearby allies move towards the Bard's AoE zone to receive buffs/heals.
3. This creates "clusters" of enemies, making them harder to block but vulnerable to AoE magic.

### C. Scripted Linear Pathing (The "Truck-kun" Logic)
Used by: **Bosses / Heavy Charges**.
- **Logic**: Select a target lane and move in a strictly straight line.
- **Rule**: Ignores `blockCount`. Deletes or "Pushes" units in its way.
- **Recalculation**: None. Once the charge starts, it cannot be stopped by standard tactics.

### D. Bypass Routing (Bounty Hunters / Assassins)
Used by: **Free Cities Assassins**.
- **Logic**: During the A* search, Ground Blockers (Vassals) are treated as **walkable tiles**.
- **Heuristic**: Focus strictly on the shortest Manhattan distance to the `castleHp` target.

## 3. Real-Time Repathing
Enemies recalculate their path every **2.0 seconds** or when:
1. A new `Bastion` unit is deployed in their immediate 3x3 vicinity.
2. A Bard ally activates an AoE field.
3. Their current target (e.g., a Trap) is destroyed.

## 4. Large-Unit Footprints (Bosses)
Bosses use **Width-Aware A***. 
- A 2x2 boss checks a 2x2 grid for every step.
- If a chokepoint is only 1-tile wide, the cost is set to `Infinity`, forcing the boss to find wider lanes or destroy the terrain.
