# Developer Prompts

## Character Asset Generation
"Create a character description for a new unit: 'The Frost Giant'. Rarity: SR. Class: Tank. Tags: Ice, Giant, Male. Stats: High HP, Low Speed. Skill: 'Avalanche' - Stuns enemies in a 3x3 area. Give me the JSON entry for `CHARACTERS` constant."

## Event Generation
"Design a Halloween Event called 'Night of the Living Bread'.
1.  **Enemies**: Pumpkin Knights, Flour Golems.
2.  **Map**: Bakery themed.
3.  **Reward Unit**: 'Baker Maou' (Skin).
4.  **Mechanic**: Collect 'Yeast' tokens to spin a roulette."

## Refactoring
"Analyze `routes/Battle.tsx`. It is currently 300 lines long. Split the Rendering logic into `components/battle/Grid.tsx` and `components/battle/HUD.tsx`. Keep the logic in a custom hook `useBattleLogic`."
