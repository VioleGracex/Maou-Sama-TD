# Future Prompts
*Copy and paste these prompts to the AI to continue development systematically.*

## Prompt 1: Refactor Battle Engine
"I need to refactor the Battle Engine logic in `game/engine.ts`. It is becoming too large. Please split it into a class-based or module-based system. Create separate files for `MovementSystem`, `CombatSystem`, and `WaveSystem`. Ensure the main update loop calls these systems sequentially. Update `routes/Battle.tsx` to use the new engine structure."

## Prompt 2: Implement Inventory & Items
"Add an Inventory System to the game.
1. Update `types.ts` to include `ItemDefinition` (id, name, type: consumable/material, effect).
2. Create `game/data/items.ts` with 5 sample items (XP Potion, Stamina Refill, Gold Bar).
3. Update `PlayerSaveData` to track inventory.
4. Create a UI Route `/bag` to view items.
5. Create a function `useItem(itemId)` in `game/state.tsx` that applies effects."

## Prompt 3: Implement Skill System
"We need active skills for units in battle.
1. Update `UnitDefinition` to include `activeSkillId` and `passiveSkillId`.
2. Define skills in `game/data/skills.ts` (damage multipliers, cooldowns, range).
3. In `routes/Battle.tsx`, add a UI button for the selected unit to trigger their active skill.
4. Update `game/engine.ts` to handle cooldown timers and skill execution (e.g., instant damage or buff)."

## Prompt 4: Add Sound & Music
"Implement an Audio Manager.
1. Create `lib/audio.ts` with methods `playBGM(track)` and `playSFX(sound)`.
2. It should handle volume settings from `PlayerSaveData`.
3. Add a mute toggle in `components/Layout.tsx`.
4. Trigger SFX in `routes/Battle.tsx` on attack and Gacha pull."

## Prompt 5: Leaderboard Mockup
"Create a Leaderboard UI.
Since we don't have a backend yet, create a mock leaderboard.
1. Create `routes/Leaderboard.tsx`.
2. Generate fake data for 'Global Rankings' (Usernames, Scores).
3. Highlight the player's fake rank at the bottom.
4. Add this route to the Home screen navigation."
