import json

with open(r"C:\Users\Ouikio\.gemini\antigravity-ide\brain\ceac6695-2e66-4706-aad1-4f4c2e21c358\.system_generated\logs\transcript.jsonl", "r", encoding="utf-8") as f:
    lines = f.readlines()

for i, l in enumerate(lines):
    if '"StartLine": 80' in l and '"EndLine": 180' in l:
        print("Found 80-180 at", i)
        content = json.loads(lines[i+1])["content"]
        with open("recovered_80_180.txt", "w", encoding="utf-8") as out:
            out.write(content)
    
    if '"StartLine": 180' in l and '"EndLine": 280' in l:
        print("Found 180-280 at", i)
        content = json.loads(lines[i+1])["content"]
        with open("recovered_180_280.txt", "w", encoding="utf-8") as out:
            out.write(content)
