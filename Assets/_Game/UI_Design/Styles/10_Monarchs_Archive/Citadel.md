# Citadel (Home) - The Monarch's Archive Style

## Visual Aesthetic
- **Character Frame**: Character positioned before a massive curved mahogany bookshelf.
- **Atmosphere**: Regal library; soft lantern light and falling dust.

## Layout & Positioning (Standard Gacha Schema)

### 1. Top Navigation Bar
- **Anchor**: Top-Left / Top-Right
- **Buttons**:
    - `BtnBack` & `BtnHome`: Brass-trimmed leather pads at top-left.
    - `Resource Counters`: Two brass coin-holders at top-right.

### 2. Side Module Bar (Left Alignment)
- **Anchor**: Middle-Left
- **Stack**: 3 vertical mahogany nameplates.
    - `BtnRanks`: Top.
    - `BtnDaily`: Middle.
    - `BtnChambers`: Lower-middle.
    - `BtnGrimore`: Bottom.

### 3. Main Dashboard Grid (Right Alignment)
- **Anchor**: Middle-Right
- **Grid Layout**:
    - **Row 1**: `CONQUEST` (Wide leather-bound ledger span).
    - **Row 2**: `COHORTS` and `VASSALS` — mahogany-paneled display boxes.
    - **Row 3**: `MANDATES` and `TREASURY` (Stacked left) + `MANIFEST VASSALS` (Regal wax-sealed book block on right).

### 4. Edge Tabs (Far Right)
- **Anchor**: Right Center (Vertical)
- **Tabs**: `THRONE` and `VAULT` (Vertical brass bookmarks).

## Navigation & Interaction Logic
- **Module Entry**: "Page Flip" transition (Screen flips like a heavy leather-bound book).
- **Sound**: Heavy wood thuds, paper rustling, and clockwork clicks.
- **Edit Mode**: A library-ladder grid appears behind the character for layout guidance.

## Button Mapping
| Identifier | Visual Element | Target Page |
| :--- | :--- | :--- |
| `BtnConquest` | Brass-latched Ledger | Conquest Map |
| `BtnVassals` | Catalog Card Portrait | Vassal Gallery |
| `BtnManifest` | Wax-sealed Mandate | Summon Altar |
| `BtnTreasury` | Brass Coin Tray | Shop |
| `BtnGrimore` | Embossed Royal Crown | Lore Archives |
| `BtnChambers` | Mahogany Door Icon | King's Suite |
| `BtnRanks` | Stamped Crest Icon | Leaderboards |
| `BtnVault` | Heavy Brass Safe | Resource Storage |
