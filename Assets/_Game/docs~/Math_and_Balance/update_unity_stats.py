import os
import csv
import re
from pathlib import Path

# Paths
base_dir = Path(r"d:\OuikiDev\Maou-Sama-TD")
csv_path = base_dir / "Assets/_Game/docs~/Math_and_Balance/Balancing_PowerGrid.csv"
asset_root = base_dir / "Assets/_Game/Data/Units/Vassals"

# Mapping for BlockCount based on Class
BLOCK_MAP = {
    "Vanguard": 2,
    "Bastion": 3,
    "Guardian": 3,
    "Overlord": 3,
    "Executioner": 1,
    "Assassin": 1,
    "Ranger": 1,
    "Gunner": 1,
    "Sage": 1,
    "Warlock": 1,
    "Necromancer": 1,
    "Support": 1,
    "Architect": 1,
}

def clean_name_for_filename(name):
    # Remove commas and handle spaces
    name = name.replace(",", "")
    # Title Case and join with underscores (Unity convention used in this project)
    parts = name.split()
    return "_".join([p.capitalize() for p in parts])

CLASS_MAP = {
    "Bastion": 0, "Vanguard": 1, "Executioner": 2, "Ranger": 3, "Warlock": 4, "Sage": 5,
    "Architect": 6, "Necromancer": 7, "Support": 8, "Gunner": 9, "Assassin": 10, "Overlord": 11
}

RARITY_MAP = {
    "Common": 0, "UC": 1, "R": 2, "SR": 3, "SSR": 4, "UR": 5
}

DAMAGE_TYPE_MAP = {
    "Melee": 0, "Ranged": 1, "Magic": 2
}

ATTACK_TYPE_MAP = {
    "SingleTarget": 0, "AreaOfEffect": 1
}

ATTACK_PATTERN_MAP = {
    "Vertical": 0, "Horizontal": 1, "Cross": 2, "Diagonal": 3, "All": 4, "Custom": 5
}

def update_asset(asset_path, stats):
    if not os.path.exists(asset_path):
        return False
        
    with open(asset_path, 'r', encoding='utf-8') as f:
        lines = f.readlines()
        
    new_lines = []
    
    # We'll use simple regex to replace values to avoid messing up YAML structure
    for line in lines:
        if re.search(r'^\s*MaxHp:', line):
            new_lines.append(f"  MaxHp: {stats['Final HP']}\n")
        elif re.search(r'^\s*AttackPower:', line):
            new_lines.append(f"  AttackPower: {stats['Final ATK']}\n")
        elif re.search(r'^\s*Defense:', line):
            new_lines.append(f"  Defense: {stats['Final DEF']}\n")
        elif re.search(r'^\s*BlockCount:', line):
            new_lines.append(f"  BlockCount: {stats['BlockCount']}\n")
        elif re.search(r'^\s*AttackInterval:', line):
            new_lines.append(f"  AttackInterval: {stats['AttackInterval']}\n")
        elif re.search(r'^\s*DeploymentCost:', line):
            new_lines.append(f"  DeploymentCost: {stats['DeploymentCost']}\n")
        elif re.search(r'^\s*RespawnTime:', line):
            new_lines.append(f"  RespawnTime: {stats['RespawnTime']}\n")
        elif re.search(r'^\s*Range:', line):
            new_lines.append(f"  Range: {stats['Range']}\n")
        elif re.search(r'^\s*CanAttackFlying:', line):
            val = 1 if stats['CanAttackFlying'] in ['True', '1'] else 0
            new_lines.append(f"  CanAttackFlying: {val}\n")
        elif re.search(r'^\s*DamageType:', line):
            val = DAMAGE_TYPE_MAP.get(stats['DamageType'], 0)
            new_lines.append(f"  DamageType: {val}\n")
        elif re.search(r'^\s*AttackType:', line):
            val = ATTACK_TYPE_MAP.get(stats['AttackType'], 0)
            new_lines.append(f"  AttackType: {val}\n")
        elif re.search(r'^\s*AttackPattern:', line):
            val = ATTACK_PATTERN_MAP.get(stats['AttackPattern'], 4)
            new_lines.append(f"  AttackPattern: {val}\n")
        elif re.search(r'^\s*Resistance:', line):
            continue 
        else:
            new_lines.append(line)
            
    with open(asset_path, 'w', encoding='utf-8') as f:
        f.writelines(new_lines)
    return True

def run_sync():
    if not csv_path.exists():
        print(f"Error: {csv_path} not found")
        return

    updated_count = 0
    with open(csv_path, mode='r', encoding='utf-8') as f:
        reader = csv.DictReader(f)
        for row in reader:
            clean_name = clean_name_for_filename(row['Name'])
            
            # Try a few common naming patterns
            patterns = [
                f"Char_{clean_name}_UnitData.asset",
                f"Char_{row['Name'].split(',')[0].strip()}_UnitData.asset",
                f"Char_{row['Name'].split()[0].strip()}_UnitData.asset",
            ]
            
            found = False
            for asset_name in patterns:
                for rarity_dir in asset_root.iterdir():
                    if rarity_dir.is_dir():
                        potential_path = rarity_dir / asset_name
                        if update_asset(potential_path, row):
                            print(f"Updated: {asset_name} in {rarity_dir.name}")
                            updated_count += 1
                            found = True
                            break
                if found: break
            
            if not found:
                print(f"Warning: Could not find asset for {row['Name']}")

    print(f"\nFinished. Updated {updated_count} Unity assets.")

if __name__ == "__main__":
    run_sync()
