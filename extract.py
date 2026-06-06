import json

lines = open(r"C:\Users\Ouikio\.gemini\antigravity-ide\brain\ceac6695-2e66-4706-aad1-4f4c2e21c358\.system_generated\logs\transcript.jsonl", "r", encoding="utf-8").readlines()

with open("dump.json", "w", encoding="utf-8") as out:
    for l in reversed(lines):
        if "1_Fresh_Start.lua" in l and "Total Lines: 373" in l and "view_file" in l:
            out.write(l)
