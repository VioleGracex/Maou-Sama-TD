import os
import shutil
import hashlib
from pathlib import Path
import re

def generate_deterministic_guid(input_string):
    m = hashlib.md5()
    m.update(input_string.encode('utf-8'))
    return m.hexdigest()

def create_sprite_meta(png_path, guid):
    meta_content = f"""fileFormatVersion: 2
guid: {guid}
TextureImporter:
  internalIdToNameTable: []
  externalObjects: {{}}
  serializedVersion: 12
  mipmaps:
    mipMapMode: 0
    enableMipMap: 0
  bumpmap:
    convertToNormalMap: 0
  isReadable: 0
  streamingMipmaps: 0
  streamingMipmapsPriority: 0
  grayScaleToAlpha: 0
  generateCubemap: 6
  cubemapConvolution: 0
  seamlessCubemap: 0
  textureFormat: 1
  maxTextureSize: 2048
  textureSettings:
    serializedVersion: 2
    filterMode: 1
    aniso: 1
    mipBias: 0
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  lightmap: 0
  compressionQuality: 50
  spriteMode: 1
  spriteExtrude: 1
  spriteMeshType: 1
  alignment: 0
  spritePivot: {{x: 0.5, y: 0.5}}
  spritePixelsToUnits: 100
  spriteBorder: {{x: 0, y: 0, z: 0, w: 0}}
  spriteGenerateFallbackPhysicsShape: 1
  alphaUsage: 1
  alphaIsTransparency: 1
  spriteTessellationDetail: -1
  textureType: 8
  textureShape: 1
  singleChannelComponent: 0
  flipbookRows: 1
  flipbookColumns: 1
"""
    with open(str(png_path) + '.meta', 'w', encoding='utf-8') as f:
        f.write(meta_content)

def assign_chibi_to_asset(asset_path, guid):
    # We assign to BaseSkin.Chibi
    # fileID 21300000 is the standard sub-asset ID for the main Sprite representation in a single-sprite texture.
    with open(asset_path, 'r', encoding='utf-8') as f:
        content = f.read()

    # Regex to find BaseSkin.Chibi
    # Looking for:
    #   Chibi:
    #     m_FileID: 0
    #     m_PathID: 0
    pattern = r'(Chibi:\s+m_FileID:\s*)\d+(\s+m_PathID:\s*)\w+'
    replacement = f"\\g<1>21300000\\g<2>{guid}"
    
    new_content = re.sub(pattern, replacement, content)
    
    # Unity uses 'fileID' format sometimes
    pattern2 = r'Chibi:\s*\{fileID:\s*\d+,\s*guid:\s*[a-zA-Z0-9]+,\s*type:\s*\d+\}'
    replacement2 = f"Chibi: {{fileID: 21300000, guid: {guid}, type: 3}}"
    new_content = re.sub(pattern2, replacement2, new_content)
    
    pattern3 = r'Chibi:\s*\{fileID:\s*\d+\}'
    new_content = re.sub(pattern3, replacement2, new_content)

    with open(asset_path, 'w', encoding='utf-8') as f:
        f.write(new_content)
    print(f"Assigned {guid} to {Path(asset_path).name}")

def process_character(temp_image_path, target_tier, character_name):
    art_dir = Path(r"d:\OuikiDev\Maou-Sama-TD\Assets\_Game\art\Characters")
    data_dir = Path(r"d:\OuikiDev\Maou-Sama-TD\Assets\_Game\data\Units\Vassals")
    
    # Paths
    char_art_folder = art_dir / target_tier / character_name
    os.makedirs(char_art_folder, exist_ok=True)
    
    final_png = char_art_folder / "Sprite_Chibi.png"
    asset_file = data_dir / target_tier / f"Char_{character_name}_UnitData.asset"
    
    # Move
    shutil.move(temp_image_path, str(final_png))
    
    # Generate Meta
    # the internal id in unity for standard sprites is 21300000, based off the file guid.
    guid = generate_deterministic_guid(character_name + "_chibi")
    create_sprite_meta(final_png, guid)
    
    # Assign
    if asset_file.exists():
        assign_chibi_to_asset(asset_file, guid)
    else:
        print(f"ERROR: {asset_file} not found!")

if __name__ == "__main__":
    import sys
    if len(sys.argv) > 3:
        process_character(sys.argv[1], sys.argv[2], sys.argv[3])
