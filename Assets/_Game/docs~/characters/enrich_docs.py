import os
import re
import random

base_path = r"d:\OuikiDev\Maou-Sama-TD\Assets\_Game\docs~\characters"
folders = ["UR", "SSR", "SR", "R", "UC", "Common"]

# Generators for features
hair_colors = ["Crimson", "Snow White", "Obsidian Black", "Silver", "Blonde", "Amethyst", "Sapphire Blue", "Ashen Grey", "Emerald Green", "Rose Pink"]
hair_lengths = ["Long flowing", "Short bob", "Twin tails", "Messy spikes", "Neatly braided", "Shoulder-length", "Tied up in a ponytail", "Waist-length straight", "Wild and untamed", "Slicked back"]
eye_colors = ["Ruby Red", "Gold", "Violet", "Ice Blue", "Emerald", "Abyssal Black", "Silver", "Heterochromia (Red/Blue)", "Glowing Yellow", "Cat-like Green"]
skin_tones = ["Pale", "Porcelain", "Sun-kissed", "Ashy Gray", "Deep Bronze", "Dusk", "Alabaster", "Copper", "Onyx", "Crimson-tinted"]
nail_styles = ["Sharp black talons", "Polished red nails", "Natural", "Pointed claws", "Chipped and worn", "Elegant purple polish", "Reinforced metallic tips", "Silver-painted nails", "Beastly claws", "Clean and trim"]

def get_race_specific_traits(race):
    race = race.lower()
    if "demon" in race:
        return "Prominent demonic horns, spade-tipped tail, subtle glowing runic tattoos."
    elif "succubus" in race:
        return "Curved horns, leathery bat wings, heart-shaped tail tip, alluring aura."
    elif "vampire" in race:
        return "Prominent fangs, elongated ears, extremely pale skin."
    elif "werewolf" in race:
        return "Fluffy wolf ears, thick bushy tail, sharp teeth, primal aura."
    elif "beastkin" in race:
        return "Feline ears, twin cat tails, whiskers, agile posture."
    elif "harpy" in race:
        return "Feathered wings along the arms, talon-like legs, feather hair ornaments."
    elif "lich" in race:
        return "Skeletal features, glowing socket eyes, floating magical aura, decaying bandages."
    elif "undead" in race or "skeleton" in race:
        return "Exposed bone, stitched skin, hollow eyes, decaying form."
    else:
        return "Dark aura, corrupted flesh, spiked accessories."

for folder in folders:
    folder_path = os.path.join(base_path, folder)
    if not os.path.exists(folder_path): continue
    
    for filename in os.listdir(folder_path):
        if not filename.endswith(".md"): continue
        file_path = os.path.join(folder_path, filename)
        
        with open(file_path, "r", encoding="utf-8", errors="ignore") as f:
            content = f.read()
            
        m_name = re.search(r'# Vassal: (.*)', content)
        m_race = re.search(r'\*\*Race\*\*: (.*)', content)
        m_cls = re.search(r'\*\*Class\*\*: (.*)', content)
        m_wpn = re.search(r'\*\*Weapon\*\*: (.*)', content)
        if not m_wpn:
             m_wpn = re.search(r'- \*\*Unique\*\*: (.*)', content)
             
        if not (m_name and m_race and m_cls): continue
        
        name = m_name.group(1).strip()
        race = m_race.group(1).strip()
        cls = m_cls.group(1).strip()
        weapon = m_wpn.group(1).strip() if m_wpn else "Standard weapon"
        
        # Determine specific predefined traits if known
        if "Lilith" in name:
            hc, hl, ec, st, ns = "Blonde", "Long flowing", "Glowing Violet", "Creamy Pale", "Painted purple nails"
        elif "Balthazar" in name:
            hc, hl, ec, st, ns = "White", "Messy spikes", "Gold", "Pale", "Sharp black talons"
        else:
            hc = random.choice(hair_colors)
            hl = random.choice(hair_lengths)
            ec = random.choice(eye_colors)
            st = random.choice(skin_tones)
            ns = random.choice(nail_styles)
            
        race_traits = get_race_specific_traits(race)
        
        new_prompt = f"chibi style, white background, standalone character design, 1:1 aspect ratio. {name}, {race} {cls}, holding {weapon}. {hl} {hc} hair, {ec} eyes, {st} skin, {ns}, {race_traits}. Elite fantasy character, highly detailed armor, vivid colors, very cute big head small body chibi proportions, red and black accents, anime style, 8k resolution, masterpiece."
        
        lore_match = re.search(r'## Lore Fragment\n(.*?)\n\n## Visual', content, re.DOTALL)
        lore = lore_match.group(1).strip() if lore_match else "A loyal combatant."

        md = f"""# Vassal: {name}
**Rarity**: {folder}
**Race**: {race}
**Class**: {cls}

## Lore Fragment
{lore}

## Visual Identity
- **Build**: Chibi proportions, big head, small body
- **Hair**: {hl} {hc} hair
- **Eyes**: {ec}
- **Skin**: {st}
- **Nails**: {ns}
- **Distinguishing Details**: {race_traits}
- **Style**: High fantasy armor, red and black accents
- **Weapon**: {weapon}

## AI Generation & Prompting
- **Gender**: Any
- **Weapon**: {weapon}
- **General Prompt**: {new_prompt}
"""
        with open(file_path, "w", encoding="utf-8") as f:
            f.write(md)
            
print("Successfully enriched 80 files with detailed visual identities!")
