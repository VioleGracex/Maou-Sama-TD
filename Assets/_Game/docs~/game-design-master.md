# Game Design Master Document

## 1. Game Modes

### A. Campaign (Main Story)
- **Structure**: Linear chapters (1-1 to 1-10).
- **Narrative**: Visual Novel segments trigger Before/After specific stages.
- **Difficulty**: Normal / Hard (unlocks after clearing Chapter).
- **Stamina**: Consumes Stamina. Returns 10% if defeated.

### B. Daily Dungeons (Resource Grinding)
- **Gold Vault**: Enemies drop high gold, no XP.
- **Exp Forest**: Enemies drop XP potions.
- **Material Mine**: Drops generic upgrade materials.
- **Rotation**: Different dungeons open on different days of the week.

### C. Tower of Babel (Endless/Challenge)
- **Structure**: 100 Floors.
- **Persistence**: HP carries over between floors (optional hardcore mode).
- **Reset**: Resets monthly.
- **Rewards**: Exclusive currency (Tower Coins) for unique units.

### D. Events
- **Token Collection**: Clear event stages -> Get Tokens -> Spin Event Gacha (Box Gacha).
- **Point Ladder**: Accumulate points for milestone rewards.

## 2. Online & Social Features

### Leaderboards
- **Time Attack**: Fastest clear of specific Boss stages.
- **Max Damage**: Highest damage dealt in a single hit (Sandbox mode).
- **Score**: Algorithm based on `(CastleHP * 100) + (SpeedBonus) - (CostUsed)`.

### Friend Support
- Player sets a "Support Unit".
- Before battle, select a stranger or friend's unit.
- That unit can be deployed once per battle for 0 cost (or reduced cost).
- Reward: Friend Points for both parties.

## 3. Character Progression (The "Gacha" Hook)

### Stats
- HP, ATK, DEF, RES (Magic Def), ASPD (Attack Speed), COST, RESPAWN_TIME.

### Growth Systems
1. **Leveling**: Uses XP items. Caps at Lv. 30/50/70/90 based on Stars.
2. **Awakening (Stars)**: Uses duplicates (Shards) to increase base stat multipliers.
3. **Affection (Romance)**:
   - Levels 1-10.
   - Unlocks: Voice lines, Profile Data, Secret CGs, Stat Bonus (small, e.g., +1% ATK).
4. **Skills**:
   - **Passive**: Always active (e.g., "Deal 10% more dmg to Humans").
   - **Active**: Manually triggered or Auto-triggered on cooldown.

## 4. Battle Mechanics Detail

### Damage Formula (Draft)
```
PhysicalDmg = (Attacker.ATK * SkillMulti) - (Defender.DEF)
MagicDmg    = (Attacker.ATK * SkillMulti * (1 - Defender.RES_Percent))
MinDmg      = Attacker.ATK * 0.05
```

### Lane Logic
- **Ground**: Melee units block enemies.
- **High Ground**: Ranged units (Archers/Mages) placed here. Cannot be hit by melee enemies unless the enemy has "Ranged Attack".
- **Flying Enemies**: Ignore ground blockers. Must be hit by Ranged units.

### Maou Skills (Player Spells)
- **Thunder Strike**: Deal DMG to single tile.
- **Rally**: Increase ASPD of all units for 10s.
- **Terror**: Stun all enemies for 3s.
- *Cost*: Uses "Commander Points" generated over time or by kills.
