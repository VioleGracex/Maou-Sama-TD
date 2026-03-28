# Citadel (Home) - Blood Contract Style

## Visual Aesthetic
- **Character Frame**: Behind the vassal is a massive stained-glass Gothic window. Blood-red moonlight filters through, casting long shadows.
- **Atmosphere**: Deep shadows, high contrast. Faint red "breathing" light from the runes on the floor.

## Layout & Positioning (Standard Gacha Schema)

### 1. Top Navigation Bar
- **Anchor**: Top-Left / Top-Right
- **Buttons**:
    - `BtnBack` & `BtnHome`: Positioned top-left in a grouped rectangular container.
    - `Resource Counters`: Two pill-shaped counters (Gold/Souls) at the top-right.
- **Visual**: Grouped by silver filigree bars.

### 2. Side Module Bar (Left Alignment)
- **Anchor**: Middle-Left
- **Stack**: 3 Parallel trapezoidal buttons.
    - `BtnRanks`: Top of the stack.
    - `BtnDaily`: Middle.
    - `BtnChambers`: Lower-middle.
    - `BtnGrimore`: Bottom of the stack.
- **Visual**: Heavy stone slabs with silver trim.

### 3. Main Dashboard Grid (Right Alignment)
- **Anchor**: Middle-Right
- **Grid Layout**:
    - **Row 1**: `CONQUEST` (Large span, horizontal).
    - **Row 2**: `COHORTS` (Left) and `VASSALS` (Right) — matching 50/50 split.
    - **Row 3**: `MANDATES` and `TREASURY` (Stacked left half) + `MANIFEST VASSALS` (Large square on the right half).
- **Visual**: Right-aligned dashboard with high-contrast active highlights.

### 4. Edge Tabs (Far Right)
- **Anchor**: Right Center (Vertical)
- **Tabs**: `THRONE` and `VAULT` (Vertical orientation).
- **Visual**: Narrow silver strips that slide out when hovered.

## Navigation & Interaction Logic
- **Module Entry**: Clicking any Dashboard button performs a "Blood-curtain" transition (Red fade-to-black).
- **Hide UI**: The button (placed near Settings) fades all elements except the character and background window.
- **Edit Mode**: Allows dragging the character. The UI grid slightly dim and shows alignment guides.

## Button Mapping
| Identifier | Visual Element | Target Page |
| :--- | :--- | :--- |
| `BtnConquest` | Stone Slab / Silver Filigree | Conquest Map |
| `BtnVassals` | Portrait Frame | Vassal Gallery |
| `BtnManifest` | Yellow Glowing Altar | Summon Altar |
| `BtnTreasury` | Velvet Pouch Icon | Shop |
| `BtnGrimore` | Ancient Book Icon | Lore Archives |
| `BtnChambers` | Heavy Iron Door | Private Chambers |
| `BtnRanks` | Silver Dagger Icon | Leaderboards |
| `BtnThrone` | Crown Seal | Endgame Hub |
