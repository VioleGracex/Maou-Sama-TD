import os
import json
import re

# Load the mapping
with open('Assets/unit_mapping.json', 'r') as f:
    mapping = json.load(f)

# Base directory for docs
docs_root = 'Assets/_Game/docs~/characters'
rarities = ['Common', 'UC', 'R', 'SR', 'SSR', 'UR']

def sync_docs():
    for rarity in rarities:
        rarity_dir = os.path.join(docs_root, rarity)
        if not os.path.exists(rarity_dir):
            continue
            
        for filename in os.listdir(rarity_dir):
            if not filename.endswith('.md'):
                continue
                
            filepath = os.path.join(rarity_dir, filename)
            with open(filepath, 'r', encoding='utf-8') as f:
                content = f.read()
            
            # Find the Unit Title/Class in the header
            # Format usually: # Vassal: [Name], [Class] OR # Vassal: [Class]
            header_match = re.search(r'^# Vassal:\s*(.*)', content, re.MULTILINE)
            if not header_match:
                continue
                
            header_text = header_match.group(1).strip()
            
            # Extract Class name from header
            # If it contains a comma, the name might already be there
            current_name = None
            current_class = header_text
            if ',' in header_text:
                parts = header_text.split(',', 1)
                current_name = parts[0].strip()
                current_class = parts[1].strip()
            
            # Find the canonical name from our mapping
            if current_class in mapping:
                target_name = mapping[current_class]
                
                # 1. Update Header
                new_header = f"# Vassal: {target_name}, {current_class}"
                content = content.replace(header_match.group(0), new_header)
                
                # 2. Update Prompts (male/female, [Class] -> male/female, [Name], [Class])
                # We look for patterns like "male, [Class]" or "Female, [Class]"
                # But to be safe, just inject target_name before current_class in the prompting sections
                # Search for current_class in prompts
                prompt_pattern = rf"([Mm]ale|[Ff]emale),\s*{re.escape(current_class)}"
                content = re.sub(prompt_pattern, rf"\1, {target_name}, {current_class}", content)
                
                # 3. Save updated content
                with open(filepath, 'w', encoding='utf-8') as f:
                    f.write(content)
                
                # 4. Prepare Rename
                # Current filename format: [ID]_[desc].md (e.g., 01_drakmora_infantry.md)
                # target format: [ID]_[name]_[desc].md
                file_parts = filename.split('_', 1)
                id_prefix = file_parts[0]
                slug = file_parts[1].lower() if len(file_parts) > 1 else filename.lower()
                
                # Check if name is already in slug
                if target_name.lower() not in slug:
                    new_filename = f"{id_prefix}_{target_name.lower()}_{slug}"
                    new_filepath = os.path.join(rarity_dir, new_filename)
                    
                    # Log the rename
                    print(f"Renaming: {filename} -> {new_filename}")
                    
                    # Rename the file (handling potential conflicts)
                    os.rename(filepath, new_filepath)
                else:
                    print(f"Updated content for: {filename}")

if __name__ == "__main__":
    sync_docs()
    print("Character documentation synchronization complete.")
