
# Content Bible: Lore & Narrative

## 1. World Overview
**Setting**: The Continent of Aethelgard.
**Premise**: The "Demon King" (Maou) is a stabilizing force of nature. The Human Kingdoms are not a united front, but a chaotic mix of Zealots, Capitalists, and Mercenaries who use the "Demon Threat" as a political tool.
**The Twist**: The true threat isn't the Demons, but the instability caused by Humans summoning "Otherworlders" (Isekai heroes) who disrupt the balance of reality.

## 2. Character Archetypes & Voice

### The Maou (Player)
- **Personality**: Stoic but secretly caring. Or customizable via dialogue choices.
- **Design**: Unseen (First person) or shadowed figure.

### Crimson General (Tank)
- **Name**: Ignis.
- **Trope**: The Loyal Knight / Tsundere.
- **Dynamic**: Wants to protect the Maou perfectly. Gets flustered when praised.
- **Key Dialogue**: "I-it's not because I like you! It's my duty!"

### Shadow Hunter (Archer)
- **Name**: Shade.
- **Trope**: The Edgy Loner / Cool Brother.
- **Dynamic**: Competes with Ignis. Sarcastic.
- **Key Dialogue**: "Target acquired. Boring."

### Amethyst Witch (Mage)
- **Name**: Lilith.
- **Trope**: The Smug Matriarch / Onee-san.
- **Dynamic**: Teases the Maou with a predatory, motherly affection. Smug, incredibly knowledgeable, and always seems to be three steps ahead.
- **Key Dialogue**: "My my, such a bold little spark you are. Careful, Sovereign... you might catch fire."

## 3. Dialogue Guidelines

### Script Format
Use the JSON structure defined in `types.ts`.
- **Emotion Keys**: crucial for sprite swapping.
- **Choice Impact**: Choices should matter.
  - *Flavor*: Changes the next line only.
  - *Affection*: (+5 Love).
  - *Flag*: Sets `met_spy_chapter3 = true` (Effects future story).

### Scene Structure
1. **Intro**: Set the scene (Narrator).
2. **Conflict**: Characters discuss the upcoming battle or recent event.
3. **Interaction**: Player input (Choice).
4. **Resolution**: Lead into gameplay or conclude the chapter.
