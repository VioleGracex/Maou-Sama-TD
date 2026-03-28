# Citadel (Home) - Astral Codex Style

## Visual Aesthetic
- **Character Frame**: Surrounded by a faint "Stardust" aura and a rotating star-chart floor.
- **Atmosphere**: Deep cosmic indigo, glowing golden constellations.

## Layout & Positioning (Standard Gacha Schema)

### 1. Top Navigation Bar
- **Anchor**: Top-Left / Top-Right
- **Buttons**:
    - `BtnBack` & `BtnHome`: Crystalline glass buttons at top-left.
    - `Resource Counters`: Two glowing nebula orbs (Star Shards/Aether) at top-right.

### 2. Side Module Bar (Left Alignment)
- **Anchor**: Middle-Left
- **Stack**: 3 floating constellation fragments.
    - `BtnRanks`: Top.
    - `BtnDaily`: Middle.
    - `BtnChambers`: Lower-middle.
    - `BtnGrimore`: Bottom.

### 3. Main Dashboard Grid (Right Alignment)
- **Anchor**: Middle-Right
- **Grid Layout**:
    - **Row 1**: `CONQUEST` (Large nebula-themed portal span).
    - **Row 2**: `COHORTS` (Left) and `VASSALS` (Right) — translucent blue glass.
    - **Row 3**: `MANDATES` and `TREASURY` (Stacked left) + `MANIFEST VASSALS` (Glowing golden galaxy block on right).

### 4. Edge Tabs (Far Right)
- **Anchor**: Right Center (Vertical)
- **Tabs**: `THRONE` and `VAULT` (Vertical crystalline strips).

## Navigation & Interaction Logic
- **Module Entry**: "Star-warp" transition effect (Screen stretches toward the center).
- **UI Interaction**: Elements "blink" in like stars when the page loads.
- **Edit Mode**: Constellation lines connect the character to the UI elements while dragging.

## Button Mapping
| Identifier | Visual Element | Target Page |
| :--- | :--- | :--- |
| `BtnConquest` | Glass Orb / Gold Star | Conquest Map |
| `BtnVassals` | Constellation Portrait | Vassal Gallery |
| `BtnManifest` | Pulsing Golden Galaxy | Summon Altar |
| `BtnTreasury` | Crystal Pouch | Shop |
| `BtnGrimore` | Prism Icon | Lore Archives |
| `BtnChambers` | Star-gate Icon | Private Vaults |
| `BtnRanks` | Triple Star Icon | Leaderboards |
| `BtnVault` | Comet Seal | Resource Storage |
