import os
import re
import shutil

base_dir = r"d:\OuikiDev\Maou-Sama-TD\Assets\_Game\docs~\characters"
folders = ["N_Tier", "R_Tier", "SR_Tier", "SSR_Tier", "Kingdom_Lore"]

# 1. Create folders
for folder in folders:
    os.makedirs(os.path.join(base_dir, folder), exist_ok=True)

# 2. Kingdom Lore Data
map_content = """# The Abyssal Dominion of Gehenna

Gehenna is the vast, dark kingdom ruled by the Maou. It is divided into several unique territories, each governed by different factions of the Demon Army.

## Territories & Terrain

### 1. The Shadow Sanctum (Central)
The capital and seat of the Maou's power. It is an imposing environment characterized by jagged obsidian spires, harsh volcanic vents, and dark mana wells that glow with eerie energy. The Royal Guard and high-ranking Trueborn Demons reside here.

### 2. The Frostbound Pines (North)
A punishing tundra covered in perpetual snowstorms and frozen lakes. Giant, mutated evergreens form dense forests that serve as the hunting grounds for Shifters and Ice-beasts. The harsh survivalist culture here breeds the toughest warriors in the army.

### 3. The Deep Vaults (Underground)
A subterranean network of massive caverns stretching beneath the continent. It features rivers of magma, glowing crystal formations, and ancient ruins. It is the industrial heart of Gehenna, populated by Corrupted Dwarves, Dark Elves, and Construct builders who forge the legion's weapons.

### 4. The Ashen Steppes (East)
Endless, wind-swept grey plains where the ashes of ancient holy crusades still settle. It is a lawless and chaotic zone, primarily serving as the staging ground for mercenary encampments, roaming Undead, and the Demon Apostles (formerly humans).

### 5. The Crimson Coast (West)
Blood-red sands meeting a treacherous, black-water ocean. It is the territory of naval powers, deep-sea familiars, and privateer lords who secure the Dominion's borders against the human republic's armadas.
"""

with open(os.path.join(base_dir, "Kingdom_Lore", "Map_And_Territories.md"), "w", encoding="utf-8") as f:
    f.write(map_content)

# 3. Rename Human Betrayers
race3_file = os.path.join(base_dir, "CHARACTERS_RACE3_HUMAN_BETRAYERS.md")
race3_new = os.path.join(base_dir, "CHARACTERS_RACE3_DEMON_APOSTLES.md")
if os.path.exists(race3_file):
    with open(race3_file, 'r', encoding='utf-8') as f:
        content = f.read().replace("Human Betrayers", "Demon Apostles").replace("Human Betrayer", "Demon Apostle")
    with open(race3_new, 'w', encoding='utf-8') as f:
        f.write(content)
    os.remove(race3_file)
    meta_old = race3_file + ".meta"
    if os.path.exists(meta_old):
        os.remove(meta_old)

# Character data dictionary
# We assign them tiers.
# N = 21, 23, 24, 25, 28, 29, 30, 31 (Generic names/low impact)
# R = 14, 15, 16, 17, 18, 19, 20
# SR = 07, 08, 10, 11, 12, 13, 22, 26, 27
# SSR = 01, 02, 03, 04, 05, 06, 09

updates = {
    # SSR (Generals / High Lords - already good, we just add build)
    "05": {"tier": "SSR_Tier", "build": "Athletic and aerodynamic", "weapon": "Storm-caller Greatbow", "lore": "Lord of the Cloud-Spire in the high mountains. She views the surface world with a mix of pity and fascination. As an SSR General, she commands the aerial forces of Gehenna."},
    "06": {"tier": "SSR_Tier", "build": "Lithesome and lethal", "weapon": "Obsidian Scythe", "lore": "The Citadel's chief executioner and an SSR Overlord. She is tasked with 'cleaning' any insubordination within the demon ranks, acting as the Maou's shadow blade."},
    "09": {"tier": "SSR_Tier", "build": "Imposing and rotund", "weapon": "Demonic Grimoire", "lore": "One of the original SSR Demon Lords. He manages the Citadel's immense budget, claiming that ledgers and taxes are more exhausting than fighting a holy crusade."},

    # SR (Territory Lords / Elite Commanders)
    "07": {"tier": "SR_Tier", "build": "Massive towering physique", "weapon": "Earth-shatter Mace", "lore": "An elite SR Commander of the Terra-Golems in the Deep Vaults. Atlas was carved from the bedrock of Gehenna itself to defend the lower passages."},
    "08": {"tier": "SR_Tier", "build": "Mechanized armor plating", "weapon": "Hex-Tech Cannon", "lore": "A prototype siege construct designed by the Deep Vault dwarves. As an SR tier siege unit, Bale provides heavy artillery support to front-line commanders."},
    "10": {"tier": "SR_Tier", "build": "Stout, heavily muscled and tattooed", "weapon": "Hell-forged Waraxe", "lore": "SR Warchief of the Northern Frostbound Clans. He only joined the Legion after the Maou defeated him in single combat. He respects strength above all."},
    "11": {"tier": "SR_Tier", "build": "Curvaceous and elegant", "weapon": "Dual Blood-Rapiers", "lore": "A High-Vampire noble holding an SR rank in the Ashen Steppes. She commands the undead cavalry and refuses to drink anything but royal blood."},
    "12": {"tier": "SR_Tier", "build": "Hulking Lupin-hybrid", "weapon": "Crescent Moon Claws", "lore": "Alpha of the Frostbound Shifters. An SR commander who leads from the front lines, tearing through enemy fortifications with his bare claws."},
    "13": {"tier": "SR_Tier", "build": "Slavishly skeletal yet regal", "weapon": "Soul-gem Staff", "lore": "An ancient Lich Lord of the Ashen Steppes. Earning SR status through her mastery of necromancy, she can revive fallen footmen to endlessly serve the Maou."},
    "22": {"tier": "SR_Tier", "build": "Tall, sharp angles, glowing eyes", "weapon": "Shadow-weave Whip", "lore": "The SR warden of the Shadow Sanctum's dungeons. Morrigan ensures that captured inquisitors provide valuable intelligence before their demise."},
    "26": {"tier": "SR_Tier", "build": "Looming and gaunt", "weapon": "Necrotic Halberd", "lore": "An SR Reaper who guides the souls of fallen demons to the Void Well. He takes great pleasure in ensuring human paladins never find their paradise."},
    "27": {"tier": "SR_Tier", "build": "Ethereal, floating crystalline body", "weapon": "Void Orb", "lore": "An SR Cosmic Spirit that slipped into Aethelgard. Void serves the Maou out of sheer curiosity, manipulating gravity to crush entire human squads."},
    
    # R (Squad Leaders / Mercenaries)
    "14": {"tier": "R_Tier", "build": "Lean and scaled", "weapon": "Dragon-fang Spear", "lore": "An R-rank mercenary captain from the Crimson Coast. Kael's draconic blood makes him highly resistant to fire magic, leading the vanguard charges."},
    "15": {"tier": "R_Tier", "build": "Stocky and disciplined", "weapon": "Iron Katana", "lore": "An Interloper from another world who found himself in the Demon Army. Now an R-tier squad leader, Kenji trains the footmen in foreign military tactics."},
    "16": {"tier": "R_Tier", "build": "Broad-shouldered and mane-haired", "weapon": "Lion-heart Broadsword", "lore": "A Beastkin mercenary. With his R-tier status, Leo leads a shock-troop division in the Frostbound Pines, known for their terrifying war-roars."},
    "17": {"tier": "R_Tier", "build": "Petite yet menacing", "weapon": "Poison Daggers", "lore": "An R-tier assassin operating in the Trade Republic's shadows. Lucia ensures that merchants who try to cheat the Demon Kingdom meet sudden, tragic accidents."},
    "18": {"tier": "R_Tier", "build": "Translucent and shifting", "weapon": "Ghost-blade", "lore": "A phantom R-tier scout who patrols the Ashen Steppes. Lucien exists between dimensions, ambushing holy scouts before they can report back."},
    "19": {"tier": "R_Tier", "build": "Feral and agile", "weapon": "Spiked Gauntlets", "lore": "An R-tier berserker of the Shifter clans. Lupa cares nothing for kingdom politics, only for the thrill of hunting down Holy Knights in the forests."},
    "20": {"tier": "R_Tier", "build": "Boxy and crude", "weapon": "Rusted Anchor", "lore": "An R-tier construct pulled from the Crimson Coast's shipwrecks. Magnus is slow but serves as an unbreakable wall against coastal blockades."},

    # N (Footmen / Normal Army)
    "21": {"tier": "N_Tier", "build": "Average footman build", "weapon": "Standard Demon Pike", "lore": "An N-tier infantry demon serving in the Shadow Sanctum's regular army. Malina dreams of one day earning enough prestige to become a squad leader."},
    "23": {"tier": "N_Tier", "build": "Tiny and fragile", "weapon": "Small Spellbook", "lore": "A generic N-tier Fae familiar. Oberon is primarily used for relaying messages across the battlefield and casting minor distraction spells."},
    "24": {"tier": "N_Tier", "build": "Slender with clipped wings", "weapon": "Old Shortsword", "lore": "An N-tier Fallen Angel forced into infantry service. Raphael battles on the front lines, desperate to prove his worth to the Maou's legion."},
    "25": {"tier": "N_Tier", "build": "Standard ranger build", "weapon": "Crossbow", "lore": "Once a human hunter, Selene is now an N-tier Demon Apostle. She patrols the borders of the Ashen Steppes as a basic ranged foot-soldier."},
    "28": {"tier": "N_Tier", "build": "Sturdy shield-bearer", "weapon": "Iron Tower Shield", "lore": "An N-tier heavy infantry demon. Hilda stands in the first rank of the Phalanx, absorbing incoming fire from the Ironclad Union."},
    "29": {"tier": "N_Tier", "build": "Thin, hunched caster", "weapon": "Wooden Staff", "lore": "An N-tier apprentice mage. Nerissa provides basic fireball support from the backlines, hoping to avoid the attention of enemy assassins."},
    "30": {"tier": "N_Tier", "build": "Muscular bruiser", "weapon": "Iron Club", "lore": "An N-tier shock trooper. Thorne relies entirely on brute strength, leading the Vanguard rushes against heavily armored Crusader positions."},
    "31": {"tier": "N_Tier", "build": "Lightweight scout", "weapon": "Hunting Bow", "lore": "An N-tier scout and Demon Apostle. Vail traverses the Frostbound Pines, setting up minor traps to slow down human incursions."}
}

for filename in os.listdir(base_dir):
    if not filename.endswith('.md') or not filename[0].isdigit():
        continue
        
    num = filename.split('_')[0]
    filepath = os.path.join(base_dir, filename)
    
    tier_info = updates.get(num, None)
    
    # Defaults for 01-04
    tier_folder = "SSR_Tier"
    if tier_info:
        tier_folder = tier_info['tier']
        
    new_filepath = os.path.join(base_dir, tier_folder, filename)
    
    # Move the file
    with open(filepath, 'r', encoding='utf-8') as f:
        content = f.read()
    
    # Process modifications if any
    if tier_info:
        # 1. Add Build
        if "- **Build**:" not in content and "## Visual Identity" in content:
            content = content.replace("## Visual Identity\n", f"## Visual Identity\n- **Build**: {tier_info['build']}\n")
            
        # 2. Update Weapons
        content = re.sub(r'- \*\*Weapon\*\*: .*', f'- **Weapon**: {tier_info["weapon"]}', content)
        
        # 3. Update Lore
        parts = content.split("## AI Generation")
        if len(parts) == 2:
            lore_section, ai_section = parts[0], "## AI Generation" + parts[1]
            # Replace lore paragraph
            lore_section = re.sub(r'(## Lore Fragment\n)(.*?)(\n##|$)', lambda m: f"{m.group(1)}{tier_info['lore']}\n\n{m.group(3)}", lore_section, flags=re.DOTALL)
            content = lore_section + ai_section

        # 4. Human Betrayer -> Demon Apostle
        content = content.replace("Human Betrayer", "Demon Apostle").replace("Human Betrayers", "Demon Apostles")
        
        # 5. Fix prompt
        if "## AI Generation" in content:
            gender = re.search(r'- \*\*Gender\*\*: (.*)', content)
            gender_val = gender.group(1).strip() if gender else ''
            prompt_line = f"1boy, male" if "male" in gender_val.lower() else ("1girl, female" if "female" in gender_val.lower() else "")
            
            # extract features etc
            features = re.search(r'- \*\*Features\*\*: (.*)', content)
            f_val = features.group(1).strip() if features else ''
            
            new_prompt = f"{prompt_line}, {tier_info['build']}, {f_val}, holding {tier_info['weapon']}"
            new_prompt = new_prompt.replace(", ,", ",").strip(", ")
            
            content = re.sub(r'- \*\*General Prompt\*\*: .*', f'- **General Prompt**: {new_prompt}', content)

    # Write to new destination
    with open(new_filepath, 'w', encoding='utf-8') as f:
        f.write(content)
        
    # delete old file
    os.remove(filepath)
    meta_path = filepath + ".meta"
    if os.path.exists(meta_path):
        os.remove(meta_path) # We just delete metas, Unity will regenerate them correctly for the new files.

print("Restructuring complete!")
