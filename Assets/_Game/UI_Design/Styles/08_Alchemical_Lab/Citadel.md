# Citadel (Home) - Alchemical Laboratory Style

## Visual Aesthetic
- **Character Frame**: Vassal is viewed through a massive, rotating brass-and-glass circular lens.
- **Atmosphere**: Scholar's chamber with bubbling vials and copper piping.

## Layout & Positioning (Standard Gacha Schema)

### 1. Top Navigation Bar
- **Anchor**: Top-Left / Top-Right
- **Buttons**:
    - `BtnBack` & `BtnHome`: Copper-rimmed glass squares at top-left.
    - `Resource Counters`: Two glass test tubes (Essence / Copper) at top-right.

### 2. Side Module Bar (Left Alignment)
- **Anchor**: Middle-Left
- **Stack**: 3 vertical parchment sheets pinned with copper clips.
    - `BtnRanks`: Top.
    - `BtnDaily`: Middle.
    - `BtnChambers`: Lower-middle.
    - `BtnGrimore`: Bottom.

### 3. Main Dashboard Grid (Right Alignment)
- **Anchor**: Middle-Right
- **Grid Layout**:
    - **Row 1**: `CONQUEST` (Horizontal copper-blueprint scroll span).
    - **Row 2**: `COHORTS` and `VASSALS` — glass-encased specimen boxes.
    - **Row 3**: `MANDATES` and `TREASURY` (Stacked left) + `MANIFEST VASSALS` (Boiling emerald-cauldron block on right).

### 4. Edge Tabs (Far Right)
- **Anchor**: Right Center (Vertical)
- **Tabs**: `THRONE` and `VAULT` (Vertical brass levers).

## Navigation & Interaction Logic
- **Module Entry**: "Distillation" transition (Liquid fills the screen and evaporates to reveal the next page).
- **Sound**: Glass clinking, liquid bubbling, and steam venting.
- **Edit Mode**: Mechanical caliper lines appear to guide the character on the blueprint grid.

## Button Mapping
| Identifier | Visual Element | Target Page |
| :--- | :--- | :--- |
| `BtnConquest` | Copper-piped Blueprint | Conquest Map |
| `BtnVassals` | Pinned Specimen Portrait | Vassal Gallery |
| `BtnManifest` | Emerald Bubble-Altar | Summon Altar |
| `BtnTreasury` | Apothecary Box | Shop |
| `BtnGrimore` | Stained Lab Journal | Lore Archives |
| `BtnChambers` | Brass Hinged Door | Curator's Quarters |
| `BtnRanks` | Brass Balance Scale | Leaderboards |
| `BtnVault` | Sealed Specimen Case | Resource Storage |
