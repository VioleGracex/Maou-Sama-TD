# Battle UX & Control Specifications

This document defines the requirements for the interaction layer of the Maou-Sama: Tower Defense engine.

## 1. Battle HUD Architecture
The HUD must remain "Light & Lethal," prioritizing tactical visibility over decorative borders.

### A. Resource Row (The Sovereignty Bar)
- **Authority Seals**: Primary resource. Large italic digits. Color: `#f9ca24` (Gold).
- **Throne Stability (Castle HP)**: Horizontal gauge with a secondary "ghost" layer that shows recent damage depletion.
- **Commander Points (CP)**: Recharging gauge for Maou Skills. Pulsates when at 100%.

### B. Command Center (Top Right)
- **Time Controls**: Toggle between 1x, 2x, and PAUSE.
- **Pause Menu**: Overlays a blurred Citadel backdrop. Options: `RESUME`, `RETRY`, `TACTICAL RETREAT (EXIT)`.
- **Wave Counter**: Shows current wave vs total. Uses segmented markers that light up as waves are purged.

---

## 2. Deployment & Interaction (The "Tactical Hand")

### A. Drag-and-Drop Protocol
- **Selection**: Dragging a Vassal Card from the bar initiates "Deployment Mode."
- **Ghosting**: A 50% opacity sprite follows the cursor/finger.
- **Grid Snapping**: The ghost snaps to the center of the nearest valid tile.
- **Validation**: 
  - **Valid**: Tile glows with `#00d2d3` (Cyan) outer ring.
  - **Invalid**: Tile and Ghost tint to `#eb4d4b` (Accent Red).
- **Range Preview**: While dragging, a persistent glowing circle (or tile-based highlight) shows the unit's reach.

### B. Unit Inspection
- **Selection Click**: Tapping a deployed Vassal opens the "Battle Inspector."
- **Info Display**: Shows Current HP/Max HP, Atk, and active status effects.
- **Range Persistence**: The unit's range circle remains visible while selected.

---

## 3. Maou Skills (Sovereign Authority)

### A. The Casting Cycle
1. **Selection**: Tapping a Maou Skill icon enters "Casting State."
2. **Field Preview**:
   - **THUNDER**: A 1.5-tile radius circle with a "lightning warning" pulse inside.
   - **RALLY**: A global screen-border glow (Gold).
   - **DESPAIR**: A global screen-border glow (Purple) with shadow particles.
3. **Trigger**: Tapping the field executes the skill at the target location.

---

## 4. Camera & Viewports

### A. Orbital Control
- **Rotation**: Rotate the 3D grid in 45-degree increments using HUD arrows or two-finger swipe.
- **Tilt**: Cycle between `ISOMETRIC` (45°), `CINEMATIC` (30°), and `TACTICAL` (90° Top-Down).
- **Zoom**: Scalable from 0.5x to 1.5x.

### B. Visual Fidelity
- **Billboard Effect**: All 2D character sprites must auto-rotate to face the camera, staying "upright" regardless of camera tilt.
- **Depth Sorting**: Ensure sprites on High Ground (Z-axis) correctly overlap units on lower paths.
