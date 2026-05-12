import os
import csv
import re
import uuid
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

RARITY_FOLDER_MAP = {
    "Common": "01_Common",
    "UC": "02_UC",
    "R": "03_R",
    "SR": "04_SR",
    "SSR": "05_SSR",
    "UR": "06_UR"
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
        elif re.search(r'^\s*Class:', line):
            val = CLASS_MAP.get(stats['Class'], 1)
            new_lines.append(f"  Class: {val}\n")
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

def rename_asset_if_exists(old_name, new_name, folder):
    old_path = folder / old_name
    new_path = folder / new_name
    if old_path.exists():
        if new_path.exists():
            os.remove(old_path)
            old_meta = old_path.with_suffix(old_path.suffix + ".meta")
            if old_meta.exists():
                os.remove(old_meta)
        else:
            os.rename(old_path, new_path)
            old_meta = old_path.with_suffix(old_path.suffix + ".meta")
            new_meta = new_path.with_suffix(new_path.suffix + ".meta")
            if old_meta.exists():
                os.rename(old_meta, new_meta)
            print(f"Renamed on disk: {old_name} -> {new_name}")

def delete_leftover_assets():
    leftovers = [
        "05_SSR/Char_Kaelia_Crimson_Vanguard_UnitData.asset",
        "05_SSR/Char_Vespera_Succubus_Envoy_UnitData.asset",
        "05_SSR/Char_Zephyria_Cloud_Scout_UnitData.asset"
    ]
    for leftover in leftovers:
        p = asset_root / leftover
        if p.exists():
            os.remove(p)
            print(f"Deleted leftover asset: {leftover}")
        meta = p.with_suffix(p.suffix + ".meta")
        if meta.exists():
            os.remove(meta)
            print(f"Deleted leftover meta: {leftover}.meta")

def update_identity(asset_path, unit_name, unit_title):
    with open(asset_path, 'r', encoding='utf-8') as f:
        content = f.read()
    
    # Replace UnitName and UnitTitle
    content = re.sub(r'^\s*UnitName:\s*(.*)', f"  UnitName: {unit_name}", content, flags=re.MULTILINE)
    content = re.sub(r'^\s*UnitTitle:\s*(.*)', f"  UnitTitle: {unit_title}", content, flags=re.MULTILINE)
    
    with open(asset_path, 'w', encoding='utf-8') as f:
        f.write(content)

def update_renamed_asset_identities():
    identities = {
        "03_R/Char_Ignatius_UnitData.asset": ("Ignatius", ""),
        "03_R/Char_Feral_Alley_Cat_UnitData.asset": ("Feral Alley-Cat", ""),
        "03_R/Char_Rune_Scarred_Gladiator_UnitData.asset": ("Rune-Scarred Gladiator", ""),
        "04_SR/Char_Kaelia_Cursed_Blademaster_UnitData.asset": ("Kaelia", "Cursed Blademaster"),
        "05_SSR/Char_Vaelin_UnitData.asset": ("Vaelin", "The Phantom Stalker")
    }
    for rel_path, (name, title) in identities.items():
        p = asset_root / rel_path
        if p.exists():
            update_identity(p, name, title)
            print(f"Updated identity for {rel_path}: UnitName='{name}', UnitTitle='{title}'")

def create_missing_asset(csv_row, template_path):
    name = csv_row['Name']
    rarity = csv_row['Rarity']
    folder_name = RARITY_FOLDER_MAP.get(rarity)
    if not folder_name:
        print(f"Error: Unknown rarity {rarity} for {name}")
        return None
        
    folder_path = asset_root / folder_name
    folder_path.mkdir(parents=True, exist_ok=True)
    
    # Generate asset name
    clean_name = clean_name_for_filename(name)
    asset_filename = f"Char_{clean_name}_UnitData.asset"
    asset_path = folder_path / asset_filename
    
    if asset_path.exists():
        return asset_path
        
    # Read template
    with open(template_path, 'r', encoding='utf-8') as f:
        content = f.read()
        
    # Replace metadata and identity fields
    new_guid_id = str(uuid.uuid4())
    new_asset_name = asset_path.stem
    
    # Split name and title
    if ',' in name:
        unit_name, unit_title = name.split(',', 1)
        unit_name = unit_name.strip()
        unit_title = unit_title.strip()
    else:
        unit_name = name.strip()
        unit_title = ""
        
    content = re.sub(r'^\s*m_Name:\s*(.*)', f"  m_Name: {new_asset_name}", content, flags=re.MULTILINE)
    content = re.sub(r'^\s*UniqueID:\s*(.*)', f"  UniqueID: {new_guid_id}", content, flags=re.MULTILINE)
    content = re.sub(r'^\s*UnitName:\s*(.*)', f"  UnitName: {unit_name}", content, flags=re.MULTILINE)
    content = re.sub(r'^\s*UnitTitle:\s*(.*)', f"  UnitTitle: {unit_title}", content, flags=re.MULTILINE)
    content = re.sub(r'^\s*Rarity:\s*(.*)', f"  Rarity: {RARITY_MAP.get(rarity, 0)}", content, flags=re.MULTILINE)
    
    # Save asset file
    with open(asset_path, 'w', encoding='utf-8') as f:
        f.write(content)
        
    # Create .meta file
    meta_path = asset_path.with_suffix(asset_path.suffix + ".meta")
    new_meta_guid = uuid.uuid4().hex
    meta_content = f"""fileFormatVersion: 2
guid: {new_meta_guid}
NativeFormatImporter:
  externalObjects: {{}}
  mainObjectFileID: 11400000
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""
    with open(meta_path, 'w', encoding='utf-8') as f:
        f.write(meta_content)
        
    print(f"Created new asset: {asset_filename} in {folder_name}")
    return asset_path

def fix_all_internal_m_names():
    print("\nVerifying internal ScriptableObject 'm_Name' values...")
    for p in asset_root.rglob('*.asset'):
        stem = p.stem
        with open(p, 'r', encoding='utf-8') as f:
            content = f.read()
        
        m_name_match = re.search(r'^\s*m_Name:\s*(.*)', content, re.MULTILINE)
        if m_name_match:
            current_m_name = m_name_match.group(1).strip()
            if current_m_name != stem:
                content = re.sub(r'^\s*m_Name:\s*(.*)', f"  m_Name: {stem}", content, flags=re.MULTILINE)
                with open(p, 'w', encoding='utf-8') as f:
                    f.write(content)
                print(f"Fixed mismatch in {p.relative_to(asset_root)}: '{current_m_name}' -> '{stem}'")

def run_sync():
    if not csv_path.exists():
        print(f"Error: {csv_path} not found")
        return

    # First, run renames and cleanup
    delete_leftover_assets()
    rename_asset_if_exists("Char_Magma_UnitData.asset", "Char_Ignatius_UnitData.asset", asset_root / "03_R")
    rename_asset_if_exists("Char_Lava_Bender_UnitData.asset", "Char_Ignatius_UnitData.asset", asset_root / "03_R")
    rename_asset_if_exists("Char_Shadow_UnitData.asset", "Char_Feral_Alley_Cat_UnitData.asset", asset_root / "03_R")
    rename_asset_if_exists("Char_Thrax_UnitData.asset", "Char_Rune_Scarred_Gladiator_UnitData.asset", asset_root / "03_R")
    rename_asset_if_exists("Char_Kaelen_Cursed_Blademaster_UnitData.asset", "Char_Kaelia_Cursed_Blademaster_UnitData.asset", asset_root / "04_SR")
    rename_asset_if_exists("Char_Shade_UnitData.asset", "Char_Vaelin_UnitData.asset", asset_root / "05_SSR")
    
    update_renamed_asset_identities()
    
    # We will build an on-disk asset dictionary based on normalized full name / split-name,
    # so we don't depend on fragile string matches!
    print("\nScanning on-disk assets for robust matching...")
    on_disk_assets = {}
    
    for p in asset_root.rglob('*.asset'):
        with open(p, 'r', encoding='utf-8') as f:
            content = f.read()
        unit_name_m = re.search(r'^\s*UnitName:\s*(.*)', content, re.MULTILINE)
        unit_title_m = re.search(r'^\s*UnitTitle:\s*(.*)', content, re.MULTILINE)
        unit_name = unit_name_m.group(1).strip() if unit_name_m else ''
        unit_title = unit_title_m.group(1).strip() if unit_title_m else ''
        
        full_name_clean = f"{unit_name}, {unit_title}" if unit_title else unit_name
        
        # Keys to match:
        on_disk_assets[full_name_clean.lower().strip()] = p
        on_disk_assets[p.stem.lower().strip()] = p
        on_disk_assets[unit_name.lower().strip()] = p
        if p.stem.startswith("Char_"):
            on_disk_assets[p.stem[5:].lower().strip()] = p
            on_disk_assets[p.stem[5:].replace("_", " ").lower().strip()] = p
            on_disk_assets[p.stem[5:].replace("_", "").lower().strip()] = p

    updated_count = 0
    created_count = 0
    
    # Check templates
    common_template = asset_root / "01_Common" / "Char_Aria_UnitData.asset"
    ssr_template = asset_root / "05_SSR" / "Char_Aquila_UnitData.asset"
    
    with open(csv_path, mode='r', encoding='utf-8') as f:
        reader = csv.DictReader(f)
        for row in reader:
            csv_name = row['Name'].strip()
            csv_rarity = row['Rarity'].strip()
            
            # Robust lookup
            lookup_key = csv_name.lower().strip()
            potential_path = on_disk_assets.get(lookup_key)
            
            if not potential_path:
                potential_path = on_disk_assets.get(csv_name.replace(",", "").lower().strip())
            if not potential_path:
                potential_path = on_disk_assets.get(csv_name.split(',')[0].strip().lower())
            if not potential_path:
                potential_path = on_disk_assets.get(clean_name_for_filename(csv_name).lower())
            if not potential_path:
                first_word = csv_name.split()[0].strip().replace(",", "")
                for rarity_dir in asset_root.iterdir():
                    if rarity_dir.is_dir():
                        p_check = rarity_dir / f"Char_{first_word}_UnitData.asset"
                        if p_check.exists():
                            potential_path = p_check
                            break
            
            # If still not found, let's create the missing asset!
            if not potential_path:
                print(f"Missing asset for CSV entry: '{csv_name}' ({csv_rarity}). Auto-creating...")
                if csv_rarity == "Common":
                    template = common_template
                else:
                    template = ssr_template
                potential_path = create_missing_asset(row, template)
                if potential_path:
                    created_count += 1
            
            if potential_path and potential_path.exists():
                if update_asset(potential_path, row):
                    print(f"Updated: {potential_path.relative_to(asset_root)}")
                    updated_count += 1
            else:
                print(f"Error: Failed to find or create asset for '{csv_name}'")

    # Fix all internal m_Name values
    fix_all_internal_m_names()

    print(f"\nFinished. Updated {updated_count} assets, created {created_count} missing assets.")

if __name__ == "__main__":
    run_sync()
