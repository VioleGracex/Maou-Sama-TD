import os
import re

directory = r'd:\OuikiDev\Maou-Sama-TD\salavan\scenarios\maou_sama_td'
lua_files = [f for f in os.listdir(directory) if f.endswith('.lua')]

wait_pattern = re.compile(r'^\s*wait\(([\d\.]+)\)')
wait_template_pattern = re.compile(r'wait_template\s*\(|ui\.wait_for\s*\(')

for file in lua_files:
    path = os.path.join(directory, file)
    with open(path, 'r', encoding='utf-8') as f:
        lines = f.readlines()
    
    new_lines = []
    for i in range(len(lines)):
        line = lines[i]
        
        # Check if this line is just a wait() command
        match = wait_pattern.search(line)
        if match:
            # Look ahead to see if the next meaningful line is a wait_template or wait_for
            is_redundant = False
            for j in range(i + 1, len(lines)):
                next_line = lines[j].strip()
                if next_line == '' or next_line.startswith('--'):
                    continue
                if wait_template_pattern.search(next_line):
                    is_redundant = True
                break
            
            if is_redundant:
                # Comment it out
                line = line.replace('wait(', '-- wait(')
                
        new_lines.append(line)
        
    with open(path, 'w', encoding='utf-8') as f:
        f.writelines(new_lines)

print('Redundant wait() calls commented out successfully.')
