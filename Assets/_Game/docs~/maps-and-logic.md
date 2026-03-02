# Maps, Aesthetics & Perspective

## 1. Fixed Isometric Perspective
The game uses a **Fixed 55-degree Isometric Tilt**. This creates a "Tactical Diorama" feel, making the battlefield look like a living miniature model.

- **Rotation**: 45 degrees horizontal.
- **Tilt**: 55 degrees vertical.
- **Scale**: Units are rendered as "SD" (Stylized Deformed) characters to ensure clarity on the grid while retaining the detail of their high-fashion designs.

---

## 2. Tile Definitions
Maps are constructed using a 2D array of `MapTileType`:

| ID | Name | Role | Visual Style |
| :--- | :--- | :--- | :--- |
| **0** | **Path** | Walkable ground. Melee units deploy here. | Dirt, Stone, or Obsidian path with glowing mana-veins. |
| **1** | **High Ground** | Overwatch spots. Ranged/Towers deploy here. | Elevated platforms, stone pillars, or wall ramparts. |
| **2** | **Spawn** | Enemy entrance node. | Dark portals or broken gates. |
| **3** | **Base / Throne** | The Maou's target. If enemies enter, HP drops. | A glowing seal or a fragment of the Dark Throne. |

---

## 3. Map Archetypes (Patterns)

### A. The Serpent
A single, long winding path.
- **Strategy**: Maximize the range of Warlocks and Rangers. Perfect for training.

### B. The Crossroads
Multiple spawn points (2) converging into a single choke-point path before the Throne.
- **Strategy**: Requires heavy `Bastion` usage at the junction to hold back swarms.

### C. The Archipelago
Small islands of High Ground (1) surrounded by Path (0).
- **Strategy**: Requires careful placement of Sages to ensure all ground units are within healing range of the islands.

### D. The Gauntlet
A short, wide straight line to the Throne with very few High Ground spots.
- **Strategy**: Requires high-cost SSR Vanguards to deal damage quickly before enemies reach the base.

---

## 4. Visual Effects (VFX)
- **Glow**: Neon mana-veins pulse with the rhythm of the BGM.
- **Depth**: Units on High Ground (1) have a higher `translateZ` to appear visually above the path.
- **Shake**: Camera shakes slightly when a Boss spawns or the Castle takes damage.