## Advanced Pathfinding & AI Checklist

Priority: High  
Category: AI & Algorithms

- [x] **Modular Pathfinding Architecture**
  - [x] Separate A* logic into `game/engine/movement/astar.ts`
  - [x] Separate Flow Field logic into `game/engine/movement/flowfield.ts`
  - [x] Implement Priority Behaviors in `game/engine/movement/behaviors.ts`
- [ ] **Weighted Cost Maps**
  - [x] Implement tile costs (Standard=1, Trap=10, Hazard=5)
  - [ ] Dynamic cost updating based on deployed traps/vassals
- [x] **Priority Behaviors**
  - [x] `STANDARD`: Balanced pathing to base
  - [x] `SAPPER`: Prioritize targets with `DeployType: TOWER` or `TRAP`
  - [x] `ASSASSIN`: Direct-to-base, ignoring non-essential path weights
  - [ ] `FLYING`: Linear distance ignore obstacles (Coming Soon)
- [ ] **Swarm & Grouping**
  - [ ] `BARD`: Flow field attraction to high-density ally groups
  - [ ] Herd mentality: Enemies follow the "leader" of a wave
- [ ] **Large Unit Logic**
  - [ ] 2x2 clearance checking for Bosses
  - [ ] Turning radius/Rotation smoothing
- [ ] **Performance**
  - [x] Path caching per spawn point
  - [ ] Staggered path updates (max 5 units per frame)
