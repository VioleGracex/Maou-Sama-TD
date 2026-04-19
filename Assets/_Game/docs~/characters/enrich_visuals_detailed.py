import os
import re
import random
import hashlib
from pathlib import Path

base_path = Path(r"d:\OuikiDev\Maou-Sama-TD\Assets\_Game\docs~\characters")
folders = ["UR", "SSR", "SR", "R", "UC", "Common"]

def seed_from_string(s):
    return int(hashlib.md5(s.encode('utf-8')).hexdigest(), 16) % (10**8)

# -- Compressed Dictionaries --

hair_colors = [
    "crimson red with black lowlights", 
    "pure silver", 
    "obsidian black, faint purple sheen", 
    "platinum blonde", 
    "violet amethyst roots", 
    "sapphire blue", 
    "ashen grey and white", 
    "emerald green with jade", 
    "rose pink",
    "rust red"
]

hair_lengths = [
    "long waving hair", 
    "short asymmetric bob", 
    "messy twin tails", 
    "feral spiky hair", 
    "intricately braided hair", 
    "straight shoulder-length blunt cut", 
    "high flowing ponytail", 
    "waist-length silky hair", 
    "wild untamed mane", 
    "sleek slicked back hair"
]

eye_details = [
    "intense ruby eyes, slit pupils", 
    "solid gold eyes", 
    "hypnotic violet eyes", 
    "icy blue eyes", 
    "predatory emerald eyes", 
    "lifeless abyssal black eyes", 
    "metallic silver eyes", 
    "heterochromia red and blue eyes", 
    "pale yellow cat eyes", 
    "narrowed cat-like green eyes"
]

skin_textures = [
    "pale porcelain skin", 
    "tanned lightly scarred skin", 
    "alabaster smooth skin", 
    "copper weathered skin", 
    "fair smooth skin",
    "warm sun-kissed skin",
    "warm beige skin",
    "pale ivory skin"
]

clothing_colors = ["Obsidian/Silver", "Crimson/Gold", "Purple/Void Black", "Grey/Neon Cyan", "Midnight Blue/Violet", "Green/Copper", "Gold/Ruby", "Bone/Rust"]

clothing_materials = [
    "tight leather armor, asymmetric spiked pauldrons", 
    "heavy steel plate, reinforced cuirass, heavy gauntlets", 
    "fine silk robes, long draping sleeves", 
    "heavy canvas mercenary gear, iron chest-plate", 
    "thick fur-lined gear, metal-reinforced boots", 
    "polished scale mail, tattered tabard", 
    "velvet aristocratic attire, integrated chestplate", 
    "boiled-leather chest piece, chainmail underlay", 
    "bone-carved macabre armor", 
    "sleek assassin's cloth, dark hood"
]

armor_scribes = [
    "intricate runic scribes",
    "smooth unadorned surfaces",
    "heavily dented battle-scarred rusted",
    "polished with ornate floral etchings",
    "covered in jagged spikes and scrawls",
    "flawless geometric metal etchings"
]

weapon_colors = ["Dark Iron", "Polished Silver", "Blood Red", "Ghostly Cyan", "Demonic Purple", "Rusted Brown"]
weapon_materials = [
    "dense dark steel", 
    "curved dark wood, intricate strings", 
    "sharp obsidian volcanic glass", 
    "ornate glittering gold and steel", 
    "heavy studded iron", 
    "petrified ancient wood, dark rivets", 
    "deadwood staff, dark gemstone", 
    "mechanical brass alloys", 
    "thick blackened bronze", 
    "light-weight silver metal, dark wood"
]

def deduce_gender(name):
    name_lower = name.lower()
    if any(x in name_lower for x in ["queen", "duchess", "empress", "lady", "succubus", "harpy", "witch", "girl", "daughter", "maid", "sister", "princess", "nyx", "shade", "lilith", "camilla", "nerissa", "zephyra", "seraphine", "isolde", "azalea", "lyra", "drusilla", "selene", "celia", "malina", "aquila"]):
        return "female"
    elif any(x in name_lower for x in ["king", "duke", "emperor", "lord", "boy", "son", "brother", "prince", "balthazar", "eidon", "vladislav", "fenris", "victor", "valerius", "lucien", "toros", "azazel", "gorm", "kaelen", "magnus"]):
        return "male"
    else:
         rng = random.Random(seed_from_string(name + "gender"))
         return rng.choice(["female", "male"])

def get_race_specific_traits(race):
    race = race.lower()
    if "succubus" in race:
        return "succubus horns, bat wings, heart tail"
    elif "vampire" in race:
        return "vampire fangs, pointed ears, pale skin"
    elif "werewolf" in race:
        return "wolf ears, bushy tail, sharp teeth, beastkin"
    elif "beastkin" in race:
        return "feline ears, cat tail, whiskers, beastkin"
    elif "harpy" in race:
        return "harpy feathered wings, talon legs"
    elif "demon" in race:
        return "demon horns, runic tattoos"  # NO TAIL FOR DEMONS
    elif "lich" in race:
        return "lich, skull face, dark aura, bandages"
    elif "undead" in race or "skeleton" in race:
        return "undead skeleton, exposed bone, stitched skin, sunken eyes"
    else:
        return "humanoid dark fantasy aura"

def process_file(file_path):
    with open(file_path, "r", encoding="utf-8", errors="ignore") as f:
        content = f.read()

    m_name = re.search(r'# Vassal: (.*)', content)
    m_race = re.search(r'\*\*Race\*\*: (.*)', content)
    m_cls = re.search(r'\*\*Class\*\*: (.*)', content)
    m_wpn = re.search(r'\*\*Weapon\*\*: (.*)', content)
    if not m_wpn:
         m_wpn = re.search(r'- \*\*Unique\*\*: (.*)', content)
         
    if not (m_name and m_race and m_cls): return False
    
    name = m_name.group(1).strip()
    race = m_race.group(1).strip()
    cls = m_cls.group(1).strip()
    base_wpn = m_wpn.group(1).strip() if m_wpn else "Standard weapon"
    
    if len(base_wpn) > 50:
        base_wpn = "unique weapon"
    
    rng = random.Random(seed_from_string(name))
    
    is_undead = ("skeleton" in race.lower() or "undead" in race.lower() or "lich" in race.lower() or "ghoul" in race.lower() or "zombie" in race.lower())
    is_drakmora = "drakmora" in name.lower()

    if is_drakmora:
        gender_str = "male"
        h_str = "hidden behind helmet"
        e_str = "red eyes visible through helmet slits"
        s_str = "fully covered by armor"
        c_col = "Obsidian Black with Red Accents"
        c_mat = "full-body heavy black steel plate armor, matching black gauntlets and sabatons. Wearing a distinct completely black helmet featuring a vertical T-visor and two large curved demonic horns protruding from the top. A bright red crown-like kingdom emblem is painted prominently on the chest plate"
        c_scribe = "clean military uniform design, highly standardized"
        w_col = "Dark Iron/Black"
        w_mat = "dense dark steel and black iron"
        race_traits = "imposing military posture, standardized Drakmora legionary"
    else:
        gender_str = deduce_gender(name)
        if is_undead:
            h_str = "bald, no hair"
            e_str = "empty black eye sockets, piercing pinprick eyes"
            if "skeleton" in race.lower() or "lich" in race.lower():
                s_str = "bare white skeleton dry bones, no flesh"
            else:
                s_str = "rotting pale ashen flesh"
        else:
            h_str = f"{rng.choice(hair_colors)}, {rng.choice(hair_lengths)}"
            e_str = rng.choice(eye_details)
            s_str = rng.choice(skin_textures)
        
        c_col = rng.choice(clothing_colors)
        c_mat = rng.choice(clothing_materials)
        c_scribe = rng.choice(armor_scribes)
        w_col = rng.choice(weapon_colors)
        w_mat = rng.choice(weapon_materials)
        race_traits = get_race_specific_traits(race)
    
    v_desc = (
        f"{gender_str}, {name}, {race} {cls}. "
        f"Holding {base_wpn} ({w_col}, {w_mat}). "
        f"Hair: {h_str}. Eyes: {e_str}. Skin: {s_str}. "
        f"Wearing attire: {c_mat}, {c_scribe}. "
        f"Traits: {race_traits}."
    )
    
    g_prompt = f"standalone design, no shadow, no glow, {v_desc} Highly detailed elite fantasy character, vibrant silhouette, fate ufotable anime style, masterpiece."
    s_prompt = f"portrait focus, white background, 3:4 aspect ratio, {g_prompt}"
    c_prompt = f"chibi style, white background, cute big head small body chibi proportions, looking left, full body in frame, {g_prompt}"
    spl_prompt = f"dynamic action pose, full background, 16:9 aspect ratio, {g_prompt}"

    lore_match = re.search(r'## Lore Fragment\n(.*?)\n\n## Visual', content, re.DOTALL)
    lore = lore_match.group(1).strip() if lore_match else "A loyal combatant."

    md = f"""# Vassal: {name}
**Rarity**: {file_path.parent.name}
**Race**: {race}
**Class**: {cls}
**Weapon**: {base_wpn}

## Lore Fragment
{lore}

## Visual Identity
- **Gender**: {gender_str}
- **Build**: Normal canonical proportions, detailed figure.
- **Hair**: {h_str}
- **Eyes**: {e_str}
- **Skin**: {s_str}
- **Clothing / Armor**: {c_col} {c_mat}, {c_scribe}
- **Distinguishing Details**: {race_traits}
- **Style**: High fantasy dark-themed.
- **Weapon Details**: {base_wpn} ({w_col}, {w_mat})

## AI Generation & Prompting
- **Gender**: {gender_str}
- **Weapon**: {base_wpn}
- **General / Normal Art (Any Aspect Ratio)**: {g_prompt}
- **Sprite / Portrait (3:4)**: {s_prompt}
- **Chibi (1:1 & 3:4)**: {c_prompt}
- **Splash Art (16:9)**: {spl_prompt}
"""
    with open(file_path, "w", encoding="utf-8") as f:
        f.write(md)
    return True

if __name__ == "__main__":
    count = 0
    for folder in folders:
        fp = base_path / folder
        if not fp.exists(): continue
        for md_file in fp.glob("*.md"):
            if process_file(md_file):
                count += 1
    print(f"Successfully highly detailed {count} character files with all 'glow' keywords removed!")
