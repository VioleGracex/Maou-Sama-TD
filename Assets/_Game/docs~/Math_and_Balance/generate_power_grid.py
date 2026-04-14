import os
import re
import csv
import glob

docs_dir = r"d:\OuikiDev\Maou-Sama-TD\Assets\_Game\docs~\characters"

# Base stats mapped to class
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
    "C": 1.0,
    "UC": 1.1,
    "R": 1.2,
    "SR": 1.3,
    "SSR": 1.4,
    "UR": 1.5,
}

output_path = os.path.join(docs_dir, "..", "Balancing_PowerGrid.csv")

results = []

def calc_power(hp, atk, df, rarity_mult, rng, skill_allowance=100):
    base_calc = (hp * 0.1) + (atk * 2.5) + (df * 1.5)
    range_bonus = 1.0 + (rng * 0.05)
    total_power = (base_calc * rarity_mult * range_bonus) + skill_allowance
    return round(total_power)

for filepath in glob.glob(os.path.join(docs_dir, "*.md")):
    filename = os.path.basename(filepath)
    with open(filepath, "r", encoding="utf-8") as f:
        content = f.read()

    # Extract name from title or filename
    title_match = re.search(r'#\s*(?:Vassal:\s*)?(.+)', content)
    name = title_match.group(1).strip() if title_match else filename.replace('.md', '')
    
    # Extract Rarity and Class
    rarity_match = re.search(r'\*\*Rarity\*\*:\s*([A-Za-z]+)', content)
    class_match = re.search(r'\*\*Class\*\*:\s*([A-Za-z]+)', content)
    
    # Only process if we found some
    if class_match:
        cls = class_match.group(1).strip()
        rarity = rarity_match.group(1).strip() if rarity_match else "R"
        
        base = ClassBases.get(cls, DEFAULT_BASE)
        mult = RarityMultipliers.get(rarity, 1.0)
        
        # apply multipliers to base stats directly to get current stats (pseudo implementation)
        final_hp = round(base["HP"] * mult)
        final_atk = round(base["ATK"] * mult)
        final_def = round(base["DEF"] * mult)
        
        power = calc_power(final_hp, final_atk, final_def, mult, base["Range"], 100)
        
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
            "Total Power": power
        })

# Sort by Power descending
results.sort(key=lambda x: x["Total Power"], reverse=True)

with open(output_path, "w", newline="", encoding="utf-8") as csvfile:
    fieldnames = ["File", "Name", "Rarity", "Class", "Base HP", "Base ATK", "Base DEF", "Range", "Final HP", "Final ATK", "Final DEF", "Total Power"]
    writer = csv.DictWriter(csvfile, fieldnames=fieldnames)
    writer.writeheader()
    for row in results:
        writer.writerow(row)

print(f"Generated {output_path} with {len(results)} characters.")
