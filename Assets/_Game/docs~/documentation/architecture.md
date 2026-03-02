# System Architecture

## 1. Overview
MAOU TOWER DEFENSE is a client-side Single Page Application (SPA) built with React. It uses a custom game loop for battle logic and React State/Context for meta-game persistence.

## 2. Directory Structure
*   `src/game/engine/`: Pure TypeScript logic for the Tower Defense simulation. Detached from React to ensure performance and testability.
*   `src/game/state.tsx`: Global Context Provider. Handles persistence (`localStorage`), currency transactions, and inventory.
*   `src/routes/`: Main screen components (Battle, Gacha, Home).
*   `src/components/`: Reusable UI atoms (Cards, Buttons, HUD).

## 3. Data Flow
1.  **Initialization**: `GameProvider` loads JSON from `localStorage`.
2.  **Meta Game**: Actions like "Gacha Pull" or "Upgrade Unit" dispatch functions in `GameContext`, updating `saveData`.
3.  **Battle Start**: `Battle.tsx` initializes `BattleState` using `INITIAL_BATTLE_STATE(stageConfig)`.
4.  **Battle Loop**: `requestAnimationFrame` triggers `updateBattle(state, dt)`. This function returns a *new* state object (immutable pattern).
5.  **Battle End**: On Victory/Defeat, `Battle.tsx` calls `completeStage()` in context to sync results back to `saveData`.

## 4. Key Systems
*   **Gacha Engine**: Uses weighted random selection based on `BannerConfig`.
*   **Save System**: JSON serialization. Versioning strategy required for future updates (e.g., migration scripts if `saveData` shape changes).
*   **Audio**: Singleton `AudioManager` class wrapping `AudioContext`.
