import os
from pathlib import Path

base_path = Path(r"d:\OuikiDev\Maou-Sama-TD\Assets\_Game\docs~\characters")

missing_chars = {
    "Common": [
        ("21_goblin_scout.md", "Goblin Scout", "Goblin", "Scout", "Short Bow"),
        ("22_swamp_slime.md", "Swamp Slime", "Monster", "Tank", "Acid Spit"),
        ("23_plague_rat.md", "Plague Rat", "Beast", "Strikers", "Claws")
    ],
    "R": [
        ("20_shadow_assassin.md", "Shadow Assassin", "Demon", "Assassin", "Twin Daggers")
    ],
    "SSR": [
        ("12_blood_drinker._demon.md", "Kaelthas Blood Drinker", "Vampire", "Mage", "Blood Orb"),
        ("13_infernal_behemoth.md", "Infernal Behemoth", "Demon", "Tank", "Obsidian Mace"),
        ("14_nightmare_weaver.md", "Nightmare Weaver", "Succubus", "Support", "Whip"),
        ("15_abyssal_dreadknight.md", "Abyssal Dreadknight", "Undead", "Vanguard", "Greatsword")
    ],
    "UR": [
        ("02_asmodeus_lord_of_lust.md", "Asmodeus Lord of Lust", "Succubus", "Assassin", "Seduction Flames"),
        ("03_lucifer_lord_of_pride.md", "Lucifer Lord of Pride", "Demon", "Vanguard", "Morningstar of Pride"),
        ("04_mammon_lord_of_greed.md", "Mammon Lord of Greed", "Demon", "Mage", "Golden Scepter"),
        ("05_belphegor_lord_of_gluttony.md", "Belphegor Lord of Gluttony", "Demon", "Tank", "Devouring Maw")
    ]
}

def create_stub(folder, file_name, name, race, cls, weapon):
    tier_path = base_path / folder
    tier_path.mkdir(parents=True, exist_ok=True)
    full_path = tier_path / file_name
    
    content = f"""# Vassal: {name}
**Rarity**: {folder}
**Race**: {race}
**Class**: {cls}
**Weapon**: {weapon}

## Lore Fragment
A very powerful and ancient combatant of the Maou's forces.

## Visual Identity
-

## AI Generation & Prompting
- 
"""
    with open(full_path, "w", encoding="utf-8") as f:
        f.write(content)
    print(f"Created {full_path}")

for tier, chars in missing_chars.items():
    for char in chars:
        create_stub(tier, *char)
