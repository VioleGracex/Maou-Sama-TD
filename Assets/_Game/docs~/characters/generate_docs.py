import os
import shutil
import re

base_path = r"d:\OuikiDev\Maou-Sama-TD\Assets\_Game\docs~\characters"
archive_path = os.path.join(base_path, "Archive", "Pre-Chibi-Overhaul")

# Create archive directory
if not os.path.exists(archive_path):
    os.makedirs(archive_path)

# Move existing folders to archive
folders_to_archive = ["UR", "SSR", "SR", "R", "UC", "Common"]
for folder in folders_to_archive:
    src = os.path.join(base_path, folder)
    if os.path.exists(src):
        dst = os.path.join(archive_path, folder)
        if os.path.exists(dst):
            shutil.rmtree(dst)
        shutil.move(src, dst)

# Read the tier list
tier_list_file = os.path.join(base_path, "CHARACTERS_MASTER_TIER_LIST.md")
with open(tier_list_file, "r", encoding="utf-8") as f:
    content = f.read()

# Regex to find sections and characters
# e.g., ## Ultra Rare (UR) — 2 Characters
sections = re.split(r'## (.*?) \((.*?)\)(?: \/ Common)? [—\-] \d+ Characters', content)

# Remove the first part containing standard Lore clarification
sections = sections[1:]

def sanitize_filename(name):
    # e.g. Balthazar, Lord of Sloth -> 01_balthazar_lord_of_sloth.md
    s = str(name).lower()
    s = re.sub(r'[^a-zA-Z0-9\s-]', '', s)
    s = s.replace(' ', '_').replace('-', '_')
    s = re.sub(r'_+', '_', s)
    return s

current_tier_name = ""
current_tier_abbr = ""

print(f"Sections extracted: {len(sections)}")

for i in range(0, len(sections), 3):
    tier_name = sections[i].strip()
    tier_abbr = sections[i+1].strip()
    tier_data = sections[i+2]
    
    if "Normal" in tier_name:
        folder_name = "Common"
    else:
        folder_name = tier_abbr
        
    out_dir = os.path.join(base_path, folder_name)
    os.makedirs(out_dir, exist_ok=True)
    
    print(f"Processing tier: {tier_abbr} / {folder_name}")
    
    # Extract characters
    # e.g., 1. **Balthazar, Lord of Sloth**
    #        - **Race/Class**: Trueborn Demon / Warlock
    #        - **Weapon**: Floating Grimoire of the Abyss
    #        - **Lore**: Oldest of the Demon Lords...
    characters = re.findall(r'\d+\.\s+\*\*(.*?)\*\*\s+-\s+\*\*Race/Class\*\*:\s+(.*?)\s+/\s+(.*?)\s+-\s+\*\*Weapon\*\*:\s+(.*?)\s*(?:-\s+\*\*Lore\*\*:\s+(.*?))?(?=\n\d+\.|\n*$|\n---)', tier_data, re.DOTALL)
    
    # Or for simple ones like: 1. **Ignis Guard Captain** (Trueborn Demon / Vanguard) - *Weapon: Longsword*
    characters_simple = re.findall(r'\d+\.\s+\*\*(.*?)\*\*\s+\((.*?)\s+/\s+(.*?)\)\s+-\s+\*Weapon:\s+(.*?)\*', tier_data, re.DOTALL)
    
    count = 1
    
    if len(characters) > 0:
        for char in characters:
            name, race, cls, weapon, lore = char
            lore = lore.strip() if lore else "A loyal soldier of the Maou's army."
            filename = f"{count:02d}_{sanitize_filename(name)}.md"
            
            prompt = f"chibi style, white background, standalone character design, 1:1 aspect ratio. {name}, {race} {cls}, holding {weapon}. Elite fantasy character, highly detailed armor, vivid colors, very cute big head small body chibi proportions, red and black accents, anime style, 8k resolution, masterpiece."
            
            md = f"""# Vassal: {name}
**Rarity**: {folder_name}
**Race**: {race}
**Class**: {cls}

## Lore Fragment
{lore}

## Visual Identity
- **Build**: Chibi proportions, big head, small body
- **Style**: High fantasy, elite armor, red and black accents
- **Unique**: {weapon}

## AI Generation & Prompting
- **Gender**: Any
- **Weapon**: {weapon}
- **General Prompt**: {prompt}
"""
            with open(os.path.join(out_dir, filename), "w") as f:
                f.write(md)
            count += 1
            
    if len(characters_simple) > 0:
        for char in characters_simple:
            name, race, cls, weapon = char
            filename = f"{count:02d}_{sanitize_filename(name)}.md"
            
            prompt = f"chibi style, white background, standalone character design, 1:1 aspect ratio. {name}, {race} {cls}, holding {weapon}. Fantasy soldier, vivid colors, very cute big head small body chibi proportions, red and black accents, anime style, 8k resolution, masterpiece."
            
            md = f"""# Vassal: {name}
**Rarity**: {folder_name}
**Race**: {race}
**Class**: {cls}

## Lore Fragment
A loyal combatant in the Maou's forces holding the rank of {folder_name}.

## Visual Identity
- **Build**: Chibi proportions, big head, small body
- **Style**: Army uniform, solid fantasy armor, standardized
- **Unique**: {weapon}

## AI Generation & Prompting
- **Gender**: Any
- **Weapon**: {weapon}
- **General Prompt**: {prompt}
"""
            with open(os.path.join(out_dir, filename), "w") as f:
                f.write(md)
            count += 1
