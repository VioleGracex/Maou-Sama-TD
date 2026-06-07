import sys
import os
import json

sys.path.insert(0, os.path.abspath('salavan/gui_pyside'))
from crypto_utils import decrypt_state

with open('salavan/game_state.json', 'r', encoding='utf-8') as f:
    content = f.read().strip()

key = "MDEyMzQ1Njc4OTAxMjM0NTY3ODkwMTIzNDU2Nzg5MDE="
decrypted = decrypt_state(content, key)
if decrypted:
    state = json.loads(decrypted)
    elements = state.get("elements", {})
    for name in ["StartButton", "ClearCacheButton"]:
        for key, val in elements.items():
            if name in key:
                print(f"{key}: x={val.get('x')}, y={val.get('y')}, fx={val.get('fx')}, fy={val.get('fy')}")
else:
    print("Failed to decrypt")
