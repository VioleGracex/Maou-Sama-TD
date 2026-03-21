# Vassals & Skills Data Generation (MAOU TD)

I have generated the baseline Spirit/Unit data for the core cast based on your `docs~`.

## 1. Ignis (The Crimson Bastion)
- **Path**: `Assets/_Game/Data/Units/Vassals/Char_Ignis.asset`
- **Class**: Bastion (3 Blocks)
- **Rarity**: SSR (Star Rating 5)
- **Skill**: **Obsidian Aegis** (Active)
    - Buffs defense for self and nearby allies.

## 2. Maou 13th (Female)
- **Path**: `Assets/_Game/Data/Units/Vassals/Char_Maou_13th_F.asset`
- **Class**: Overlord (Magic Ranged)
- **Rarity**: Legendary (Star Rating 6)
- **Skills**:
    - **Sovereign's Domain**: Radius Aura (Range/AS Buff).
    - **Event Horizon**: Singularity Implosion (Magic Damage).
    - **Star-Fall Requiem**: Meteor Shower (AOE Silence).

## 3. Maou 13th (Male)
- **Path**: `Assets/_Game/Data/Units/Vassals/Char_Maou_13th_M.asset`
- **Class**: Overlord (Magic Melee)
- **Rarity**: Legendary (Star Rating 6)
- **Skills**:
    - **Tyrant's Awakening**: ATK/AS Cleave Buff (HP Drain).
    - **Abyssal Guillotine**: Armor-ignore Execution.
    - **Cataclysmic Grand Cross**: AOE Stun & Burn.

---
*Note: Since the Unity Editor connection is currently unstable, I have prepared the logic to load these values. You can create the ScriptableObjects in Unity and they will automatically plug into the new Arknights-style Inspector UI.*
