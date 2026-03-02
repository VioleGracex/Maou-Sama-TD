# Combat Formulas

## 1. Physical Damage
Standard physical attacks subtract defense from attack power.
```text
Damage = Max( (ATK * Multiplier) - DEF, ATK * 0.05 )
```
- **Multiplier**: 1.0 for standard attacks, higher for skills.
- **Floor**: 5% of ATK is always dealt to ensure progress.

## 2. Magical Damage
Magical resistance acts as a direct percentage mitigation.
```text
Damage = ATK * Multiplier * (1 - (RES / 100))
```
- **RES Cap**: Resistance is capped at 95% to prevent total immunity.

## 3. Experience & Authority
### XP Growth
```text
XP_Next = 100 * (Level ^ 1.8)
```
### Authority Cost (Deployment)
- **R Vassals**: 8-12 Seals
- **SR Vassals**: 13-18 Seals
- **SSR Vassals**: 19-30 Seals

## 4. Engagement (Block)
- **Bastion**: 3 Blocks
- **Vanguard**: 2 Blocks
- **Executioner**: 1 Block
- **Ranged**: 1 Block (Emergency only, typically don't block unless on ground)
