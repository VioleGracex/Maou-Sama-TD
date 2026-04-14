# Scaling and Power Formula

To ensure all characters are properly balanced, we use a standardized formula to calculate a unit's `Total Power`. This allows us to compare a Bastion (high HP, low ATK) directly with an Executioner (Low HP, high ATK) and ensure their overall mathematical value is equivalent for their rarity tier.

## The Formula

`Total_Power = ((MaxHp * 0.1) + (Attack * 2.5) + (Defense * 1.5)) * RarityMultiplier * RangeBonus + SkillAllowance`

### Formula Breakdown:

- **Base Stats Weights**:
  - `MaxHp * 0.1`: HP is valued lower per point than other stats since it typically scales into the thousands. 10 HP = 1 Power.
  - `Attack * 2.5`: Attack is highly valued as it dictates clear speed. 1 ATK = 2.5 Power.
  - `Defense * 1.5`: Defense mitigates damage effectively but is passive. 1 DEF = 1.5 Power.

- **Rarity Multiplier**:
  Rarity directly scales all base stats significantly.
  - Common: 1.0x
  - Uncommon: 1.1x
  - Rare: 1.2x
  - Elite (SR): 1.3x
  - Master (SSR): 1.4x
  - Legendary (UR): 1.5x

- **Range Bonus**:
  Ranged units have inherently lower base stats but gain a tactical advantage from their range. This is factored into the power.
  - `RangeBonus = 1.0 + (Range * 0.05)` (e.g. Range 3 gives a 15% boost to raw stat power value).

- **Skill Allowance**:
  A flat modifier (defaulting to 100) added for active and ultimate skill utility that cannot easily be calculated strictly through stats (e.g., healing, CC, invulnerability).

## Class Stat Archetypes (Baseline)
When generating baseline stats, archetypes are applied before any multipliers:

* **Vanguard**: HP 1500, ATK 60, DEF 30
* **Bastion**: HP 2500, ATK 30, DEF 60
* **Executioner/Assassin**: HP 800, ATK 100, DEF 15
* **Ranger/Gunner**: HP 600, ATK 80, DEF 10
* **Sage/Warlock/Necromancer**: HP 600, ATK 90, DEF 10
* **Support/Architect**: HP 700, ATK 40, DEF 20
* **Overlord**: HP 2000, ATK 120, DEF 50

By adhering to this formula, adjusting a unit's stat block can be instantly quantified without rigorous playtesting for minor tweaks.
