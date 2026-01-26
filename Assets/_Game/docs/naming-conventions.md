# Naming Conventions & Thematic Glossary

This document establishes the standard for naming files, variables, and in-game entities to ensure consistency with the **"Maou-Sama" (Demon King)** aesthetic.

---

## 1. Codebase Standards

### File Naming
*   **React Components**: `PascalCase` (e.g., `DarkCitadel.tsx`, `MinionCard.tsx`).
*   **Logic/Utilities**: `camelCase` (e.g., `damageCalculator.ts`, `demonNameGenerator.ts`).
*   **Assets**: `snake_case` (e.g., `bg_throne_room.png`, `icon_soul_gem.png`).

### ID Generation
IDs for game data must follow a strictly typed semantic structure to allow easy filtering.

**Format**: `[type]_[race/origin]_[role]_[variant]`

*   **Characters**: `char_[race]_[role]_[number]`
    *   *Example*: `char_demon_general_01` (Ignis)
    *   *Example*: `char_succubus_mage_02`
*   **Enemies**: `enemy_[faction]_[class]_[tier]`
    *   *Example*: `enemy_human_paladin_elite`
    *   *Example*: `enemy_machine_tank_boss`
*   **Stages**: `stage_[chapter]_[node]`
    *   *Example*: `stage_1_boss`
*   **Items**: `item_[category]_[name]`
    *   *Example*: `item_consumable_xp_potion_l`

---

## 2. Thematic Glossary (UI Text)

Avoid generic Gacha/Tower Defense terms. Use language that reinforces the player's role as the **Demon Overlord**.

| Generic Term | **Maou-Sama Term** | Notes |
| :--- | :--- | :--- |
| **Player** | `Maou-sama` / `My Lord` / `Overlord` | Never "Doctor" or "Commander". |
| **Units/Operators** | `Vassals` / `Subjects` / `The Legion` | They serve you. They are not employees. |
| **Team/Squad** | `Cohort` / `Phalanx` | Military terms fitting an ancient army. |
| **Base** | `Dark Citadel` / `Throne Room` | Where you rule from. |
| **Stamina** | `Dominance` / `Authority` | You don't get "tired", you run out of influence. |
| **Gacha/Summon** | `Soul Binding` / `Dark Ritual` | You are pulling souls, not buying toys. |
| **Premium Currency** | `Soul Gems` / `Chaos Crystals` | |
| **Gold** | `Tribute` | Taxes collected from your domain. |
| **Level Up** | `Ascension` / `Empowerment` | Granting them more of your power. |
| **Dupes/Shards** | `Essence` | |

---

## 3. Class Naming (Flavor vs Code)

In code (`types.ts`), keep standard RPG names for clarity. In UI, use thematic titles.

| Code ID (`UnitClass`) | **UI Display Name** | Description |
| :--- | :--- | :--- |
| `tank` | **Bastion** | Heavy armor, protects the layout. |
| `warrior` | **Vanguard** | Frontline fighters, moderate damage. |
| `assassin` | **Executioner** | High burst, glass cannon. |
| `archer` | **Ranger** | Physical ranged damage. |
| `caster` | **Warlock** | Magical nukers. |
| `healer` | **Blood Sage** | Heals using blood magic (vampiric theme). |
| `summoner` | **Necromancer** | Spawns minions (skeletons/imps). |
| `support` | **Tactician** | Buffs/Debuffs. |

---

## 4. Character Naming Guide

Characters should have names that sound ancient, dangerous, or mythological.

*   **Demons**: Latin or Hebrew roots (e.g., Ignis, Malphas, Lilith, Astaroth).
*   **Undead**: Gothic names (e.g., Silas, Victorian, Mortis).
*   **Beasts**: Guttral or Descriptive (e.g., Fang, Gore, Fenrir).

### Title Format
Display names should often include a title to emphasize hierarchy.
*   *Format*: `[Title] [Name]`
*   *Examples*: "General Ignis", "High Warlock Malzahar", "Shadowblade Kael".

---

## 5. Dialogue Tone Guide

*   **To the Player**: Characters should speak with reverence, fear, or intimate familiarity (if high affection).
    *   *Bad*: "Hey boss, what's the plan?"
    *   *Good*: "Your orders, My Lord? The humans approach."
*   **About Humans**: Dismissive. Humans are "insects", "fodder", or "fanatics".
*   **About Defeat**: Never "We lost". Instead, "We must tactically retreat" or "They got lucky."