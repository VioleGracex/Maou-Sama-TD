import os
import re

chars_dir = r'd:\OuikiDev\Maou-Sama-TD\Assets\_Game\docs~\characters'
anim_dir_base = r'd:\OuikiDev\Maou-Sama-TD\Assets\_Game\docs~\Math_and_Balance\Mythic_Batches'

tiers = ['Common', 'UC', 'R', 'SR', 'SSR', 'UR']
updated_count = 0

for tier in tiers:
    tier_dir = os.path.join(chars_dir, tier)
    if not os.path.exists(tier_dir): continue
    
    anim_dir = os.path.join(anim_dir_base, tier, 'animation')
    os.makedirs(anim_dir, exist_ok=True)
    
    for filename in os.listdir(tier_dir):
        if not filename.endswith('.md'): continue
        if filename.lower() == 'readme.md': continue
        if 'ignis' in filename.lower(): continue
        
        path = os.path.join(tier_dir, filename)
        with open(path, 'r', encoding='utf-8') as f:
            lines = f.readlines()
            
        char_name = ''
        char_title = ''
        race = ''
        char_class = ''
        
        vis = {}
        in_vis = False
        for line in lines:
            line_str = line.strip()
            if line_str.startswith('# Vassal:'):
                v_str = line_str.replace('# Vassal:', '').strip()
                if ',' in v_str:
                    char_name, char_title = [x.strip() for x in v_str.split(',', 1)]
                else:
                    char_name = v_str
                    char_title = ''
            elif line_str.startswith('**Race**:'):
                race = line_str.split(':', 1)[1].strip().replace('**', '')
            elif line_str.startswith('**Class**:'):
                char_class = line_str.split(':', 1)[1].strip().replace('**', '')
            elif line_str.startswith('## Visual Identity'):
                in_vis = True
                continue
            elif in_vis and line_str.startswith('## '):
                in_vis = False
            elif in_vis and line_str.startswith('- **'):
                match = re.match(r'- \*\*(.+?)\*\*:\s*(.*)', line_str)
                if match:
                    vis[match.group(1)] = match.group(2)
                    
        gender = vis.get('Gender', 'unknown')
        weapon = vis.get('Weapon Details', vis.get('Weapon', 'weapon'))
        hair = vis.get('Hair', 'hair')
        eyes = vis.get('Eyes', 'eyes')
        skin = vis.get('Skin', 'skin')
        clothing = vis.get('Clothing / Armor', 'clothing')
        traits = vis.get('Distinguishing Details', 'none')
        
        title_str = f', {char_title}' if char_title else ''
        
        chibi_prompt = f"chibi style, white background, cute big head small body chibi proportions, no shadow, no glow, {gender}, {char_name}{title_str}, {race} {char_class}. Holding {weapon}. Hair: {hair}. Eyes: {eyes}. Skin: {skin}. Wearing attire: {clothing}. Traits: {traits}. Highly detailed elite fantasy character, vibrant silhouette, fate ufotable anime style, masterpiece."
        
        prefix = 'side profile 180 flat, 2d game sprite style, '
        bp_clean = chibi_prompt
        idle_p = prefix + 'dynamic combat idle pose, ready for battle, ' + bp_clean
        attack_p = prefix + 'attacking with weapon, combat action, swinging weapon, motion blur, ' + bp_clean
        ult_p = prefix + 'unleashing ultimate attack, massive energy aura, extreme dynamic action, spectacular visual effects, ' + bp_clean
        
        new_lines = []
        for line in lines:
            if line.startswith('- **Chibi'):
                new_lines.append(f'- **Chibi (1:1 & 3:4)**: {chibi_prompt}\n')
            elif line.startswith('- **Animation (Idle/Walk)**:'):
                new_lines.append(f'- **Animation (Idle/Walk)**: {idle_p}\n')
            elif line.startswith('- **Animation (Attack)**:'):
                new_lines.append(f'- **Animation (Attack)**: {attack_p}\n')
            elif line.startswith('- **Animation (Ultimate)**:'):
                new_lines.append(f'- **Animation (Ultimate)**: {ult_p}\n')
            else:
                new_lines.append(line)
                
        with open(path, 'w', encoding='utf-8') as f:
            f.writelines(new_lines)
            
        base_name = filename.replace('.md', '')
        name_no_nums = re.sub(r'^\d+_?[a-z]?_', '', base_name, flags=re.IGNORECASE)
        char_name_clean = '_'.join([w.title() for w in name_no_nums.split('_')])
        
        if filename == '01_drakmora_infantry.md':
            char_name_clean = 'Kaldor_Drakmora_Infantry'
            
        batch_lines = []
        batch_lines.append(f'{char_name_clean}_Idle | 1:1 | {idle_p}')
        batch_lines.append(f'{char_name_clean}_Attack | 1:1 | {attack_p}')
        batch_lines.append(f'{char_name_clean}_Ultimate | 1:1 | {ult_p}')
        
        if filename == '01_drakmora_infantry.md':
            out_file = os.path.join(anim_dir, '01_kaldor_drakmora_infantry_animation.txt')
        else:
            out_file = os.path.join(anim_dir, base_name + '_animation.txt')
            
        with open(out_file, 'w', encoding='utf-8') as f:
            f.write('\n'.join(batch_lines))
        
        updated_count += 1
            
print(f'Successfully re-synced {updated_count} characters with their exact Visual Identity stats.')
