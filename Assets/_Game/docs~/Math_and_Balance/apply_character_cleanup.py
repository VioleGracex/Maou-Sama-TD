import os
import shutil
from pathlib import Path

art_dir = Path(r"d:\OuikiDev\Maou-Sama-TD\Assets\_Game\art\Characters")
data_dir = Path(r"d:\OuikiDev\Maou-Sama-TD\Assets\_Game\data\Units\Vassals")
docs_dir = Path(r"d:\OuikiDev\Maou-Sama-TD\Assets\_Game\docs~\Characters")

def move_or_rename_unity_item(old_path, new_path):
    if not old_path.exists():
        print(f"  [SKIP] Not found: {old_path}")
        return
        
    print(f"  [MOVE/RENAME] {old_path.relative_to(old_path.parent.parent.parent)} \n        -> {new_path.relative_to(new_path.parent.parent.parent)}")
    
    # Create target dir if needed
    os.makedirs(new_path.parent, exist_ok=True)
    
    # Move main item
    shutil.move(str(old_path), str(new_path))
    
    # Move meta item
    old_meta = Path(str(old_path) + ".meta")
    new_meta = Path(str(new_path) + ".meta")
    if old_meta.exists():
        shutil.move(str(old_meta), str(new_meta))

def delete_unity_item(path):
    if not path.exists():
        return
    print(f"  [DELETE] {path.relative_to(path.parent.parent.parent)}")
    
    if path.is_dir():
        shutil.rmtree(path)
    else:
        os.remove(path)
        
    meta_path = Path(str(path) + ".meta")
    if meta_path.exists():
        os.remove(meta_path)

transitions = [
    # TIER MOVES
    ("04_SR", "Valerius_Crimson_Defector", "05_SSR", "Valerius_Crimson_Defector"),
    ("05_SSR", "Balthazar_Lord_Of_Sloth", "06_UR", "Balthazar_Lord_Of_Sloth"),
    
    # RENAMES AND TIERS (Astaroth in 05_SSR old -> 04_SR new)
    ("05_SSR", "Astaroth", "04_SR", "Astaroth_Queen_Of_Pain"),
    
    # RENAMES (SAME TIER)
    ("02_UC", "Armored_Direwolf", "02_UC", "Armored_Dire_Wolf"),
    ("03_R", "Lava-bender", "03_R", "Lava_Bender"),
    ("03_R", "Rune-scarred_Gladiator", "03_R", "Rune_Scarred_Gladiator"),
    ("04_SR", "Isolde_Dusk-bound_Reaver", "04_SR", "Isolde_Dusk_Bound_Reaver"),
    ("04_SR", "Lyra_Blood-moon_Sentinel", "04_SR", "Lyra_Blood_Moon_Sentinel"),
    ("04_SR", "Vesper_Void-caller", "04_SR", "Vesper_Void_Caller"),
    ("04_SR", "Zephyra_Storm-bound_Duchess", "04_SR", "Zephyra_Storm_Bound_Duchess"),
    ("05_SSR", "Fenris_Wolf_King_Of_The_North", "05_SSR", "Fenris_Alpha_Of_The_North"),
    ("04_SR", "Nyx_Shadow_Weaver", "05_SSR", "Nyx_Phantom_Beastkin"), # Wait, Nyx is SSR!
    ("04_SR", "Toros_Minotaur_Chieftain", "04_SR", "Toros_Savage_Packleader"),
    ("02_UC", "Feral_Feline", "03_R", "Feral_Alley_Cat"), # Feral alley cat is R
    ("03_R", "Ironclad_Juggernaut", "03_R", "Bone_Juggernaut"),
    ("03_R", "Nightwing_Familiar", "02_UC", "Nightwing_Harpy"), # Nightwing Harpy is UC!
]

# Quick overrides derived from Tier List
transitions_dict = {
    # Nyx -> SSR
    ("04_SR", "Nyx_Shadow_Weaver"): ("05_SSR", "Nyx_Phantom_Beastkin"),
    ("02_UC", "Feral_Feline"): ("03_R", "Feral_Alley_Cat"),
    ("03_R", "Nightwing_Familiar"): ("02_UC", "Nightwing_Harpy")
}

for i in range(len(transitions)):
    key = (transitions[i][0], transitions[i][1])
    if key in transitions_dict:
        transitions[i] = (key[0], key[1], transitions_dict[key][0], transitions_dict[key][1])

deletions = [
    ("02_UC", "Clockwork_Medic"),
    ("02_UC", "Imp_Prankster"),
    ("03_R", "Desert_Serpent_Shifter"),
    ("03_R", "Fallen_Squire"),
    ("03_R", "Gargoyle_Sentry"),
    ("04_SR", "Magnus_Iron_Forgemaster"),
    ("05_SSR", "Aquila"),
    ("05_SSR", "Kael_Dragonblood_Duelist"),
    ("05_SSR", "Raphael_Golden_Exile"),
    ("06_UR", "Khaos_The_Primal_Engine"),
    ("06_UR", "Maou"),
    ("06_UR", "Zephyr_Eternal_Wind")
]

print("--- EXECUTING TRANSITIONS ---")
for old_tier, old_name, new_tier, new_name in transitions:
    # 1. Art Folder
    art_old = art_dir / old_tier / old_name
    art_new = art_dir / new_tier / new_name
    if art_old != art_new:
        move_or_rename_unity_item(art_old, art_new)
        
    # 2. Data Asset
    data_old = data_dir / old_tier / f"Char_{old_name}_UnitData.asset"
    data_new = data_dir / new_tier / f"Char_{new_name}_UnitData.asset"
    if data_old != data_new:
        move_or_rename_unity_item(data_old, data_new)

print("\n--- EXECUTING DELETIONS ---")
for tier, name in deletions:
    # 1. Art Folder
    delete_unity_item(art_dir / tier / name)
    # 2. Data Asset
    delete_unity_item(data_dir / tier / f"Char_{name}_UnitData.asset")

print("\n--- CLEANING LOOSE META FILES ---")
deleted_metas = 0
for meta_file in docs_dir.glob("*.md.meta"):
    md_file = docs_dir / meta_file.name.replace(".md.meta", ".md")
    if not md_file.exists():
        os.remove(meta_file)
        deleted_metas += 1
print(f"Deleted {deleted_metas} orphaned .md.meta files in docs root.")
