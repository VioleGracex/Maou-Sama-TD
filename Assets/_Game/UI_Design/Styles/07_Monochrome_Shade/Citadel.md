# Citadel (Home) - Monochrome Shade Style

## Visual Aesthetic
- **Character Frame**: High-contrast charcoal sketch on textured white paper.
- **Atmosphere**: Stark, minimalist, with occasional ink splatters.

## Layout & Positioning (Standard Gacha Schema)

### 1. Top Navigation Bar
- **Anchor**: Top-Left / Top-Right
- **Buttons**:
    - `BtnBack` & `BtnHome`: Hand-sketched boxes with thick black ink.
    - `Resource Counters`: Two ink-pot icons (Ink / Paper) at top-right.

### 2. Side Module Bar (Left Alignment)
- **Anchor**: Middle-Left
- **Stack**: 3 vertical sketchy pen-strokes.
    - `BtnRanks`: Top.
    - `BtnDaily`: Middle.
    - `BtnChambers`: Lower-middle.
    - `BtnGrimore`: Bottom.

### 3. Main Dashboard Grid (Right Alignment)
- **Anchor**: Middle-Right
- **Grid Layout**:
    - **Row 1**: `CONQUEST` (Wide charcoal landscape sketch span).
    - **Row 2**: `COHORTS` and `VASSALS` — roughly-drawn shaded squares.
    - **Row 3**: `MANDATES` and `TREASURY` (Stacked left) + `MANIFEST VASSALS` (Blackest-ink ritual circle on right).

### 4. Edge Tabs (Far Right)
- **Anchor**: Right Center (Vertical)
- **Tabs**: `THRONE` and `VAULT` (Vertical torn-paper strips).

## Navigation & Interaction Logic
- **Module Entry**: "Scribble" transition (Screen is quickly covered in black lines and erased).
- **Sound**: Sound of pencil on paper and paper tearing.
- **Edit Mode**: Cross-hatch shadows appear to guide the character on the paper grid.

## Button Mapping
| Identifier | Visual Element | Target Page |
| :--- | :--- | :--- |
| `BtnConquest` | Rough Landscape Sketch | Conquest Map |
| `BtnVassals` | Charcoal Portrait | Vassal Gallery |
| `BtnManifest` | Solid Ink Circle | Summon Altar |
| `BtnTreasury` | Sketched Pouch | Shop |
| `BtnGrimore` | Crinkled Paper Icon | Lore Archives |
| `BtnChambers` | Sketchy Doorway | Artist's Studio |
| `BtnRanks` | Tally Mark Icon | Leaderboards |
| `BtnThrone` | Masterpiece Sketch | Endgame Hub |
