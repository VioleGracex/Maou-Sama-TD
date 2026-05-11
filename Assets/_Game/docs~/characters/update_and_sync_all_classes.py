import os
import csv
import re
from pathlib import Path

base_path = Path(r"d:\OuikiDev\Maou-Sama-TD\Assets\_Game\docs~\characters")
csv_path = Path(r"d:\OuikiDev\Maou-Sama-TD\Assets\_Game\docs~\Math_and_Balance\Balancing_PowerGrid.csv")

# Highly-diversified, lore-accurate 12-class assignments for all characters
class_assignments = {
    # Overlord (7)
    "balthazar": "Overlord",
    "asmodeus": "Overlord",
    "lucifer": "Overlord",
    "mammon": "Overlord",
    "belphegor": "Overlord",
    "leviathan": "Overlord",
    "satan": "Overlord",
    
    # Necromancer (5)
    "eidon": "Necromancer",
    "seraphine": "Necromancer",
    "ravenna": "Necromancer",
    "blight": "Necromancer",
    "mort": "Necromancer",
    "grave_digger": "Necromancer",
    
    # Architect (4)
    "pyrrhus": "Architect",
    "hellfire_alchemist": "Architect",
    "lava_bender": "Architect",
    "vesper": "Architect",
    "void_caller": "Architect",
    "malphas": "Architect",
    "abyssal_tactician": "Architect",
    
    # Gunner (6)
    "mordred": "Gunner",
    "drakmora_crossbowman": "Gunner",
    "callum": "Gunner",
    "defected_marksman": "Gunner",
    "eira": "Gunner",
    "frostbane_sniper": "Gunner",
    "korr": "Gunner",
    "skeletal_archer": "Gunner",
    "noire": "Gunner",
    "outcast_archer": "Gunner",
    "dusk": "Gunner",
    "skeleton_bowman": "Gunner",
    
    # Sage (6)
    "nerissa": "Sage",
    "midnight_archivist": "Sage",
    "celia": "Sage",
    "sanctus_traitor": "Sage",
    "caius": "Sage",
    "heretic_priest": "Sage",
    "corvus": "Sage",
    "plague_doctor": "Sage",
    "elowen": "Sage",
    "exiled_cleric": "Sage",
    "vesta": "Sage",
    "corrupt_acolyte": "Sage",
    
    # Warlock (8)
    "lilith": "Warlock",
    "morrigan": "Warlock",
    "banefire_enchantress": "Warlock",
    "azazel": "Warlock",
    "ruined_choirboy": "Warlock",
    "draco": "Warlock",
    "vampiric_aristocrat": "Warlock",
    "sibyl": "Warlock",
    "bloodied_oracle": "Warlock",
    "xylia": "Warlock",
    "scorned_adept": "Warlock",
    "solas": "Warlock",
    "apprentice_cultist": "Warlock",
    "kaelthas": "Warlock",
    "blood_drinker": "Warlock",
    
    # Assassin (13)
    "vladislav": "Assassin",
    "crimson_duke": "Assassin",
    "nyx": "Assassin",
    "phantom_beastkin": "Assassin",
    "lucien": "Assassin",
    "phantom_operative": "Assassin",
    "selene": "Assassin",
    "stray_hunter": "Assassin",
    "drusilla": "Assassin",
    "seductive_spy": "Assassin",
    "lumina": "Assassin",
    "lunar_stalker": "Assassin",
    "feral_alley": "Assassin",
    "viona": "Assassin",
    "shadow_stalker": "Assassin",
    "tika": "Assassin",
    "feline_scout": "Assassin",
    "jax": "Assassin",
    "cursed_bandit": "Assassin",
    "val_lesser": "Assassin",
    "lesser_vampire": "Assassin",
    "vane": "Assassin",
    "camilla": "Assassin",
    "ebon_lady_of_chains": "Assassin",
    "zit": "Assassin",
    "goblin_scout": "Assassin",
    "goblin_assassin": "Assassin",
    
    # Support (6)
    "valerius": "Support",
    "crimson_defector": "Support",
    "lila": "Support",
    "fledgling_succubus": "Support",
    "mina": "Support",
    "charming_novice": "Support",
    "nightmare_weaver": "Support",
    "luna_stray": "Support",
    "stray_kitten": "Support",
    "luna": "Support",
    "cinder": "Support",
    "undead_guard": "Support",
    
    # Executioner (9)
    "astaroth": "Executioner",
    "queen_of_pain": "Executioner",
    "isolde": "Executioner",
    "dusk_bound_reaver": "Executioner",
    "kaelia": "Executioner",
    "cursed_blademaster": "Executioner",
    "kaelen": "Executioner",
    "thresh": "Executioner",
    "wraith_executioner": "Executioner",
    "gnaw": "Executioner",
    "feral_ghoul": "Executioner",
    "verm": "Executioner",
    "plague_rat": "Executioner",
    "fenris": "Executioner",
    "alpha_of_the_north": "Executioner",
    "anubis": "Executioner",
    "tomb_guardian": "Executioner",
    "gladiator": "Executioner",
    "rune_scarred_gladiator": "Executioner",
    
    # Ranger (9)
    "aquila": "Ranger",
    "harpy_queen": "Ranger",
    "shade": "Ranger",
    "phantom_stalker": "Ranger",
    "zephyra": "Ranger",
    "storm_bound_duchess": "Ranger",
    "lyra": "Ranger",
    "blood_moon_sentinel": "Ranger",
    "zephyr": "Ranger",
    "wind_rider": "Ranger",
    "skye": "Ranger",
    "lesser_harpy": "Ranger",
    "fang": "Ranger",
    "wolf_pup": "Ranger",
    "skyra": "Ranger",
    "nightwing_harpy": "Ranger",
    "talon": "Ranger",
    "sky_piercer": "Ranger",
    
    # Bastion (11)
    "malina": "Bastion",
    "ignis_guard": "Bastion",
    "azalea": "Bastion",
    "scarlet_viceroy": "Bastion",
    "gorm": "Bastion",
    "abyssal_dreadnought": "Bastion",
    "vex": "Bastion",
    "skeletal_warden": "Bastion",
    "grim": "Bastion",
    "bone_juggernaut": "Bastion",
    "tarkus": "Bastion",
    "reanimated_shieldbearer": "Bastion",
    "karrow": "Bastion",
    "skeleton_guard": "Bastion",
    "gloo": "Bastion",
    "swamp_slime": "Bastion",
    "behemoth": "Bastion",
    "infernal_behemoth": "Bastion",
    
    # Vanguard (15)
    "victor": "Vanguard",
    "fallen_paladin": "Vanguard",
    "toros": "Vanguard",
    "savage_packleader": "Vanguard",
    "alaric": "Vanguard",
    "dusk_knight": "Vanguard",
    "lupus": "Vanguard",
    "howling_marauder": "Vanguard",
    "ulf": "Vanguard",
    "armored_dire_wolf": "Vanguard",
    "fyr": "Vanguard",
    "ember_bruiser": "Vanguard",
    "kaldor": "Vanguard",
    "drakmora_infantry": "Vanguard",
    "skeleton_militia": "Vanguard",
    "aria": "Vanguard",
    "renegade_peasant": "Vanguard",
    "elias": "Vanguard",
    "forsaken_conscript": "Vanguard",
    "stubbs": "Vanguard",
    "zombie_footman": "Vanguard",
    "grog": "Vanguard",
    "demonized_thug": "Vanguard",
    "varkas": "Vanguard",
    "drakmora_lancer": "Vanguard",
    "thug": "Vanguard",
    "abyssal_dreadknight": "Vanguard"
}

# Sort key mappings descending by length to ensure precise substring/prefix matching
sorted_assignments = sorted(class_assignments.items(), key=lambda x: len(x[0]), reverse=True)

def get_assigned_class(name_or_file):
    s = str(name_or_file).lower().replace("-", "_").replace(" ", "_")
    for key, val in sorted_assignments:
        if key in s:
            return val
    # Fallbacks based on common words
    if "crossbow" in s or "archer" in s or "sniper" in s or "bowman" in s:
        return "Ranger"
    if "cleric" in s or "priest" in s or "acolyte" in s:
        return "Sage"
    if "alchemist" in s:
        return "Architect"
    if "summoner" in s or "lich" in s:
        return "Necromancer"
    if "shield" in s or "guard" in s or "warden" in s or "dreadnought" in s or "behemoth" in s:
        return "Bastion"
    if "infantry" in s or "militia" in s or "knight" in s or "bruiser" in s or "thug" in s or "footman" in s:
        return "Vanguard"
    if "stalker" in s or "assassin" in s or "scout" in s or "spy" in s:
        return "Assassin"
    if "witch" in s or "wizard" in s or "enchantress" in s or "warlock" in s or "oracle" in s:
        return "Warlock"
    return "Vanguard" # ultimate fallback

print("1. Updating Balancing_PowerGrid.csv...")
rows = []
with open(csv_path, "r", encoding="utf-8") as f:
    reader = csv.DictReader(f)
    fieldnames = reader.fieldnames
    for row in reader:
        name = row["Name"]
        file_name = row["File"]
        assigned_class = get_assigned_class(name) or get_assigned_class(file_name)
        row["Class"] = assigned_class
        rows.append(row)

with open(csv_path, "w", encoding="utf-8", newline="") as f:
    writer = csv.DictWriter(f, fieldnames=fieldnames)
    writer.writeheader()
    writer.writerows(rows)
print(f"Successfully updated {len(rows)} rows in CSV with correct classes.")

print("\n2. Updating individual .md files in docs~/characters...")
updated_md_count = 0
for root, dirs, files in os.walk(base_path):
    if "Archive" in root:
        continue
    for file in files:
        if file.endswith(".md") and file != "CHARACTERS_MASTER_TIER_LIST.md" and file != "CHARACTERS_MASTER_TIER_LIST_archive.md" and file != "RACE_VISUAL_GUIDE.md" and file != "Tina_Profile.md":
            path = Path(root) / file
            with open(path, "r", encoding="utf-8") as f:
                content = f.read()
                
            name_match = re.search(r"# Vassal:\s*(.*)", content)
            name = name_match.group(1).strip() if name_match else file
            
            assigned_class = get_assigned_class(name) or get_assigned_class(file)
            
            # Replace Class line
            new_content = re.sub(r"\*\*Class\*\*:\s*.*", f"**Class**: {assigned_class}", content)
            
            # Also update Class inside the general prompt or details
            class_match = re.search(r"\*\*Class\*\*:\s*(.*)", content)
            if class_match:
                old_class = class_match.group(1).strip()
                if old_class != assigned_class:
                    new_content = new_content.replace(old_class, assigned_class)
            
            with open(path, "w", encoding="utf-8") as f:
                f.write(new_content)
            updated_md_count += 1

print(f"Successfully updated {updated_md_count} individual markdown profiles.")

print("\n3. Updating CHARACTERS_MASTER_TIER_LIST.md...")
tier_list_path = base_path / "CHARACTERS_MASTER_TIER_LIST.md"
with open(tier_list_path, "r", encoding="utf-8") as f:
    tier_list_content = f.read()

lines = tier_list_content.splitlines()
new_lines = []
current_vassal_name = None

for line in lines:
    updated_line = line
    # Detect Balthazar style start of a block
    m_block = re.search(r"^\d+\.\s+\*\*([^*]+)\*\*", line)
    if m_block:
        current_vassal_name = m_block.group(1).split(",")[0].strip()
        
    # Detect inline like: 1. **Nerissa, Midnight Archivist** (Trueborn Demon / Sage)
    m_inline = re.search(r"^\d+\.\s+\*\*([^*]+)\*\*\s+\(([^/]+)\s*/\s*([^)]+)\)", line)
    if m_inline:
        vassal_name = m_inline.group(1).split(",")[0].strip()
        race = m_inline.group(2).strip()
        assigned = get_assigned_class(vassal_name)
        updated_line = re.sub(
            r"(\d+\.\s+\*\*([^*]+)\*\*\s+\()([^/]+)\s*/\s*([^)]+)(\))",
            rf"\1\3 / {assigned}\5",
            line
        )
    elif current_vassal_name:
        # Detect block class: - **Race/Class**: Trueborn Demon / Warlock
        m_class = re.search(r"^(\s*-\s+\*\*Race/Class\*\*:\s+)([^/]+)\s*/\s*(.*)", line)
        if m_class:
            prefix = m_class.group(1)
            race = m_class.group(2).strip()
            assigned = get_assigned_class(current_vassal_name)
            updated_line = f"{prefix}{race} / {assigned}"
            
    new_lines.append(updated_line)

result_content = "\n".join(new_lines)

# Write the final file
with open(tier_list_path, "w", encoding="utf-8") as f:
    f.write(result_content)
print("Successfully updated CHARACTERS_MASTER_TIER_LIST.md with the balanced roster.")
