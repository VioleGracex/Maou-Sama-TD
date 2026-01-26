
# Vassal Polish & Passive Evolution Checklist

Priority: High  
Category: Content & Systems

## Data Iteration (Passives & Assets)
- [ ] **Iteration: Asset Mapping**
  - [ ] Map unique `assets.sprite` paths for all 100 vassals.
  - [ ] Standardize `assets.portrait` naming convention (`char_[name].png`).
- [ ] **Iteration: Mechanical Passives**
  - [ ] Add `passiveSkillId` to `UnitDefinition` in `types.ts`.
  - [ ] Implement passive triggers in `game/engine/systems/combat.ts`.
  - [ ] Specific Passive Logic:
    - [ ] `Ignis`: "Shield Wall" - Damage reduction when HP > 50%.
    - [ ] `Lilith`: "Witch's Presence" - Chance to slow enemies in range.
    - [ ] `Kenji`: "Overclock" - Scaling ASPD based on current CP.
- [ ] **Stellar Gate Integration**
  - [ ] Link `mechanicalChangeId` to engine-level state modifications.
  - [ ] UI: Add "Passive Preview" to the Stellar Gate tab.

## Character-Specific Logic (Passives)
- [ ] **Executioner Passives**
  - [ ] Implement "Finisher": Extra damage to enemies below 20% HP.
- [ ] **Bastion Passives**
  - [ ] Implement "Iron Resolve": Bonus DEF for every engaged enemy.
- [ ] **Warlock Passives**
  - [ ] Implement "Mana Well": Generate 1 SP per 5s passively.
