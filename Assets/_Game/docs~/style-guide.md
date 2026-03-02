# Maou-Sama: Visual Identity & Style Guide

This document defines the core aesthetic components used throughout the Citadel and Battlefield interfaces.

---

## 1. Palette: The "Neon-Gothic" Spectrum

The application uses a high-contrast dark palette with vibrant primary accents representing different mana types and UI functions.

| Token | Hex / Value | Usage |
| :--- | :--- | :--- |
| **Maou Dark** | `#0f1014` | Primary background, deep shadows. |
| **Maou Base** | `#1a1d26` | Component backgrounds, card backings. |
| **Maou Accent** | `#eb4d4b` | Crimson. Alerts, Lady Hecatina, high-intensity buttons. |
| **Maou Gold** | `#f9ca24` | SSR Rarity, Gold currency, highlights, active selections. |
| **Maou Cyan** | `#00d2d3` | Crystal currency, tech/circuit elements, support effects. |
| **Maou Purple** | `#6c5ce7` | SR Rarity, magic effects, Tower of Babel theme. |
| **Maou Glass** | `rgba(20,20,30,0.6)` | Translucent overlays, backdrop-blur panels. |

---

## 2. Typography: Technical Elegance

### Primary Display: **Rajdhani**
Used for headers, buttons, digits, and stylized world-building text.
- **Vibe**: High-tech, sharp, geometric, and anime-industrial.
- **Weights**: 500 (Medium), 700 (Bold), 900 (Black).
- **Styling**: Frequently used with `italic` and `uppercase` for a dynamic "fast" feel.

### System Body: **Inter**
Used for descriptions, tooltips, dialogue text, and complex data lists.
- **Vibe**: Clean, neutral, high readability.
- **Weights**: 400 (Regular), 700 (Bold).

---

## 3. Shape Language

- **Slanted Edges**: Most buttons and panels use a `5%` or `15px` slant/cut corner to simulate a tactical HUD.
- **Clip Paths**: 
  - `clip-slant-right`: `polygon(0 0, 100% 0, 95% 100%, 0% 100%)`
  - `clip-cut-corner`: A signature octagonal cut-out for cards and headers.

---

## 4. Visual Effects (VFX)

- **Noise Texture**: A global `5%` opacity fractal noise overlay is applied to give surfaces a "gritty" physical texture.
- **Glows**: Accent colors are paired with `blur` and `drop-shadow` effects (e.g., `shadow-[0_0_20px_#eb4d4b]`) to simulate mana-emission.
- **Skews**: Non-essential decorative headers often use `skew-x-[-15deg]` to imply motion and aggression.
