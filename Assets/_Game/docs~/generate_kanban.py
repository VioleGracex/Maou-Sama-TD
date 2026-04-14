import os
import datetime

base_dir = r"d:\OuikiDev\Maou-Sama-TD\.devtool\features\todo"
os.makedirs(base_dir, exist_ok=True)

tasks = [
    {"title": "Remake Level 2", "id": "remake-level-2", "desc": "Remake the layout and flow of Level 2."},
    {"title": "Add more units and waves in Level 2", "id": "add-units-waves-level-2", "desc": "Expand Level 2 with more diverse units and challenging wave patterns."},
    {"title": "Create up to 10 levels in total", "id": "create-10-levels", "desc": "Design and implement the complete set of 10 campaign levels."},
    {"title": "Finish generating anime art for all 80 characters", "id": "finish-anime-art", "desc": "Complete the batch generation and integration of Ufotable-style art for the full 80-character roster."},
    {"title": "Create skills little by little for characters SR and up", "id": "create-skills-sr-up", "desc": "Iteratively design and implement unique active/passive skills for all SR, SSR, and UR characters."},
    {"title": "Refine core gameplay", "id": "refine-core-gameplay", "desc": "Polish and iterate on the tower defense core mechanics to ensure smooth and engaging player loops."},
    {"title": "Try to make a cool 3D map for the environment", "id": "create-3d-map", "desc": "Experiment with creating a fully 3D functional environment/map to replace or enhance the current 2D layouts."}
]

today = datetime.datetime.now(datetime.timezone.utc)
date_str = today.strftime("%Y-%m-%d")
# ISO 8601 with Z
iso_str = today.strftime("%Y-%m-%dT%H:%M:%S.000Z")

for i, t in enumerate(tasks):
    full_id = f"{t['id']}-{date_str}"
    filename = f"{base_dir}\\{full_id}.md"
    
    content = f"""---
id: "{full_id}"
status: "todo"
priority: "medium"
assignee: ""
dueDate: ""
created: "{iso_str}"
modified: "{iso_str}"
labels: ["game-design"]
order: {i}
---

# {t['title']}

{t['desc']}
"""
    with open(filename, "w", encoding="utf-8") as f:
        f.write(content)

print(f"Created {len(tasks)} tasks.")
