import os
import re

directory = r'd:\OuikiDev\Maou-Sama-TD\Assets\_Game\docs~\characters'

for filename in sorted(os.listdir(directory)):
    if not filename.endswith('.md') or not filename[0].isdigit():
        continue
        
    num = int(filename.split('_')[0])
    if num < 5 or num > 31:
        continue
        
    filepath = os.path.join(directory, filename)
    with open(filepath, 'r', encoding='utf-8') as f:
        content = f.read()

    name = re.search(r'# Vassal: #\d+\s+(.*)', content)
    name = name.group(1).strip() if name else 'Unknown'
    
    weapon = re.search(r'- \*\*Weapon\*\*: (.*)', content)
    weapon = weapon.group(1).strip() if weapon else 'Unknown'
    
    race = re.search(r'\*\*Race\*\*: (.*)', content)
    race = race.group(1).strip() if race else 'Unknown'
    
    print(f"{filename} | {name} | Race: {race} | Weapon: {weapon}")
