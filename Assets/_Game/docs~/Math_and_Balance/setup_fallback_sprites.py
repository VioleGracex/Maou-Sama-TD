import os
import shutil
import uuid
import re

base_dir = r"d:\OuikiDev\Maou-Sama-TD"
tina_dir = os.path.join(base_dir, "Assets", "_Game", "Art", "Characters", "Tina")
ssr_art_dir = os.path.join(base_dir, "Assets", "_Game", "Art", "Characters", "05_SSR")
asset_root = os.path.join(base_dir, "Assets", "_Game", "Data", "Units", "Vassals", "05_SSR")

characters = [
    "Abyssal_Dreadknight",
    "Infernal_Behemoth",
    "Kaelthas_Blood_Drinker",
    "Nightmare_Weaver"
]

types = ["Avatar", "Chibi", "WaistUp", "FullBody"]

# 1. Copy files and generate GUIDs
guid_map = {} # (char, type) -> guid

print("Generating fallback sprite files and assigning unique GUIDs...")
for char in characters:
    char_dir = os.path.join(ssr_art_dir, char)
    os.makedirs(char_dir, exist_ok=True)
    guid_map[char] = {}
    
    for t in types:
        src_png = os.path.join(tina_dir, f"Art_Tina_{t}.png")
        src_meta = os.path.join(tina_dir, f"Art_Tina_{t}.png.meta")
        
        dst_png = os.path.join(char_dir, f"Art_{char}_{t}.png")
        dst_meta = os.path.join(char_dir, f"Art_{char}_{t}.png.meta")
        
        # Copy PNG
        if os.path.exists(src_png):
            shutil.copy2(src_png, dst_png)
            
        # Copy Meta and generate a new GUID
        if os.path.exists(src_meta):
            with open(src_meta, "r", encoding="utf-8") as f:
                meta_content = f.read()
            
            # Generate new GUID
            new_guid = uuid.uuid4().hex
            
            # Replace 'guid: ...'
            new_lines = []
            for line in meta_content.splitlines():
                if line.startswith("guid:"):
                    new_lines.append(f"guid: {new_guid}")
                else:
                    new_lines.append(line)
            
            with open(dst_meta, "w", encoding="utf-8") as f:
                f.write("\n".join(new_lines) + "\n")
                
            guid_map[char][t] = new_guid
            print(f"  {char} -> {t}: GUID {new_guid}")

# 2. Update UnitData ScriptableObjects
print("\nUpdating UnitData ScriptableObject assets with unique GUIDs...")
for char in characters:
    asset_path = os.path.join(asset_root, f"Char_{char}_UnitData.asset")
    if not os.path.exists(asset_path):
        print(f"  Warning: Asset not found at {asset_path}")
        continue
        
    with open(asset_path, "r", encoding="utf-8") as f:
        content = f.read()
        
    # Get the mapped guids
    char_guids = guid_map.get(char, {})
    avatar_guid = char_guids.get("Avatar")
    chibi_guid = char_guids.get("Chibi")
    waistup_guid = char_guids.get("WaistUp")
    fullbody_guid = char_guids.get("FullBody")
    
    if not (avatar_guid and chibi_guid and waistup_guid and fullbody_guid):
        print(f"  Error: Missing guids for {char}")
        continue
        
    # Replace the fields in BaseSkin block
    # Avatar: {fileID: 21300000, guid: b0ebda7fb0a56f4478cf8311800df6e9, type: 3}
    content = re.sub(
        r'Avatar:\s*\{\s*fileID:\s*21300000,\s*guid:\s*[0-9a-fA-F]+,\s*type:\s*3\s*\}',
        f'Avatar: {{fileID: 21300000, guid: {avatar_guid}, type: 3}}',
        content
    )
    content = re.sub(
        r'Chibi:\s*\{\s*fileID:\s*21300000,\s*guid:\s*[0-9a-fA-F]+,\s*type:\s*3\s*\}',
        f'Chibi: {{fileID: 21300000, guid: {chibi_guid}, type: 3}}',
        content
    )
    content = re.sub(
        r'WaistUp:\s*\{\s*fileID:\s*21300000,\s*guid:\s*[0-9a-fA-F]+,\s*type:\s*3\s*\}',
        f'WaistUp: {{fileID: 21300000, guid: {waistup_guid}, type: 3}}',
        content
    )
    content = re.sub(
        r'FullSplashArt:\s*\{\s*fileID:\s*21300000,\s*guid:\s*[0-9a-fA-F]+,\s*type:\s*3\s*\}',
        f'FullSplashArt: {{fileID: 21300000, guid: {fullbody_guid}, type: 3}}',
        content
    )
    content = re.sub(
        r'FullBodyCutout:\s*\{\s*fileID:\s*21300000,\s*guid:\s*[0-9a-fA-F]+,\s*type:\s*3\s*\}',
        f'FullBodyCutout: {{fileID: 21300000, guid: {fullbody_guid}, type: 3}}',
        content
    )
    
    with open(asset_path, "w", encoding="utf-8") as f:
        f.write(content)
        
    print(f"  Successfully updated BaseSkin in {os.path.basename(asset_path)}")

print("\nFallback sprite initialization complete!")
