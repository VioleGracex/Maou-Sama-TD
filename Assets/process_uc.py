import os
import re

# Personal Names Mapping
names = {
    "shadow_stalker": "Viona",
    "defected_marksman": "Callum",
    "armored_dire_wolf": "Ulf",
    "reanimated_shieldbearer": "Tarkus",
    "scorned_adept": "Xylia",
    "ember_bruiser": "Fyr",
    "feline_scout": "Tika",
    "exiled_cleric": "Elowen",
    "skeletal_archer": "Korr",
    "nightwing_harpy": "Skyra"
}

char_dir = "Assets/_Game/docs~/characters/UC"
batch_dir_root = "Assets/_Game/docs~/Math_and_Balance/Mythic_Batches/UC"

chibi_lines = []
general_lines = []
waist_up_lines = []

files = sorted([f for f in os.listdir(char_dir) if f.endswith(".md")])

for f in files:
    key = f[3:-3] # remove "01_" and ".md"
    name = names.get(key, "Unknown")
    
    with open(os.path.join(char_dir, f), 'r', encoding='utf-8') as file:
        content = file.read()
        
        # Extract Prompts
        general_match = re.search(r"- \*\*General / Normal Art \(Any Aspect Ratio\)\*\*: (.*)", content)
        waist_up_match = re.search(r"- \*\*Sprite / Portrait \(3:4\)\*\*: (.*)", content)
        chibi_match = re.search(r"- \*\*Chibi \(1:1 & 3:4\)\*\*: (.*)", content)
        
        # Replace the generic name with Personal Name in prompts
        def sub_name(prompt, p_name, g_name):
             # shadow stalker -> Viona
             return prompt.replace(g_name, f"{p_name} ({g_name})")

        generic_name = content.split('\n')[0].replace('# Vassal: ', '').strip()
        
        if general_match:
            general_lines.append(f"{name}_{generic_name.replace(' ', '_')} | {sub_name(general_match.group(1), name, generic_name)}")
        if waist_up_match:
            waist_up_lines.append(f"{name}_{generic_name.replace(' ', '_')} | {sub_name(waist_up_match.group(1), name, generic_name)}")
        if chibi_match:
            chibi_lines.append(f"{name}_{generic_name.replace(' ', '_')} | {sub_name(chibi_match.group(1), name, generic_name)}")
            
        # Update Markdown Title
        new_content = content.replace(f"# Vassal: {generic_name}", f"# Vassal: {name} ({generic_name})")
        with open(os.path.join(char_dir, f), 'w', encoding='utf-8') as fw:
            fw.write(new_content)

# Write Batch Files
with open(os.path.join(batch_dir_root, "chibi/Batch_UC_1.txt"), 'w', encoding='utf-8') as f:
    f.write("\n".join(chibi_lines))
with open(os.path.join(batch_dir_root, "general/Batch_UC_1.txt"), 'w', encoding='utf-8') as f:
    f.write("\n".join(general_lines))
with open(os.path.join(batch_dir_root, "waist_up/Batch_UC_1.txt"), 'w', encoding='utf-8') as f:
    f.write("\n".join(waist_up_lines))

print("Batch files generated and markdown titles updated.")
