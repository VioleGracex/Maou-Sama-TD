import os
import re

directory = r'd:\\OuikiDev\\Maou-Sama-TD\\Assets\\_Game\\docs~\\characters'

for filename in os.listdir(directory):
    if not filename.endswith('.md'):
        continue
    filepath = os.path.join(directory, filename)
    with open(filepath, 'r', encoding='utf-8') as f:
        content = f.read()

    if '## AI Generation & Prompting' not in content:
        continue

    # Extract section
    parts = content.split('## AI Generation & Prompting')
    if len(parts) != 2:
        continue
        
    before = parts[0]
    section = parts[1]
    
    gender = re.search(r'- \*\*Gender\*\*: (.*)', section)
    gender_val = gender.group(1).strip() if gender else ''
    
    weapon = re.search(r'- \*\*Weapon\*\*: (.*)', section)
    weapon_val = weapon.group(1).strip() if weapon else ''
    
    base_visual = re.search(r'- \*\*Base Visual Prompt\*\*: (.*)', section)
    base_visual_val = base_visual.group(1).strip() if base_visual else ''
    
    # Format the tag
    tags = []
    if gender_val.lower() == 'female':
        tags.append('1girl')
        tags.append('female')
    elif gender_val.lower() == 'male':
        tags.append('1boy')
        tags.append('male')
    else:
        if gender_val: tags.append(gender_val)
        
    if base_visual_val:
        tags.append(base_visual_val)
        
    if weapon_val:
        tags.append(f'holding {weapon_val}')
        
    general_prompt = ', '.join(tags)
    general_prompt = re.sub(r',\s*,', ',', general_prompt)
    general_prompt = re.sub(r'\.\s*,', ',', general_prompt) # Clean up "., " combinations
    
    print(f"{filename}: {general_prompt}")
