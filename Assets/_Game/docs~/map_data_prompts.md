# Level 1 and Level 2 Map Data & Top-Down Prompts

This document compiles the layout data and visual aesthetic information for **Level 1** and **Level 2** of the game to help you generate top-down 3D map references using Gemini or other AI Image Generators.

## Overall Environment Aesthetic
Both maps take place in **The Obsidian Sanctuary** (Subterranean Ritual Chambers).
*   **Theme:** Dark Fantasy, Industrial Gothic Architecture, Subterranean Tomb.
*   **Colors & Lighting:** High contrast. Deep ambient violet/dark purple fog, floating embers, and dark obsidian stone tiles.
*   **Key Props:** Glowing orange demonic seals on the floor (Grand Cross), Ritual Pillars with purple/orange glow, cracked stone tiles.

---

## Level 1 Map Data
*   **Grid Size:** 15 (Width) x 9 (Height)
*   **Spawn Point (Enemy Entrance):** (14, 2)
*   **Exit Point (Defense Objective):** (5, 4)
*   **Layout Description:** The map is primarily a chokepoint/bottleneck. The main path runs horizontally along the center (`y=4` mostly), with a sharp turn or narrow section where the player sets up defenses to protect the exit. The rest of the grid consists of unwalkable high ground or void tiles (Type 7). 
*   **Initial Deployment:** The tutorial has the player place Ignis at (7, 4).

### Prompt for Level 1 Top-Down Image
> **Prompt:** A top-down, bird's-eye view of a 3D grid-based level map for a tower defense game. The environment is a subterranean gothic tomb called "The Obsidian Sanctuary." The map is 15 tiles wide by 9 tiles high, built entirely out of 1:1 cubic blocks. The aesthetic is premium dark fantasy anime style. The ground tiles are dark, cracked obsidian stone. The map features a main horizontal pathway cutting through the center, acting as a narrow bottleneck. The un-walkable areas are raised platforms made of gothic industrial stone blocks and gothic pillars. The scene is illuminated by dramatic dark purple and violet cinematic lighting, with thick purple fog in the lower depths. Near the left side, there is a glowing orange demonic seal (a Grand Cross) burned into the floor tiles, with molten lava erupting from the cracks. High contrast, highly detailed 3D environment, no characters, no UI.

---

## Level 2 Map Data
*   **Grid Size:** 9 (Width) x 9 (Height)
*   **Spawn Points (Enemy Entrances):** (0, 4) and (0, 5)
*   **Exit Points (Defense Objectives):** (8, 4) and (8, 5)
*   **Layout Description:** This map is perfectly square. It features a wider, dual-lane central corridor that runs straight from the left side (x=0) to the right side (x=8) along the `y=4` and `y=5` rows. The top and bottom sections (`y=0` to `y=3`, and `y=6` to `y=8`) are un-walkable blocked tiles (Type 2).

### Prompt for Level 2 Top-Down Image
> **Prompt:** A top-down, bird's-eye view of a square 3D grid-based level map for a tower defense game, exactly 9 by 9 tiles. The setting is a subterranean gothic tomb with an industrial dark fantasy aesthetic. The map features a straight, two-tile-wide horizontal corridor running directly through the center from left to right. The top and bottom sections of the map are raised un-walkable areas built from dark obsidian stone blocks and gothic architectural pillars. The environment is bathed in a dark purple ambient light with floating embers. At the center of the main corridor, there are subtle glowing orange runes etched into the cracked stone floor. The lighting is dramatic and cinematic, highlighting the grid structure of the 1:1 cubic blocks. Masterpiece, premium 3D game art, no characters, no UI.
