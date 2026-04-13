import os
import re

directory = r'd:\OuikiDev\Maou-Sama-TD\Assets\_Game\docs~\characters'

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
        tags.extend(['1girl', 'female'])
    elif gender_val.lower() == 'male':
        tags.extend(['1boy', 'male'])
    else:
        if gender_val: tags.append(gender_val)
        
    if base_visual_val:
        bv_lower = base_visual_val.lower()
        if bv_lower.startswith('male,'):
            base_visual_val = base_visual_val[5:].strip()
        elif bv_lower.startswith('female,'):
            base_visual_val = base_visual_val[7:].strip()
        tags.append(base_visual_val)
        
    if weapon_val:
        tags.append(f'holding {weapon_val}')
        
    general_prompt = ', '.join(tags)
    general_prompt = re.sub(r',\s*,', ',', general_prompt)
    general_prompt = re.sub(r'\.\s*,', ',', general_prompt).strip(', ').strip()
    
    new_section = f"\n- **Gender**: {gender_val}\n- **Weapon**: {weapon_val}\n- **General Prompt**: {general_prompt}\n"
    
    new_content = before + "## AI Generation & Prompting" + new_section
    
    with open(filepath, 'w', encoding='utf-8') as f:
        f.write(new_content)
        
    print(f"Updated {filename}")
