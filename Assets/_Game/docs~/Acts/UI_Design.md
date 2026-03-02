# User Interface (UI) Design Document

## 1. Design Philosophy
**"The Overseer's HUD"**
The UI represents the magical interface projected by **Lady Hecatina**, your Seneschal. It should feel diegetic—part of the world's magic system.

*   **Style:** Flat design with subtle depth (glassmorphism). Sharp angles (45-degree cuts).
*   **Font:** *Rajdhani* (Headers/Numbers) for a tech/magic feel, *Inter* (Body) for readability.
*   **Palette:**
    *   **Primary:** Maou Dark (`#0f1014`)
    *   **Action:** Maou Accent (Crimson `#eb4d4b`)
    *   **Highlight:** Maou Gold (`#f9ca24`)
    *   **Support:** Maou Cyan (`#00d2d3`)

## 2. Key Screen Flows

### A. Main Menu (The Throne Room)
*   **Background:** A 3D or Live2D view of the Throne Room. The Maou sits on the throne (1st person view optionally).
*   **Vassals:** Your favorite/assigned "Secretary" vassal stands beside the throne, reacting to taps.
*   **Navigation:** A radial or bottom-bar menu.
    *   [Battle] (Prominent, Glowing Red)
    *   [Vassals] (Unit Management)
    *   [Summon] (Summoning Ritual)
    *   [Shop]
    *   [Missions]

### B. Unit Management (The Barracks)
*   **List View:** Grid of character cards showing Portrait, Star Rating, Level, and Class Icon.
*   **Detail View:**
    *   **Left:** Full-body character art (Live2D).
    *   **Right:** Stats block, Equipment slots, Skill tree.
    *   **Interaction:** "Gift" button to raise affection. "Voice" button to play lines.

### C. Battle Interface (The Command Board)
*   **Top Bar:** Wave Count (e.g., "Wave 3/5"), Enemy Count, Authority (Mana) Gauge.
*   **Bottom Bar:** The Deployable Hand.
    *   Scrollable list of Vassals in your squad.
    *   Drag-and-drop mechanics to place them on grid tiles.
    *   Visual indicator of "Cost" vs "Available Authority".
*   **Unit Interaction:**
    *   Tap a placed unit -> Show Range visuals (Blue tiles) and Skill Button (if active).
    *   "Retreat" button to refund cost.
*   **Speed Controls:** 1x / 2x / Pause buttons in top-right.

### D. Gacha Screen (The Summoning Ritual)
*   **Atmosphere:** Dark room, glowing runic circle on the floor.
*   **Animation:**
    *   Player draws a seal or taps the circle.
    *   Colors explode: White (Common), Blue (Rare), Purple (Epic), Gold (Legendary/SSR).
    *   Silhouette reveal -> Flash -> Character Art.

## 3. Feedback & Juiciness
*   **Button Press:** Instant color shift + mechanical "click" sound.
*   **Error:** Screen momentarily flashes red vignette (e.g., "Not enough Authority!").
*   **Victory:** "STAGE CLEAR" slams onto the screen with gold particles.
*   **Defeat:** "BROKEN" or "RETREAT" appears with a cracking glass effect.
