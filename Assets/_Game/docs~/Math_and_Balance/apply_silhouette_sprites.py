import os
import shutil

base_dir = r"d:\OuikiDev\Maou-Sama-TD"
brain_dir = r"C:\Users\Ouikio\.gemini\antigravity\brain\c04ac51a-934c-48c6-9d10-293ed697e61a"
ssr_art_dir = os.path.join(base_dir, "Assets", "_Game", "Art", "Characters", "05_SSR")

# Generated silhouette files
src_images = {
    "Avatar": os.path.join(brain_dir, "avatar_silhouette_1778481622442.png"),
    "Chibi": os.path.join(brain_dir, "chibi_silhouette_1778481638178.png"),
    "WaistUp": os.path.join(brain_dir, "waist_up_silhouette_1778481654543.png"),
    "FullBody": os.path.join(brain_dir, "full_body_silhouette_1778481668357.png")
}

characters = [
    "Abyssal_Dreadknight",
    "Infernal_Behemoth",
    "Kaelthas_Blood_Drinker",
    "Nightmare_Weaver"
]

print("Overwriting placeholder images with generated silhouettes...")
for char in characters:
    char_dir = os.path.join(ssr_art_dir, char)
    
    for t, src_path in src_images.items():
        dst_png = os.path.join(char_dir, f"Art_{char}_{t}.png")
        if os.path.exists(src_path):
            shutil.copy2(src_path, dst_png)
            print(f"  Replaced {char} - {t} with custom silhouette.")
        else:
            print(f"  Error: Source silhouette for {t} not found at {src_path}")

print("\nSilhouette assignment complete!")
