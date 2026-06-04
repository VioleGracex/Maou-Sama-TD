# Maou-Sama TD: Item Icon Generation Prompts

This guide defines the unified art style and provides a batch list of copy-pasteable AI generation prompts for all items, materials, and currencies in the game. Using these blueprints ensures a cohesive, premium dark-fantasy aesthetic across your entire inventory.

---

## 🎨 Unified Art Style Blueprint

To maintain a consistent look and feel across all inventory items, every prompt must combine a **Subject Description** with this **Unified Style Modifier**:

```text
[SUBJECT_DESCRIPTION], premium 2.5D game item icon, dark fantasy anime style, hand-painted digital illustration, thick dark bronze metallic trim, subtle magical light emission, vibrant saturated colors, fine sharp details, game-ready icon asset, volumetric shading, cinematic rim lighting, 8k resolution
```

### Key Style Rules:
- **Borders/Frame**: The `thick dark bronze metallic trim` gives all icons a heavy, physical, dark-fantasy weight, typical of premium RPG/TD games.
- **Lighting**: Emphasize `subtle magical light emission` and `cinematic rim lighting` to make the icons pop off the dark inventory slots.

---

## 📦 Batch Prompt Registry

Below are the tailored, copy-pasteable prompts for every material, XP core, and currency currently in the game.

| Item ID | Item Name | Category | Glow Color | Copy-Pasteable Prompt |
| :--- | :--- | :--- | :--- | :--- |
| **mat_shadow_essence** | Shadow Essence | Material | Violet / Purple | `A glowing orb of swirling dark violet mist and shadowy black smoke trapped inside an ornate gothic rune vessel, premium 2.5D game item icon, dark fantasy anime style, hand-painted digital illustration, thick dark bronze metallic trim, violet magical light emission, vibrant saturated colors, fine sharp details, game-ready icon asset, volumetric shading, cinematic rim lighting, 8k resolution` |
| **mat_bandit_insignia** | Bandit Insignia | Material | Crimson / Steel | `A worn steel rogue emblem with crossed daggers and a carved crimson demonic skull, premium 2.5D game item icon, dark fantasy anime style, hand-painted digital illustration, thick dark bronze metallic trim, subtle red magical light emission, vibrant saturated colors, fine sharp details, game-ready icon asset, volumetric shading, cinematic rim lighting, 8k resolution` |
| **mat_animal_fang** | Animal Fang | Material | Amber / Bone | `A massive pristine predatory beast fang wrapped in leather straps and runic steel wiring, premium 2.5D game item icon, dark fantasy anime style, hand-painted digital illustration, thick dark bronze metallic trim, subtle amber magical light emission, vibrant saturated colors, fine sharp details, game-ready icon asset, volumetric shading, cinematic rim lighting, 8k resolution` |
| **mat_golem_core** | Golem Core | Material | Cyan / Stone | `A highly intricate mechanical stone heart containing a pulsing bright cyan energy core with active electrical arcs, premium 2.5D game item icon, dark fantasy anime style, hand-painted digital illustration, thick dark bronze metallic trim, cyan magical light emission, vibrant saturated colors, fine sharp details, game-ready icon asset, volumetric shading, cinematic rim lighting, 8k resolution` |
| **xp_core_common** | Common XP Core | XP Core | White / Silver | `A glowing silver crystalline fragment pulsing with bright white energy, premium 2.5D game item icon, dark fantasy anime style, hand-painted digital illustration, thick dark bronze metallic trim, white magical light emission, vibrant saturated colors, fine sharp details, game-ready icon asset, volumetric shading, cinematic rim lighting, 8k resolution` |
| **xp_core_rare** | Rare XP Core | XP Core | Vibrant Blue | `A complex glowing blue crystalline matrix pulsing with intense azure light, premium 2.5D game item icon, dark fantasy anime style, hand-painted digital illustration, thick dark bronze metallic trim, deep blue magical light emission, vibrant saturated colors, fine sharp details, game-ready icon asset, volumetric shading, cinematic rim lighting, 8k resolution` |
| **xp_core_epic** | Epic XP Core | XP Core | Pure Purple | `A multifaceted violet crystal cluster radiating waves of purple magic and dark void particles, premium 2.5D game item icon, dark fantasy anime style, hand-painted digital illustration, thick dark bronze metallic trim, purple magical light emission, vibrant saturated colors, fine sharp details, game-ready icon asset, volumetric shading, cinematic rim lighting, 8k resolution` |
| **xp_core_legendary** | Legendary XP Core | XP Core | Fiery Gold | `An ultimate geometric star-shaped crystal pulsing with blinding gold and amber solar fire, premium 2.5D game item icon, dark fantasy anime style, hand-painted digital illustration, thick dark bronze metallic trim, bright gold magical light emission, vibrant saturated colors, fine sharp details, game-ready icon asset, volumetric shading, cinematic rim lighting, 8k resolution` |
| **currency_gold** | Gold Coin | Currency | Warm Gold | `A thick, heavy, ancient gold coin engraved with an overlord's crown, glowing with a soft treasure luster, premium 2.5D game item icon, dark fantasy anime style, hand-painted digital illustration, thick dark bronze metallic trim, warm gold light emission, vibrant saturated colors, fine sharp details, game-ready icon asset, volumetric shading, cinematic rim lighting, 8k resolution` |
| **currency_blood_crest** | Blood Crest | Currency | Crimson Ruby | `A dark metallic shield crest inset with a perfectly cut, glowing crimson drop-shaped ruby gem, premium 2.5D game item icon, dark fantasy anime style, hand-painted digital illustration, thick dark bronze metallic trim, crimson red light emission, vibrant saturated colors, fine sharp details, game-ready icon asset, volumetric shading, cinematic rim lighting, 8k resolution` |

---

## 🛠️ Unity Import Settings

When you drag the transparent PNGs into your `Assets/_Game/Art/Items/` folder, select them in the **Project Window** and apply these high-fidelity settings in the **Inspector**:

1. **Texture Type**: Set to `Sprite (2D and UI)`.
2. **Sprite Mode**: Set to `Single`.
3. **Generate Physics Shapes**: *Uncheck* (saves performance for pure UI icons).
4. **Advanced -> Read/Write**: *Uncheck* (reduces memory consumption unless runtime editing is needed).
5. **Mip Maps -> Generate Mip Maps**: *Uncheck* (keeps sprites perfectly crisp at all scales and avoids blur).
6. **Filter Mode**: Set to `Bilinear` (or `Trilinear` for smooth scaling).
7. **Compression**: Set to `High Quality` (ensures that fine metal trim details and magic glows do not introduce blocky compression artifacts).
