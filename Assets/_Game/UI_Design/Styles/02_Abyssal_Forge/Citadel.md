# Citadel (Home) - Abyssal Forge Style

## Visual Aesthetic
- **Character Frame**: Chosen vassal stands on a glowing, reinforced forge platform with heat-haze effects.
- **Atmosphere**: Heavy industrial, sparks from an invisible hammer, and molten lava glowing.

## Layout & Positioning (Standard Gacha Schema)

### 1. Top Navigation Bar
- **Anchor**: Top-Left / Top-Right
- **Buttons**:
    - `BtnBack` & `BtnHome`: Thick, blackened iron buttons with orange glowing glyphs.
    - `Resource Counters`: Two iron pressure gauges (Iron / Embers).

### 2. Side Module Bar (Left Alignment)
- **Anchor**: Middle-Left
- **Stack**: 3 riveted iron plates stacked vertically.
    - `BtnRanks`: Top.
    - `BtnDaily`: Middle.
    - `BtnChambers`: Lower-middle.
    - `BtnGrimore`: Bottom.

### 3. Main Dashboard Grid (Right Alignment)
- **Anchor**: Middle-Right
- **Grid Layout**:
    - **Row 1**: `CONQUEST` (Massive iron-slab landscape span).
    - **Row 2**: `COHORTS` and `VASSALS` — heavy stone/iron blocks with heat-haze.
    - **Row 3**: `MANDATES` and `TREASURY` (Stacked left) + `MANIFEST VASSALS` (Glowing furnace-door block on right).

### 4. Edge Tabs (Far Right)
- **Anchor**: Right Center (Vertical)
- **Tabs**: `THRONE` and `VAULT` (Vertical iron shutters).

## Navigation & Interaction Logic
- **Module Entry**: "Heavy Slam" transition (Screen shakes as the module slams into focus).
- **Sound**: Steam hiss, metallic clangs, and bubbling magma.
- **Edit Mode**: Magnetic alignment lines appear, snapping the character to the iron grid.

## Button Mapping
| Identifier | Visual Element | Target Page |
| :--- | :--- | :--- |
| `BtnConquest` | Heavy Riveted Slab | Conquest Map |
| `BtnVassals` | Iron Anvil Frame | Vassal Gallery |
| `BtnManifest` | Molten Furnace Core | Summon Altar |
| `BtnTreasury` | Industrial Strongbox | Shop |
| `BtnGrimore` | Sooty Iron Journal | Lore Archives |
| `BtnChambers` | Iron Portcullis | Forge Dorms |
| `BtnRanks` | Jagged Iron Spike | Leaderboards |
| `BtnVault` | Steam-vent Seal | Resource Storage |
