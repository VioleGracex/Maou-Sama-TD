import os
import re
import csv
from pathlib import Path

base_dir = Path(r"d:\OuikiDev\Maou-Sama-TD\Assets\_Game\docs~")
char_dir = base_dir / "Characters"
csv_path = base_dir / "Math_and_Balance" / "Balancing_PowerGrid.csv"

ClassBases = {
    "Vanguard": {"HP": 1500, "ATK": 60, "DEF": 30, "Range": 1},
    "Executioner": {"HP": 800, "ATK": 100, "DEF": 15, "Range": 1},
    "Assassin": {"HP": 800, "ATK": 100, "DEF": 15, "Range": 1},
    "Bastion": {"HP": 2500, "ATK": 30, "DEF": 60, "Range": 1},
    "Ranger": {"HP": 600, "ATK": 80, "DEF": 10, "Range": 3},
    "Gunner": {"HP": 600, "ATK": 80, "DEF": 10, "Range": 3},
    "Sage": {"HP": 600, "ATK": 90, "DEF": 10, "Range": 2},
    "Warlock": {"HP": 600, "ATK": 90, "DEF": 10, "Range": 2},
    "Necromancer": {"HP": 600, "ATK": 90, "DEF": 10, "Range": 2},
    "Support": {"HP": 700, "ATK": 40, "DEF": 20, "Range": 2},
    "Architect": {"HP": 700, "ATK": 40, "DEF": 20, "Range": 2},
    "Overlord": {"HP": 2000, "ATK": 120, "DEF": 50, "Range": 1},
}
DEFAULT_BASE = {"HP": 1000, "ATK": 50, "DEF": 25, "Range": 1}
RarityMultipliers = {
    "Common": 1.0,
    "UC": 1.1,
    "R": 1.2,
    "SR": 1.3,
    "SSR": 1.4,
    "UR": 1.5,
}

def calc_power(hp, atk, df, rarity_mult, rng, skill_allowance=100):
    base_calc = (hp * 0.1) + (atk * 2.5) + (df * 1.5)
    range_bonus = 1.0 + (rng * 0.05)
    total_power = (base_calc * rarity_mult * range_bonus) + skill_allowance
    return round(total_power)

results = []

clean_patterns = [
    r"chibi style,\s*",
    r"white background,\s*",
    r"standalone character design,\s*",
    r"1:1 aspect ratio\.\s*",
    r"very cute big head small body chibi proportions,\s*",
    r"chibi proportions,\s*",
    r"very cute,\s*"
]

def clean_prompt_text(text):
    for p in clean_patterns:
        text = re.sub(p, "", text, flags=re.IGNORECASE)
    return text.strip()

for file_path in char_dir.rglob("*.md"):
    if "Archive" in file_path.parts:
        continue
        
    with open(file_path, "r", encoding="utf-8") as f:
        content = f.read()
        
    # 1. Update Build
    content = re.sub(
        r"-\s*\*\*Build\*\*:\s*Chibi proportions[^\n]*", 
        "- **Build**: Canonical normal proportions, Ufotable/Fate Series anime style", 
        content, flags=re.IGNORECASE
    )
    
    # 2. Extract and rewrite Prompts
    prompt_match = re.search(r"- \*\*General Prompt\*\*:\s*(.*)", content)
    original_prompt = ""
    if prompt_match:
        original_prompt = prompt_match.group(1)
        cleaned = clean_prompt_text(original_prompt)
        
        replacement = (
            f"- **General / Normal Art (Any Aspect Ratio)**: Ufotable anime style, high detail. {cleaned}\n"
            f"- **Sprite / Portrait (3:4)**: Character portrait focus, white background, standalone character design, 3:4 aspect ratio. {cleaned}\n"
            f"- **Chibi (1:1 & 3:4)**: chibi style, waist-up & full body options, white background, standalone character design, cute big head small body chibi proportions. {cleaned}\n"
            f"- **Splash Art (16:9)**: Dynamic action pose, full background, masterpiece composition, 16:9 aspect ratio. {cleaned}"
        )
        # replace the single prompt line with the 4 new lines
        content = re.sub(r"- \*\*General Prompt\*\*:\s*.*", replacement, content)
        
        with open(file_path, "w", encoding="utf-8") as f:
            f.write(content)
    else:
        # Check if already updated by seeing if General / Normal Art exists
        if "- **General / Normal Art" not in content:
            print(f"Skipping {file_path.name} - No General Prompt found")
            
    # 3. Add to CSV calculations
    filename = file_path.name
    title_match = re.search(r'#\s*(?:Vassal:\s*)?(.+)', content)
    name = title_match.group(1).strip() if title_match else filename.replace('.md', '')
    
    rarity_match = re.search(r'\*\*Rarity\*\*:\s*([A-Za-z]+)', content)
    class_match = re.search(r'\*\*Class\*\*:\s*([A-Za-z]+)', content)
    
    if class_match:
        cls = class_match.group(1).strip()
        rarity = rarity_match.group(1).strip() if rarity_match else "Common"
        
        base = ClassBases.get(cls, DEFAULT_BASE)
        mult = RarityMultipliers.get(rarity, 1.0)
        
        final_hp = round(base["HP"] * mult)
        final_atk = round(base["ATK"] * mult)
        final_def = round(base["DEF"] * mult)
        
        power = calc_power(final_hp, final_atk, final_def, mult, base["Range"], 100)
        
        # Standard defaults based on Class & Rarity
        atk_interval = 1.0
        if cls in ["Assassin", "Executioner"]:
            atk_interval = 0.8
        elif cls in ["Warlock", "Sage", "Necromancer"]:
            atk_interval = 1.5
        elif cls == "Bastion":
            atk_interval = 1.2

        cost_map = {
            "Common": 10,
            "UC": 12,
            "R": 15,
            "SR": 18,
            "SSR": 22,
            "UR": 26,
        }
        deployment_cost = cost_map.get(rarity, 10)
        if cls == "Bastion":
            deployment_cost += 4
        elif cls == "Assassin":
            deployment_cost -= 2

        block_map = {
            "Vanguard": 2,
            "Bastion": 3,
            "Guardian": 3,
            "Overlord": 3,
        }
        block_count = block_map.get(cls, 1)

        respawn_map = {
            "Common": 15.0,
            "UC": 18.0,
            "R": 20.0,
            "SR": 22.0,
            "SSR": 25.0,
            "UR": 30.0,
        }
        respawn_time = respawn_map.get(rarity, 20.0)

        can_attack_flying = cls in ["Ranger", "Gunner", "Warlock", "Sage", "Necromancer", "Support", "Architect"]

        damage_type = "Melee"
        if cls in ["Warlock", "Sage", "Necromancer"]:
            damage_type = "Magic"
        elif cls in ["Ranger", "Gunner"]:
            damage_type = "Physical"

        attack_type = "SingleTarget"
        if cls in ["Warlock", "Necromancer"]:
            attack_type = "AreaOfEffect"

        attack_pattern = "All"
        if cls in ["Ranger", "Gunner"]:
            attack_pattern = "Horizontal"

        results.append({
            "File": filename,
            "Name": name,
            "Rarity": rarity,
            "Class": cls,
            "Base HP": base["HP"],
            "Base ATK": base["ATK"],
            "Base DEF": base["DEF"],
            "Range": base["Range"],
            "Final HP": final_hp,
            "Final ATK": final_atk,
            "Final DEF": final_def,
            "Total Power": power,
            "AttackInterval": atk_interval,
            "DeploymentCost": deployment_cost,
            "BlockCount": block_count,
            "RespawnTime": respawn_time,
            "CanAttackFlying": "True" if can_attack_flying else "False",
            "DamageType": damage_type,
            "AttackType": attack_type,
            "AttackPattern": attack_pattern
        })

results.sort(key=lambda x: x["Total Power"], reverse=True)

with open(csv_path, "w", newline="", encoding="utf-8") as csvfile:
    fieldnames = [
        "File", "Name", "Rarity", "Class", "Base HP", "Base ATK", "Base DEF", "Range", 
        "Final HP", "Final ATK", "Final DEF", "Total Power", "AttackInterval", 
        "DeploymentCost", "BlockCount", "RespawnTime", "CanAttackFlying", 
        "DamageType", "AttackType", "AttackPattern"
    ]
    writer = csv.DictWriter(csvfile, fieldnames=fieldnames)
    writer.writeheader()
    for row in results:
        writer.writerow(row)

print(f"Generated {csv_path} with {len(results)} characters.")

