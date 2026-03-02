# Architecture & Refactoring Guide

## Current System: Reactive State + Pure Engine
The game uses a **Hybrid State** architecture. 

1.  **Meta-State (React Context)**: Handles the "Citadel" layer—inventory, character levels, and progression. Persistent via `localStorage`.
2.  **Battle-State (Immutable Engine)**: A separate state object updated 60 times per second. The engine (`engine.ts`) is a collection of pure functions that take `(State, DT)` and return `NextState`.

## Key Logic Modules

### 1. The Pathfinder (`engine.ts`)
Uses a BFS algorithm to calculate a static path from Spawn (2) to Base (3). This is cached at the start of battle to save CPU cycles.

### 2. The Engagement System
Instead of simple distance checks, the engine iterates through units and checks if an enemy is "on" their tile.
- **Rules**: Only ground classes (Bastion, Vanguard, Executioner) can populate `engagedEnemies`. 
- **Block**: If an enemy is in `engagedEnemies`, their `speed` is effectively 0 in the movement system.

### 3. Authority Resource
Replaces standard Mana. It is the lifeblood of the battle engine, managed in `BattleState`. All deployments and skills are gated by this value.

## Planned Refactors
- **System-Based Update**: Split `updateBattle` into `moveSystem`, `combatSystem`, and `statusSystem` for better readability.
- **Asset Preloader**: Move from `placehold.co` to local assets with a loading screen.
- **Component Atomization**: Extract `HealthBar`, `VassalCard`, and `HecatinaHUD` into standalone atomic components.
