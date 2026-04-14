# Maou-Sama TD: AI Art Generation Rules

This document outlines the strict global guidelines and prompt injection rules for generating any character, item, or asset art using AI (Midjourney, Stable Diffusion, etc.) for the project.

## 1. Universal Composition Rules
All generated character portraits and sprites must adhere to the following composition parameters:
*   **Pure White Background**: Every image must clearly specify a solid white background to allow for clean cutouts and alpha-mask generation for Unity UI and Sprites.
*   **Aspect Ratio**: For standard character portraits or cards, heavily bias towards vertical/tall alignment (`3:4 aspect ratio`, `vertical portrait`).
*   **Mandatory Structural Prompt Modifiers**: 
    `isolated on pure white background, solid white backdrop, simple background, vertical portrait, 3:4 aspect ratio`

## 2. Universal Aesthetic Style (The Ufotable Standard)
The project aims for a high-contrast, sharply animated dark-fantasy aesthetic heavily inspired by Ufotable's Fate series adaptations.
*   **Mandatory Aesthetic Prompt Modifiers**: 
    `fate series anime style, ufotable studio style, high-end digital animation, cinematic lighting, sharp shadows, high contrast, epic fantasy, masterpiece, best quality, vibrant magical effects`

## 3. Rarity-Based Context Rules
When generating units based on their `UnitData` rarity:
*   **Common / Fodder Units (01_Common)**: Must explicitly be designed as generic, faceless mobs. Append: `full face mask, closed helmet, faceless, generic foot soldier, non-distinct face, uniform design`.
*   **Named Characters (UC to UR)**: Must heavily incorporate specific character traits, unique armor patterns, and distinct facial features as described in their individual Markdown files (`Visual Identity` and `Lore Fragment`).

## 4. Race-Specific Guidelines
Always reference the `characters/RACE_VISUAL_GUIDE.md` when generating a specific race (e.g., Trueborn Demon, Interloper, Familiar). The AI generation modifiers associated with that specific race's aesthetics should be injected immediately before the Universal Aesthetic Modifiers.

---

### Example Master Character Prompt
```text
[Character Specifics & Race] + [Structural Modifiers] + [Aesthetic Modifiers]

1boy, male, Trueborn Demon, imposing height, obsidian horns, dark plate armor, holding greatsword, isolated on pure white background, solid white backdrop, vertical portrait, 3:4 aspect ratio, fate series anime style, ufotable studio style, high-end digital animation, cinematic lighting, sharp shadows, high contrast, epic fantasy, masterpiece, vibrant magical effects
```
