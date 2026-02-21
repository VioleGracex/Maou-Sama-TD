# NotebookLLM Source: Full Game UI Scene Breakdown

**Project:** Maou-Sama TD
**Document Type:** UI Source Data for LLM Context
**Purpose:** This document provides a granular breakdown of every User Interface (UI) scene in the game, intended for use as a primary source for NotebookLLM. It details the visual hierarchy, state transitions, and interaction logic for "Hecatina's HUD" (the diegetic interface).

---

## 1. Global UI Elements (The HUD)
*These elements persist across multiple screens.*

### 1.1. The Top Bar (Resource Anchor)
*   **Visuals:** A black semi-transparent glass bar (`#0f1014` at 90% opacity) with a gold trim on the bottom edge.
*   **Left Anchor:** Player Rank (Level) and Profile Name.
*   **Center:** Authority (Stamina) Gauge. Refills over time. Tapping shows generic "Time until full" tooltip.
*   **Right Anchor:** Currencies.
    *   **Gold:** Standard currency. Icon: Gold Coin with Demon crest.
    *   **Gems:** Premium currency. Icon: Red Crystal.
    *   **Menu Button:** Hamburger icon (3 horizontal lines). Opens Settings/Pause overlay.

### 1.2. The Bottom Navigation (Citadel Only)
*   **Visuals:** Floating dock. Slightly curved upwards.
*   **Buttons (Left to Right):**
    1.  **Shop:** Icon: Bag of Coins. (Notification Badge if daily free item available).
    2.  **Vassals:** Icon: Helmet/Face. (Notification Badge if upgrade available).
    3.  **Battle (Center):** Larger scale (1.5x). Icon: Crossed Swords. Pulsing red glow effect.
    4.  **Summon:** Icon: Magic Circle.
    5.  **Missions:** Icon: Scroll.

---

## 2. Scene: Logic & Layouts

### 2.1. Title Screen
*   **Background:** 2D parallax illustration of the Demon Castle at night. Clouds moving slowly.
*   **Logo:** large "Maou-Sama TD" logo centers top. animated "glitch" effect every 5 seconds.
*   **Start Trigger:** "Touch to Start" text blinking at the bottom.
*   **Interaction:** Tapping anywhere triggers the "Gate Open" animation (screen splits horizontally) and transitions to Main Menu.

### 2.2. Main Menu (The Throne Room)
*   **Background:** 3D Environment or Live2D. The interior of the Throne Room.
*   **Center Character:** The "Secretary" character (default: Ignis) stands to the right of the empty throne.
    *   **Interact:** Tapping Character -> Plays random voice line and animation (Blush, Bow, Salute).
*   **Foreground UI:**
    *   **Left Side:** Banner Events (Carousel). Sliding list of active events (e.g., "Login Bonus", "Rate Up Summon").
    *   **Right Side:** "Quick Start" to continue latest Campaign stage.

### 2.3. Vassal Management (The Barracks)
*   **List Layout:** Infinite scroll grid (4 columns). Sortable by Level, Rarity, Class.
    *   **Card Anatomy:** Character Portrait (Face), Star Rating (Top Left), Class Icon (Bottom Right), Level (Bottom Left).
*   **Detail View (Overlay):** Tapping a card opens this full-screen overlay.
    *   **Left Half:** Full body Live2D art. Drag to rotate/zoom.
    *   **Right Half:** Tabbed Panel.
        *   *Tab 1: Attributes.* HP, ATK, DEF bars.
        *   *Tab 2: Gear.* 4 Slots (Weapon, Armor, Accessory, Artifact).
        *   *Tab 3: Skills.* Skill Tree visualization. Nodes light up when unlocked.
    *   **Upgrade Action:** "Level Up" button. Holding it down continuously consumes XP potions with a "filling up" sound effect.

### 2.4. Battle Interface (The Command Board)
*   **View:** Isometric 55-degree angle.
*   **Deployment Zone:** The bottom 20% of the screen.
    *   **Hand:** Scrollable row of Unit Cards. Costs are shown in a blue circle on the card.
    *   **dragging:**
        *   *State: Drag Begin.* The grid lights up. Valid tiles (Low ground for Melee, High for Ranged) glow Green. Invalid tiles glow Red.
        *   *State: Drop.* Unit spawns with a teleport effect. Cost deducted.
*   **Selected Unit Context Menu:** Tapping a deployed unit.
    *   Shows Attack Range (Blue tiles overlay).
    *   **Retreat Button:** Red "Return" icon. Tapping removes unit and refunds 50% cost.
    *   **Skill Button:** If skill charged, a specific icon appears above the unit's head.
*   **Maou Skills (Sovereign Power):** 3 distinct buttons on the far right edge (Vertical stack).
    *   Uses **Authority Seals** charges.
    *   Cooldowns visualized by a radial dark overlay clock wipe.

### 2.5. Gacha (The Summoning Ritual)
*   **Layout:** First person view looking at a magical circle on the floor.
*   **Input:** Player must trace a pattern or "Sign" the seal on screen.
*   **Animation Sequence:**
    1.  The circle glows.
    2.  Pillars of light shoot up. Color coding:
        *   *White:* Common (Soldier).
        *   *Blue:* Rare.
        *   *Purple:* SR (Super Rare).
        *   *Gold:* SSR (Legendary).
    3.  **The Reveal:** Card flips over. Character announces their name.
*   **Skip:** "Skip Animation" button in top right.

### 2.6. Dialogue Event (Visual Novel)
*   **Layout:**
    *   **Background:** Blurred version of the current location.
    *   **Characters:** Live2D sprites slide in from Left/Right. Active speaker is fully opaque; listener is dimmed (70% opacity).
    *   **TextBox:** Bottom 25% of screen. Dark semi-transparent box.
        *   *Name Tag:* Top left of the box. Colored by character theme (e.g., Ignis = Red).
    *   **Log:** Small button to view conversation history.
    *   **Auto:** Button to auto-advance text.

## 3. Feedback FX (Juice)
*   **Damage Numbers:** Pop up from units in World Space.
    *   White: Normal damage.
    *   Yellow (Big): Critical Hit.
    *   Green: Healing.
    *   "Block!": Blue text when a Defender blocks an attack.
*   **Low HP Warning:** When Maou (Castle) HP < 30%, the screen edges throb red, and heart-beat audio plays.
*   **Stage Clear:**
    *   Slow motion on final kill.
    *   UI slides away.
    *   "VICTORY" graphic slams down.
    *   Star count (1-3) fills up sequentially with "Ding... Ding... Ding!" sounds.
