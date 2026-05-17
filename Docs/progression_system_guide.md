# Vassal Progression & Promotion System Guide

Welcome to the comprehensive guide for the **Vassal Progression and Promotion System** in *Maou-Sama TD*. This document provides players and designers with a detailed breakdown of how levels, stars (rank-up), resonance (ascension nodes), and lore chambers are unlocked.

---

## 1. Level-Up System

Units gain base statistics (HP, Attack, Defense) by leveling up. The experience required to level up follows an exponential curve:

$$\text{Required XP} = \lfloor 100 \times \text{Level}^{1.8} \rfloor$$

### XP Sources
Players can level up their units in the Vassal Inspector by using the following resources:
* **XP Core Items**: Consumed directly from the player's inventory.
  * **Common XP Core** (`xp_core_common`): $+100$ XP
  * **Rare XP Core** (`xp_core_rare`): $+500$ XP
  * **Epic XP Core** (`xp_core_epic`): $+2,000$ XP
  * **Legendary XP Core** (`xp_core_legendary`): $+10,000$ XP
* **Duplicate Vassals**: Sacrificing duplicate copies of the same character yields a substantial amount of experience:
  * Sacrificing a duplicate gives **5,000 XP** flat.

### Smart Auto-Add Helper
The XP tab features an **Auto-Add** engine that helps players select the perfect amount of items to reach their target level. It includes highly customizable settings:
1. **Prioritize Duplicates**: Toggles whether the engine should consume duplicate character cards before using standard XP cores.
2. **Rarity Limit**: Caps the highest core rarity the system is allowed to auto-consume (e.g., prevents accidentally spending Legendary Cores on low-level units).
3. **Stop at Level Cap**: Automatically halts item selection if adding another item would waste XP past the unit's maximum level limit.

---

## 2. Rank-Up (Promotion) System

A unit's Star Rating limits its maximum level and grants a massive base stat multiplier. When a unit reaches its current maximum level, it can be **Promoted (Ranked Up)** to the next star rating.

### Star Ratings & Level Caps
* **1 Star**: Max Level 20 (Base Stat Multiplier: $1.0\times$)
* **2 Stars**: Max Level 30 (Base Stat Multiplier: $1.2\times$)
* **3 Stars**: Max Level 45 (Base Stat Multiplier: $1.4\times$)
* **4 Stars**: Max Level 60 (Base Stat Multiplier: $1.6\times$)
* **5 Stars**: Max Level 80 (Base Stat Multiplier: $1.8\times$)
* **6 Stars**: Max Level 90 (Base Stat Multiplier: $2.0\times$)

### Promotion Costs & Recipe Table
Promoting a unit resets their current level to **1**, increases their Star Rating by 1, and grants a **+20% permanent base stat multiplier**. The gold and item costs scale by the unit's target star rating and are class-dependent:

| Target Stars | Gold Cost | Required Primary Material | Required Secondary Material |
| :--- | :--- | :--- | :--- |
| **2 Stars** | 1,000 | 5x Class Primary | 2x Class Secondary |
| **3 Stars** | 3,000 | 10x Class Primary | 5x Class Secondary |
| **4 Stars** | 8,000 | 18x Class Primary | 10x Class Secondary |
| **5 Stars** | 20,000 | 30x Class Primary | 15x Class Secondary |
| **6 Stars** | 50,000 | 50x Class Primary | 25x Class Secondary |

### Class Material Requirements Matrix
* **Melee / Tanks** (*Vanguard*, *Bastion*):
  * **Primary**: `mat_bandit_insignia` (Insignias obtained from Bandits)
  * **Secondary**: `mat_golem_core` (Cores obtained from Golems)
* **Physical DPS / Ranged** (*Ranger*, *Rogue*):
  * **Primary**: `mat_animal_fang` (Fangs obtained from Animals/Beasts)
  * **Secondary**: `mat_bandit_insignia` (Insignias obtained from Bandits)
* **Magic / Supports** (*Warlock*, *Sage*, *Support*, *Necromancer*):
  * **Primary**: `mat_shadow_essence` (Essence obtained from Shadows)
  * **Secondary**: `mat_golem_core` (Cores obtained from Golems)

---

## 3. Enemy Drops & Classification Matrix

Enemies drop progression resources when defeated in battle. Each enemy is assigned an **Enemy Category** and an **Enemy Rank** in the Unity Editor:

### Drop Rates
When a normal or elite enemy is defeated, a loot roll is triggered:
* **Category Material**: **40% chance** to drop their classification material.
* **XP Core**: **20% chance** to drop an XP Core (15% Common, 4% Rare, 1% Epic).

When a **Boss** is defeated, it drops premium items with a **100% guarantee**:
* **3x Category Materials**
* **1x Legendary XP Core**

### Category Mapping
* **Shadows** drop **Shadow Essence** (`mat_shadow_essence`)
* **Bandits** drop **Bandit Insignia** (`mat_bandit_insignia`)
* **Animals** drop **Beast Fang** (`mat_animal_fang`)
* **Golems** drop **Golem Core** (`mat_golem_core`)
* **Fallback Rule**: If an enemy has no assigned category, it falls back to **Golem** if it is a boss, and **Bandit** if it is a standard unit.

---

## 4. Resonance / Ascension Nodes

Every character has 6 **Ascension Nodes** representing their potential.
* **Unlock Cost**: Unlocking each node requires **1 duplicate token** of that character.
* **Stat Bonus**: Each unlocked node grants a permanent **+5% boost** to the unit's Max HP, Attack Power, and Defense. Fully unlocking all 6 nodes provides a massive **+30% overall stat scaling boost**, transforming duplicates into invaluable assets for late-game challenges.

---

## 5. Memories / Chambers System

The Memories / Chambers system allows players to explore character backstories, personalities, and lore entries.
* **Chamber 0 (Introduction)**: Unlocked by default upon obtaining the unit.
* **Chambers 1 - 4**: Unlocked using duplicate copies.
* **Unlock Cost**: Unlocking a locked lore chamber requires consuming **1 duplicate copy** of the character. Unlocked lore is permanently accessible and readable, expanding the player's connection to their vassals.
