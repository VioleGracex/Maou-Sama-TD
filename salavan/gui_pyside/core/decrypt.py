import sys
import os
import json
sys.path.append(r'd:\OuikiDev\Maou-Sama-TD\salavan\gui_pyside\core')
from crypto_utils import decrypt_state, find_element_in_state

with open(r'D:\OuikiDev\Maou-Sama-TD\salavan\game_state.json', 'r', encoding='utf-8-sig') as f:
    content = f.read().strip()
    
dec = decrypt_state(content, 'salavantester')
state = json.loads(dec)
elem = find_element_in_state('UnitButton_Ignis', state)
print(elem)
